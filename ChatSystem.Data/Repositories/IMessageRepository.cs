using ChatSystem.Core.DTOs;
using ChatSystem.Core.Models;

namespace ChatSystem.Data.Repositories;

public interface IMessageRepository
{
    Task<PrivateMessage> AddPrivateMessageAsync(PrivateMessage message);
    Task<PagedResult<PrivateMessage>> GetPrivateMessagesAsync(int userId1, int userId2, int page, int pageSize);
    Task SoftDeleteMessageAsync(long messageId, int userId);
    Task ForceDeleteMessageAsync(long messageId);
    Task<List<PrivateMessage>> SearchAllMessagesAsync(string? keyword, DateTime? from, DateTime? to, int page, int pageSize);
    Task<int> SearchAllMessagesCountAsync(string? keyword, DateTime? from, DateTime? to);
    Task SaveChangesAsync();
}
