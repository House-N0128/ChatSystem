using ChatSystem.Core.DTOs;
using ChatSystem.Core.Enums;
using ChatSystem.Data.Repositories;
using ChatSystem.Server.Helpers;
using ChatSystem.Server.Hubs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;

namespace ChatSystem.Server.Controllers.Api;

[ApiController]
[Route("api/[controller]")]
[Authorize(Policy = "AdminOnly")]
public class AdminController : ControllerBase
{
    private readonly IUserRepository _userRepo;
    private readonly IMessageRepository _msgRepo;
    private readonly IGroupRepository _groupRepo;
    private readonly IHubContext<ChatHub> _hubContext;

    public AdminController(IUserRepository userRepo, IMessageRepository msgRepo,
        IGroupRepository groupRepo, IHubContext<ChatHub> hubContext)
    {
        _userRepo = userRepo;
        _msgRepo = msgRepo;
        _groupRepo = groupRepo;
        _hubContext = hubContext;
    }

    [HttpGet("users/pending")]
    public async Task<IActionResult> GetPendingUsers()
    {
        var users = await _userRepo.GetPendingUsersAsync();
        var dtos = users.Select(u => new UserDTO
        {
            Id = u.Id,
            Username = u.Username,
            Nickname = u.Nickname,
            Role = u.Role,
            Status = u.Status,
            CreatedAt = u.CreatedAt
        }).ToList();

        return Ok(ApiResponse<List<UserDTO>>.Ok(dtos));
    }

    [HttpPost("users/{id}/approve")]
    public async Task<IActionResult> ApproveUser(int id)
    {
        var user = await _userRepo.GetByIdAsync(id);
        if (user == null)
            return Ok(ApiResponse.Fail("用户不存在"));

        user.Status = UserStatus.Active;
        _userRepo.Update(user);
        await _userRepo.SaveChangesAsync();

        return Ok(ApiResponse.Ok("用户已审核通过"));
    }

    [HttpPost("users/{id}/reject")]
    public async Task<IActionResult> RejectUser(int id)
    {
        var user = await _userRepo.GetByIdAsync(id);
        if (user == null)
            return Ok(ApiResponse.Fail("用户不存在"));
        if (user.Status != UserStatus.Pending)
            return Ok(ApiResponse.Fail("只能拒绝待审核用户"));

        _userRepo.Delete(user);
        await _userRepo.SaveChangesAsync();

        return Ok(ApiResponse.Ok("用户已拒绝"));
    }

    [HttpPost("users/{id}/ban")]
    public async Task<IActionResult> BanUser(int id)
    {
        var user = await _userRepo.GetByIdAsync(id);
        if (user == null)
            return Ok(ApiResponse.Fail("用户不存在"));
        if (user.Role == UserRole.Admin)
            return Ok(ApiResponse.Fail("不能封禁管理员"));

        user.Status = UserStatus.Banned;
        _userRepo.Update(user);
        await _userRepo.SaveChangesAsync();

        // 通知被封禁用户（如果在线）
        await _hubContext.Clients.Group($"user:{id}")
            .SendAsync("UserBanned", "您的账号已被管理员封禁");

        return Ok(ApiResponse.Ok("用户已被封禁"));
    }

    [HttpPost("users/{id}/unban")]
    public async Task<IActionResult> UnbanUser(int id)
    {
        var user = await _userRepo.GetByIdAsync(id);
        if (user == null)
            return Ok(ApiResponse.Fail("用户不存在"));

        user.Status = UserStatus.Active;
        _userRepo.Update(user);
        await _userRepo.SaveChangesAsync();

        return Ok(ApiResponse.Ok("用户已解禁"));
    }

    [HttpGet("groups")]
    public async Task<IActionResult> GetAllGroups()
    {
        var groups = await _groupRepo.GetAllGroupsAsync();
        var dtos = groups.Select(g => new GroupDTO
        {
            Id = g.Id,
            Name = g.Name,
            CreatorId = g.CreatorId,
            CreatorName = g.Creator?.Username ?? "",
            MemberCount = g.Members.Count,
            CreatedAt = g.CreatedAt
        }).ToList();

        return Ok(ApiResponse<List<GroupDTO>>.Ok(dtos));
    }

    [HttpGet("users/{userId}/messages")]
    public async Task<IActionResult> GetUserMessages(int userId, [FromQuery] int page = 1, [FromQuery] int pageSize = 50)
    {
        var result = await _msgRepo.GetUserSentMessagesAsync(userId, page, pageSize);
        var dtos = result.Items.Select(m => new MessageDTO
        {
            Id = m.Id,
            SenderId = m.SenderId,
            SenderName = m.Sender?.Username ?? "",
            SenderNickname = m.Sender?.Nickname ?? "",
            ReceiverId = m.ReceiverId,
            ReceiverName = m.Receiver?.Username ?? "",
            ReceiverNickname = m.Receiver?.Nickname ?? "",
            Content = m.Content,
            MessageType = m.MessageType,
            SentAt = m.SentAt
        }).ToList();

        return Ok(ApiResponse<PagedResult<MessageDTO>>.Ok(new PagedResult<MessageDTO>
        {
            TotalCount = result.TotalCount,
            Page = result.Page,
            PageSize = result.PageSize,
            Items = dtos
        }));
    }

    [HttpGet("users")]
    public async Task<IActionResult> GetAllUsers([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        var users = await _userRepo.GetAllUsersAsync(page, pageSize);
        var total = await _userRepo.GetTotalUserCountAsync();
        var dtos = users.Select(u => new UserDTO
        {
            Id = u.Id,
            Username = u.Username,
            Nickname = u.Nickname,
            Role = u.Role,
            Status = u.Status,
            CreatedAt = u.CreatedAt,
            LastLoginAt = u.LastLoginAt
        }).ToList();

        return Ok(ApiResponse<PagedResult<UserDTO>>.Ok(new PagedResult<UserDTO>
        {
            TotalCount = total,
            Page = page,
            PageSize = pageSize,
            Items = dtos
        }));
    }

    [HttpGet("messages")]
    public async Task<IActionResult> SearchMessages(
        [FromQuery] string? keyword = null,
        [FromQuery] DateTime? from = null,
        [FromQuery] DateTime? to = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var messages = await _msgRepo.SearchAllMessagesAsync(keyword, from, to, page, pageSize);
        var total = await _msgRepo.SearchAllMessagesCountAsync(keyword, from, to);

        var dtos = messages.Select(m => new MessageDTO
        {
            Id = m.Id,
            SenderId = m.SenderId,
            SenderName = m.Sender?.Username ?? "",
            SenderNickname = m.Sender?.Nickname ?? "",
            ReceiverId = m.ReceiverId,
            ReceiverName = m.Receiver?.Username ?? "",
            ReceiverNickname = m.Receiver?.Nickname ?? "",
            Content = m.Content,
            MessageType = m.MessageType,
            SentAt = m.SentAt
        }).ToList();

        return Ok(ApiResponse<PagedResult<MessageDTO>>.Ok(new PagedResult<MessageDTO>
        {
            TotalCount = total,
            Page = page,
            PageSize = pageSize,
            Items = dtos
        }));
    }

    [HttpDelete("messages/{id}")]
    public async Task<IActionResult> ForceDeleteMessage(long id)
    {
        await _msgRepo.ForceDeleteMessageAsync(id);

        await _hubContext.Clients.All.SendAsync("MessageDeleted", id);

        return Ok(ApiResponse.Ok("消息已强制删除"));
    }
}
