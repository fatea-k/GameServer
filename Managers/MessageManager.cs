// 文件名：MessageBatcher.cs
using GameServer.Attributes;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Channels;

namespace GameServer.Managers
{
    /// <summary>
    /// 静态消息批处理管理器
    /// 负责控制消息的异步发送，遵循定时触发和批处理规则。
    /// </summary>
    [Service(ServiceLifetime.Singleton)] // 自动化注册单例服务
    public  class  MessageManagner
    {
        // 用于存储每个通道对应的等待发送消息队列和WebSocket连接组的字典
        private static readonly ConcurrentDictionary<string, Channel<string>> _messageChannels = new ConcurrentDictionary<string, Channel<string>>();

        //用用于储存合并后的消息字典
        private static readonly ConcurrentDictionary<string, StringBuilder> _messageStringBuilders = new ConcurrentDictionary<string, StringBuilder>();

        // 用于控制消息发送间隔的每个通道对应的Timer字典
        private static readonly ConcurrentDictionary<string, Timer> _timers = new ConcurrentDictionary<string, Timer>();

        // 控制发送并发的锁字典
        private static readonly ConcurrentDictionary<string, SemaphoreSlim> _locks = new ConcurrentDictionary<string, SemaphoreSlim>();

        // 默认的消息发送超时时间，单位为毫秒
        private static readonly int _defaultTimeoutMs = 10000; // 默认超时时间为10秒

        //构造函数
        public MessageManagner()
        {
            //输出日志
            Console.WriteLine("MessageManagner inited");
        }
        /// <summary>
        /// 将消息入队至指定的消息通道，并根据条件开始异步处理。
        /// </summary>
        /// <param name="channelId">通道标识符，区分不同消息队列</param>
        /// <param name="clients">WebSocket客户端列表，消息将发送给这些客户端</param>
        /// <param name="message">发送的消息</param>
        /// <param name="intervalMs">批处理触发的间隔时间，为0立即发送</param>
        /// <param name="batchSize">批处理大小，为0时并发发送</param>
        /// <returns>无返回值的Task</returns>
        public static async Task SendMessageAsync(string channelId, IEnumerable<WebSocket> clients, string message, int intervalMs = 0, int batchSize = 0, CancellationToken cancellationToken = default)
        {
          
            // 确保通道存在
            var channel = _messageChannels.GetOrAdd(channelId, _ => Channel.CreateUnbounded<string>());

            // 如果不需要批处理和定时发送，则并发广播所有消息
            if (intervalMs == 0 && batchSize == 0)
            {
                //只为状态为Open的WebSocket创建发送任务
                var sendTasks = clients.Where(client => client.State == WebSocketState.Open)
                                       .Select(client => SendMessageWithRetryAsync(client, message, cancellationToken));
                await Task.WhenAll(sendTasks);
                return;
            }
            else if ((intervalMs == 0 && batchSize > 0) || (intervalMs > 0 && batchSize == 0))//有任意一个为0,另一个不为0,循环发送消息
            {
                //循环发送消息
                foreach (var client in clients)
                {
                    if (client.State == WebSocketState.Open)
                    {
                        await SendMessageWithRetryAsync(client, message, cancellationToken);
                    }
                }
                return;
            }
            else//批处理
            {
                // 将消息入队,放在消息缓冲通道中
                channel.Writer.TryWrite(message);

                // 初始化计时器，确保每个通道只有一个Timer
                if (!_timers.ContainsKey(channelId))//判断有没有指定channel的计时器,如果没有,则创建计时器,如果有,则不创建
                {
                    _timers[channelId] = new Timer(callback: _ => TimerCallback(clients, channelId, intervalMs), state: null, dueTime: intervalMs, period: Timeout.Infinite);
                }

                //异步执行消息处理方法
                _ = Task.Run(async () => await ProcessMessagesAsync(clients, channelId, batchSize, cancellationToken));


               return;
            }
        }

        // Timer回调函数，用于处理消息队列
        //当计时器时间到了,触发回调,开始执行内容
        private static async void TimerCallback(IEnumerable<WebSocket> clients, string channelId, int intervalMs)
        {

            var semaphore = _locks.GetOrAdd(channelId, new SemaphoreSlim(1, 1));
            await semaphore.WaitAsync(); // 等待获取锁

            try
            {
                //尝试从指定的channle中获取消息
                if (_messageStringBuilders.TryGetValue(channelId, out var messageStringBuilder))
                {
                    var messageToSend = messageStringBuilder.ToString(); // 先取出要发送的消息
                    messageStringBuilder.Clear(); // 清空StringBuilder以准备下一批次的消息

                    foreach (var client in clients)
                    {
                        if (client.State == WebSocketState.Open)
                        {
                            await SendMessageWithRetryAsync(client, messageToSend);
                        }
                    }
                }
            }
            finally
            {
                // 确保发送完成后才释放锁，并且重新设定计时器
                semaphore.Release();
            }
            //发送完以后,检查计时器是否存在
            if (_timers.TryGetValue(channelId, out var timer))
            {
                //如果存在,则重新设置计时器
                timer.Change(intervalMs, Timeout.Infinite);
            }
        }

        // 处理消息队列中的所有消息
        private static async Task ProcessMessagesAsync(IEnumerable<WebSocket> clients, string channelId, int batchSize = 0, CancellationToken cancellationToken = default)
        {
            //获取指定通道的消息队列
            if (_messageChannels.TryGetValue(channelId, out var channel))
            {
                //创建或获取指定通道的批处理消息
                StringBuilder messageStringBuilder = _messageStringBuilders.GetOrAdd(channelId, _ => new StringBuilder());


                //读取缓存通道中的消息
                while (await channel.Reader.WaitToReadAsync())
                {
                    while (channel.Reader.TryRead(out var message))
                    {

                        //如果批处理消息+当前去取出的消息长度大于等于设定的批处理消息大小
                        if (messageStringBuilder.Length + message.Length >= batchSize)
                        {
                            //计时器锁
                            var semaphore = _locks.GetOrAdd(channelId, new SemaphoreSlim(1, 1));
                            await semaphore.WaitAsync(); // 发送之前加锁

                            try
                            {
                                // 在锁内部复制要发送的消息并清空StringBuilder
                                var messageToSend = messageStringBuilder.ToString();
                                messageStringBuilder.Clear();

                                foreach (var client in clients)
                                {
                                    if (client.State == WebSocketState.Open)
                                    {
                                        await SendMessageWithRetryAsync(client, messageToSend, cancellationToken);
                                    }
                                }
                            }
                            finally
                            {
                                semaphore.Release(); // 发送完毕后释放锁
                            }
                        }

                        //将消息加入到批处理消息中
                        messageStringBuilder.Append(message);

                    }
                }

                CleanUp(channelId);
            }

        }



        // 向WebSocket客户端发送消息，失败时重试
        private static async Task SendMessageWithRetryAsync(WebSocket client, string message, CancellationToken cancellationToken = default)
        {
            byte[] buffer = Encoding.UTF8.GetBytes(message);

            for (int i = 0; i < 3; i++)
            {
                try
                {
                    await client.SendAsync(new ArraySegment<byte>(buffer), WebSocketMessageType.Text, true, CancellationToken.None);
                    break;
                }
                catch (WebSocketException)
                {
                    if (i < 2)
                    {
                        await Task.Delay(1000);
                    }
                }
            }
        }

        // 清理指定通道的资源
        private static void CleanUp(string channelId)
        {
            if (_messageChannels.TryRemove(channelId, out var channel))
            {
                channel.Writer.Complete();
            }
            if (_timers.TryRemove(channelId, out var timer))
            {
                timer.Dispose();
            }
            if (_locks.TryRemove(channelId, out var semaphore))
            {
                semaphore.Dispose();
            }
        }

    }
}