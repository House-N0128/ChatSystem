using ChatSystem.Core.Enums;
using ChatSystem.Core.Models;
using Microsoft.EntityFrameworkCore;

namespace ChatSystem.Data.Repositories;

public class FriendRepository : IFriendRepository
{
    private readonly ChatSystemDbContext _db;

    public FriendRepository(ChatSystemDbContext db) => _db = db;

    public async Task<List<User>> GetFriendsAsync(int userId)
        => await _db.Friends
            .Where(f => f.UserId == userId)
            .Select(f => f.FriendUser!)
            .ToListAsync();

    public async Task<bool> AreFriendsAsync(int userId1, int userId2)
        => await _db.Friends.AnyAsync(f =>
            f.UserId == userId1 && f.FriendUserId == userId2);

    public async Task AddFriendAsync(int userId, int friendUserId)
    {
        // 双向插入
        var now = DateTime.UtcNow;
        _db.Friends.Add(new Friend { UserId = userId, FriendUserId = friendUserId, AddedAt = now });
        _db.Friends.Add(new Friend { UserId = friendUserId, FriendUserId = userId, AddedAt = now });
        await _db.SaveChangesAsync();
    }

    public async Task RemoveFriendAsync(int userId, int friendUserId)
    {
        var rows = await _db.Friends
            .Where(f => (f.UserId == userId && f.FriendUserId == friendUserId)
                     || (f.UserId == friendUserId && f.FriendUserId == userId))
            .ToListAsync();
        _db.Friends.RemoveRange(rows);
        await _db.SaveChangesAsync();
    }

    // --- Friend Requests ---

    public async Task<FriendRequest?> GetRequestByIdAsync(int requestId)
        => await _db.FriendRequests
            .Include(r => r.FromUser)
            .Include(r => r.ToUser)
            .FirstOrDefaultAsync(r => r.Id == requestId);

    public async Task<List<FriendRequest>> GetPendingRequestsAsync(int userId)
        => await _db.FriendRequests
            .Include(r => r.FromUser)
            .Where(r => r.ToUserId == userId && r.Status == FriendRequestStatus.Pending)
            .OrderByDescending(r => r.SentAt)
            .ToListAsync();

    public async Task<List<FriendRequest>> GetSentRequestsAsync(int userId)
        => await _db.FriendRequests
            .Include(r => r.ToUser)
            .Where(r => r.FromUserId == userId && r.Status == FriendRequestStatus.Pending)
            .OrderByDescending(r => r.SentAt)
            .ToListAsync();

    public async Task<bool> HasPendingRequestAsync(int fromUserId, int toUserId)
        => await _db.FriendRequests.AnyAsync(r =>
            r.FromUserId == fromUserId && r.ToUserId == toUserId && r.Status == FriendRequestStatus.Pending);

    public async Task AddFriendRequestAsync(FriendRequest request)
    {
        _db.FriendRequests.Add(request);
        await _db.SaveChangesAsync();
    }

    public async Task AcceptRequestAsync(int requestId)
    {
        var request = await _db.FriendRequests.FindAsync(requestId);
        if (request == null || request.Status != FriendRequestStatus.Pending) return;

        request.Status = FriendRequestStatus.Accepted;
        request.RespondedAt = DateTime.UtcNow;
        _db.FriendRequests.Update(request);

        // 建立双向好友关系
        _db.Friends.Add(new Friend { UserId = request.FromUserId, FriendUserId = request.ToUserId, AddedAt = DateTime.UtcNow });
        _db.Friends.Add(new Friend { UserId = request.ToUserId, FriendUserId = request.FromUserId, AddedAt = DateTime.UtcNow });

        await _db.SaveChangesAsync();
    }

    public async Task RejectRequestAsync(int requestId)
    {
        var request = await _db.FriendRequests.FindAsync(requestId);
        if (request == null || request.Status != FriendRequestStatus.Pending) return;

        request.Status = FriendRequestStatus.Rejected;
        request.RespondedAt = DateTime.UtcNow;
        _db.FriendRequests.Update(request);
        await _db.SaveChangesAsync();
    }

    public async Task SaveChangesAsync()
        => await _db.SaveChangesAsync();
}
