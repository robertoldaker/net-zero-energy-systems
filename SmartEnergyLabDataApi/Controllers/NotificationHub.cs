
using Microsoft.AspNetCore.SignalR;
using HaloSoft.EventLogger;


public class NotificationHub : Hub {
    public override Task OnConnectedAsync()
    {
        Console.WriteLine($"Connected! {this.Context.ConnectionId}");
        return base.OnConnectedAsync();
    }

    public override Task OnDisconnectedAsync(Exception? exception)
    {
        Console.WriteLine("Disconnected!");
        return base.OnDisconnectedAsync(exception);
    }
}