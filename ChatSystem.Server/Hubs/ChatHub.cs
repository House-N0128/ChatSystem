using System.Collections.Concurrent;
using System.Security.Claims;
using ChatSystem.Core.DTOs;
using ChatSystem.Core.Enums;
using ChatSystem.Core.Models;
using ChatSystem.Data.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace ChatSystem.Server.Hubs;

[Authorize]
public class ChatHub : Hub
{
    // 在线用户追踪: userId -> set of connectionIds
    private static readonly ConcurrentDictionary<int, HashSet<string>> _onlineUsers = new();

    private readonly IUserRepository _userRepo;
    private readonly IMessageRepository _msgRepo;
    private readonly IGroupRepository _groupRepo;
    private readonly IFriendRepository _friendRepo;

    public ChatHub(IUserRepository userRepo, IMessageRepository msgRepo,
        IGroupRepository groupRepo, IFriendRepository friendRepo)
    {
        _userRepo = userRepo;
        _msgRepo = msgRepo;
        _groupRepo = groupRepo;
        _friendRepo = friendRepo;
    }

    private int UserId => int.Parse(Context.User!.FindFirst(ClaimTypes.NameIdentifier)!.Value);
    private string Username => Context.User!.FindFirst(ClaimTypes.Name)!.Value;
    private string Nickname => Context.User!.FindFirst("nickname")?.Value ?? Username;

    public static bool IsOnline(int userId) => _onlineUsers.ContainsKey(userId);

    public override async Task OnConnectedAsync()
    {
        var userId = UserId;
        var connections = _onlineUsers.GetOrAdd(userId, _ => new HashSet<string>());
        lock (connections)
        {
            connections.Add(Context.ConnectionId);
        }

        await Groups.AddToGroupAsync(Context.ConnectionId, $"user:{userId}");

        // 加入所有已加入的群组
        var groups = await _groupRepo.GetUserGroupsAsync(userId);
        foreach (var g in groups)
            await Groups.AddToGroupAsync(Context.ConnectionId, $"group:{g.Id}");

        // 通知好友上线
        var friends = await _friendRepo.GetFriendsAsync(userId);
        var userDto = new UserDTO
        {
            Id = userId, Username = Username, Nickname = Nickname
        };
        foreach (var friend in friends)
            await Clients.Group($"user:{friend.Id}").SendAsync("UserOnline", userDto);

        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var userId = UserId;
        if (_onlineUsers.TryGetValue(userId, out var connections))
        {
            lock (connections)
            {
                connections.Remove(Context.ConnectionId);
                if (connections.Count == 0)
                    _onlineUsers.TryRemove(userId, out _);
            }
        }

        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"user:{userId}");

        // 如果该用户的所有连接都断开，通知好友下线
        if (!_onlineUsers.ContainsKey(userId))
        {
            var friends = await _friendRepo.GetFriendsAsync(userId);
            foreach (var friend in friends)
                await Clients.Group($"user:{friend.Id}").SendAsync("UserOffline", userId);
        }

        await base.OnDisconnectedAsync(exception);
    }

    // ===== 私聊 =====

    public async Task SendPrivateMessage(int receiverId, string content)
    {
        var senderId = UserId;

        // 检查是否为好友
        var areFriends = await _friendRepo.AreFriendsAsync(senderId, receiverId);
        if (!areFriends) return;

        var msg = new PrivateMessage
        {
            SenderId = senderId,
            ReceiverId = receiverId,
            Content = content,
            MessageType = MessageType.Text,
            SentAt = DateTime.Now
        };
        await _msgRepo.AddPrivateMessageAsync(msg);

        var dto = new MessageDTO
        {
            Id = msg.Id,
            SenderId = senderId,
            SenderName = Username,
            SenderNickname = Nickname,
            ReceiverId = receiverId,
            Content = content,
            MessageType = MessageType.Text,
            SentAt = msg.SentAt
        };

        // 发送给接收者
        await Clients.Group($"user:{receiverId}").SendAsync("ReceivePrivateMessage", dto);
        // 也发送给发送者自己（多端同步）
        await Clients.Group($"user:{senderId}").SendAsync("ReceivePrivateMessage", dto);
    }

    // ===== 群聊 =====

    public async Task SendGroupMessage(int groupId, string content)
    {
        var senderId = UserId;

        var isMember = await _groupRepo.IsMemberAsync(groupId, senderId);
        if (!isMember) return;

        var msg = new GroupMessage
        {
            GroupId = groupId,
            SenderId = senderId,
            Content = content,
            MessageType = MessageType.Text,
            SentAt = DateTime.Now
        };
        await _groupRepo.AddGroupMessageAsync(msg);

        var dto = new GroupMessageDTO
        {
            Id = msg.Id,
            GroupId = groupId,
            SenderId = senderId,
            SenderName = Username,
            SenderNickname = Nickname,
            Content = content,
            MessageType = MessageType.Text,
            SentAt = msg.SentAt
        };

        await Clients.Group($"group:{groupId}").SendAsync("ReceiveGroupMessage", dto);
    }

    // ===== 打字状态 =====

    public async Task NotifyTyping(int receiverId)
    {
        await Clients.Group($"user:{receiverId}").SendAsync("UserTyping", UserId);
    }

    public async Task NotifyStopTyping(int receiverId)
    {
        await Clients.Group($"user:{receiverId}").SendAsync("UserStopTyping", UserId);
    }

    // ===== 群组加入/离开 =====

    public async Task JoinGroup(int groupId)
    {
        var isMember = await _groupRepo.IsMemberAsync(groupId, UserId);
        if (isMember)
            await Groups.AddToGroupAsync(Context.ConnectionId, $"group:{groupId}");
    }

    public async Task LeaveGroup(int groupId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"group:{groupId}");
    }
}
