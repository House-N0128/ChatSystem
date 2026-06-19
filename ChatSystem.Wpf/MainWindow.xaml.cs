using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
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

    // ===== Enter 键发送消息 =====
    private void MessageBox_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter && Keyboard.Modifiers != ModifierKeys.Shift)
        {
            e.Handled = true;
            if (_viewModel.CanSend)
                _viewModel.SendMessageCommand.Execute(null);
        }
        // Shift+Enter 允许换行
    }

    // ===== Emoji 面板 =====
    private static readonly string[][] EmojiGroups = new[]
    {
        new[] { "😀", "😃", "😄", "😁", "😅", "😂", "🤣", "😊", "😍", "🥰", "😘", "😜", "🤔", "😏", "😌", "😴" },
        new[] { "👍", "👎", "👏", "🙌", "💪", "🤝", "✌️", "🤞", "👋", "🙏", "💀", "🤡" },
        new[] { "❤️", "🧡", "💛", "💚", "💙", "💜", "🖤", "💔", "💯", "✅", "❌", "⭐", "🔥", "🎉", "💡", "💬" },
        new[] { "🐱", "🐶", "🐼", "🐨", "🐸", "🦊", "🌸", "🌺", "🌞", "🌈", "🍀", "🌙" },
        new[] { "🎂", "🍰", "☕", "🍺", "🎵", "🎮", "📱", "💻", "🚀", "✈️", "🏠", "💰" }
    };

    private static readonly string[] EmojiGroupNames = { "笑脸", "手势", "爱心", "动物", "物品" };

    private void EmojiButton_Click(object sender, RoutedEventArgs e)
    {
        // 判断哪个输入框是目标
        var targetBox = _viewModel.SelectedFriend != null ? PrivateChatTextBox
            : _viewModel.SelectedGroup != null ? GroupChatTextBox : null;
        if (targetBox == null) return;

        var popup = new Popup
        {
            PlacementTarget = sender as UIElement,
            Placement = PlacementMode.Top,
            StaysOpen = false,
            AllowsTransparency = true,
            PopupAnimation = PopupAnimation.Fade
        };

        var rootStack = new StackPanel();
        var tabRow = new WrapPanel { Margin = new Thickness(8, 8, 8, 4) };
        var emojiGrid = new UniformGrid { Columns = 8, Margin = new Thickness(8, 0, 8, 8) };
        foreach (var e2 in EmojiGroups[0])
            emojiGrid.Children.Add(CreateEmojiButton(e2, popup, targetBox));

        for (int i = 0; i < EmojiGroupNames.Length; i++)
        {
            var idx = i;
            var tab = new Button
            {
                Content = EmojiGroupNames[i],
                FontSize = 12,
                Padding = new Thickness(8, 4, 8, 4),
                Margin = new Thickness(2),
                Background = idx == 0 ? Brushes.LightGray : Brushes.Transparent,
                BorderThickness = new Thickness(0),
                Cursor = Cursors.Hand
            };
            tab.Click += (_, _) =>
            {
                foreach (Button tb in tabRow.Children) tb.Background = Brushes.Transparent;
                tab.Background = Brushes.LightGray;
                emojiGrid.Children.Clear();
                foreach (var e2 in EmojiGroups[idx])
                    emojiGrid.Children.Add(CreateEmojiButton(e2, popup, targetBox));
            };
            tabRow.Children.Add(tab);
        }

        rootStack.Children.Add(tabRow);
        rootStack.Children.Add(emojiGrid);

        popup.Child = new Border
        {
            Background = Brushes.White,
            BorderBrush = Brushes.LightGray,
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(8),
            Child = rootStack
        };
        popup.IsOpen = true;
    }

    private Button CreateEmojiButton(string emoji, Popup popup, TextBox targetBox)
    {
        var btn = new Button
        {
            Content = new TextBlock { Text = emoji, FontSize = 20, TextAlignment = TextAlignment.Center },
            Width = 36, Height = 36,
            Padding = new Thickness(0),
            Background = Brushes.Transparent,
            BorderThickness = new Thickness(0),
            Cursor = Cursors.Hand,
            ToolTip = emoji
        };
        btn.Click += (_, _) =>
        {
            var caret = targetBox.CaretIndex;
            targetBox.Text = targetBox.Text.Insert(caret, emoji);
            targetBox.CaretIndex = caret + emoji.Length;
            _viewModel.MessageText = targetBox.Text;
            popup.IsOpen = false;
            targetBox.Focus();
        };
        return btn;
    }
}
