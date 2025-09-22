
using Microsoft.AspNetCore.SignalR;
using HaloSoft.EventLogger;
using SmartEnergyLabDataApi.Data;
using Microsoft.Extensions.ObjectPool;
using NHibernate.Id;


public class NotificationHub : Hub {
    public static ConnectedUsers ConnectedUsers = new ConnectedUsers();
    public override async Task OnConnectedAsync()
    {
        var userIdStr = this.Context.User?.Identity?.Name;
        if (int.TryParse(userIdStr, out int userId)) {
            ConnectedUsers.AddConnection(userId, this.Context.ConnectionId);
        }
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var userIdStr = this.Context.User?.Identity?.Name;
        if (int.TryParse(userIdStr, out int userId)) {
            ConnectedUsers.RemoveConnection(userId, this.Context.ConnectionId);
        }
        await base.OnDisconnectedAsync(exception);
    }

    public async Task Pong()
    {
        await Task.Run(() => {
            var userIdStr = this.Context.User?.Identity?.Name;
            if (int.TryParse(userIdStr, out int userId)) {
                ConnectedUsers.AddConnection(userId, this.Context.ConnectionId);
            }
        });

    }

    public async Task PingUsers()
    {
        ConnectedUsers.Clear();
        await this.Clients.All.SendAsync("Ping");
    }
}

public class ConnectedUsers {
    private Dictionary<int, Dictionary<string,bool>> _connectedUsers = new Dictionary<int, Dictionary<string,bool>>();

    public void AddConnection(int userId, string connectionId)
    {
        lock (_connectedUsers) {
            // create connection dict if not exists
            if (!_connectedUsers.ContainsKey(userId)) {
                _connectedUsers.Add(userId, new Dictionary<string, bool>());
            }
            var connectionDict = _connectedUsers[userId];
            // add this connection Id if not already there
            if (!connectionDict.ContainsKey(connectionId)) {
                connectionDict.Add(connectionId, true);
            }
        }
    }

    public void RemoveConnection(int userId, string connectionId)
    {
        lock (_connectedUsers) {
            if (_connectedUsers.ContainsKey(userId)) {
                var connectionDict = _connectedUsers[userId];
                // Remove this connectionId from users lists of valid connections
                // (will not raise exception if not in list)
                connectionDict.Remove(connectionId);
            }
        }
    }

    public void Clear()
    {
        lock (_connectedUsers) {
            _connectedUsers.Clear();
        }
    }

    public int NumConnections(int userId)
    {
        lock (_connectedUsers) {
            if (_connectedUsers.TryGetValue(userId, out Dictionary<string,bool> connectionDict)) {
                return connectionDict.Count;
            } else {
                return 0;
            }
        }
    }
}
