//using Newtonsoft.Json;
//using System;

//namespace GameServer.Models
//{
//    public class User
//    {
//        [JsonProperty("id")]
//        public string Id { get; set; } // 用户唯一标识符
//        [JsonProperty("username")]
//        public string UserName { get; set; } // 用户名
//        [JsonIgnore]
//        public string Password { get; set; } // 用户密码（警告：实际应用中密码应该用加密存储）


//        // 构造方法
//        public User(string? username, string? password)
//        {
//            Id = Guid.NewGuid().ToString(); // 自动生成用户ID
//            UserName = username;
//            Password = password;
//        }
//    }
//}



using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson;
using Newtonsoft.Json;
using System;

namespace GameServer.Models
{
    public class User
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        [JsonProperty("userid")]
        public string Id { get; set; } // 用户唯一标识符

        /// <summary>
        /// 用户名
        /// </summary>
        [BsonElement("userName")]
        [JsonProperty("username")]
        public string UserName { get; set; } // 用户名

        /// <summary>
        /// 用户密码
        /// </summary>
        [BsonElement("password")]
        [JsonIgnore]// json格式化的时候忽略此属性
        public string Password { get; set; } // 用户密码（警告：实际应用中密码应该用加密存储）

        /// <summary>
        /// 用户创建时间
        /// </summary>
        [BsonElement("createTime")]
        [JsonIgnore]
        public DateTime CreateTime { get; set; } = DateTime.Now;
        /// <summary>
        /// 用户最后登录时间
        /// </summary>
        [BsonElement("lastLoginTime")]
        [JsonIgnore]
        public DateTime LastLoginTime { get; set; } 
        /// <summary>
        /// 用户登录次数
        /// </summary>
        [BsonElement("loginCount")]
        [JsonIgnore]
        public int LoginCount { get; set; } = 0;
        /// <summary>
        /// 注册IP
        /// </summary>
        [BsonElement("registerIp")]
        [JsonIgnore]
        public string RegisterIp { get; set; } = "";
        
        /// <summary>
        /// 用户最后登录IP
        /// </summary>
        [BsonElement("lastLoginIp")]
        [JsonIgnore]
        public string LastLoginIp { get; set; } = "";

        /// <summary>
        /// 用户角色(单一角色)
        /// </summary>
        [BsonElement("role")]
        [JsonProperty("role")]
        public Role Role { get; set; }

    }
}