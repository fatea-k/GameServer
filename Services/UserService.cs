using GameServer.Attributes;
using GameServer.Models;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.Generic;
using System.Linq;

namespace GameServer.Services
{
    [Service(ServiceLifetime.Singleton)]
    public class UserService
    {
        private readonly List<User> _users = new List<User>(); // 简化示例：用户存储列表

        // 注册用户
        public User Register(string username, string password)
        {
            var user = new User(username, password);
            _users.Add(user);
            return user; // 返回新注册的用户
        }

        // 登录用户（这里没有进行密码验证）
        public User Login(string username, string password)
        {
            return _users.FirstOrDefault(u => u.UserName == username && u.Password == password); 
        }
    }
}