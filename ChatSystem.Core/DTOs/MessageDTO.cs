using ChatSystem.Core.Enums;

namespace ChatSystem.Core.DTOs;

public class MessageDTO
{
    public long Id { get; set; }
    public int SenderId { get; set; }
    public string SenderName { get; set; } = string.Empty;
    public string SenderNickname { get; set; } = string.Empty;
    public int ReceiverId { get; set; }
    public string ReceiverName { get; set; } = string.Empty;
    public string ReceiverNickname { get; set; } = string.Empty;
    public string GroupName { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public MessageType MessageType { get; set; }
    public string? FileName { get; set; }
    public string? FilePath { get; set; }
    public DateTime SentAt { get; set; }
}
