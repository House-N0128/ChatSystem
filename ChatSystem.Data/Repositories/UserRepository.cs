using ChatSystem.Core.Enums;
using ChatSystem.Core.Models;
using Microsoft.EntityFrameworkCore;

namespace ChatSystem.Data.Repositories;

public class UserRepository : IUserRepository
{
    private readonly ChatSystemDbContext _db;

    public UserRepository(ChatSystemDbContext db) => _db = db;

    public async Task<User?> GetByIdAsync(int id)
        => await _db.Users.FindAsync(id);

    public async Task<User?> GetByUsernameAsync(string username)
        => await _db.Users.FirstOrDefaultAsync(u => u.Username == username);

    public async Task<List<User>> SearchUsersAsync(string keyword, int excludeUserId)
        => await _db.Users
            .Where(u => u.Id != excludeUserId
                && u.Status == UserStatus.Active
                && (u.Username.Contains(keyword) || u.Nickname.Contains(keyword)))
            .Take(20)
            .ToListAsync();

    public async Task<List<User>> GetPendingUsersAsync()
        => await _db.Users
            .Where(u => u.Status == UserStatus.Pending)
            .OrderBy(u => u.CreatedAt)
            .ToListAsync();

    public async Task<List<User>> GetAllUsersAsync(int page, int pageSize)
        => await _db.Users
            .OrderBy(u => u.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

    public async Task<int> GetTotalUserCountAsync()
        => await _db.Users.CountAsync();

    public async Task AddAsync(User user)
        => await _db.Users.AddAsync(user);

    public void Update(User user)
        => _db.Users.Update(user);

    public async Task SaveChangesAsync()
        => await _db.SaveChangesAsync();
}
