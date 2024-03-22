using GameServer.Attributes;
using GameServer.Models;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Driver;

namespace GameServer.Utilities
{
    // DataContext 类用于管理与 MongoDB 的连接和数据集合的引用
    //自动服务注册
    [Service(ServiceLifetime.Scoped)]
    public class DataContext
    {
        // 定义一个只读的 IMongoDatabase 类型的私有字段，以保存MongoDB数据库的引用
        private readonly IMongoDatabase _database;

        // DataContext 类的构造函数
        public DataContext()
        {
            // 创建一个 MongoClient 对象，连接到本地的 MongoDB 实例
            // 在生产环境中，连接字符串应从配置文件或环境变量中获取
            var client = new MongoClient("mongodb://localhost:27017");
            // 指定数据库名称并获取 IMongoDatabase 的实例
            _database = client.GetDatabase("game_server_db");
        }

        // 添加公共属性以提供对_private字段的访问
        public IMongoDatabase Database => _database;

    }
}