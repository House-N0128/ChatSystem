using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;
using ChatSystem.Core.DTOs;
using ChatSystem.Wpf.Services;

namespace ChatSystem.Wpf.ViewModels;

public class MainViewModel : BaseViewModel
{
    private readonly ApiService _api;
    private readonly SignalRService _signalR;

    public MainViewModel(ApiService api, SignalRService signalR)
    {
        _api = api;
        _signalR = signalR;

        // 导航命令
        NavigateCommand = new RelayCommand(param => CurrentPage = param?.ToString() ?? "chat");
        LogoutCommand = new RelayCommand(_ => Logout());
        RefreshFriendsCommand = new RelayCommand(async _ => await LoadFriendsAsync());

        // SignalR 事件
        _signalR.MessageReceived += OnMessageReceived;
        _signalR.FriendAdded += OnFriendAdded;
        _signalR.FriendRemoved += OnFriendRemoved;
        _signalR.FriendRequestReceived += r => { _ = LoadPendingRequestsAsync(); };
        _signalR.UserOnline += OnUserOnline;
        _signalR.UserOffline += OnUserOffline;

        // 默认页面
        CurrentPage = "chat";
    }

    // ===== 导航 =====
    private string _currentPage = "chat";
    public string CurrentPage
    {
        get => _currentPage;
        set
        {
            SetField(ref _currentPage, value);
            OnPropertyChanged(nameof(IsChatPage));
            OnPropertyChanged(nameof(IsFriendsPage));
            OnPropertyChanged(nameof(IsRequestsPage));
            OnPropertyChanged(nameof(IsHistoryPage));
            OnPropertyChanged(nameof(IsAdminPage));
            OnPropertyChanged(nameof(ShowAdmin));
        }
    }

    public bool IsChatPage => CurrentPage == "chat";
    public bool IsFriendsPage => CurrentPage == "friends";
    public bool IsRequestsPage => CurrentPage == "requests";
    public bool IsHistoryPage => CurrentPage == "history";
    public bool IsAdminPage => CurrentPage == "admin";
    public bool ShowAdmin => AuthService.CurrentUser?.Role == Core.Enums.UserRole.Admin;

    public ICommand NavigateCommand { get; }
    public ICommand LogoutCommand { get; }
    public ICommand RefreshFriendsCommand { get; }

    public string UserDisplayName => AuthService.CurrentUser?.Nickname ?? "";
    public string UserRoleText => AuthService.CurrentUser?.Role == Core.Enums.UserRole.Admin ? "管理员" : "用户";

    // ===== 好友列表 =====
    public ObservableCollection<FriendItemViewModel> Friends { get; } = new();

    public async Task LoadFriendsAsync()
    {
        try
        {
            var result = await _api.GetFriendsAsync();
            if (result.Success && result.Data != null)
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    Friends.Clear();
                    foreach (var f in result.Data)
                        Friends.Add(new FriendItemViewModel(f));
                });
            }
        }
        catch { }
    }

    // ===== 选中的好友（聊天对象） =====
    private FriendItemViewModel? _selectedFriend;
    public FriendItemViewModel? SelectedFriend
    {
        get => _selectedFriend;
        set
        {
            if (SetField(ref _selectedFriend, value) && value != null)
            {
                value.HasUnread = false;
                _ = LoadChatHistoryAsync(value.FriendUserId);
            }
        }
    }

    // ===== 聊天消息 =====
    public ObservableCollection<MessageViewModel> Messages { get; } = new();

    public async Task LoadChatHistoryAsync(int friendId)
    {
        try
        {
            var result = await _api.GetMessagesAsync(friendId);
            Application.Current.Dispatcher.Invoke(() =>
            {
                Messages.Clear();
                if (result.Success && result.Data != null)
                {
                    foreach (var m in result.Data.Items)
                        Messages.Add(new MessageViewModel(m, AuthService.CurrentUser?.Id ?? 0));
                }
            });
        }
        catch { }
    }

    private string _messageText = "";
    public string MessageText { get => _messageText; set => SetField(ref _messageText, value); }

    public ICommand SendMessageCommand => new RelayCommand(async _ =>
    {
        if (string.IsNullOrWhiteSpace(MessageText) || SelectedFriend == null) return;
        await _signalR.SendPrivateMessageAsync(SelectedFriend.FriendUserId, MessageText);
        MessageText = "";
    }, _ => !string.IsNullOrWhiteSpace(MessageText) && SelectedFriend != null);

    // ===== 好友请求 =====
    public ObservableCollection<FriendRequestDTO> PendingRequests { get; } = new();

    public async Task LoadPendingRequestsAsync()
    {
        try
        {
            var result = await _api.GetPendingRequestsAsync();
            Application.Current.Dispatcher.Invoke(() =>
            {
                PendingRequests.Clear();
                if (result.Success && result.Data != null)
                    foreach (var r in result.Data) PendingRequests.Add(r);
            });
        }
        catch { }
    }

    public ICommand AcceptRequestCommand => new RelayCommand(async param =>
    {
        if (param is int requestId)
        {
            await _api.AcceptRequestAsync(requestId);
            await LoadPendingRequestsAsync();
            await LoadFriendsAsync();
        }
    });

    public ICommand RejectRequestCommand => new RelayCommand(async param =>
    {
        if (param is int requestId)
        {
            await _api.RejectRequestAsync(requestId);
            await LoadPendingRequestsAsync();
        }
    });

    // ===== 添加好友 =====
    private string _searchKeyword = "";
    public string SearchKeyword { get => _searchKeyword; set => SetField(ref _searchKeyword, value); }

    public ObservableCollection<UserDTO> SearchResults { get; } = new();

    public ICommand SearchUsersCommand => new RelayCommand(async _ =>
    {
        if (string.IsNullOrWhiteSpace(SearchKeyword)) return;
        var result = await _api.SearchUsersAsync(SearchKeyword);
        Application.Current.Dispatcher.Invoke(() =>
        {
            SearchResults.Clear();
            if (result.Success && result.Data != null)
                foreach (var u in result.Data) SearchResults.Add(u);
        });
    });

    public ICommand AddFriendCommand => new RelayCommand(async param =>
    {
        if (param is int userId)
        {
            var result = await _api.SendFriendRequestAsync(userId);
            System.Windows.MessageBox.Show(result.Message);
        }
    });

    public ICommand RemoveFriendCommand => new RelayCommand(async param =>
    {
        if (param is int friendId)
        {
            var result = await _api.RemoveFriendAsync(friendId);
            if (result.Success) await LoadFriendsAsync();
            System.Windows.MessageBox.Show(result.Message);
        }
    });

    // ===== 事件处理 =====
    private void OnMessageReceived(MessageDTO msg)
    {
        var friendId = msg.SenderId == AuthService.CurrentUser?.Id ? msg.ReceiverId : msg.SenderId;
        Application.Current.Dispatcher.Invoke(() =>
        {
            if (SelectedFriend != null && SelectedFriend.FriendUserId == friendId)
            {
                Messages.Add(new MessageViewModel(msg, AuthService.CurrentUser?.Id ?? 0));
            }
            // 未读标记
            else
            {
                var friend = Friends.FirstOrDefault(f => f.FriendUserId == friendId);
                if (friend != null) friend.HasUnread = true;
            }
        });
    }

    private void OnFriendAdded(FriendDTO friend)
    {
        Application.Current.Dispatcher.Invoke(() =>
        {
            Friends.Add(new FriendItemViewModel(friend));
        });
    }

    private void OnFriendRemoved(int friendId)
    {
        Application.Current.Dispatcher.Invoke(() =>
        {
            var item = Friends.FirstOrDefault(f => f.FriendUserId == friendId);
            if (item != null) Friends.Remove(item);
        });
    }

    private void OnUserOnline(UserDTO user)
    {
        var item = Friends.FirstOrDefault(f => f.FriendUserId == user.Id);
        if (item != null) item.IsOnline = true;
    }

    private void OnUserOffline(int userId)
    {
        var item = Friends.FirstOrDefault(f => f.FriendUserId == userId);
        if (item != null) item.IsOnline = false;
    }

    private void Logout()
    {
        _signalR.DisconnectAsync();
        AuthService.Clear();
        OnLogout?.Invoke();
    }

    public event Action? OnLogout;
}

// ===== 好友项 ViewModel =====
public class FriendItemViewModel : BaseViewModel
{
    public int FriendUserId { get; }
    public string FriendUsername { get; }
    public string FriendNickname { get; }

    private bool _isOnline;
    public bool IsOnline { get => _isOnline; set => SetField(ref _isOnline, value); }

    private bool _hasUnread;
    public bool HasUnread { get => _hasUnread; set => SetField(ref _hasUnread, value); }

    public FriendItemViewModel(FriendDTO dto)
    {
        FriendUserId = dto.FriendUserId;
        FriendUsername = dto.FriendUsername;
        FriendNickname = dto.FriendNickname;
        IsOnline = dto.IsOnline;
    }
}

// ===== 消息项 ViewModel =====
public class MessageViewModel : BaseViewModel
{
    public long Id { get; }
    public int SenderId { get; }
    public string SenderName { get; }
    public string SenderNickname { get; }
    public string Content { get; }
    public bool IsMine { get; }
    public string SentAt { get; }

    public MessageViewModel(MessageDTO dto, int currentUserId)
    {
        Id = dto.Id;
        SenderId = dto.SenderId;
        SenderName = dto.SenderName;
        SenderNickname = dto.SenderNickname;
        Content = dto.Content;
        IsMine = dto.SenderId == currentUserId;
        SentAt = dto.SentAt.ToLocalTime().ToString("HH:mm:ss");
    }
}
