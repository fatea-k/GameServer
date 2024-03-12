using GameServer.Attributes;
using GameServer.Models;
using GameServer.Services;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Net.WebSockets;
using System.Text;
using System.Threading.Tasks;

namespace GameServer.Handlers
{
    //自动服务注册,注册为单例模式
    [Service(ServiceLifetime.Singleton)]
    //处理映射注册,注册action
    [HandlerMapping("login")]
    [HandlerMapping("register")]
    [HandlerMapping("logins")]
    // WebSocket消息处理器类，用于处理用户注册和登录
    /// <summary>
    /// 123123
    /// </summary>
    public class UserHandler : IHandler
    {
        private readonly UserService _userService;

        // 构造函数，通过依赖注入获取UserService服务
        /// <summary>
        /// 
        /// </summary>
        /// <param name="userService"></param>
        public UserHandler(UserService userService)
        {
            _userService = userService;
        }

        // 异步处理消息的方法
        public async Task HandleMessageAsync(WebSocket clientSocket, string action, JObject data, CancellationToken cancellationToken)
        {
         
          
            switch (action.ToLower())
            {
                case "register":
                    await RegisterAsync(clientSocket, data, cancellationToken);
                    break;
                case "login":
                    await LoginAsync(clientSocket, data, cancellationToken);
                    break;
                default:
                    // 如果没有匹配的动作，返回错误信息
                    await SendAsync(clientSocket, "未知的动作类型"); 
                    break;
            }
        }

        // 注册逻辑的异步方法 
        public async Task<User> RegisterAsync(WebSocket clientSocket, JObject data, CancellationToken cancellationToken)
        {
            var userName = data.Value<string>("userName");
            var password = data.Value<string>("password");

            // 调用UserService注册用户
            var user = _userService.Register(userName, password);

            // 创建注册成功的响应消息
            var response = JsonConvert.SerializeObject(new { action = "你注册了一个账号", success = true, userId = user.Id });

            // 发送响应消息给客户端
            await SendAsync(clientSocket, response);

            return user;
        }

        // 登录逻辑的异步方法
        public async Task<User> LoginAsync(WebSocket clientSocket, JObject data, CancellationToken cancellationToken)
        {
            var userName = data.Value<string>("userName");
            var password = data.Value<string>("password");

            // 调用UserService登录用户
            var user = _userService.Login(userName, password);

            // 根据登录结果，创建响应消息
            var response = JsonConvert.SerializeObject(new { action = "你登录了一个账号", success = user != null, userId = user?.Id });

            // 发送响应消息给客户端
            await SendAsync(clientSocket, response);
            return user;
        }

        // 发送消息的共享异步方法
        private async Task SendAsync(WebSocket clientSocket, string message)
        {
            if (clientSocket.State != WebSocketState.Open)
                return;

            // 将消息字符串转换成字节数据
            var buffer = Encoding.UTF8.GetBytes(message);

            // 异步发送消息到客户端
            await clientSocket.SendAsync(new ArraySegment<byte>(buffer), WebSocketMessageType.Text, true, System.Threading.CancellationToken.None);
        }
    }
}