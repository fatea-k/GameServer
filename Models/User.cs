﻿using System;

namespace GameServer.Models
{
    public class User
    {
        public Guid Id { get; set; } // 用户唯一标识符
        public string UserName { get; set; } // 用户名
        public string Password { get; set; } // 用户密码（警告：实际应用中密码应该用加密存储）

        // 构造方法
        public User(string username, string password)
        {
            Id = Guid.NewGuid(); // 自动生成用户ID
            UserName = username;
            Password = password;
        }
    }
}