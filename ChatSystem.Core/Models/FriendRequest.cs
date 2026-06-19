using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using ChatSystem.Core.Enums;

namespace ChatSystem.Core.Models;

public class FriendRequest
{
    [Key]
    public int Id { get; set; }

    [Required]
    public int FromUserId { get; set; }

    [Required]
    public int ToUserId { get; set; }

    public FriendRequestStatus Status { get; set; } = FriendRequestStatus.Pending;

    public DateTime SentAt { get; set; } = DateTime.Now;

    public DateTime? RespondedAt { get; set; }

    // Navigation
    [ForeignKey(nameof(FromUserId))]
    public User? FromUser { get; set; }

    [ForeignKey(nameof(ToUserId))]
    public User? ToUser { get; set; }
}
