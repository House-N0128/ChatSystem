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
            ReceiverName = m.Receiver?.Username ?? "",
            ReceiverNickname = m.Receiver?.Nickname ?? "",
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
    public async Task<IActionResult> UploadFile([FromForm] int receiverId, [FromForm] IFormFile file)
    {
        var (userId, username, nickname, _) = JwtHelper.ParseToken(User);

        var maxSize = 20 * 1024 * 1024; // 20MB
        if (file.Length > maxSize)
            return Ok(ApiResponse.Fail("文件大小不能超过20MB"));

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
            SentAt = DateTime.Now
        };
        await _msgRepo.AddPrivateMessageAsync(msg);

        var dto = new MessageDTO
        {
            Id = msg.Id,
            SenderId = msg.SenderId,
            SenderName = username,
            SenderNickname = nickname,
            ReceiverId = msg.ReceiverId,
            Content = msg.Content,
            MessageType = msg.MessageType,
            FileName = msg.FileName,
            FilePath = msg.FilePath,
            SentAt = msg.SentAt
        };

        // 通知接收方
        await _hubContext.Clients.Group($"user:{receiverId}").SendAsync("ReceivePrivateMessage", dto);
        // 通知发送方自己（多端同步）
        await _hubContext.Clients.Group($"user:{userId}").SendAsync("ReceivePrivateMessage", dto);

        return Ok(ApiResponse<MessageDTO>.Ok(dto, "文件上传成功"));
    }
}
