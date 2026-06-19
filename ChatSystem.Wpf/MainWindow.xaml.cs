using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using ChatSystem.Wpf.ViewModels;

namespace ChatSystem.Wpf;

public partial class MainWindow : Window
{
    private readonly MainViewModel _viewModel;

    public MainWindow(MainViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
        _viewModel = viewModel;

        Loaded += (_, _) =>
        {
            _viewModel.ScrollToBottom += OnScrollToBottom;
            _ = _viewModel.LoadGroupsAsync();
        };
    }

    private void FriendList_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (e.AddedItems.Count > 0 && e.AddedItems[0] is FriendItemViewModel friend)
        {
            _viewModel.SelectedFriend = friend;
        }
    }

    private void GroupList_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (e.AddedItems.Count > 0 && e.AddedItems[0] is GroupItemViewModel group)
        {
            _viewModel.SelectedGroup = group;
        }
    }

    private void FileLink_MouseDown(object sender, MouseButtonEventArgs e)
    {
        var fe = sender as FrameworkElement;
        if (fe?.DataContext == null) return;
        var url = fe.DataContext switch
        {
            MessageViewModel pm => pm.FileUrl,
            GroupMessageViewModel gm => gm.FileUrl,
            _ => null
        };
        if (url != null)
        {
            try { Process.Start(new ProcessStartInfo(url) { UseShellExecute = true }); }
            catch { MessageBox.Show("无法打开文件链接", "提示"); }
        }
    }

    private void OldPwd_PasswordChanged(object sender, RoutedEventArgs e)
    {
        _viewModel.OldPassword = OldPwdBox.Password;
    }

    private void NewPwd_PasswordChanged(object sender, RoutedEventArgs e)
    {
        _viewModel.NewPassword = NewPwdBox.Password;
    }

    private void OnScrollToBottom()
    {
        Dispatcher.Invoke(() =>
        {
            if (ChatScrollViewer is ScrollViewer sv)
            {
                sv.ScrollToBottom();
            }
            if (GroupScrollViewer is ScrollViewer gsv)
            {
                gsv.ScrollToBottom();
            }
        });
    }
}
