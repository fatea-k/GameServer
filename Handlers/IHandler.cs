using Newtonsoft.Json.Linq;
using System.Net.WebSockets;

namespace GameServer.Handlers
{
    // 创建一个处理器接口，所有消息处理器将实现这个接口
    public interface IHandler
    {
        // 处理消息的方法，由具体的Handler实现
        Task HandleMessageAsync(WebSocket clientSocket, string action, JObject data, CancellationToken concellationToken);
    }
}