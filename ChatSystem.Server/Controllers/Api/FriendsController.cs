using ChatSystem.Core.DTOs;
using ChatSystem.Core.Enums;
using ChatSystem.Core.Models;
using ChatSystem.Data.Repositories;
using ChatSystem.Server.Helpers;
using ChatSystem.Server.Hubs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;

namespace ChatSystem.Server.Controllers.Api;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class FriendsController : ControllerBase
{
    private readonly IFriendRepository _friendRepo;
    private readonly IUserRepository _userRepo;
    private readonly IHubContext<ChatHub> _hubContext;

    public FriendsController(IFriendRepository friendRepo, IUserRepository userRepo,
        IHubContext<ChatHub> hubContext)
    {
        _friendRepo = friendRepo;
        _userRepo = userRepo;
        _hubContext = hubContext;
    }

    [HttpGet]
    public async Task<IActionResult> GetFriends()
    {
        var (userId, _, _, _) = JwtHelper.ParseToken(User);
        var friends = await _friendRepo.GetFriendsAsync(userId);
        var dtos = friends.Select(f => new FriendDTO
        {
            FriendUserId = f.Id,
            FriendUsername = f.Username,
            FriendNickname = f.Nickname,
            IsOnline = ChatHub.IsOnline(f.Id)
        }).ToList();

        return Ok(ApiResponse<List<FriendDTO>>.Ok(dtos));
    }

    [HttpPost("request")]
    public async Task<IActionResult> SendRequest([FromBody] SendFriendRequestDTO dto)
    {
        var (userId, username, nickname, _) = JwtHelper.ParseToken(User);

        if (userId == dto.ToUserId)
            return Ok(ApiResponse.Fail("不能添加自己为好友"));

        var areFriends = await _friendRepo.AreFriendsAsync(userId, dto.ToUserId);
        if (areFriends)
            return Ok(ApiResponse.Fail("已经是好友了"));

        var hasPending = await _friendRepo.HasPendingRequestAsync(userId, dto.ToUserId);
        if (hasPending)
            return Ok(ApiResponse.Fail("已发送过好友申请，请等待对方处理"));

        var request = new FriendRequest
        {
            FromUserId = userId,
            ToUserId = dto.ToUserId,
            Status = FriendRequestStatus.Pending,
            SentAt = DateTime.UtcNow
        };
        await _friendRepo.AddFriendRequestAsync(request);

        // 通知对方有新好友申请
        var requestDto = new FriendRequestDTO
        {
            Id = request.Id,
            FromUserId = userId,
            FromUsername = username,
            FromNickname = nickname,
            ToUserId = dto.ToUserId,
            Status = FriendRequestStatus.Pending,
            SentAt = request.SentAt
        };
        await _hubContext.Clients.Group($"user:{dto.ToUserId}")
            .SendAsync("FriendRequestReceived", requestDto);

        return Ok(ApiResponse.Ok("好友申请已发送"));
    }

    [HttpGet("requests/pending")]
    public async Task<IActionResult> GetPendingRequests()
    {
        var (userId, _, _, _) = JwtHelper.ParseToken(User);
        var requests = await _friendRepo.GetPendingRequestsAsync(userId);
        var dtos = requests.Select(r => new FriendRequestDTO
        {
            Id = r.Id,
            FromUserId = r.FromUserId,
            FromUsername = r.FromUser?.Username ?? "",
            FromNickname = r.FromUser?.Nickname ?? "",
            ToUserId = r.ToUserId,
            Status = r.Status,
            SentAt = r.SentAt
        }).ToList();

        return Ok(ApiResponse<List<FriendRequestDTO>>.Ok(dtos));
    }

    [HttpGet("requests/sent")]
    public async Task<IActionResult> GetSentRequests()
    {
        var (userId, _, _, _) = JwtHelper.ParseToken(User);
        var requests = await _friendRepo.GetSentRequestsAsync(userId);
        var dtos = requests.Select(r => new FriendRequestDTO
        {
            Id = r.Id,
            FromUserId = r.FromUserId,
            ToUserId = r.ToUserId,
            ToUsername = r.ToUser?.Username ?? "",
            ToNickname = r.ToUser?.Nickname ?? "",
            Status = r.Status,
            SentAt = r.SentAt
        }).ToList();

        return Ok(ApiResponse<List<FriendRequestDTO>>.Ok(dtos));
    }

    [HttpPost("requests/{id}/accept")]
    public async Task<IActionResult> AcceptRequest(int id)
    {
        var (userId, _, nickname, _) = JwtHelper.ParseToken(User);
        var request = await _friendRepo.GetRequestByIdAsync(id);

        if (request == null || request.ToUserId != userId)
            return Ok(ApiResponse.Fail("申请不存在"));
        if (request.Status != FriendRequestStatus.Pending)
            return Ok(ApiResponse.Fail("申请已处理"));

        await _friendRepo.AcceptRequestAsync(id);

        // 通知对方好友已添加
        var friendDto = new FriendDTO
        {
            FriendUserId = userId,
            FriendUsername = User.FindFirst(System.Security.Claims.ClaimTypes.Name)!.Value,
            FriendNickname = nickname,
            IsOnline = true
        };
        await _hubContext.Clients.Group($"user:{request.FromUserId}")
            .SendAsync("FriendAdded", friendDto);

        return Ok(ApiResponse.Ok("已同意好友申请"));
    }

    [HttpPost("requests/{id}/reject")]
    public async Task<IActionResult> RejectRequest(int id)
    {
        var (userId, _, _, _) = JwtHelper.ParseToken(User);
        var request = await _friendRepo.GetRequestByIdAsync(id);

        if (request == null || request.ToUserId != userId)
            return Ok(ApiResponse.Fail("申请不存在"));

        await _friendRepo.RejectRequestAsync(id);
        return Ok(ApiResponse.Ok("已拒绝好友申请"));
    }

    [HttpDelete("{friendId}")]
    public async Task<IActionResult> RemoveFriend(int friendId)
    {
        var (userId, _, _, _) = JwtHelper.ParseToken(User);
        var areFriends = await _friendRepo.AreFriendsAsync(userId, friendId);
        if (!areFriends)
            return Ok(ApiResponse.Fail("不是好友关系"));

        await _friendRepo.RemoveFriendAsync(userId, friendId);

        await _hubContext.Clients.Group($"user:{friendId}")
            .SendAsync("FriendRemoved", userId);

        return Ok(ApiResponse.Ok("已删除好友"));
    }
}
