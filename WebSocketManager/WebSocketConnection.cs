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
using GameServer.Utilities; // 这需要您有一个处理用户登录的服务类

namespace GameServer.WebSocketManager
{
    // WebSocket连接类，用于管理与单个客户端的WebSocket通信
    public class WebSocketConnection
    {
        private readonly WebSocket _webSocket;// WebSocket连接实例
        private readonly IServiceProvider _serviceProvider;
        private bool _isAuthenticated = false; // 表示用户是否通过身份验证


         // 构造函数，初始化WebSocket连接和服务提供者
        public WebSocketConnection(WebSocket webSocket, IServiceProvider serviceProvider)
        {
            _webSocket = webSocket;
            _serviceProvider = serviceProvider; // 保存服务容器
        }

        // 处理WebSocket消息的核心方法,异步处理WebSocket消息
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

                    // 检查消息类型，如果是关闭请求则关闭连接
                    if (result.MessageType == WebSocketMessageType.Close)
                    {
                        await _webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, string.Empty, cancellationToken);
                    }
                    else if (result.MessageType == WebSocketMessageType.Text)
                    {
                        // 将接收到的字节序列转换为字符串消息
                        var message = Encoding.UTF8.GetString(buffer, 0, result.Count);
                        // 输出接收到的消息
                        Console.WriteLine($"接收到消息: {message}");

                        // 解析消息为JSON对象
                        dynamic json = JsonConvert.DeserializeObject(message);
                        string action = json.action;
                        var data = (JObject)json.data;

                        // 检查动作是否为登录或注册，这些动作需要特殊处理
                        if (action == "login" || action == "register")
                        {
                            // 使用UserHandler处理登录和注册请求
                            await HandleAuthentication(action, data.ToObject<JObject>(), cancellationToken);

                        }
                        else if (_isAuthenticated /* 用户已经通过登录认证 */)//注册和登录之外的操作
                        {
                            // 处理已登录用户的其他操作

                            // 根据action分派消息到其他已经注册过的Handler处理
                            Type handlerType = HandlerTypeProvider.GetHandlerTypeForAction(action);
                            object handler = _serviceProvider.GetService(handlerType);
                            if (handler is IHandler handlerInstance)
                            {
                                await handlerInstance.HandleMessageAsync(_webSocket, action, data.ToObject<JObject>(), cancellationToken);

                            }
                            else
                            {
                                Console.WriteLine($"未找到 action '{action}' 对应的 Handler。");
                                await SendMessageAsync($"未知操作: {action}", cancellationToken);
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
                if (_webSocket.State != WebSocketState.Closed)
                {
                    await _webSocket.CloseAsync(WebSocketCloseStatus.InternalServerError, "WebSocket连接错误", cancellationToken);
                }
            }
        }
        // 处理身份验证
        private async Task HandleAuthentication(string action, JObject data, CancellationToken cancellationToken)
        {
            var userHandler = _serviceProvider.GetRequiredService<UserHandler>();
            if (action == "login")
            {
                _isAuthenticated = await userHandler.LoginAsync(_webSocket, data, cancellationToken)!=null;
            }
            else if (action == "register")
            {
                _isAuthenticated = await userHandler.RegisterAsync(_webSocket, data, cancellationToken) != null;
            }
        }
        // 发送消息的辅助方法
        private async Task SendMessageAsync(string message, CancellationToken cancellationToken)
        {
            var response = Encoding.UTF8.GetBytes(message);
            await _webSocket.SendAsync(new ArraySegment<byte>(response), WebSocketMessageType.Text, true, cancellationToken);
        }
    }
}

