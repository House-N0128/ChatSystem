using ChatSystem.Core.Models;

namespace ChatSystem.Data.Repositories;

public interface IGroupRepository
{
    Task<Group> CreateGroupAsync(Group group, List<int> memberIds);
    Task<Group?> GetByIdAsync(int groupId);
    Task<List<Group>> GetUserGroupsAsync(int userId);
    Task<List<Group>> GetAllGroupsAsync();
    Task AddMemberAsync(int groupId, int userId);
    Task RemoveMemberAsync(int groupId, int userId);
    Task<bool> IsMemberAsync(int groupId, int userId);
    Task<GroupMessage> AddGroupMessageAsync(GroupMessage message);
    Task<List<GroupMessage>> GetGroupMessagesAsync(int groupId, int page, int pageSize);
    Task SoftDeleteGroupMessageAsync(long messageId, int userId);
    Task<bool> DeleteGroupAsync(int groupId, int userId);
    Task SaveChangesAsync();
}
