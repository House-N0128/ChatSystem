using System.Collections.ObjectModel;
using ChatSystem.Core.DTOs;
using Microsoft.AspNetCore.SignalR.Client;

namespace ChatSystem.Wpf.Services;

public class SignalRService
{
    private HubConnection? _connection;

    // 事件 — ViewModel 订阅
    public event Action<MessageDTO>? MessageReceived;
    public event Action<GroupMessageDTO>? GroupMessageReceived;
    public event Action<FriendDTO>? FriendAdded;
    public event Action<int>? FriendRemoved;
    public event Action<FriendRequestDTO>? FriendRequestReceived;
    public event Action<UserDTO>? UserOnline;
    public event Action<int>? UserOffline;
    public event Action<int>? UserTyping;
    public event Action<int>? UserStopTyping;

    public async Task ConnectAsync(string serverUrl)
    {
        if (AuthService.AccessToken == null) return;

        // 先断开旧连接
        if (_connection != null)
        {
            await _connection.DisposeAsync();
            _connection = null;
        }

        _connection = new HubConnectionBuilder()
            .WithUrl($"{serverUrl}/hubs/chat", options =>
            {
                options.AccessTokenProvider = () => Task.FromResult(AuthService.AccessToken)!;
            })
            .WithAutomaticReconnect()
            .Build();

        RegisterHandlers();

        await _connection.StartAsync();
    }

    private void RegisterHandlers()
    {
        if (_connection == null) return;

        _connection.On<MessageDTO>("ReceivePrivateMessage", msg =>
            System.Windows.Application.Current.Dispatcher.Invoke(() => MessageReceived?.Invoke(msg)));

        _connection.On<GroupMessageDTO>("ReceiveGroupMessage", msg =>
            System.Windows.Application.Current.Dispatcher.Invoke(() => GroupMessageReceived?.Invoke(msg)));

        _connection.On<FriendDTO>("FriendAdded", f =>
            System.Windows.Application.Current.Dispatcher.Invoke(() => FriendAdded?.Invoke(f)));

        _connection.On<int>("FriendRemoved", id =>
            System.Windows.Application.Current.Dispatcher.Invoke(() => FriendRemoved?.Invoke(id)));

        _connection.On<FriendRequestDTO>("FriendRequestReceived", r =>
            System.Windows.Application.Current.Dispatcher.Invoke(() => FriendRequestReceived?.Invoke(r)));

        _connection.On<UserDTO>("UserOnline", u =>
            System.Windows.Application.Current.Dispatcher.Invoke(() => UserOnline?.Invoke(u)));

        _connection.On<int>("UserOffline", id =>
            System.Windows.Application.Current.Dispatcher.Invoke(() => UserOffline?.Invoke(id)));

        _connection.On<int>("UserTyping", id =>
            System.Windows.Application.Current.Dispatcher.Invoke(() => UserTyping?.Invoke(id)));

        _connection.On<int>("UserStopTyping", id =>
            System.Windows.Application.Current.Dispatcher.Invoke(() => UserStopTyping?.Invoke(id)));
    }

    public async Task SendPrivateMessageAsync(int receiverId, string content)
    {
        if (_connection?.State == HubConnectionState.Connected)
            await _connection.InvokeAsync("SendPrivateMessage", receiverId, content);
    }

    public async Task SendGroupMessageAsync(int groupId, string content)
    {
        if (_connection?.State == HubConnectionState.Connected)
            await _connection.InvokeAsync("SendGroupMessage", groupId, content);
    }

    public async Task JoinGroupAsync(int groupId)
    {
        if (_connection?.State == HubConnectionState.Connected)
            await _connection.InvokeAsync("JoinGroup", groupId);
    }

    public async Task LeaveGroupAsync(int groupId)
    {
        if (_connection?.State == HubConnectionState.Connected)
            await _connection.InvokeAsync("LeaveGroup", groupId);
    }

    public async Task DisconnectAsync()
    {
        if (_connection != null)
        {
            await _connection.DisposeAsync();
            _connection = null;
        }
    }

    public bool IsConnected => _connection?.State == HubConnectionState.Connected;
}
