using ChatSystem.Core.Models;

namespace ChatSystem.Data.Repositories;

public interface IFriendRepository
{
    Task<List<User>> GetFriendsAsync(int userId);
    Task<bool> AreFriendsAsync(int userId1, int userId2);
    Task AddFriendAsync(int userId, int friendUserId);
    Task RemoveFriendAsync(int userId, int friendUserId);

    // Friend Requests
    Task<FriendRequest?> GetRequestByIdAsync(int requestId);
    Task<List<FriendRequest>> GetPendingRequestsAsync(int userId);
    Task<List<FriendRequest>> GetSentRequestsAsync(int userId);
    Task<bool> HasPendingRequestAsync(int fromUserId, int toUserId);
    Task AddFriendRequestAsync(FriendRequest request);
    Task AcceptRequestAsync(int requestId);
    Task RejectRequestAsync(int requestId);
    Task SaveChangesAsync();
}
