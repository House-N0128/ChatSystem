using ChatSystem.Core.Models;
using Microsoft.EntityFrameworkCore;

namespace ChatSystem.Data.Repositories;

public class GroupRepository : IGroupRepository
{
    private readonly ChatSystemDbContext _db;

    public GroupRepository(ChatSystemDbContext db) => _db = db;

    public async Task<Group> CreateGroupAsync(Group group, List<int> memberIds)
    {
        // 创建者自动加入
        memberIds.Add(group.CreatorId);
        var distinctIds = memberIds.Distinct().ToList();

        _db.Groups.Add(group);
        foreach (var userId in distinctIds)
        {
            _db.GroupMembers.Add(new GroupMember
            {
                Group = group,
                UserId = userId,
                JoinedAt = DateTime.Now
            });
        }
        await _db.SaveChangesAsync();
        return group;
    }

    public async Task<Group?> GetByIdAsync(int groupId)
        => await _db.Groups
            .Include(g => g.Creator)
            .Include(g => g.Members).ThenInclude(m => m.User)
            .FirstOrDefaultAsync(g => g.Id == groupId);

    public async Task<List<Group>> GetAllGroupsAsync()
        => await _db.Groups
            .Include(g => g.Members)
            .OrderBy(g => g.Name)
            .ToListAsync();

    public async Task<List<Group>> GetUserGroupsAsync(int userId)
    {
        var groupIds = await _db.GroupMembers
            .Where(gm => gm.UserId == userId)
            .Select(gm => gm.GroupId)
            .ToListAsync();
        return await _db.Groups
            .Include(g => g.Members)
            .Where(g => groupIds.Contains(g.Id))
            .ToListAsync();
    }

    public async Task AddMemberAsync(int groupId, int userId)
    {
        if (!await IsMemberAsync(groupId, userId))
        {
            _db.GroupMembers.Add(new GroupMember
            {
                GroupId = groupId,
                UserId = userId,
                JoinedAt = DateTime.Now
            });
            await _db.SaveChangesAsync();
        }
    }

    public async Task RemoveMemberAsync(int groupId, int userId)
    {
        var member = await _db.GroupMembers
            .FirstOrDefaultAsync(gm => gm.GroupId == groupId && gm.UserId == userId);
        if (member != null)
        {
            _db.GroupMembers.Remove(member);
            await _db.SaveChangesAsync();
        }
    }

    public async Task<bool> IsMemberAsync(int groupId, int userId)
        => await _db.GroupMembers.AnyAsync(gm => gm.GroupId == groupId && gm.UserId == userId);

    public async Task<GroupMessage> AddGroupMessageAsync(GroupMessage message)
    {
        _db.GroupMessages.Add(message);
        await _db.SaveChangesAsync();
        return message;
    }

    public async Task<List<GroupMessage>> GetGroupMessagesAsync(int groupId, int page, int pageSize)
        => await _db.GroupMessages
            .Include(m => m.Sender)
            .Where(m => m.GroupId == groupId && !m.IsDeleted)
            .OrderByDescending(m => m.SentAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .OrderBy(m => m.SentAt)
            .ToListAsync();

    public async Task SoftDeleteGroupMessageAsync(long messageId, int userId)
    {
        var msg = await _db.GroupMessages
            .FirstOrDefaultAsync(m => m.Id == messageId && m.SenderId == userId);
        if (msg != null)
        {
            msg.IsDeleted = true;
            await _db.SaveChangesAsync();
        }
    }

    public async Task<bool> DeleteGroupAsync(int groupId, int userId)
    {
        var group = await _db.Groups.Include(g => g.Members).FirstOrDefaultAsync(g => g.Id == groupId);
        if (group == null || group.CreatorId != userId) return false;
        _db.Groups.Remove(group);
        await _db.SaveChangesAsync();
        return true;
    }

    public async Task SaveChangesAsync()
        => await _db.SaveChangesAsync();
}
