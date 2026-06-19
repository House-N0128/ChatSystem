using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ChatSystem.Core.Models;

public class GroupMember
{
    [Key]
    public int Id { get; set; }

    [Required]
    public int GroupId { get; set; }

    [Required]
    public int UserId { get; set; }

    public DateTime JoinedAt { get; set; } = DateTime.Now;

    // Navigation
    [ForeignKey(nameof(GroupId))]
    public Group? Group { get; set; }

    [ForeignKey(nameof(UserId))]
    public User? User { get; set; }
}
