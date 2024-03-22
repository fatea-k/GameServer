using GameServer.Attributes;
using GameServer.Enums;
using GameServer.Manager;
using GameServer.Managers;
using GameServer.Models;
using GameServer.Services;
using GameServer.Utilities;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Collections.Concurrent;
using System.Net.WebSockets;

namespace GameServer.Handlers
{
    //自动服务注册,注册为单例模式
    [Service(ServiceLifetime.Singleton)]
    //处理映射注册,注册action
    [HandlerMapping(UserActions.login)]
    [HandlerMapping(UserActions.register)]

    // WebSocket消息处理器类，用于处理用户注册和登录
    /// <summary>
    /// 123123
    /// </summary>
    public class UserHandler : IHandler
    {

        /// <summary>
        /// 内部类,用于同一组织管理消息头
        /// </summary>
        internal static class UserActions
        {
            public const string login = "login";//登录
            public const string register = "register";//注册  
        }

        #region 私有内容初始化
        private readonly UserService _userService;
        #endregion

        /// <summary>
        /// 构造方法
        /// </summary>
        /// <param name="userService">用户内容处理服务器</param>
        public UserHandler(
            UserService userService)
        {
            //依赖注入获得UserService服务
            _userService = userService;
        }



        /// <summary>
        /// 异步处理接收到的消息
        /// </summary>
        /// <param name="clientSocket"></param>
        /// <param name="action"></param>
        /// <param name="data"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async Task HandleMessageAsync(WebSocket clientSocket, string action, JObject data, CancellationToken cancellationToken)
        {
            //转为小写
            action = action.ToLower();
            switch (action)
            {
                case UserActions.register://跳转注册方法
                    await RegisterAsync(clientSocket, data, action.ToLower(), cancellationToken);
                    break;
                case UserActions.login://跳转登录方法
                    await LoginAsync(clientSocket, data, action.ToLower(), cancellationToken);
                    break;
                default:
                    //未知的消息 不进行处理
                    break;
            }
        }

        /// <summary>
        /// 异步用户注册逻辑
        /// </summary>
        /// <param name="clientSocket"></param>
        /// <param name="data"></param>
        /// <param name="action"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async Task<User> RegisterAsync(WebSocket clientSocket, JObject data, string action, CancellationToken cancellationToken)
        {

            //解析出用户名和密码
            var userName = data.Value<string>("username");
            var password = data.Value<string>("password");
           


            //检查用户名和密码是否为空
            if (string.IsNullOrEmpty(userName) || string.IsNullOrEmpty(password))
            {
                // 发送响应消息给客户端
                await Clients.SendErrorAsync(clientSocket, action, ErrorEnum.用户名或密码为空, cancellationToken);
                return null;
            }

            //检查字典中是否有该链接
            var userId = ConnectionManager.GetUserIdByConnection(clientSocket);

            if (!string.IsNullOrEmpty(userId))
            {
                //当已字典中已存在,返回一个错误消息
                await Clients.SendErrorAsync(clientSocket, action, ErrorEnum.用户已登录, cancellationToken);
                return null;
            }


           
                // 调用UserService注册用户
                var user = await _userService.Register(userName, password);//如果数据库中有该用户,返回null

                if (user != null) //当返回不为空,说明注册成功
                {
                    // 添加用户ID与WebSocket连接到线程安全的字典
                    ConnectionManager.AddConcurrent(user.Id, clientSocket); // 使用TryAdd确保添加操作的原子性

                // 随机分配一个角色
                // var jObject = JObject.FromObject(_roleService.GetRoleBirth()); //JObject.FromObject(user);
                //添加Userid到JObject
                // var jObject.Add("userid", user.Id);

                var jObject= JObject.FromObject(user);

                var websocketmessage = new WSMessage()
                    {
                        Action = action,
                        Data = jObject
                    };
                    // 发送响应消息给客户端
                    await Clients.All.SendAsync(websocketmessage.Serialize(), 0, 0, cancellationToken);


                }
                else//当返回为空,说明用户名已存在
                {
                    // 发送响应消息给客户端
                    await Clients.SendErrorAsync(clientSocket, action, ErrorEnum.用户名已存在, cancellationToken);
                }

                return user;
            

        }

        /// <summary>
        /// 异步用户登录逻辑,增加了单账号登录的实现
        /// </summary>
        /// <param name="clientSocket"></param>
        /// <param name="data"></param>
        /// <param name="action"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async Task<User> LoginAsync(WebSocket clientSocket, JObject data, string action, CancellationToken cancellationToken)
        {
            //解析出用户名和密码
            var userName = data.Value<string>("username");
            var password = data.Value<string>("password");
            //检查用户名和密码是否为空
            if (string.IsNullOrEmpty(userName) || string.IsNullOrEmpty(password))
            {
                // 发送响应消息给客户端
                await Clients.SendErrorAsync(clientSocket, action, ErrorEnum.用户名或密码为空, cancellationToken);
                return null;
            }

            // 调用UserService登录用户
            var user = await _userService.Login(userName, password);


            // 检查用户是否成功登录
            if (user == null) //登录失败
            {
                // 发送错误消息给客户端
                await Clients.SendErrorAsync(clientSocket, action, ErrorEnum.用户名或密码不正确, cancellationToken);
            }
            else
            {
                
                // 添加用户ID与WebSocket连接到线程安全的字典
                ConnectionManager.AddConcurrent(user.Id, clientSocket); // 使用TryAdd确保添加操作,如果有旧链接 会更新成新的链接

                //编辑返回的数据
                var message = new WSMessage()
                {
                    Action = action,
                    Data = JObject.FromObject(user)
                };

                // 发送响应消息给客户端
                await Clients.SendAsync(clientSocket, message.Serialize(), cancellationToken);
            }

            return user;
        }

    }
}