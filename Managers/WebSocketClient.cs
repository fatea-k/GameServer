////using System.Collections.Generic;
////using System.Linq;
////using System.Threading;
////using System.Threading.Tasks;

////// WebSocketClient 类提供了一个静态 API，模仿 SignalR 的 Clients API，
////// 允许开发者方便地向不同的客户端集合发送消息。
////public static class WebSocketClient
////{
////    // 获取单例的 ConnectionManager 实例。
////    private static ConnectionManager _connectionManager = ConnectionManager.Instance;

////    // 用来发送消息给所有连接的客户端。
////    public static ClientsCaller All => new ClientsCaller(_connectionManager);

////    // 用来发送消息给除当前客户端以外的其他所有客户端。
////    // 需要从外部传入当前客户端的 ID。
////    public static ClientsCaller Others(string currentConnectionId) => new ClientsCaller(_connectionManager, currentConnectionId);

////    // 用来发送消息给特定的客户端。
////    public static ClientsCaller Client(string connectionId) => new ClientsCaller(_connectionManager, null, connectionId);

////    // 用来发送消息给特定组内的所有客户端。群组的管理逻辑需要另行实现。
////    public static ClientsCaller Group(string groupName) => new ClientsCaller(_connectionManager, groupName: groupName);
////}

////// ClientsCaller 类封装了不同的消息发送方式。
////// 这个类的对象会被用来实际发送消息给一个或多个客户端。
////public class ClientsCaller
////{
////    private ConnectionManager _connectionManager;
////    private HashSet<string> _excludeConnectionIds;
////    private string _targetConnectionId;
////    private string _groupName;

////    // ClientsCaller 的构造函数。
////    // 参数 connectionManager 用于发送消息的连接管理器，
////    // currentConnectionId 为当前客户端 ID（如果存在的话，将在 Others 实例中使用），
////    // targetConnectionId 为目标客户端 ID（如果存在的话，将在 Client 实例中使用），
////    // groupName 为群组名（如果存在的话，将在 Group 实例中使用）。
////    public ClientsCaller(
////        ConnectionManager connectionManager,
////        string currentConnectionId = null,
////        string targetConnectionId = null,
////        string groupName = null)
////    {
////        _connectionManager = connectionManager;
////        _excludeConnectionIds = string.IsNullOrEmpty(currentConnectionId) ? null : new HashSet<string> { currentConnectionId };
////        _targetConnectionId = targetConnectionId;
////        _groupName = groupName;
////    }

////    // 发送消息的方法，支持 CancellationToken。
////    // 参数 message 为要发送的消息文本，
////    // cancellationToken 为取消操作的令牌。
////    public async Task SendAsync(string message, CancellationToken cancellationToken)
////    {
////        // 如果是群组消息
////        if (_groupName != null)
////        {
////            // 发送消息给群组内所有客户端
////            var groupMembers = GroupManager.GetGroupMembers(_groupName);

////            // 创建并执行所有成员的发送消息任务
////            var tasks = groupMembers.Select(connectionId =>
////                _connectionManager.SendMessageAsync(connectionId, message, cancellationToken));

////            // 等待所有发送任务完成
////            await Task.WhenAll(tasks);
////        }
////        // 如果是向特定客户端发送消息
////        else if (_targetConnectionId != null)
////        {
////            await _connectionManager.SendMessageAsync(_targetConnectionId, message, cancellationToken);
////        }
////        // 如果发送消息给除了某些客户端之外的所有客户端
////        else if (_excludeConnectionIds != null)
////        {
////            var allConnectionIds = _connectionManager.GetAllConnectionIds();
////            IEnumerable<string> targetIds = allConnectionIds.Except(_excludeConnectionIds);
////            foreach (var connectionId in targetIds)
////            {
////                await _connectionManager.SendMessageAsync(connectionId, message, cancellationToken);
////            }
////        }
////        // 如果是向所有客户端发送消息
////        else
////        {
////            await _connectionManager.SendMessageToAllAsync(message, cancellationToken);
////        }
////    }
////}


//using GameServer.Manager;
//using System.Collections.Generic;
//using System.Linq;
//using System.Net.WebSockets;
//using System.Text;
//using System.Threading;
//using System.Threading.Channels;
//using System.Threading.Tasks;

//namespace GameServer.Utilities
//{
//    public static class WebSocketClient
//    {
//        private static ConnectionManager _connectionManager = ConnectionManager.Instance; // 单例的连接管理器对象
//        private static Timer _timer; // 计时器对象，用于实现消息批处理
//        private static Channel<MessageContent> _messagesChannel; // 消息通道，用于存储待发送的消息

//        static WebSocketClient()
//        {
//            _messagesChannel = Channel.CreateUnbounded<MessageContent>(); // 初始化消息通道
//            _timer = new Timer(TimerCallback, null, Timeout.Infinite, Timeout.Infinite); // 初始化计时器，并设置为不激活状态
//        }

//        // 此方法设置消息发送的定时器
//        // intervalMs: 表示定时器触发的时间间隔，单位毫秒
//        public static void SetTimerInterval(int intervalMs)
//        {
//            _timer.Change(intervalMs, Timeout.Infinite);
//        }

//        // 定时器的回调方法，当定时器触发时调用
//        private static void TimerCallback(object state)
//        {
//            // 锁定通道，防止数据竞争
//            lock (_messagesChannel)
//            {
//                // 如果通道中有消息，则处理消息
//                if (_messagesChannel.Reader.TryRead(out var content))
//                {
//                    // 如果定义的批处理间隔时间不为0，进行批处理
//                    if (content.Interval > 0)
//                    {
//                        // 执行批处理逻辑
//                        ProcessBatchMessages(content);
//                    }
//                }
//                // 如果通道为空，则停止计时器以节省资源
//                else
//                {
//                    _timer.Change(Timeout.Infinite, Timeout.Infinite);
//                }
//            }
//        }

//        // 向所有客户端发送消息的API接口
//        public static ClientsCaller All
//            => new ClientsCaller(_connectionManager, BroadcastType.All);

//        // 向除了当前客户端外的所有客户端发送消息的API接口
//        public static ClientsCaller Others 
//            => new ClientsCaller(_connectionManager, BroadcastType.Others);

//        // 向特定客户端发送消息的API接口
//        public static ClientsCaller Client(string connectionId)
//            => new ClientsCaller(_connectionManager, BroadcastType.Client, connectionId);

//        // 向群组发送消息的API接口
//        public static ClientsCaller Group(string groupName)
//            => new ClientsCaller(_connectionManager, BroadcastType.Group, groupName);

//        // ClientsCaller 类用于封装不同的WebSocket发送消息逻辑
//        public class ClientsCaller
//        {
//            private ConnectionManager _connectionManager;
//            private BroadcastType _broadcastType;
//            private string _connectionId;
//            private string _groupName;

//            public ClientsCaller(ConnectionManager connectionManager, BroadcastType broadcastType, string identifier = null)
//            {
              

//                _connectionManager = connectionManager;
//                _broadcastType = broadcastType;
//                // 根据广播类型设置相应的标识（用户连接ID或群组名）
//                if (broadcastType == BroadcastType.Others || broadcastType == BroadcastType.Client)
//                {
//                    _connectionId = identifier;
//                }
//                else if (broadcastType == BroadcastType.Group)
//                {
//                    _groupName = identifier;
//                }
//            }

//            // 发送消息，并且可以设定消息发送的时间间隔
//            public async Task SendAsync(string message, int intervalMs, int batchSize, CancellationToken cancellationToken = default)
//            {
//                // 根据广播类型选择合适的WebSocket集合
//                var targets = _broadcastType switch
//                {
//                    BroadcastType.All => _connectionManager.GetAllSockets(),
//                    BroadcastType.Others => _connectionManager.GetAllSockets().Where(s => s.ConnectionId != _connectionId),
//                    BroadcastType.Client => new List<WebSocket> { _connectionManager.GetSocketById(_connectionId) },
//                    BroadcastType.Group => _connectionManager.GetGroupMembers(_groupName).Select(_connectionManager.GetSocketById),
//                    _ => Enumerable.Empty<WebSocket>()
//                };
//                // 创建新的消息内容
//                var messageContent = new MessageContent
//                {
//                    Sockets = targets,
//                    Message = message,
//                    Interval = intervalMs,
//                    Size = Encoding.UTF8.GetByteCount(message),
//                    Sent = false  // 标记消息未发送
//                };
//                // 写入消息到通道
//                await _messagesChannel.Writer.WriteAsync(messageContent, cancellationToken);
//                // 如果批处理大小达到限制或者计时器间隔为0，则立即尝试发送消息
//                if (batchSize >= 2048 || intervalMs == 0)
//                {
//                    // 执行发送逻辑
//                    await ProcessMessagesAsync(intervalMs);
//                }
//                else
//                {
//                    // 设置计时器
//                    SetTimerInterval(intervalMs);
//                }
//            }
//        }

//        // 处理通道中积累的消息
//        private static async Task ProcessMessagesAsync(int intervalMs)
//        {
//            // 读取所有消息，并检查是否有可以发送的
//            while (await _messagesChannel.Reader.WaitToReadAsync())
//            {
//                if (_messagesChannel.Reader.TryRead(out var content))
//                {
//                    if (!content.Sent) // 如果消息未发送
//                    {
//                        ProcessBatchMessages(content);
//                        content.Sent = true; // 标记消息为已发送
//                    }
//                }
//            }
//        }

//        // 批处理消息
//        private static async Task ProcessBatchMessages(MessageContent content)
//        {
//            var tasks = content.Sockets.Select(socket => SendAsync(socket, content.Message));
//            await Task.WhenAll(tasks); // 等待所有的发送任务完成
//        }

//        // 异步发送单条消息给指定的 WebSocket
//        private static async Task SendAsync(WebSocket socket, string message)
//        {
//            if (socket.State == WebSocketState.Open)
//            {
//                var buffer = Encoding.UTF8.GetBytes(message);
//                await socket.SendAsync(new ArraySegment<byte>(buffer), WebSocketMessageType.Text, true, CancellationToken.None);
//            }
//        }

//        // 支持的WebSocket广播类型
//        private enum BroadcastType
//        {
//            All,
//            Others,
//            Client,
//            Group
//        }

//        // 存储消息内容的类
//        private class MessageContent
//        {
//            public IEnumerable<WebSocket> Sockets { get; set; } // 目标 WebSocket 集合
//            public string Message { get; set; } // 发送的消息
//            public int Interval { get; set; } // 消息发送间隔
//            public int Size { get; set; } // 消息大小
//            public bool Sent { get; set; } // 消息是否已经发送
//        }
//    }
//}