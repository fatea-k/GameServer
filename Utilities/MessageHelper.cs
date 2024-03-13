using System;
using System.Text;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using System.Net.WebSockets;
using GameServer.Enums;
using GameServer.Models;

// 游戏服务器的命名空间
namespace GameServer.Utilities
{
    // WebSocket消息助手类，用于管理消息的接收和发送
    public class MessageHelper
    {
        // 用于缓存消息的通道
        private readonly Channel<(WebSocket clientSocket, string message)> _messagesChannel;

        // 标记是否有消息批处理正在进行
        private bool _isProcessingBatch;

        // 定时器，用于控制消息的定时发送
        private readonly Timer _timer;

        // 消息批处理的最大尺寸
        private readonly int _maxBatchSize;

        // 用于取消操作的CancellationTokenSource
        private CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();

        // 时间间隔变量，用于定时器的时间间隔设置
        private TimeSpan _batchInterval;

        // 最小时间间隔阈值设为5毫秒
        private readonly TimeSpan _minBatchInterval = TimeSpan.FromMilliseconds(5);

        // 最大时间间隔阈值设为100毫秒
        private readonly TimeSpan _maxBatchInterval = TimeSpan.FromMilliseconds(100);

        // 构造函数，设置消息批处理的最大尺寸
        public MessageHelper(int maxBatchSize)
        {
            _maxBatchSize = maxBatchSize;
            _messagesChannel = Channel.CreateUnbounded<(WebSocket clientSocket, string message)>();
            _isProcessingBatch = false;
            // 初始化定时器，设置为不自动启动
            _timer = new Timer(TimerCallback, null, Timeout.Infinite, Timeout.Infinite);
        }

        // 添加消息到队列的方法，可以设置用户自定义的时间间隔和取消标记
        public async Task QueueMessageAsync(WebSocket clientSocket, string message, CancellationToken externalCancellationToken, TimeSpan? userDefinedInterval = null)
        {
            // 如果传入的外部CancellationToken被取消，则同时取消类内部CancellationTokenSource
            externalCancellationToken.Register(() => _cancellationTokenSource.Cancel());

            // 调整时间间隔，如果用户指定了时间间隔参数，则自动调整时间间隔，否则使用默认的最大时间间隔
            //如果用户没有给定时间,使用默认值,否则使用用户指定的时间
            _batchInterval = userDefinedInterval ?? _maxBatchInterval;

            //                                           和min值比对选最大,                                         和max值比对选最小
            _batchInterval = (_batchInterval < _minBatchInterval) ? _minBatchInterval : (_batchInterval > _maxBatchInterval) ? _maxBatchInterval : _batchInterval;

            // 将消息添加到消息通道中
            await _messagesChannel.Writer.WriteAsync((clientSocket, message), _cancellationTokenSource.Token);

            // 如果当前没有消息批处理正在进行，则启动消息处理
            if (!_isProcessingBatch)
            {
                _isProcessingBatch = true;
                _timer.Change(_batchInterval, Timeout.InfiniteTimeSpan);//启动计时器
                // 异步执行消息处理
                _ = ProcessMessagesAsync(_cancellationTokenSource.Token);
            }
        }
        // 从消息通道读取并处理消息的异步方法
        private async Task ProcessMessagesAsync(CancellationToken cancellationToken)
        {
            var batchBuilder = new StringBuilder(); // 用于构建消息批次的StringBuilder
            int currentBatchSize = 0; // 当前批次消息的总字节大小

            // 等待并读取通道中的消息
            await foreach (var (socket, message) in _messagesChannel.Reader.ReadAllAsync(cancellationToken))
            {
                // 检查取消标志
                cancellationToken.ThrowIfCancellationRequested();

                int messageSize = Encoding.UTF8.GetByteCount(message); // 获取当前消息的大小
                currentBatchSize += messageSize; // 累加到批次大小

                // 如果当前消息批次大小超过了设置的最大值
                if (currentBatchSize >= _maxBatchSize)
                {
                    // 发送累积的消息
                    await SendAsync(socket, batchBuilder.ToString(), cancellationToken);
                    // 清空StringBuilder，准备接收下一批次的消息
                    batchBuilder.Clear();
                    // 重置当前批次大小
                    currentBatchSize = 0;
                    // 批处理完成，停止定时器，并标记_isProcessingBatch为false
                    _timer.Change(Timeout.Infinite, Timeout.Infinite);//停止计时器,防止触发同一个消息批次的发送
                    _isProcessingBatch = false;
                }
                // 如果不需要发送，则继续累积消息到StringBuilder
                else
                {
                    batchBuilder.AppendLine(message);
                }
            }
        }

        // 定时器回调方法，用于处理超过最大时间间隔的消息
        private void TimerCallback(object state)
        {
            // 使用类内部的CancellationToken进行操作取消检查
            var cancellationToken = _cancellationTokenSource.Token;

            // 检查通道中是否有待处理的消息
            if (_messagesChannel.Reader.TryRead(out var item))
            {
                var (clientSocket, message) = item;

                // 创建一个新的任务来处理消息的发送
                Task.Run(async () =>
                {
                    // 检查取消标志
                    cancellationToken.ThrowIfCancellationRequested();

                    // 发送消息
                    await SendAsync(clientSocket, message, cancellationToken);
                }, cancellationToken).ContinueWith(t =>
                {
                    // 捕获可能发生的任务取消异常
                    if (t.IsCanceled)
                    {
                        // 这里可以根据具体需要进行处理，如记录日志等
                    }
                });
            }
            // 标记当前没有消息批处理任务在执行
            _isProcessingBatch = false;
        }

        // 异步发送WebSocket消息的助手方法
        public static async Task SendAsync(WebSocket clientSocket, string message, CancellationToken cancellationToken)
        {
            if (clientSocket.State == WebSocketState.Open)
            {
                var buffer = Encoding.UTF8.GetBytes(message);
                var segment = new ArraySegment<byte>(buffer);
                // 异步发送消息，等待完成
                await clientSocket.SendAsync(segment, WebSocketMessageType.Text, true, cancellationToken);
            }
        }

        // 封装发送错误消息的方法
        public static async Task SendErrorAsync(WebSocket clientSocket, string action, ErrorEnum errorEnum, CancellationToken cancellationToken)
        {
            var message = new WebSocketMessage
            {
                Action = action,
                Error = errorEnum //错误枚举
            };
            await SendAsync(clientSocket, message.Serialize(), cancellationToken);
        }

        // 停止并清理消息助手，释放资源
        public void Stop()
        {
            // 触发取消操作
            _cancellationTokenSource.Cancel();

            // 停止定时器
            _timer.Change(Timeout.Infinite, Timeout.Infinite);
            _timer.Dispose();

            // 完成消息通道的写入
            _messagesChannel.Writer.Complete();

            // 清除CancellationTokenSource
            _cancellationTokenSource.Dispose();

            // 标记当前没有消息批处理任务在执行
            _isProcessingBatch = false;
        }
    }
}