using GameServer.Attributes;
using GameServer.Models;
using GameServer.Utilities;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.Linq;
using System.Net.WebSockets;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace GameServer.Services
{
    [Service(ServiceLifetime.Singleton)]
    public class UserService
    {
        private readonly List<User> _users = new List<User>(); //用户存储列表

        // 注册用户
        public async Task<User> Register(JObject data)
        {

            var userName = data.Value<string>("userName");
            var password = data.Value<string>("password");

            //搜索数据库,查找重复
            //如果查到用户,说明已经有这个账号,直接返回空对象
            if (userName != null)
            {
                return null;
            }

            var user = new User(userName, password);
            _users.Add(user);
            return user; // 返回新注册的用户
        }

        // 登录用户（这里没有进行密码验证）
        public async Task<User> Login(JObject data)
        {
            var userName = data.Value<string>("userName");
            var password = data.Value<string>("password");

            var user= _users.FirstOrDefault(u => u.UserName == userName && u.Password == password);


            return user; 
        }
    }
}