using ChatSystem.Core.Enums;

namespace ChatSystem.Core.DTOs;

public class FriendDTO
{
    public int FriendUserId { get; set; }
    public string FriendUsername { get; set; } = string.Empty;
    public string FriendNickname { get; set; } = string.Empty;
    public DateTime AddedAt { get; set; }
    public bool IsOnline { get; set; }
}

public class FriendRequestDTO
{
    public int Id { get; set; }
    public int FromUserId { get; set; }
    public string FromUsername { get; set; } = string.Empty;
    public string FromNickname { get; set; } = string.Empty;
    public int ToUserId { get; set; }
    public string ToUsername { get; set; } = string.Empty;
    public string ToNickname { get; set; } = string.Empty;
    public FriendRequestStatus Status { get; set; }
    public DateTime SentAt { get; set; }
}

public class SendFriendRequestDTO
{
    public int ToUserId { get; set; }
}
