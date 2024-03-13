using GameServer.Attributes;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Concurrent;
using System.Net.WebSockets;

namespace GameServer.Services
{
    /// <summary>
    /// 用户字典
    /// </summary>
    [Service(ServiceLifetime.Singleton)] //自动化注册单例服务
    public class ConnectionManager
    {
        //采用正反字典以保证数据的一致性,缺点占用了一部分内存
        private readonly ConcurrentDictionary<Guid, WebSocket> _userConnections = new ConcurrentDictionary<Guid, WebSocket>();
        private readonly ConcurrentDictionary<WebSocket, Guid> _connectionUsers = new ConcurrentDictionary<WebSocket, Guid>();

        /// <summary>
        /// 添加到字典
        /// </summary>
        /// <param name="userId">用户id</param>
        /// <param name="webSocket">用户连接</param>
        /// <returns></returns>
        public bool TryAdd(Guid userId, WebSocket webSocket)
        {
            bool added = _userConnections.TryAdd(userId, webSocket);
            if (added)
            {
                _connectionUsers.TryAdd(webSocket, userId);
            }
            return added;
        }

        /// <summary>
        /// 查询字典
        /// </summary>
        /// <param name="userId">用户id</param>
        /// <param name="webSocket">用户连接</param>
        /// <returns></returns>
        public bool TryGet(Guid userId, out WebSocket webSocket)
        {
            return _userConnections.TryGetValue(userId, out webSocket);
        }

        /// <summary>
        /// 移除字典项
        /// </summary>
        /// <param name="userId">用户id</param>
        /// <param name="webSocket">用户连接</param>
        /// <returns></returns>
        public bool TryRemove(Guid userId, out WebSocket webSocket)
        {
            bool removed = _userConnections.TryRemove(userId, out webSocket);
            if (removed)
            {
                _connectionUsers.TryRemove(webSocket, out _);
            }
            return removed;
        }

        /// <summary>
        /// 通过WebSocket连接获取关联的UserId
        /// </summary>
        /// <param name="webSocket">用户连接</param>
        /// <returns></returns>
        public Guid? GetUserIdByConnection(WebSocket webSocket)
        {
            // 反向字典查询
            if (_connectionUsers.TryGetValue(webSocket, out Guid userId))
            {
                return userId;
            }
            return null; // 未找到对应UserId则返回null
        }

        // 您还可以根据需求添加其他管理WebSocket连接所需的方法
    }
}