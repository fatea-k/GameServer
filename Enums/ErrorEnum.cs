using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GameServer.Enums
{
    public enum ErrorEnum
    {
        没有错误 = 0,
        用户名或密码不正确 = 1,
        用户名已存在 = 2,
        账号在其它地方登录 = 3,
        无法注册=4,

    }
}
