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
public class MessagesController : ControllerBase
{
    private readonly IMessageRepository _msgRepo;
    private readonly IFriendRepository _friendRepo;
    private readonly IHubContext<ChatHub> _hubContext;

    public MessagesController(IMessageRepository msgRepo, IFriendRepository friendRepo,
        IHubContext<ChatHub> hubContext)
    {
        _msgRepo = msgRepo;
        _friendRepo = friendRepo;
        _hubContext = hubContext;
    }

    [HttpGet("private/{userId}")]
    public async Task<IActionResult> GetPrivateMessages(
        int userId, [FromQuery] int page = 1, [FromQuery] int pageSize = 50)
    {
        var (currentUserId, _, _, _) = JwtHelper.ParseToken(User);
        var result = await _msgRepo.GetPrivateMessagesAsync(currentUserId, userId, page, pageSize);

        var dtos = result.Items.Select(m => new MessageDTO
        {
            Id = m.Id,
            SenderId = m.SenderId,
            SenderName = m.Sender?.Username ?? "",
            SenderNickname = m.Sender?.Nickname ?? "",
            ReceiverId = m.ReceiverId,
            Content = m.Content,
            MessageType = m.MessageType,
            FileName = m.FileName,
            FilePath = m.FilePath,
            SentAt = m.SentAt
        }).ToList();

        var paged = new PagedResult<MessageDTO>
        {
            TotalCount = result.TotalCount,
            Page = result.Page,
            PageSize = result.PageSize,
            Items = dtos
        };

        return Ok(ApiResponse<PagedResult<MessageDTO>>.Ok(paged));
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteMessage(long id)
    {
        var (userId, _, _, _) = JwtHelper.ParseToken(User);
        await _msgRepo.SoftDeleteMessageAsync(id, userId);
        return Ok(ApiResponse.Ok("消息已删除"));
    }

    [HttpPost("file")]
    public async Task<IActionResult> UploadFile(int receiverId, IFormFile file)
    {
        var (userId, _, _, _) = JwtHelper.ParseToken(User);

        var maxSize = 10 * 1024 * 1024; // 10MB
        if (file.Length > maxSize)
            return Ok(ApiResponse.Fail("文件大小不能超过10MB"));

        var uploadDir = Path.Combine("wwwroot", "uploads");
        if (!Directory.Exists(uploadDir))
            Directory.CreateDirectory(uploadDir);

        var fileName = $"{Guid.NewGuid()}_{file.FileName}";
        var filePath = Path.Combine(uploadDir, fileName);

        using (var stream = new FileStream(filePath, FileMode.Create))
        {
            await file.CopyToAsync(stream);
        }

        var msg = new PrivateMessage
        {
            SenderId = userId,
            ReceiverId = receiverId,
            Content = $"[文件] {file.FileName}",
            MessageType = MessageType.File,
            FileName = file.FileName,
            FilePath = $"/uploads/{fileName}",
            SentAt = DateTime.UtcNow
        };
        await _msgRepo.AddPrivateMessageAsync(msg);

        return Ok(ApiResponse<MessageDTO>.Ok(new MessageDTO
        {
            Id = msg.Id,
            SenderId = msg.SenderId,
            ReceiverId = msg.ReceiverId,
            Content = msg.Content,
            MessageType = msg.MessageType,
            FileName = msg.FileName,
            FilePath = msg.FilePath,
            SentAt = msg.SentAt
        }, "文件上传成功"));
    }
}
