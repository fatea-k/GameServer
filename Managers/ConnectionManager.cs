using GameServer.Attributes;
using GameServer.Enums;
using GameServer.Models;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.Concurrent;
using System.Net.WebSockets;

namespace GameServer.Manager
{
    /// <summary>
    /// 用户字典
    /// </summary>
    [Service(ServiceLifetime.Singleton)] //自动化注册单例服务
    public class ConnectionManager
    {
        //采用正反字典以保证数据的一致性,缺点占用了一部分内存
        private static  readonly ConcurrentDictionary<Guid, WebSocket> _userConnections = new ConcurrentDictionary<Guid, WebSocket>();



        /// <summary>
        /// 添加到字典
        /// </summary>
        /// <param name="userId">用户id</param>
        /// <param name="webSocket">用户连接</param>
        /// <returns></returns>
        public static void AddConcurrent(Guid userId, WebSocket webSocket)
        {

           
            _userConnections.AddOrUpdate(userId, webSocket, (key, oldSocket) =>
            {
                if(webSocket!= oldSocket)
                {
                    oldSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, ErrorEnum.账号在其它地方登录.ToString(), CancellationToken.None).Wait();
                }
                return webSocket; // 使用新的 WebSocket 实例替代旧的实例
            });

            var code = webSocket.GetHashCode();
            var num = GetAllConnection().Count();
            Console.WriteLine($"连接:{code}  已添加到字典,当前连接数量:{num}");
        }

        /// <summary>
        /// 根据userId查找连接
        /// </summary>
        /// <param name="userId">用户id</param>
        /// <returns></returns>
        public static WebSocket GetWebSocketByUserId(Guid userId)
        {

            _userConnections.TryGetValue(userId, out WebSocket webSocket);
            return webSocket;
        }
        /// <summary>
        /// 根据用户id移除连接
        /// </summary>
        /// <param name="userId"></param>
        public static void RemoveConnectionByUserId(Guid userId)
        {
            _userConnections.TryRemove(userId, out var _);  // 移除连接，忽略移除的结果
        }
        /// <summary>
        /// 根据websocket移除连接
        /// </summary>
        /// <param name="webSocket"></param>
        public static void RemoveConnectionByWebSocket(WebSocket webSocket)
        {
            var guid = GetUserIdByConnection(webSocket);
            if(guid != Guid.Empty)
            {
                _userConnections.TryRemove(guid, out var _);  // 移除连接，忽略移除的结果

            }
            var code = webSocket.GetHashCode();
            var num = GetAllConnection().Count();
            Console.WriteLine($"连接:{code}  以被移除,当前连接数量:{num}");

        }


        /// <summary>
        /// 通过WebSocket连接获取关联的UserId
        /// </summary>
        /// <param name="webSocket">用户连接</param>
        /// <returns></returns>
        public static Guid GetUserIdByConnection(WebSocket webSocket)
        {
            var userId = _userConnections.FirstOrDefault(pair => pair.Value == webSocket).Key;
            if (userId != Guid.Empty)
            {
                return userId;
            }

            return Guid.Empty; // 未找到对应UserId则返回null
        }

        /// <summary>
        /// 获得所有连接
        /// </summary>
        /// <returns></returns>
        public static IEnumerable<WebSocket> GetAllConnection()
        {
            return _userConnections.Values;
        }
    }
}