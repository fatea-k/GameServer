using GameServer.Attributes;
using GameServer.Manager;
using GameServer.Managers;
using GameServer.Models;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json.Linq;

namespace GameServer.Services
{
    //同一个请求内有效的服务,可以注入到控制器中
    //用户服务
    [Service(ServiceLifetime.Scoped)]
    public class UserService
    {
        private readonly  DBManager<User> _userRepository;

        //构造函数注入
        public UserService(DBManager<User> userRepository)
        {
            _userRepository = userRepository;
        }

        // 注册用户
        public async Task<User> Register(string userName, string password)
        {
            //检查是否已有相同用户名的用户
            var user = _userRepository.GetAsync(u => u.UserName == userName).Result;
            //如果查到用户,说明已经有这个账号,直接返回空对象
            if (user != null)
            {
                return null; //当用户已存在时,返回空对象
            }

            var hpw = PWH.HashPassword(password);//对密码进行加密
            var _user = new User { UserName = userName, Password = hpw };
            //添加到数据库
            await _userRepository.CreateAsync(_user);


            return _user; // 返回新注册的用户
        }

        // 登录用户（这里没有进行密码验证）
        public async Task<User> Login(string userName, string password)
        {
            //查找用户是否存在
            var user = _userRepository.GetAsync(u => u.UserName == userName).Result;
            if (user != null)
            {
                if (PWH.VerifyPassword(password, user.Password))
                {
                    return user; //密码正确,返回用户对象
                }

            }

            return null;
        }
    }
}