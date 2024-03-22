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
    [HandlerMapping(CreateRoleActions.getRole)]
    // WebSocket消息处理器类，用于处理用户注册和登录
    /// <summary>
    /// 创建角色处理器
    /// </summary>
    public class CreateRoleHandler : IHandler
    {

        /// <summary>
        /// 内部类,用于同一组织管理消息头
        /// </summary>
        internal static class CreateRoleActions
        {
            public const string getRole = "getrole";//获得角色
        }

        #region 私有内容初始化
        private readonly RoleService _roleService;
        private readonly ConnectionManager _connectionManager;
        #endregion

        /// <summary>
        /// 构造方法
        /// </summary>
        /// <param name="userService">用户内容处理服务器</param>
        public CreateRoleHandler(
            RoleService roleService
            )
        {
            //依赖注入获得UserService服务
            _roleService = roleService;
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
                case CreateRoleActions.getRole://获取一个角色
                   
                    await CreateRoleAsync(clientSocket, data, action.ToLower(), cancellationToken);
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
        public async Task CreateRoleAsync(WebSocket clientSocket, JObject data, string action, CancellationToken cancellationToken)
        {
            
            //检查是否合法用户
            var userId = ConnectionManager.GetUserIdByConnection(clientSocket);
            Console.WriteLine();

            if (userId == null)
            {
                //用户未登录
                //当已字典中已存在,返回一个错误消息
                return;
            }
            if (data == null)
            {
                //请求的数据为空

                await Clients.SendErrorAsync(clientSocket, action, ErrorEnum.非法请求, cancellationToken);
                return;
            }
            if (data.Value<string>("userid") != userId)
            {
                //请求的用户与当前连接的用户不一致
                await Clients.SendErrorAsync(clientSocket, action, ErrorEnum.非法请求, cancellationToken);
                return;
            }



            var _role = await _roleService.GetRoleBirth();

            // 序列化角色
            var jObject = JObject.FromObject(_role); //JObject.FromObject(user);


            jObject.Add("userid", ConnectionManager.GetUserIdByConnection(clientSocket));



            var websocketmessage = new WSMessage()
            {
                Action = action,
                Data = jObject
            };
            // 发送响应消息给客户端
            await Clients.SendAsync(clientSocket, websocketmessage.Serialize(),cancellationToken);


        }


    }
}