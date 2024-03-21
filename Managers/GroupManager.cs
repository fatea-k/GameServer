using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using GameServer.Attributes;
using Microsoft.Extensions.DependencyInjection;

namespace GameServer.Manager
{
    /// <summary>
    /// WebSocket组管理器
    /// </summary>
    [Service(ServiceLifetime.Singleton)] // 自动化注册单例服务
    public class GroupManager
    {
        // 存储组名和对应的WebSocket连接及用户ID映射的集合
        private static readonly ConcurrentDictionary<string, ConcurrentDictionary<string,WebSocket>> _groups = new ConcurrentDictionary<string, ConcurrentDictionary<string, WebSocket>>();
        /// <summary>
        /// 添加连接到指定的组
        /// </summary>
        /// <param name="groupName">组名</param>
        /// <param name="userId">用户ID，用于获取WebSocket连接</param>
        public static void AddToGroup(string groupName, string userId)
        {
            var websocket = ConnectionManager.GetWebSocketByUserId(userId);
            var group = _groups.GetOrAdd(groupName, _ => new ConcurrentDictionary<string, WebSocket>());
            group.TryAdd(userId,websocket);
        }

        /// <summary>
        /// 从指定组中移除指定用户的连接
        /// </summary>
        /// <param name="groupName">组名</param>
        /// <param name="userId">用户id</param>
        public static void RemoveFromGroup(string groupName, string userId)
        {
            if (_groups.TryGetValue(groupName, out var group))
            {
                group.TryRemove(userId, out _);
            }
        }

       
        /// <summary>
        /// 获取组中的所有连接
        /// </summary>
        /// <param name="groupName">组名</param>
        /// <returns>WebSocket连接集合</returns>
        public static IEnumerable<WebSocket> GetConnectionsByGroupName(string groupName)
        {
            if (_groups.TryGetValue(groupName, out var group))
            {
                return group.Values;
            }
            return Enumerable.Empty<WebSocket>();
        }

        /// <summary>
        /// 从所有组中移除指定用户的连接
        /// </summary>
        /// <param name="userId">要移除连接的用户</param>
        public static void RemoveConnectionFromAllGroups(string userId)
        {
            foreach (var group in _groups.Values)
            {
                group.TryRemove(userId, out _);
            }
        }
    }
}
