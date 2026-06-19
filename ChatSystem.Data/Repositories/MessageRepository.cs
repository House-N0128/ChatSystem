using ChatSystem.Core.DTOs;
using ChatSystem.Core.Models;
using Microsoft.EntityFrameworkCore;

namespace ChatSystem.Data.Repositories;

public class MessageRepository : IMessageRepository
{
    private readonly ChatSystemDbContext _db;

    public MessageRepository(ChatSystemDbContext db) => _db = db;

    public async Task<PrivateMessage> AddPrivateMessageAsync(PrivateMessage message)
    {
        _db.PrivateMessages.Add(message);
        await _db.SaveChangesAsync();
        return message;
    }

    public async Task<PagedResult<PrivateMessage>> GetPrivateMessagesAsync(
        int userId1, int userId2, int page, int pageSize)
    {
        var query = _db.PrivateMessages
            .Include(m => m.Sender)
            .Include(m => m.Receiver)
            .Where(m => !m.IsDeleted
                && ((m.SenderId == userId1 && m.ReceiverId == userId2)
                 || (m.SenderId == userId2 && m.ReceiverId == userId1)))
            .OrderByDescending(m => m.SentAt);

        var total = await query.CountAsync();
        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .OrderBy(m => m.SentAt)
            .ToListAsync();

        return new PagedResult<PrivateMessage>
        {
            TotalCount = total,
            Page = page,
            PageSize = pageSize,
            Items = items
        };
    }

    public async Task<PagedResult<PrivateMessage>> GetUserAllMessagesAsync(int userId, int page, int pageSize)
    {
        var query = _db.PrivateMessages
            .Include(m => m.Sender)
            .Include(m => m.Receiver)
            .Where(m => !m.IsDeleted
                && (m.SenderId == userId || m.ReceiverId == userId))
            .OrderByDescending(m => m.SentAt);

        var total = await query.CountAsync();
        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .OrderBy(m => m.SentAt)
            .ToListAsync();

        return new PagedResult<PrivateMessage>
        {
            TotalCount = total,
            Page = page,
            PageSize = pageSize,
            Items = items
        };
    }

    public async Task<PagedResult<PrivateMessage>> GetUserSentMessagesAsync(int userId, int page, int pageSize)
    {
        var query = _db.PrivateMessages
            .Include(m => m.Sender)
            .Include(m => m.Receiver)
            .Where(m => !m.IsDeleted && m.SenderId == userId)
            .OrderByDescending(m => m.SentAt);

        var total = await query.CountAsync();
        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .OrderBy(m => m.SentAt)
            .ToListAsync();

        return new PagedResult<PrivateMessage>
        {
            TotalCount = total,
            Page = page,
            PageSize = pageSize,
            Items = items
        };
    }

    public async Task SoftDeleteMessageAsync(long messageId, int userId)
    {
        var msg = await _db.PrivateMessages
            .FirstOrDefaultAsync(m => m.Id == messageId && m.SenderId == userId);
        if (msg != null)
        {
            msg.IsDeleted = true;
            await _db.SaveChangesAsync();
        }
    }

    public async Task ForceDeleteMessageAsync(long messageId)
    {
        var msg = await _db.PrivateMessages.FindAsync(messageId);
        if (msg != null)
        {
            _db.PrivateMessages.Remove(msg);
            await _db.SaveChangesAsync();
        }
    }

    public async Task<List<PrivateMessage>> SearchAllMessagesAsync(
        string? keyword, DateTime? from, DateTime? to, int page, int pageSize)
    {
        var query = _db.PrivateMessages
            .Include(m => m.Sender)
            .Include(m => m.Receiver)
            .Where(m => !m.IsDeleted)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(keyword))
            query = query.Where(m => m.Content.Contains(keyword));
        if (from.HasValue)
            query = query.Where(m => m.SentAt >= from.Value);
        if (to.HasValue)
            query = query.Where(m => m.SentAt <= to.Value);

        return await query
            .OrderByDescending(m => m.SentAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
    }

    public async Task<int> SearchAllMessagesCountAsync(
        string? keyword, DateTime? from, DateTime? to)
    {
        var query = _db.PrivateMessages.Where(m => !m.IsDeleted).AsQueryable();

        if (!string.IsNullOrWhiteSpace(keyword))
            query = query.Where(m => m.Content.Contains(keyword));
        if (from.HasValue)
            query = query.Where(m => m.SentAt >= from.Value);
        if (to.HasValue)
            query = query.Where(m => m.SentAt <= to.Value);

        return await query.CountAsync();
    }

    public async Task SaveChangesAsync()
        => await _db.SaveChangesAsync();
}
