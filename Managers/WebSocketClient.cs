using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

// WebSocketClient 类提供了一个静态 API，模仿 SignalR 的 Clients API，
// 允许开发者方便地向不同的客户端集合发送消息。
public static class WebSocketClient
{
    // 获取单例的 ConnectionManager 实例。
    private static ConnectionManager _connectionManager = ConnectionManager.Instance;

    // 用来发送消息给所有连接的客户端。
    public static ClientsCaller All => new ClientsCaller(_connectionManager);

    // 用来发送消息给除当前客户端以外的其他所有客户端。
    // 需要从外部传入当前客户端的 ID。
    public static ClientsCaller Others(string currentConnectionId) => new ClientsCaller(_connectionManager, currentConnectionId);

    // 用来发送消息给特定的客户端。
    public static ClientsCaller Client(string connectionId) => new ClientsCaller(_connectionManager, null, connectionId);

    // 用来发送消息给特定组内的所有客户端。群组的管理逻辑需要另行实现。
    public static ClientsCaller Group(string groupName) => new ClientsCaller(_connectionManager, groupName: groupName);
}

// ClientsCaller 类封装了不同的消息发送方式。
// 这个类的对象会被用来实际发送消息给一个或多个客户端。
public class ClientsCaller
{
    private ConnectionManager _connectionManager;
    private HashSet<string> _excludeConnectionIds;
    private string _targetConnectionId;
    private string _groupName;

    // ClientsCaller 的构造函数。
    // 参数 connectionManager 用于发送消息的连接管理器，
    // currentConnectionId 为当前客户端 ID（如果存在的话，将在 Others 实例中使用），
    // targetConnectionId 为目标客户端 ID（如果存在的话，将在 Client 实例中使用），
    // groupName 为群组名（如果存在的话，将在 Group 实例中使用）。
    public ClientsCaller(
        ConnectionManager connectionManager,
        string currentConnectionId = null,
        string targetConnectionId = null,
        string groupName = null)
    {
        _connectionManager = connectionManager;
        _excludeConnectionIds = string.IsNullOrEmpty(currentConnectionId) ? null : new HashSet<string> { currentConnectionId };
        _targetConnectionId = targetConnectionId;
        _groupName = groupName;
    }

    // 发送消息的方法，支持 CancellationToken。
    // 参数 message 为要发送的消息文本，
    // cancellationToken 为取消操作的令牌。
    public async Task SendAsync(string message, CancellationToken cancellationToken)
    {
        // 如果是群组消息
        if (_groupName != null)
        {
            // 发送消息给群组内所有客户端
            var groupMembers = GroupManager.GetGroupMembers(_groupName);

            // 创建并执行所有成员的发送消息任务
            var tasks = groupMembers.Select(connectionId =>
                _connectionManager.SendMessageAsync(connectionId, message, cancellationToken));

            // 等待所有发送任务完成
            await Task.WhenAll(tasks);
        }
        // 如果是向特定客户端发送消息
        else if (_targetConnectionId != null)
        {
            await _connectionManager.SendMessageAsync(_targetConnectionId, message, cancellationToken);
        }
        // 如果发送消息给除了某些客户端之外的所有客户端
        else if (_excludeConnectionIds != null)
        {
            var allConnectionIds = _connectionManager.GetAllConnectionIds();
            IEnumerable<string> targetIds = allConnectionIds.Except(_excludeConnectionIds);
            foreach (var connectionId in targetIds)
            {
                await _connectionManager.SendMessageAsync(connectionId, message, cancellationToken);
            }
        }
        // 如果是向所有客户端发送消息
        else
        {
            await _connectionManager.SendMessageToAllAsync(message, cancellationToken);
        }
    }
}