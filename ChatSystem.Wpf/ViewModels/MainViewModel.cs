using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using ChatSystem.Core.DTOs;
using ChatSystem.Core.Enums;
using ChatSystem.Wpf.Services;

namespace ChatSystem.Wpf.ViewModels;

public class MainViewModel : BaseViewModel
{
    private readonly ApiService _api;
    private readonly SignalRService _signalR;
    private readonly RelayCommand _sendMessageCommand;

    public MainViewModel(ApiService api, SignalRService signalR)
    {
        _api = api;
        _signalR = signalR;

        NavigateCommand = new RelayCommand(param => CurrentPage = param?.ToString() ?? "chat");
        LogoutCommand = new RelayCommand(_ => Logout());
        RefreshFriendsCommand = new RelayCommand(async _ => await LoadFriendsAsync());

        _sendMessageCommand = new RelayCommand(async _ =>
        {
            if (string.IsNullOrWhiteSpace(MessageText)) return;
            if (SelectedFriend != null)
                await _signalR.SendPrivateMessageAsync(SelectedFriend.FriendUserId, MessageText);
            else if (SelectedGroup != null)
                await _signalR.SendGroupMessageAsync(SelectedGroup.Id, MessageText);
            MessageText = "";
        }, _ => CanSend);

        _signalR.MessageReceived += OnMessageReceived;
        _signalR.GroupMessageReceived += OnGroupMessageReceived;
        _signalR.FriendAdded += OnFriendAdded;
        _signalR.FriendRemoved += OnFriendRemoved;
        _signalR.FriendRequestReceived += r => { _ = LoadPendingRequestsAsync(); };
        _signalR.UserOnline += OnUserOnline;
        _signalR.UserOffline += OnUserOffline;

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
            OnPropertyChanged(nameof(IsGroupsPage));
            OnPropertyChanged(nameof(IsProfilePage));
            OnPropertyChanged(nameof(IsAdminPage));
            OnPropertyChanged(nameof(ShowAdmin));

            if (value == "admin")
            {
                _ = LoadPendingUsersAsync();
                _ = LoadAllUsersAsync();
                _ = LoadAdminGroupsAsync();
            }
            if (value == "history")
            {
                _ = LoadHistoryAsync();
            }
        }
    }

    public bool IsChatPage => CurrentPage == "chat";
    public bool IsFriendsPage => CurrentPage == "friends";
    public bool IsRequestsPage => CurrentPage == "requests";
    public bool IsHistoryPage => CurrentPage == "history";
    public bool IsGroupsPage => CurrentPage == "groups";
    public bool IsProfilePage => CurrentPage == "profile";
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

    // ===== 聊天（好友私聊） =====
    private FriendItemViewModel? _selectedFriend;
    public FriendItemViewModel? SelectedFriend
    {
        get => _selectedFriend;
        set
        {
            if (SetField(ref _selectedFriend, value) && value != null)
            {
                MessageText = "";
                value.HasUnread = false;
                SelectedGroup = null;
                GroupMessages.Clear();
                _ = LoadChatHistoryAsync(value.FriendUserId);
            }
            _sendMessageCommand?.RaiseCanExecuteChanged();
        }
    }

    public ObservableCollection<MessageViewModel> Messages { get; } = new();

    public async Task LoadChatHistoryAsync(int friendId)
    {
        try
        {
            Messages.Clear();
            var result = await _api.GetMessagesAsync(friendId);
            if (result.Success && result.Data != null)
            {
                foreach (var m in result.Data.Items)
                    Messages.Add(new MessageViewModel(m, AuthService.CurrentUser?.Id ?? 0));
                ScrollToBottom?.Invoke();
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"加载聊天记录失败: {ex.Message}", "错误",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private string _messageText = "";
    public string MessageText
    {
        get => _messageText;
        set
        {
            SetField(ref _messageText, value);
            _sendMessageCommand?.RaiseCanExecuteChanged();
        }
    }

    public bool CanSend => !string.IsNullOrWhiteSpace(MessageText) && (SelectedFriend != null || SelectedGroup != null);

    public ICommand SendMessageCommand => _sendMessageCommand;

    private string _currentChatPartnerName = "";
    public string CurrentChatPartnerName
    {
        get => _currentChatPartnerName;
        set => SetField(ref _currentChatPartnerName, value);
    }

    public event Action? ScrollToBottom;

    // ===== 文件上传 =====
    public ICommand UploadFileCommand => new RelayCommand(async _ =>
    {
        if (SelectedFriend == null) return;
        var dialog = new Microsoft.Win32.OpenFileDialog
        {
            Title = "选择要发送的文件",
            Filter = "所有文件|*.*"
        };
        if (dialog.ShowDialog() == true)
        {
            try
            {
                var result = await _api.UploadFileAsync(SelectedFriend.FriendUserId, dialog.FileName);
                if (!result.Success)
                    MessageBox.Show(result.Message, "上传失败", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"文件上传出错: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    });

    public ICommand UploadGroupFileCommand => new RelayCommand(async _ =>
    {
        if (SelectedGroup == null) return;
        var dialog = new Microsoft.Win32.OpenFileDialog
        {
            Title = "选择要发送的文件",
            Filter = "所有文件|*.*"
        };
        if (dialog.ShowDialog() == true)
        {
            try
            {
                var result = await _api.UploadGroupFileAsync(SelectedGroup.Id, dialog.FileName);
                if (!result.Success)
                    MessageBox.Show(result.Message, "上传失败", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"文件上传出错: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    });

    // ===== 删除消息 =====
    public ICommand DeleteMessageCommand => new RelayCommand(async param =>
    {
        if (param is long msgId)
        {
            // 先从集合中移除
            Application.Current.Dispatcher.Invoke(() =>
            {
                var pm = Messages.FirstOrDefault(m => m.Id == msgId);
                if (pm != null) { Messages.Remove(pm); }
                var gm = GroupMessages.FirstOrDefault(m => m.Id == msgId);
                if (gm != null) { GroupMessages.Remove(gm); }
            });
            // 调用对应的 API 删除
            if (SelectedGroup != null)
                await _api.DeleteGroupMessageAsync(SelectedGroup.Id, msgId);
            else
                await _api.DeleteMessageAsync(msgId);
        }
    });

    // ===== 群聊 =====
    public ObservableCollection<GroupItemViewModel> Groups { get; } = new();

    public bool HasSelectedGroup => _selectedGroup != null;

    private GroupItemViewModel? _selectedGroup;
    public GroupItemViewModel? SelectedGroup
    {
        get => _selectedGroup;
        set
        {
            if (SetField(ref _selectedGroup, value) && value != null)
            {
                MessageText = "";
                SelectedFriend = null;
                Messages.Clear();
                CurrentChatPartnerName = value.Name;
                _ = LoadGroupMessagesAsync(value.Id);
                _ = _signalR.JoinGroupAsync(value.Id);
            }
            OnPropertyChanged(nameof(HasSelectedGroup));
            _sendMessageCommand?.RaiseCanExecuteChanged();
        }
    }

    public async Task LoadGroupsAsync()
    {
        try
        {
            var result = await _api.GetMyGroupsAsync();
            if (result.Success && result.Data != null)
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    Groups.Clear();
                    foreach (var g in result.Data)
                        Groups.Add(new GroupItemViewModel(g));
                });
            }
        }
        catch { }
    }

    public ObservableCollection<GroupMessageViewModel> GroupMessages { get; } = new();

    public async Task LoadGroupMessagesAsync(int groupId)
    {
        try
        {
            GroupMessages.Clear();
            var result = await _api.GetGroupMessagesAsync(groupId);
            if (result.Success && result.Data != null)
            {
                foreach (var m in result.Data)
                    GroupMessages.Add(new GroupMessageViewModel(m, AuthService.CurrentUser?.Id ?? 0));
                ScrollToBottom?.Invoke();
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"加载群聊记录失败: {ex.Message}", "错误",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private string _newGroupName = "";
    public string NewGroupName { get => _newGroupName; set => SetField(ref _newGroupName, value); }

    public ICommand CreateGroupCommand => new RelayCommand(async _ =>
    {
        if (string.IsNullOrWhiteSpace(NewGroupName)) return;
        var result = await _api.CreateGroupAsync(NewGroupName, new List<int>());
        if (result.Success)
        {
            NewGroupName = "";
            // 加入 SignalR 群组以便接收实时消息
            await _signalR.JoinGroupAsync(result.Data!.Id);
            await LoadGroupsAsync();
            MessageBox.Show("群组创建成功", "", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        else
        {
            MessageBox.Show(result.Message, "创建失败", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    });

    // ===== 个人信息 =====
    private string _newNickname = "";
    public string NewNickname
    {
        get => _newNickname;
        set => SetField(ref _newNickname, value);
    }

    public ICommand UpdateNicknameCommand => new RelayCommand(async _ =>
    {
        if (string.IsNullOrWhiteSpace(NewNickname)) return;
        var result = await _api.UpdateProfileAsync(NewNickname);
        if (result.Success)
        {
            if (AuthService.CurrentUser != null)
                AuthService.CurrentUser.Nickname = NewNickname;
            OnPropertyChanged(nameof(UserDisplayName));
            MessageBox.Show("昵称已更新", "完成");
        }
        else
        {
            MessageBox.Show(result.Message, "失败", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    });

    private string _oldPassword = "";
    public string OldPassword { get => _oldPassword; set => SetField(ref _oldPassword, value); }

    private string _newPassword = "";
    public string NewPassword { get => _newPassword; set => SetField(ref _newPassword, value); }

    public ICommand UpdatePasswordCommand => new RelayCommand(async _ =>
    {
        if (string.IsNullOrWhiteSpace(OldPassword) || string.IsNullOrWhiteSpace(NewPassword))
        {
            MessageBox.Show("请填写原密码和新密码", "提示");
            return;
        }
        if (NewPassword.Length < 6)
        {
            MessageBox.Show("新密码至少6个字符", "提示");
            return;
        }
        var result = await _api.UpdatePasswordAsync(OldPassword, NewPassword);
        if (result.Success)
        {
            OldPassword = ""; NewPassword = "";
            OnPropertyChanged(nameof(OldPassword));
            OnPropertyChanged(nameof(NewPassword));
            MessageBox.Show("密码已更新", "完成");
        }
        else
        {
            MessageBox.Show(result.Message, "失败", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    });

    // ===== 邀请好友加入群聊 =====
    public ICommand InviteFriendCommand => new RelayCommand(async _ =>
    {
        if (SelectedGroup == null) return;

        var groupResult = await _api.GetGroupAsync(SelectedGroup.Id);
        var memberIds = groupResult.Success && groupResult.Data != null
            ? groupResult.Data.Members.Select(m => m.UserId).ToHashSet()
            : new HashSet<int>();

        var friendsResult = await _api.GetFriendsAsync();
        if (!friendsResult.Success || friendsResult.Data == null) return;

        var available = friendsResult.Data.Where(f => !memberIds.Contains(f.FriendUserId)).ToList();
        if (available.Count == 0)
        {
            MessageBox.Show("所有好友都已在群中", "提示");
            return;
        }

        var checkedIds = new HashSet<int>();
        var okBtn = new Button
        {
            Content = "邀请",
            Style = (Style)Application.Current.FindResource("PrimaryBtn"),
            Margin = new Thickness(0, 12, 0, 0),
            HorizontalAlignment = HorizontalAlignment.Right,
            Padding = new Thickness(24, 8, 24, 8),
            IsEnabled = false
        };

        var stack = new StackPanel { Margin = new Thickness(16) };
        stack.Children.Add(new TextBlock
        {
            Text = "选择要邀请的好友：",
            FontWeight = FontWeights.Bold,
            FontSize = 14,
            Margin = new Thickness(0, 0, 0, 10)
        });

        foreach (var f in available)
        {
            var cb = new CheckBox
            {
                Content = $"{f.FriendNickname} ({f.FriendUsername})",
                Margin = new Thickness(4, 3, 0, 3),
                FontSize = 13
            };
            var id = f.FriendUserId;
            cb.Checked += (_, _) => { checkedIds.Add(id); okBtn.IsEnabled = true; };
            cb.Unchecked += (_, _) => { checkedIds.Remove(id); okBtn.IsEnabled = checkedIds.Count > 0; };
            stack.Children.Add(cb);
        }

        var scroll = new ScrollViewer { Content = stack, MaxHeight = 350 };
        var root = new StackPanel();
        root.Children.Add(scroll);
        root.Children.Add(okBtn);

        var dialog = new Window
        {
            Title = "邀请成员",
            Content = root,
            Width = 340,
            Height = 420,
            WindowStartupLocation = WindowStartupLocation.CenterScreen,
            ResizeMode = ResizeMode.NoResize,
            ShowInTaskbar = false,
            Topmost = true
        };

        okBtn.Click += async (_, _) =>
        {
            okBtn.IsEnabled = false;
            foreach (var id in checkedIds)
                await _api.AddGroupMemberAsync(SelectedGroup.Id, id);
            // 刷新群列表并更新选中群的成员数
            var groupId = SelectedGroup.Id;
            await LoadGroupsAsync();
            var updated = Groups.FirstOrDefault(g => g.Id == groupId);
            if (updated != null)
            {
                SelectedGroup.MemberCount = updated.MemberCount;
                OnPropertyChanged(nameof(SelectedGroup));
            }
            dialog.Close();
        };

        dialog.ShowDialog();
    });

    // ===== 解散群聊 =====
    public ICommand DissolveGroupCommand => new RelayCommand(async _ =>
    {
        if (SelectedGroup == null) return;
        if (MessageBox.Show($"确定解散群聊「{SelectedGroup.Name}」？此操作不可恢复。",
                "解散群聊", MessageBoxButton.YesNo, MessageBoxImage.Warning) != MessageBoxResult.Yes)
            return;
        var result = await _api.DeleteGroupAsync(SelectedGroup.Id);
        if (result.Success)
        {
            SelectedGroup = null;
            GroupMessages.Clear();
            await LoadGroupsAsync();
            CurrentPage = "groups";
            MessageBox.Show("群聊已解散", "提示");
        }
        else
        {
            MessageBox.Show(result.Message, "失败", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    });

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
            MessageBox.Show(result.Message);
        }
    });

    public ICommand RemoveFriendCommand => new RelayCommand(async param =>
    {
        if (param is int friendId)
        {
            var result = await _api.RemoveFriendAsync(friendId);
            if (result.Success) await LoadFriendsAsync();
            MessageBox.Show(result.Message);
        }
    });

    // ===== 聊天记录（历史消息） =====
    public ObservableCollection<HistoryMessageViewModel> HistoryMessages { get; } = new();

    private string _historySearchKeyword = "";
    public string HistorySearchKeyword { get => _historySearchKeyword; set => SetField(ref _historySearchKeyword, value); }

    public ICommand LoadHistoryCommand => new RelayCommand(async _ => await LoadHistoryAsync());

    public async Task LoadHistoryAsync()
    {
        try
        {
            Application.Current.Dispatcher.Invoke(() => HistoryMessages.Clear());
            var keyword = HistorySearchKeyword;

            var friendsResult = await _api.GetFriendsAsync();
            if (friendsResult.Success && friendsResult.Data != null)
            {
                foreach (var friend in friendsResult.Data)
                {
                    var msgResult = await _api.GetMessagesAsync(friend.FriendUserId, 1, 20);
                    if (msgResult.Success && msgResult.Data != null)
                    {
                        var filtered = msgResult.Data.Items
                            .Where(m => string.IsNullOrEmpty(keyword) ||
                                        m.Content.Contains(keyword, StringComparison.OrdinalIgnoreCase))
                            .Select(m => new HistoryMessageViewModel(m, friend.FriendNickname, AuthService.CurrentUser?.Id ?? 0));
                        Application.Current.Dispatcher.Invoke(() =>
                        {
                            foreach (var m in filtered) HistoryMessages.Add(m);
                        });
                    }
                }
            }

            var groupsResult = await _api.GetMyGroupsAsync();
            if (groupsResult.Success && groupsResult.Data != null)
            {
                foreach (var group in groupsResult.Data)
                {
                    var msgResult = await _api.GetGroupMessagesAsync(group.Id, 1, 20);
                    if (msgResult.Success && msgResult.Data != null)
                    {
                        var filtered = msgResult.Data
                            .Where(m => string.IsNullOrEmpty(keyword) ||
                                        m.Content.Contains(keyword, StringComparison.OrdinalIgnoreCase))
                            .Select(m => new HistoryMessageViewModel(m, group.Name, AuthService.CurrentUser?.Id ?? 0));
                        Application.Current.Dispatcher.Invoke(() =>
                        {
                            foreach (var m in filtered) HistoryMessages.Add(m);
                        });
                    }
                }
            }
        }
        catch { }
    }

    public ICommand DeleteHistoryMessageCommand => new RelayCommand(async param =>
    {
        if (param is long msgId)
        {
            await _api.DeleteMessageAsync(msgId);
            Application.Current.Dispatcher.Invoke(() =>
            {
                var hm = HistoryMessages.FirstOrDefault(m => m.Id == msgId);
                if (hm != null) HistoryMessages.Remove(hm);
                var pm = Messages.FirstOrDefault(m => m.Id == msgId);
                if (pm != null) Messages.Remove(pm);
                var gm = GroupMessages.FirstOrDefault(m => m.Id == msgId);
                if (gm != null) GroupMessages.Remove(gm);
            });
        }
    });

    // ===== 管理员功能 =====
    public ObservableCollection<UserDTO> PendingUsers { get; } = new();
    public ObservableCollection<UserDTO> AllUsers { get; } = new();
    public ObservableCollection<GroupDTO> AdminGroups { get; } = new();
    public ObservableCollection<MessageDTO> AdminMessages { get; } = new();
    private string _adminMsgKeyword = "";
    public string AdminMsgKeyword { get => _adminMsgKeyword; set => SetField(ref _adminMsgKeyword, value); }

    private int _adminConvType = 0;
    public int AdminConvType
    {
        get => _adminConvType;
        set
        {
            if (SetField(ref _adminConvType, value))
            {
                OnPropertyChanged(nameof(IsAdminConvUser));
                OnPropertyChanged(nameof(IsAdminConvGroup));
                OnPropertyChanged(nameof(IsAdminConvAll));
                _ = RefreshAdminMessagesAsync();
            }
        }
    }
    public bool IsAdminConvAll => AdminConvType == 0;
    public bool IsAdminConvUser => AdminConvType == 1;
    public bool IsAdminConvGroup => AdminConvType == 2;

    private UserDTO? _adminSelectedUser;
    public UserDTO? AdminSelectedUser
    {
        get => _adminSelectedUser;
        set
        {
            if (SetField(ref _adminSelectedUser, value) && value != null)
            {
                AdminConvType = 1;
                _ = RefreshAdminMessagesAsync();
            }
        }
    }

    private GroupDTO? _adminSelectedGroup;
    public GroupDTO? AdminSelectedGroup
    {
        get => _adminSelectedGroup;
        set
        {
            if (SetField(ref _adminSelectedGroup, value) && value != null)
            {
                AdminConvType = 2;
                _ = RefreshAdminMessagesAsync();
            }
        }
    }

    private async Task RefreshAdminMessagesAsync()
    {
        AdminMessages.Clear();

        if (AdminConvType == 1 && AdminSelectedUser != null)
        {
            var result = await _api.AdminGetUserMessagesAsync(AdminSelectedUser.Id);
            if (result.Success && result.Data != null)
            {
                var msgs = string.IsNullOrWhiteSpace(AdminMsgKeyword)
                    ? result.Data.Items
                    : result.Data.Items.Where(m => m.Content.Contains(AdminMsgKeyword, StringComparison.OrdinalIgnoreCase)).ToList();
                foreach (var m in msgs) AdminMessages.Add(m);
            }
        }
        else if (AdminConvType == 2 && AdminSelectedGroup != null)
        {
            var result = await _api.GetGroupMessagesAsync(AdminSelectedGroup.Id, 1, 100);
            if (result.Success && result.Data != null)
            {
                var source = string.IsNullOrWhiteSpace(AdminMsgKeyword)
                    ? result.Data
                    : result.Data.Where(m => m.Content.Contains(AdminMsgKeyword, StringComparison.OrdinalIgnoreCase)).ToList();
                var groupName = AdminSelectedGroup.Name;
                var msgs = source.Select(m => new MessageDTO
                {
                    Id = m.Id, SenderId = m.SenderId,
                    SenderName = m.SenderName, SenderNickname = m.SenderNickname,
                    Content = m.Content, MessageType = m.MessageType,
                    GroupName = groupName, SentAt = m.SentAt
                }).ToList();
                foreach (var m in msgs) AdminMessages.Add(m);
            }
        }
        else
        {
            var keyword = AdminMsgKeyword;
            var privateResult = await _api.SearchMessagesAsync(keyword);
            if (privateResult.Success && privateResult.Data != null)
            {
                foreach (var m in privateResult.Data.Items) AdminMessages.Add(m);
            }

            var groupsResult = await _api.AdminGetAllGroupsAsync();
            if (groupsResult.Success && groupsResult.Data != null)
            {
                foreach (var group in groupsResult.Data)
                {
                    var groupMsgs = await _api.GetGroupMessagesAsync(group.Id, 1, 100);
                    if (groupMsgs.Success && groupMsgs.Data != null)
                    {
                        var filtered = string.IsNullOrWhiteSpace(keyword)
                            ? groupMsgs.Data
                            : groupMsgs.Data.Where(m => m.Content.Contains(keyword, StringComparison.OrdinalIgnoreCase)).ToList();
                        var dtos = filtered.Select(m => new MessageDTO
                        {
                            Id = m.Id, SenderId = m.SenderId,
                            SenderName = m.SenderName, SenderNickname = m.SenderNickname,
                            Content = m.Content, MessageType = m.MessageType,
                            GroupName = group.Name, SentAt = m.SentAt
                        }).ToList();
                        foreach (var m in dtos) AdminMessages.Add(m);
                    }
                }
            }
        }
    }

    public ICommand LoadPendingUsersCommand => new RelayCommand(async _ => await LoadPendingUsersAsync());

    public async Task LoadPendingUsersAsync()
    {
        var result = await _api.GetPendingUsersAsync();
        Application.Current.Dispatcher.Invoke(() =>
        {
            PendingUsers.Clear();
            if (result.Success && result.Data != null)
                foreach (var u in result.Data) PendingUsers.Add(u);
        });
    }

    public ICommand ApproveUserCommand => new RelayCommand(async param =>
    {
        if (param is int userId)
        {
            await _api.ApproveUserAsync(userId);
            await LoadPendingUsersAsync();
            await LoadAllUsersAsync();
        }
    });

    public ICommand RejectUserCommand => new RelayCommand(async param =>
    {
        if (param is int userId)
        {
            await _api.RejectUserAsync(userId);
            await LoadPendingUsersAsync();
        }
    });

    public ICommand LoadAllUsersCommand => new RelayCommand(async _ => await LoadAllUsersAsync());

    public async Task LoadAllUsersAsync()
    {
        var result = await _api.GetAllUsersAsync();
        Application.Current.Dispatcher.Invoke(() =>
        {
            AllUsers.Clear();
            if (result.Success && result.Data != null)
            {
                var currentUserId = AuthService.CurrentUser?.Id ?? 0;
                foreach (var u in result.Data.Items)
                    if (u.Id != currentUserId && u.Status != 0) AllUsers.Add(u);
            }
        });
    }

    public ICommand BanUserCommand => new RelayCommand(async param =>
    {
        if (param is int userId)
        {
            await _api.BanUserAsync(userId);
            await LoadAllUsersAsync();
        }
    });

    public ICommand UnbanUserCommand => new RelayCommand(async param =>
    {
        if (param is int userId)
        {
            await _api.UnbanUserAsync(userId);
            await LoadAllUsersAsync();
        }
    });

    public ICommand AdminSearchMessagesCommand => new RelayCommand(async _ => await RefreshAdminMessagesAsync());

    public ICommand AdminDeleteMessageCommand => new RelayCommand(async param =>
    {
        if (param is long msgId)
        {
            await _api.ForceDeleteMessageAsync(msgId);
            await RefreshAdminMessagesAsync();
        }
    });

    public async Task LoadAdminGroupsAsync()
    {
        var result = await _api.AdminGetAllGroupsAsync();
        Application.Current.Dispatcher.Invoke(() =>
        {
            AdminGroups.Clear();
            if (result.Success && result.Data != null)
                foreach (var g in result.Data) AdminGroups.Add(g);
        });
    }

    private void OnMessageReceived(MessageDTO msg)
    {
        var friendId = msg.SenderId == AuthService.CurrentUser?.Id ? msg.ReceiverId : msg.SenderId;
        Application.Current.Dispatcher.Invoke(() =>
        {
            if (SelectedFriend != null && SelectedFriend.FriendUserId == friendId)
            {
                Messages.Add(new MessageViewModel(msg, AuthService.CurrentUser?.Id ?? 0));
                ScrollToBottom?.Invoke();
            }
            else
            {
                var friend = Friends.FirstOrDefault(f => f.FriendUserId == friendId);
                if (friend != null) friend.HasUnread = true;
            }
        });
    }

    private void OnGroupMessageReceived(GroupMessageDTO msg)
    {
        Application.Current.Dispatcher.Invoke(() =>
        {
            if (SelectedGroup != null && SelectedGroup.Id == msg.GroupId)
            {
                GroupMessages.Add(new GroupMessageViewModel(msg, AuthService.CurrentUser?.Id ?? 0));
                ScrollToBottom?.Invoke();
            }
        });
    }

    private void OnFriendAdded(FriendDTO friend)
    {
        Application.Current.Dispatcher.Invoke(() => Friends.Add(new FriendItemViewModel(friend)));
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
        _ = _signalR.DisconnectAsync();
        AuthService.Clear();
        OnLogout?.Invoke();
    }

    public event Action? OnLogout;
}

public class FriendItemViewModel : BaseViewModel
{
    public int FriendUserId { get; set; }
    public string FriendUsername { get; set; } = "";
    public string FriendNickname { get; set; } = "";

    private bool _isOnline;
    public bool IsOnline { get => _isOnline; set => SetField(ref _isOnline, value); }

    private bool _hasUnread;
    public bool HasUnread { get => _hasUnread; set => SetField(ref _hasUnread, value); }

    public FriendItemViewModel() { }
    public FriendItemViewModel(FriendDTO dto)
    {
        FriendUserId = dto.FriendUserId;
        FriendUsername = dto.FriendUsername;
        FriendNickname = dto.FriendNickname;
        IsOnline = dto.IsOnline;
    }
}

public class MessageViewModel : BaseViewModel
{
    public long Id { get; set; }
    public int SenderId { get; set; }
    public string SenderName { get; set; } = "";
    public string SenderNickname { get; set; } = "";
    public string Content { get; set; } = "";
    public bool IsMine { get; set; }
    public string SentAt { get; set; } = "";
    public bool IsFile { get; set; }
    public string? FileName { get; set; }
    public string? FileUrl { get; set; }

    public MessageViewModel() { }
    public MessageViewModel(MessageDTO dto, int currentUserId, string serverUrl = "http://localhost:5136")
    {
        Id = dto.Id; SenderId = dto.SenderId; SenderName = dto.SenderName;
        SenderNickname = dto.SenderNickname; Content = dto.Content;
        IsMine = dto.SenderId == currentUserId;
        SentAt = dto.SentAt.ToString("HH:mm:ss");
        IsFile = dto.MessageType == MessageType.File && !string.IsNullOrEmpty(dto.FilePath);
        FileName = dto.FileName;
        FileUrl = IsFile ? $"{serverUrl}{dto.FilePath}" : null;
    }
}

public class GroupItemViewModel : BaseViewModel
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public int MemberCount { get; set; }
    public string DisplayText => $"{Name} ({MemberCount}人)";

    public GroupItemViewModel() { }
    public GroupItemViewModel(GroupDTO dto)
    {
        Id = dto.Id; Name = dto.Name; MemberCount = dto.MemberCount;
    }
}

public class GroupMessageViewModel : BaseViewModel
{
    public long Id { get; set; }
    public int GroupId { get; set; }
    public int SenderId { get; set; }
    public string SenderNickname { get; set; } = "";
    public string Content { get; set; } = "";
    public bool IsMine { get; set; }
    public string SentAt { get; set; } = "";
    public bool IsFile { get; set; }
    public string? FileName { get; set; }
    public string? FileUrl { get; set; }

    public GroupMessageViewModel() { }
    public GroupMessageViewModel(GroupMessageDTO dto, int currentUserId, string serverUrl = "http://localhost:5136")
    {
        Id = dto.Id; GroupId = dto.GroupId; SenderId = dto.SenderId;
        SenderNickname = dto.SenderNickname; Content = dto.Content;
        IsMine = dto.SenderId == currentUserId;
        SentAt = dto.SentAt.ToString("HH:mm:ss");
        IsFile = dto.MessageType == MessageType.File && !string.IsNullOrEmpty(dto.FilePath);
        FileName = dto.FileName;
        FileUrl = IsFile ? $"{serverUrl}{dto.FilePath}" : null;
    }
}

public class HistoryMessageViewModel : BaseViewModel
{
    public long Id { get; set; }
    public string Content { get; set; } = "";
    public string PartnerNickname { get; set; } = "";
    public string SentAt { get; set; } = "";
    public string DirectionText { get; set; } = "";
    public bool IsMine { get; set; }

    public HistoryMessageViewModel() { }
    public HistoryMessageViewModel(MessageDTO dto, string friendNickname, int currentUserId)
    {
        Id = dto.Id; Content = dto.Content; PartnerNickname = friendNickname;
        SentAt = dto.SentAt.ToString("yyyy-MM-dd HH:mm");
        IsMine = dto.SenderId == currentUserId;
        DirectionText = IsMine ? $"发送给 {friendNickname}" : $"来自 {friendNickname}";
    }
    public HistoryMessageViewModel(GroupMessageDTO dto, string groupName, int currentUserId)
    {
        Id = dto.Id; Content = dto.Content; PartnerNickname = groupName;
        SentAt = dto.SentAt.ToString("yyyy-MM-dd HH:mm");
        IsMine = dto.SenderId == currentUserId;
        DirectionText = $"在 {groupName} 中 {(IsMine ? "我说" : $"{dto.SenderNickname}说")}";
    }
}
