using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ChatSystem.Core.Models;

public class Friend
{
    [Key]
    public int Id { get; set; }

    [Required]
    public int UserId { get; set; }

    [Required]
    public int FriendUserId { get; set; }

    public DateTime AddedAt { get; set; } = DateTime.Now;

    // Navigation
    [ForeignKey(nameof(UserId))]
    public User? User { get; set; }

    [ForeignKey(nameof(FriendUserId))]
    public User? FriendUser { get; set; }
}
