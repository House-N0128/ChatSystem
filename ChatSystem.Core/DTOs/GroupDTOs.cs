using ChatSystem.Core.Enums;

namespace ChatSystem.Core.DTOs;

public class GroupDTO
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int CreatorId { get; set; }
    public string CreatorName { get; set; } = string.Empty;
    public int MemberCount { get; set; }
    public DateTime CreatedAt { get; set; }
    public List<GroupMemberDTO> Members { get; set; } = new();
}

public class GroupMemberDTO
{
    public int UserId { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Nickname { get; set; } = string.Empty;
    public DateTime JoinedAt { get; set; }
}

public class GroupMessageDTO
{
    public long Id { get; set; }
    public int GroupId { get; set; }
    public int SenderId { get; set; }
    public string SenderName { get; set; } = string.Empty;
    public string SenderNickname { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public MessageType MessageType { get; set; }
    public DateTime SentAt { get; set; }
}

public class CreateGroupDTO
{
    public string Name { get; set; } = string.Empty;
    public List<int> MemberIds { get; set; } = new();
}
