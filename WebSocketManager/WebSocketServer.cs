using GameServer.Attributes;
using System;
using System.Net;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;

namespace GameServer.WebSocketManager
{
    // WebSocket服务器类，负责启动、监听和关闭WebSocket连接
    [Service(ServiceLifetime.Singleton)]
    public class WebSocketServer
    {
        private readonly HttpListener _httpListener; // 用于监听HTTP请求的HttpListener实例
        private CancellationTokenSource _cancellationTokenSource; // 用于取消操作的CancellationTokenSource实例
        private readonly IServiceProvider _serviceProvider; // 服务提供者，用于依赖注入

        // 构造方法，指定监听地址和服务提供者
        public WebSocketServer(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider; // 保存服务提供者实例
            _httpListener = new HttpListener(); // 创建HttpListener实例
            _cancellationTokenSource = new CancellationTokenSource(); // 创建CancellationTokenSource实例
            _httpListener.Prefixes.Add("http://localhost:12345/"); // 添加一个监听地址前缀
        }

        // 启动服务器的异步方法
        public async Task Start()
        {
            _httpListener.Start(); // 启动HttpListener实例
            Console.WriteLine("WebSocket服务器已启动……");

            // 持续监听连接请求
            while (_httpListener.IsListening)
            {
                try
                {
                    var context = await _httpListener.GetContextAsync(); // 异步获取HTTP上下文
                    if (context.Request.IsWebSocketRequest) // 检查请求是否为WebSocket请求
                    {
                        var webSocketContext = await context.AcceptWebSocketAsync(subProtocol: null); // 接受WebSocket请求，建立连接
                        var webSocket = webSocketContext.WebSocket; // 获取WebSocket对象

                        OnClientConnected(webSocket); // 处理客户端连接事件

                        var connection = ActivatorUtilities.CreateInstance<WebSocketConnection>(_serviceProvider, webSocket); // 使用服务提供者创建WebSocketConnection实例
                        _ = Task.Run(() => connection.ProcessMessagesAsync(_cancellationTokenSource.Token)) // 开启一个新任务处理消息
                            .ContinueWith(task =>
                            {
                                // 客户端断开连接后执行
                                OnClientDisconnected(webSocket); // 处理客户端断开连接事件
                                webSocket.Dispose(); // 释放WebSocket资源
                            }, TaskContinuationOptions.OnlyOnRanToCompletion);
                    }
                    else
                    {
                        // 如果不是WebSocket请求，返回400 Bad Request状态码
                        context.Response.StatusCode = 400;
                        context.Response.Close(); // 关闭响应流
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"错误: {ex.Message}"); // 输出错误信息

                    // 如果监听器已经停止，则退出循环
                    if (!_httpListener.IsListening)
                        break;
                }
            }
        }

        // 客户端连接事件处理方法
        private void OnClientConnected(WebSocket socket)
        {
            Console.WriteLine($"客户端已连接：{socket.GetHashCode()}");
            // 这里可以添加更多的客户端连接处理逻辑
        }

        // 客户端断开连接事件处理方法
        private void OnClientDisconnected(WebSocket socket)
        {
            Console.WriteLine($"客户端已断开连接：{socket.GetHashCode()}");
            // 这里可以添加更多的客户端断开连接处理逻辑
        }

        // 停止服务器的方法
        public void Stop()
        {
            _cancellationTokenSource.Cancel(); // 触发取消操作
            _httpListener.Stop(); // 停止HttpListener实例
            _httpListener.Close(); // 关闭HttpListener实例
            Console.WriteLine("WebSocket服务器已关闭。");
        }
    }
}