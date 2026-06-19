using ChatSystem.Core.DTOs;
using ChatSystem.Core.Enums;
using ChatSystem.Core.Models;
using ChatSystem.Data.Repositories;
using ChatSystem.Server.Helpers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using ChatSystem.Server.Hubs;

namespace ChatSystem.Server.Controllers.Api;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class GroupsController : ControllerBase
{
    private readonly IGroupRepository _groupRepo;
    private readonly IUserRepository _userRepo;
    private readonly IHubContext<ChatHub> _hubContext;

    public GroupsController(IGroupRepository groupRepo, IUserRepository userRepo, IHubContext<ChatHub> hubContext)
    {
        _groupRepo = groupRepo;
        _userRepo = userRepo;
        _hubContext = hubContext;
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateGroupDTO dto)
    {
        var (userId, username, _, _) = JwtHelper.ParseToken(User);

        var group = new Group
        {
            Name = dto.Name,
            CreatorId = userId,
            CreatedAt = DateTime.Now
        };

        var created = await _groupRepo.CreateGroupAsync(group, dto.MemberIds);

        return Ok(ApiResponse<GroupDTO>.Ok(new GroupDTO
        {
            Id = created.Id,
            Name = created.Name,
            CreatorId = created.CreatorId,
            CreatorName = username,
            CreatedAt = created.CreatedAt
        }, "群组创建成功"));
    }

    [HttpGet]
    public async Task<IActionResult> GetMyGroups()
    {
        var (userId, _, _, _) = JwtHelper.ParseToken(User);
        var groups = await _groupRepo.GetUserGroupsAsync(userId);

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

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var group = await _groupRepo.GetByIdAsync(id);
        if (group == null)
            return Ok(ApiResponse.Fail("群组不存在"));

        return Ok(ApiResponse<GroupDTO>.Ok(new GroupDTO
        {
            Id = group.Id,
            Name = group.Name,
            CreatorId = group.CreatorId,
            CreatorName = group.Creator?.Username ?? "",
            MemberCount = group.Members.Count,
            CreatedAt = group.CreatedAt,
            Members = group.Members.Select(m => new GroupMemberDTO
            {
                UserId = m.UserId,
                Username = m.User?.Username ?? "",
                Nickname = m.User?.Nickname ?? "",
                JoinedAt = m.JoinedAt
            }).ToList()
        }));
    }

    [HttpPost("{id}/members")]
    public async Task<IActionResult> AddMember(int id, [FromBody] int userId)
    {
        var (adderId, adderName, adderNickname, _) = JwtHelper.ParseToken(User);
        await _groupRepo.AddMemberAsync(id, userId);

        // 广播通知群内所有成员
        var addedUser = await _userRepo.GetByIdAsync(userId);
        await _hubContext.Clients.Group($"group:{id}").SendAsync("GroupMemberAdded",
            id, userId, addedUser?.Username ?? "", addedUser?.Nickname ?? "");

        return Ok(ApiResponse.Ok("成员已添加"));
    }

    [HttpDelete("{id}/members/{userId}")]
    public async Task<IActionResult> RemoveMember(int id, int userId)
    {
        await _groupRepo.RemoveMemberAsync(id, userId);

        // 广播通知群内所有成员
        await _hubContext.Clients.Group($"group:{id}").SendAsync("GroupMemberRemoved", id, userId);

        return Ok(ApiResponse.Ok("成员已移除"));
    }

    [HttpGet("{id}/messages")]
    public async Task<IActionResult> GetMessages(int id, [FromQuery] int page = 1, [FromQuery] int pageSize = 50)
    {
        var messages = await _groupRepo.GetGroupMessagesAsync(id, page, pageSize);
        var dtos = messages.Select(m => new GroupMessageDTO
        {
            Id = m.Id,
            GroupId = m.GroupId,
            SenderId = m.SenderId,
            SenderName = m.Sender?.Username ?? "",
            SenderNickname = m.Sender?.Nickname ?? "",
            Content = m.Content,
            MessageType = m.MessageType,
            FileName = m.FileName,
            FilePath = m.FilePath,
            SentAt = m.SentAt
        }).ToList();

        return Ok(ApiResponse<List<GroupMessageDTO>>.Ok(dtos));
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteGroup(int id)
    {
        var (userId, _, _, _) = JwtHelper.ParseToken(User);
        var deleted = await _groupRepo.DeleteGroupAsync(id, userId);
        if (deleted)
        {
            // 广播通知群内所有成员群已被解散
            await _hubContext.Clients.Group($"group:{id}").SendAsync("GroupDissolved", id);
            return Ok(ApiResponse.Ok("群聊已解散"));
        }
        return Ok(ApiResponse.Fail("只有群主才能解散群聊"));
    }

    [HttpDelete("{groupId}/messages/{messageId}")]
    public async Task<IActionResult> DeleteGroupMessage(int groupId, long messageId)
    {
        var (userId, _, _, _) = JwtHelper.ParseToken(User);
        await _groupRepo.SoftDeleteGroupMessageAsync(messageId, userId);
        return Ok(ApiResponse.Ok("消息已删除"));
    }

    [HttpPost("file")]
    public async Task<IActionResult> UploadGroupFile([FromForm] int groupId, [FromForm] IFormFile file)
    {
        var (userId, username, nickname, _) = JwtHelper.ParseToken(User);

        var maxSize = 20 * 1024 * 1024;
        if (file.Length > maxSize)
            return Ok(ApiResponse.Fail("文件大小不能超过20MB"));

        var isMember = await _groupRepo.IsMemberAsync(groupId, userId);
        if (!isMember)
            return Ok(ApiResponse.Fail("你不是群成员"));

        var uploadDir = Path.Combine("wwwroot", "uploads");
        if (!Directory.Exists(uploadDir))
            Directory.CreateDirectory(uploadDir);

        var fileName = $"{Guid.NewGuid()}_{file.FileName}";
        var filePath = Path.Combine(uploadDir, fileName);

        using (var stream = new FileStream(filePath, FileMode.Create))
            await file.CopyToAsync(stream);

        var msg = new GroupMessage
        {
            GroupId = groupId,
            SenderId = userId,
            Content = $"[文件] {file.FileName}",
            MessageType = MessageType.File,
            FileName = file.FileName,
            FilePath = $"/uploads/{fileName}",
            SentAt = DateTime.Now
        };
        await _groupRepo.AddGroupMessageAsync(msg);

        // 通知群成员
        var groupMsgDto = new GroupMessageDTO
        {
            Id = msg.Id, GroupId = msg.GroupId, SenderId = msg.SenderId,
            SenderName = username, SenderNickname = nickname,
            Content = msg.Content, MessageType = msg.MessageType,
            FileName = msg.FileName, FilePath = msg.FilePath,
            SentAt = msg.SentAt
        };
        await _hubContext.Clients.Group($"group:{groupId}").SendAsync("ReceiveGroupMessage", groupMsgDto);

        return Ok(ApiResponse<GroupMessageDTO>.Ok(new GroupMessageDTO
        {
            Id = msg.Id,
            GroupId = msg.GroupId,
            SenderId = msg.SenderId,
            SenderName = username,
            SenderNickname = nickname,
            Content = msg.Content,
            MessageType = msg.MessageType,
            FileName = msg.FileName,
            FilePath = msg.FilePath,
            SentAt = msg.SentAt
        }, "文件上传成功"));
    }
}
