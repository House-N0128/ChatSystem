using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using ChatSystem.Core.Enums;

namespace ChatSystem.Core.Models;

public class PrivateMessage
{
    [Key]
    public long Id { get; set; }

    [Required]
    public int SenderId { get; set; }

    [Required]
    public int ReceiverId { get; set; }

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
    [ForeignKey(nameof(SenderId))]
    public User? Sender { get; set; }

    [ForeignKey(nameof(ReceiverId))]
    public User? Receiver { get; set; }
}
