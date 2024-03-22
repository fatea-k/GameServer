
//创建Clients类, 模仿SignalR的Clients API，允许开发者方便地向不同的客户端集合发送消息。, 并且代码中需要有完整的中文注释, 功能需求:
//1.创建Clients类，模仿SignalR的Clients API，允许开发者方便地向不同的客户端集合发送消息。
//2.需要有以下几种消息发送方法:
// 2.1 await clients.SendAsync("message", cancellationToken)//发送给当前连接的客户端
// 2.2 await clients.All.SendAsync("Hello everyone!", time, batchSize, cancellationToken); //发送给所有人,通过参数设置计时器,消息批处理大小,多个用户不会发送同一组消息
// 2.3 await clients.Others.SendAsync("Hello others!", time, batchSize, cancellationToken); //发送给除当前客户端以外的所有客户端,通过参数设置计时器,消息批处理大小,多个用户不会发送同一组消息
// 2.4 await clients.Client("connectionId").SendAsync("Hello client!", cancellationToken); //发送给特定客户端,不需要设置计时器和批处理
// 2.5 await clients.Group("groupName").SendAsync("Hello group!", time, batchSize, cancellationToken); //发送给特定群组,通过参数设置计时器,消息批处理大小,多个用户不会发送同一组消息


using GameServer.Enums;
using GameServer.Manager;
using GameServer.Models;
using Newtonsoft.Json.Linq;
using System;
using System.Net.Sockets;
using System.Net.WebSockets;
using System.Text;

namespace GameServer.Managers
{
    public class Clients
    {
        /// <summary>
        /// 发送给所有人
        /// </summary>
        public static ClientsCaller All => new ClientsCaller(TargetType.All);
        /// <summary>
        /// 发送给指定客户端
        /// </summary>
        /// <param name="userId">指定的客户端用户ID</param>
        /// <returns></returns>
        public static ClientsCaller Client(string userId) => new ClientsCaller(TargetType.Client, userId);

        /// <summary>
        /// 发送消息给其他客户端
        /// </summary>
        /// <param name="groupName"></param>
        /// <returns></returns>
        public static ClientsCaller Others => new ClientsCaller(TargetType.Others);

        // 发送消息给特定的群组
        public static ClientsCaller Group(string groupName) => new ClientsCaller(TargetType.Group, null, groupName);


        public static async Task SendAsync(WebSocket webSocket, string message, CancellationToken cancellationToken = default)
        {
            Console.WriteLine("发送消息:" + message);
            await webSocket.SendAsync(new ArraySegment<byte>(Encoding.UTF8.GetBytes(message)), WebSocketMessageType.Text, true, cancellationToken);
        }
        // 封装发送错误消息的方法
        public static async Task SendErrorAsync(WebSocket clientSocket, string action, ErrorEnum errorEnum, CancellationToken cancellationToken)
        {
            var message = new WSMessage
            {
                Action = action,
                Error = errorEnum,//错误枚举
                Data = new JObject()
                {
                    ["message"] = errorEnum.ToString()//错误信息
                }

            };

            Console.WriteLine("发送错误消息:"+message.Serialize());
            await SendAsync(clientSocket, message.Serialize(), cancellationToken);
        }
        // ClientsCaller类定义
        public class ClientsCaller(TargetType targetType, string userid = null, string groupName = null)
        {
           
            private TargetType _targetType = targetType;
            private string _userid = userid;
            private string _groupName = groupName;

            /// <summary>
            /// 发送消息
            /// </summary>
            /// <param name="message">序列化后的消息</param>
            /// <param name="intervalMs"> 计时器间隔</param>
            /// <param name="batchSize"> 消息批处理大小</param>
            /// <param name="cancellationToken"> cancellationToken</param>
            /// <returns> </returns>
            public async Task SendAsync(string message, int intervalMs = 0, int batchSize = 0, CancellationToken cancellationToken = default)
            {
                IEnumerable<WebSocket> clients;
                switch (_targetType)
                {
                    case TargetType.All:
                        clients = ConnectionManager.GetAllConnection();
                        _groupName = "all";
                        break;
                    case TargetType.Client:
                        clients = new List<WebSocket> { ConnectionManager.GetWebSocketByUserId(_userid) };
                        break;
                        case TargetType.Others:
                        clients = ConnectionManager.GetAllConnection().Where(c => c != ConnectionManager.GetWebSocketByUserId(_userid));
                        _groupName = "others";
                        break;
                    case TargetType.Group:
                        clients = GroupManager.GetConnectionsByGroupName(_groupName);
                        break;
                    default:
                        clients = Enumerable.Empty<WebSocket>();
                        break;
                }

                // 发送消息
                if (_targetType == TargetType.Client)
                {

                    //发送消息给指定客户端
                    await clients.First().SendAsync(new ArraySegment<byte>(Encoding.UTF8.GetBytes(message)), WebSocketMessageType.Text, true, cancellationToken);

                }
                else
                {
                    await MessageManagner.SendMessageAsync(_groupName,clients,message, intervalMs, batchSize, cancellationToken);
                }

            }
          
        }


        public enum TargetType//发送目标类型
        {
            All,//所有人
            Client,//指定客户端
            Group,//指定群组
            Others//除当前客户端以外的所有客户端
        }
    }

}