using ChatSystem.Core.Models;

namespace ChatSystem.Data.Repositories;

public interface IUserRepository
{
    Task<User?> GetByIdAsync(int id);
    Task<User?> GetByUsernameAsync(string username);
    Task<List<User>> SearchUsersAsync(string keyword, int excludeUserId);
    Task<List<User>> GetPendingUsersAsync();
    Task<List<User>> GetAllUsersAsync(int page, int pageSize);
    Task<int> GetTotalUserCountAsync();
    Task AddAsync(User user);
    void Update(User user);
    Task SaveChangesAsync();
}
