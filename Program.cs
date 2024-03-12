using GameServer.Handlers;
using GameServer.Services;
using GameServer.Utilities;
using GameServer.WebSocketManager;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Reflection;
using System.Threading;

namespace GameServer
{
    class Program
    {
        public static IServiceProvider ServiceProvider;
        static void Main(string[] args)
        {


            // 创建服务集合，建立依赖注入容器
            ServiceProvider = new ServiceCollection()

                .AddAutoRegisteredServices(new[] { Assembly.GetExecutingAssembly() }) // 自动注册标记 [Service] 和 [HandlerMapping] 特性的服务和映射

                .BuildServiceProvider(); // 生成实际的服务提供器，以便在整个应用中使用

            // 使用容器运行你的 WebSocket 服务器
            var server = ServiceProvider.GetRequiredService<WebSocketServer>();
            _ = server.Start();
            while (true)
            {
                // 可以在此处添加服务器运行时需要执行的代码
            }
        }
    }
}