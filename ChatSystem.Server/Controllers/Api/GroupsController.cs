using ChatSystem.Core.DTOs;
using ChatSystem.Core.Enums;
using ChatSystem.Core.Models;
using ChatSystem.Data.Repositories;
using ChatSystem.Server.Helpers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ChatSystem.Server.Controllers.Api;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class GroupsController : ControllerBase
{
    private readonly IGroupRepository _groupRepo;

    public GroupsController(IGroupRepository groupRepo)
    {
        _groupRepo = groupRepo;
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateGroupDTO dto)
    {
        var (userId, username, _, _) = JwtHelper.ParseToken(User);

        var group = new Group
        {
            Name = dto.Name,
            CreatorId = userId,
            CreatedAt = DateTime.UtcNow
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
        await _groupRepo.AddMemberAsync(id, userId);
        return Ok(ApiResponse.Ok("成员已添加"));
    }

    [HttpDelete("{id}/members/{userId}")]
    public async Task<IActionResult> RemoveMember(int id, int userId)
    {
        await _groupRepo.RemoveMemberAsync(id, userId);
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
            SentAt = m.SentAt
        }).ToList();

        return Ok(ApiResponse<List<GroupMessageDTO>>.Ok(dtos));
    }
}
