using GameServer.Handlers;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using GameServer.Services;
using GameServer.Utilities;
using GameServer.Manager; // 这需要您有一个处理用户登录的服务类



namespace GameServer.WebSocketManager
{
    // WebSocket连接类，用于管理与单个客户端的WebSocket通信
    public class WebSocketConnection
    {
        private readonly WebSocket _webSocket;// WebSocket连接实例
        private readonly IServiceProvider _serviceProvider;
        private bool _isAuthenticated = false; // 表示用户是否通过身份验证


        // 设置心跳间隔和超时时间
        private static readonly TimeSpan HeartbeatInterval = TimeSpan.FromSeconds(30);//计时器检查间隔(秒)
        private static readonly TimeSpan ConnectionTimeout = TimeSpan.FromMinutes(2);//超时断线时间(分钟)
        private DateTimeOffset _lastHeartbeat; // 最后一次心跳的时间
        private Timer _heartbeatTimer;


        // 构造函数，初始化WebSocket连接和服务提供者
        public WebSocketConnection(WebSocket webSocket, IServiceProvider serviceProvider)
        {
            _webSocket = webSocket;
            _serviceProvider = serviceProvider; // 保存服务容器

        }


        /// <summary>
        /// 异步处理websocket消息的核心方法
        /// </summary>
        /// <param name="cancellationToken">取消令牌,允许调用cancel方法来取消所有拥有同一个令牌的异步方法</param>
        /// <returns></returns>
        public async Task ProcessMessagesAsync(CancellationToken cancellationToken)
        {
            var buffer = new byte[1024 * 4]; // 消息缓冲区

            try
            {
                // 循环接收来自WebSocket的消息
                while (_webSocket.State == WebSocketState.Open && !cancellationToken.IsCancellationRequested)
                {
                    // 异步接收消息
                    var result = await _webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), cancellationToken);
                    _lastHeartbeat = DateTimeOffset.UtcNow; // 收到任何客户端消息都更新心跳时间

                    // 检查消息类型，如果是关闭请求则关闭连接
                    if (result.MessageType == WebSocketMessageType.Close)
                    {
                        CloseConnectionAsycn(WebSocketCloseStatus.NormalClosure, "链接已关闭", CancellationToken.None).ConfigureAwait(false).GetAwaiter().GetResult();
                    }
                    else if (result.MessageType == WebSocketMessageType.Text)
                    {
                        // 将接收到的字节序列转换为字符串消息
                        var message = Encoding.UTF8.GetString(buffer, 0, result.Count);
                        var socketcode = _webSocket.GetHashCode();
                        // 输出接收到的消息
                        Console.WriteLine($"接收到消息:{socketcode}: {message}");

                        // 解析消息为JSON对象
                        dynamic json = JsonConvert.DeserializeObject(message);
                        string action = json.action;
                        var data = (JObject)json.data;

                        if (action == "heartbeat")
                        {
                            // 创建注册成功的响应消息.返回时间戳,用于计算延迟
                            var response = JsonConvert.SerializeObject(new { action = "heartbeat", data = new { timestamp = (long)(DateTime.UtcNow - DateTimeOffset.UnixEpoch).TotalMilliseconds } });

                            await MessageHelper.SendAsync(_webSocket, response, cancellationToken);
                        }
                        // 检查动作是否为登录或注册，这些动作需要特殊处理
                        else if (action == "login" || action == "register")
                        {
                            // 使用UserHandler处理登录和注册请求
                            await HandleAuthentication(action, data.ToObject<JObject>(), cancellationToken);

                        }
                        else if ((action != "login" || action != "register")&&_isAuthenticated /* 用户已经通过登录认证 */)//注册和登录之外的操作
                        {
                            // 处理已登录用户的其他操作

                            // 根据action分派消息到其他已经注册过的Handler处理
                            Type handlerType = HandlerTypeProvider.GetHandlerTypeForAction(action);
                            object handler = _serviceProvider.GetService(handlerType);
                            if (handler is IHandler handlerInstance) //当handler不为空分批消息并赋值,
                            {
                                await handlerInstance.HandleMessageAsync(_webSocket, action, data.ToObject<JObject>(), cancellationToken);
                            }
                            else
                            {
                                Console.WriteLine($"未找到 action '{action}' 对应的 Handler。");
                                await MessageHelper.SendAsync(_webSocket, $"未知操作: {action}", cancellationToken);
                            }

                        }
                    }
                }
            }
            catch (WebSocketException ex)
            {
                // 如果在接收消息时发生异常，输出错误信息并关闭WebSocket连接
                Console.WriteLine("WebSocket连接异常: " + ex.Message);
                // 如果WebSocket不是已关闭状态，尝试关闭WebSocket连接
                CloseConnectionAsycn(WebSocketCloseStatus.NormalClosure, "连接异常", CancellationToken.None).ConfigureAwait(false).GetAwaiter().GetResult();
            }
        }

        /// <summary>
        /// 检查是否超时的回调函数,如果超时,清除心跳,执行清除\关闭连接方法
        /// </summary>
        /// <param name="state">暂时没什么用的参数</param>
        private void CheckHeartbeatTimeout(object state)
        {
            if (DateTimeOffset.UtcNow - _lastHeartbeat > ConnectionTimeout)
            {

                CloseConnectionAsycn(WebSocketCloseStatus.NormalClosure, "连接超时", CancellationToken.None).ConfigureAwait(false).GetAwaiter().GetResult();
            }
        }

        /// <summary>
        /// 关闭连接的方法
        /// </summary>
        /// <param name="closeStatus">关闭枚举</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns></returns>
        private async Task CloseConnectionAsycn(WebSocketCloseStatus closeStatus, string errmessage, CancellationToken cancellationToken)
        {
            DisposeHeartbeatTimer();//清除计时器
            if (_webSocket.State != WebSocketState.Closed)
            {
                ConnectionManager.RemoveConnectionByWebSocket(_webSocket);
                await _webSocket.CloseAsync(closeStatus, errmessage, cancellationToken);
            }

        }


        /// <summary>
        /// 收到login或gegister消息后执行的认证方法,认证通过,开始进行心跳检测
        /// </summary>
        /// <param name="action">消息动作名称</param>
        /// <param name="data">消息体</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns></returns>
        private async Task HandleAuthentication(string action, JObject data, CancellationToken cancellationToken)
        {
            var userHandler = _serviceProvider.GetRequiredService<UserHandler>();
            if (action == "login")
            {
                _isAuthenticated = await userHandler.LoginAsync(_webSocket, data, action, cancellationToken) != null;
            }
            else if (action == "register")
            {
                _isAuthenticated = await userHandler.RegisterAsync(_webSocket, data, action, cancellationToken) != null;
            }

            if (_isAuthenticated)
            {
                // 初始化心跳时间
                _lastHeartbeat = DateTimeOffset.UtcNow;

                // 启动或重置心跳定时器
                _heartbeatTimer?.Dispose(); // 如果计时器已经存在，则先释放
                _heartbeatTimer = new Timer(CheckHeartbeatTimeout, null, HeartbeatInterval, HeartbeatInterval);
            }
        }

        /// <summary>
        /// 清除心跳计时器
        /// </summary>
        private void DisposeHeartbeatTimer()
        {
            _heartbeatTimer?.Dispose(); // 释放计时器
            _heartbeatTimer = null; // 将计时器引用设为null，避免内存泄露
            _isAuthenticated = false;//更改用户状态为非认证状态
        }



    }
}

