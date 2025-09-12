
using Microsoft.AspNetCore.SignalR;
using HaloSoft.EventLogger;
using SmartEnergyLabDataApi.Data;


public class NotificationHub : Hub {
    public static ConnectedUsers ConnectedUsers = new ConnectedUsers();
    public override Task OnConnectedAsync()
    {
        var userIdStr = this.Context.User?.Identity?.Name;
        if (int.TryParse(userIdStr, out int userId)) {
            ConnectedUsers.Add(userId);
        }
        return base.OnConnectedAsync();
    }

    public override Task OnDisconnectedAsync(Exception? exception)
    {
        var userIdStr = this.Context.User?.Identity?.Name;
        if (int.TryParse(userIdStr, out int userId)) {
            ConnectedUsers.Remove(userId);
        }
        return base.OnDisconnectedAsync(exception);
    }
}

public class ConnectedUsers {
    private Dictionary<int, int> _connectedUsers = new Dictionary<int, int>();

    public void Add(int userId)
    {
        lock (_connectedUsers) {
            if (_connectedUsers.ContainsKey(userId)) {
                _connectedUsers[userId]++;
            } else {
                _connectedUsers.Add(userId, 1);
            }
        }
    }

    public void Remove(int userId)
    {
        lock (_connectedUsers) {
            if (_connectedUsers.ContainsKey(userId)) {
                _connectedUsers[userId]--;
                if (_connectedUsers[userId] <= 0) {
                    _connectedUsers.Remove(userId);
                }
            }
        }
    }

    public bool IsConnected(int userId)
    {
        lock (_connectedUsers) {
            return _connectedUsers.ContainsKey(userId);
        }
    }

}
