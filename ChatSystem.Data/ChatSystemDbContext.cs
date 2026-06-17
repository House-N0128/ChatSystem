using ChatSystem.Core.Enums;
using ChatSystem.Core.Models;
using Microsoft.EntityFrameworkCore;

namespace ChatSystem.Data;

public class ChatSystemDbContext : DbContext
{
    public ChatSystemDbContext(DbContextOptions<ChatSystemDbContext> options)
        : base(options) { }

    public DbSet<User> Users => Set<User>();
    public DbSet<Friend> Friends => Set<Friend>();
    public DbSet<FriendRequest> FriendRequests => Set<FriendRequest>();
    public DbSet<PrivateMessage> PrivateMessages => Set<PrivateMessage>();
    public DbSet<Group> Groups => Set<Group>();
    public DbSet<GroupMember> GroupMembers => Set<GroupMember>();
    public DbSet<GroupMessage> GroupMessages => Set<GroupMessage>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // === User ===
        modelBuilder.Entity<User>(e =>
        {
            e.HasIndex(u => u.Username).IsUnique();
            e.Property(u => u.Role).HasDefaultValue(UserRole.User);
            e.Property(u => u.Status).HasDefaultValue(UserStatus.Pending);
            e.HasMany(u => u.Friends)
                .WithOne(f => f.User)
                .HasForeignKey(f => f.UserId)
                .OnDelete(DeleteBehavior.Cascade);
            e.HasMany(u => u.SentFriendRequests)
                .WithOne(r => r.FromUser)
                .HasForeignKey(r => r.FromUserId)
                .OnDelete(DeleteBehavior.Cascade);
            e.HasMany(u => u.ReceivedFriendRequests)
                .WithOne(r => r.ToUser)
                .HasForeignKey(r => r.ToUserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // === Friend ===
        modelBuilder.Entity<Friend>(e =>
        {
            e.HasIndex(f => new { f.UserId, f.FriendUserId }).IsUnique();
            e.HasOne(f => f.FriendUser)
                .WithMany()
                .HasForeignKey(f => f.FriendUserId)
                .OnDelete(DeleteBehavior.NoAction);
        });

        // === FriendRequest ===
        modelBuilder.Entity<FriendRequest>(e =>
        {
            e.HasIndex(r => new { r.FromUserId, r.ToUserId });
            e.HasIndex(r => new { r.ToUserId, r.Status });
        });

        // === PrivateMessage ===
        modelBuilder.Entity<PrivateMessage>(e =>
        {
            e.HasIndex(m => new { m.SenderId, m.ReceiverId, m.SentAt });
            e.HasOne(m => m.Sender)
                .WithMany()
                .HasForeignKey(m => m.SenderId)
                .OnDelete(DeleteBehavior.NoAction);
            e.HasOne(m => m.Receiver)
                .WithMany()
                .HasForeignKey(m => m.ReceiverId)
                .OnDelete(DeleteBehavior.NoAction);
        });

        // === Group ===
        modelBuilder.Entity<Group>(e =>
        {
            e.HasOne(g => g.Creator)
                .WithMany()
                .HasForeignKey(g => g.CreatorId)
                .OnDelete(DeleteBehavior.NoAction);
        });

        // === GroupMember ===
        modelBuilder.Entity<GroupMember>(e =>
        {
            e.HasIndex(gm => new { gm.GroupId, gm.UserId }).IsUnique();
            e.HasOne(gm => gm.Group)
                .WithMany(g => g.Members)
                .HasForeignKey(gm => gm.GroupId);
            e.HasOne(gm => gm.User)
                .WithMany()
                .HasForeignKey(gm => gm.UserId)
                .OnDelete(DeleteBehavior.NoAction);
        });

        // === GroupMessage ===
        modelBuilder.Entity<GroupMessage>(e =>
        {
            e.HasIndex(m => new { m.GroupId, m.SentAt });
            e.HasOne(m => m.Group)
                .WithMany(g => g.Messages)
                .HasForeignKey(m => m.GroupId);
            e.HasOne(m => m.Sender)
                .WithMany()
                .HasForeignKey(m => m.SenderId)
                .OnDelete(DeleteBehavior.NoAction);
        });
    }
}
