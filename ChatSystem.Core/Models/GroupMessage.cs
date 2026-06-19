using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using ChatSystem.Core.Enums;

namespace ChatSystem.Core.Models;

public class GroupMessage
{
    [Key]
    public long Id { get; set; }

    [Required]
    public int GroupId { get; set; }

    [Required]
    public int SenderId { get; set; }

    [Required]
    public string Content { get; set; } = string.Empty;

    public MessageType MessageType { get; set; } = MessageType.Text;

    [MaxLength(256)]
    public string? FileName { get; set; }

    [MaxLength(512)]
    public string? FilePath { get; set; }

    public bool IsDeleted { get; set; } = false;

    public DateTime SentAt { get; set; } = DateTime.Now;

    // Navigation
    [ForeignKey(nameof(GroupId))]
    public Group? Group { get; set; }

    [ForeignKey(nameof(SenderId))]
    public User? Sender { get; set; }
}
