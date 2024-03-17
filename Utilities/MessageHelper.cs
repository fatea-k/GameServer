using GameServer.Enums; // 使用自定义枚举
using GameServer.Models; // 使用游戏服务器模型
using System.Net.WebSockets; // WebSocket相关命名空间
using System.Text;
using System.Threading.Channels; // Channel相关命名空间

// 定义游戏服务器的命名空间
namespace GameServer.Utilities
{
    /// <summary>
    /// WebSocket消息助手类，用于管理消息的异步广播与发送。
    /// </summary>
    public class MessageHelper
    {
        // 定义一个通道，用于缓存WebSocket发送的消息
        private readonly Channel<MessageContent> _messagesChannel;

        // 标记是否有消息正在被处理
        private bool _isProcessingBatch;

        // 定时器，用于在设定的间隔后处理消息
        private readonly Timer _timer;

        // 定义一次处理消息的最大批次大小
        private readonly int _maxBatchSize;

        // 创建CancellationTokenSource，用于处理取消操作
        private readonly CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();

        // 定义处理消息的时间间隔
        private TimeSpan _batchInterval;

        // 设置处理消息的最小时间间隔
        private readonly TimeSpan _minBatchInterval = TimeSpan.FromMilliseconds(5);

        // 设置处理消息的最大时间间隔
        private readonly TimeSpan _maxBatchInterval = TimeSpan.FromMilliseconds(100);

        /// <summary>
        /// 自定义的消息内容类，支持广播和单独消息发送。
        /// </summary>
        private class MessageContent
        {
            public IEnumerable<WebSocket> Sockets { get; set; } // 目标客户端WebSocket集合
            public string Message { get; set; } // 要发送的消息内容
            public bool IsBroadcast { get; set; } // 指示是否为广播消息
        }

        /// <summary>
        /// 初始化MessageHelper的新实例。
        /// </summary>
        /// <param name="maxBatchSize">单次消息处理的最大批次大小。</param>
        public MessageHelper(int maxBatchSize)
        {
            _maxBatchSize = maxBatchSize;
            _messagesChannel = Channel.CreateUnbounded<MessageContent>(); // 创建无界消息通道
            _timer = new Timer(TimerCallback, null, Timeout.Infinite, Timeout.Infinite); // 初始化定时器，配置转为空闲状态
        }

        /// <summary>
        /// 把消息异步放入队列中，并安排发送时间。
        /// </summary>
        /// <param name="sockets">目标WebSocket对象集合。</param>
        /// <param name="message">要发送的消息。</param>
        /// <param name="externalCancellationToken">用于外部取消操作的CancellationToken。</param>
        /// <param name="userDefinedInterval">用户定义的消息处理时间间隔。</param>
        /// <param name="isBroadcast">指示消息是否需要广播。</param>
        /// <returns>Task表示异步操作的状态。</returns>
        public async Task QueueMessageAsync(IEnumerable<WebSocket> sockets, string message, CancellationToken externalCancellationToken, TimeSpan? userDefinedInterval = null, bool isBroadcast = false)
        {
            // 当外部请求取消时，使用CancellationTokenSource发出取消信号
            externalCancellationToken.Register(() => _cancellationTokenSource.Cancel());

            // 根据用户定义的间隔设置时间间隔，或者使用最大间隔
            _batchInterval = userDefinedInterval ?? _maxBatchInterval;
            // 保证间隔时间在最小和最大值之间
            _batchInterval = (_batchInterval < _minBatchInterval) ? _minBatchInterval : (_batchInterval > _maxBatchInterval) ? _maxBatchInterval : _batchInterval;

            // 创建一个消息内容对象并放入通道中
            await _messagesChannel.Writer.WriteAsync(new MessageContent
            {
                Sockets = sockets,
                Message = message,
                IsBroadcast = isBroadcast
            }, _cancellationTokenSource.Token);

            // 如果当前没有消息正在处理，则开始处理流程
            if (!_isProcessingBatch)
            {
                _isProcessingBatch = true;
                _timer.Change(_batchInterval, Timeout.InfiniteTimeSpan); // 启动定时器
                _ = ProcessMessagesAsync(_cancellationTokenSource.Token); // 开始异步处理消息
            }
        }

        // 定时器回调函数
        private void TimerCallback(object state)
        {
            // 如果没有取消请求，尝试处理消息
            if (!_cancellationTokenSource.IsCancellationRequested)
            {
                // 尝试从通道中读取一条待处理的消息
                if (_messagesChannel.Reader.TryRead(out var messageContent))
                {
                    // 根据消息是否是广播，使用相应的处理逻辑
                    var task = messageContent.IsBroadcast ?
                        BroadcastMessageAsync(messageContent.Sockets, messageContent.Message, _cancellationTokenSource.Token) : // 广播消息
                        SendAsync(messageContent.Sockets.First(), messageContent.Message, _cancellationTokenSource.Token); // 单独发送消息

                    // 开始任务并等待其完成
                    Task.Run(async () => await task);
                    _isProcessingBatch = false; // 完成后更新处理标记
                }
                else
                {
                    _isProcessingBatch = false; // 如果没有消息则更新标记
                }
            }
        }

        // 处理消息队列中的任务
        private async Task ProcessMessagesAsync(CancellationToken cancellationToken)
        {
            // 使用异步遍历处理所有消息
            await foreach (var messageContent in _messagesChannel.Reader.ReadAllAsync(cancellationToken))
            {
                // 如果请求了取消，则抛出异常中断操作
                cancellationToken.ThrowIfCancellationRequested();

                // 根据消息是广播还是单独发送，选择相应处理逻辑
                if (messageContent.IsBroadcast)
                {
                    // 广播消息给所有客户端
                    await BroadcastMessageAsync(messageContent.Sockets, messageContent.Message, cancellationToken);
                }
                else
                {
                    // 单独发送消息给每个客户端
                    foreach (var socket in messageContent.Sockets)
                    {
                        await SendAsync(socket, messageContent.Message, cancellationToken);
                    }
                }
            }
        }

        // 广播消息到所有WebSocket客户端
        private async Task BroadcastMessageAsync(IEnumerable<WebSocket> sockets, string message, CancellationToken cancellationToken)
        {
            // 为每个客户端创建发送任务
            var tasks = sockets.Select(socket => SendAsync(socket, message, cancellationToken));
            // 等待所有发送任务完成
            await Task.WhenAll(tasks);
        }

        /// <summary>
        /// 广播消息到列表中的所有WebSocket客户端。
        /// </summary>
        /// <param name="message">要广播的消息。</param>
        /// <param name="sockets">目标WebSocket集合。</param>
        /// <param name="useParallel">是否使用并行发送。</param>
        /// <param name="cancellationToken">用于操作取消的CancellationToken。</param>
        /// <returns>Task表示异步操作的状态。</returns>
        public async Task BroadcastAsync(string message, IEnumerable<WebSocket> sockets, bool useParallel, CancellationToken cancellationToken)
        {
            if (useParallel)
            {
                // 并行发送消息到所有客户端
                await Task.WhenAll(sockets.Select(socket => SendAsync(socket, message, cancellationToken)));
            }
            else
            {
                // 顺序发送消息到每个客户端
                foreach (var socket in sockets)
                {
                    await SendAsync(socket, message, cancellationToken);
                }
            }
        }

        // 发送单条消息到指定的WebSocket客户端
        public static async Task SendAsync(WebSocket socket, string message, CancellationToken cancellationToken)
        {
            // 确保WebSocket连接处于打开状态
            if (socket.State == WebSocketState.Open)
            {
                // 将消息编码为字节数组
                var buffer = Encoding.UTF8.GetBytes(message);
                // 发送消息
                await socket.SendAsync(new ArraySegment<byte>(buffer), WebSocketMessageType.Text, true, cancellationToken);
            }
        }

        // 发送错误信息到指定的WebSocket客户端
        public static async Task SendErrorAsync(WebSocket socket, string action, ErrorEnum errorEnum, CancellationToken cancellationToken)
        {
            // 构造错误消息
            var message = new WebSocketMessage
            {
                Action = action,
                Error = errorEnum
            };
            // 发送错误消息
            await SendAsync(socket, message.Serialize(), cancellationToken);
        }

        // 停止消息处理并清理资源
        public void Stop()
        {
            // 发出取消请求
            _cancellationTokenSource.Cancel();
            // 停止定时器，释放资源
            _timer.Change(Timeout.Infinite, Timeout.Infinite);
            _timer.Dispose();
            // 关闭消息通道
            _messagesChannel.Writer.Complete();
            // 清理CancellationTokenSource
            _cancellationTokenSource.Dispose();
            // 重置处理消息的标记
            _isProcessingBatch = false;
        }
    }
}