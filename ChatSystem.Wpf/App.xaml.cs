using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using ChatSystem.Wpf.Services;
using ChatSystem.Wpf.ViewModels;
using ChatSystem.Wpf.Views;

namespace ChatSystem.Wpf;

public partial class App : Application
{
    private const string ServerUrl = "http://localhost:5136";

    private ApiService? _api;
    private SignalRService? _signalR;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // 全局异常捕获
        AppDomain.CurrentDomain.UnhandledException += (_, args) =>
            MessageBox.Show($"未处理异常: {args.ExceptionObject}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
        Current.DispatcherUnhandledException += (_, args) =>
        {
            MessageBox.Show($"UI异常: {args.Exception.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            args.Handled = true;
        };

        _api = new ApiService(ServerUrl);
        _signalR = new SignalRService();

        // 清除上次登录信息，每次启动都显示登录界面
        AuthService.Clear();
        ShowLoginWindow();
    }

    private void ShowLoginWindow()
    {
        var loginVm = new LoginViewModel(_api!, _signalR!, ServerUrl);
        loginVm.OnLoginSuccess += ShowMainWindow;
        loginVm.OnNavigateToRegister += () => ShowRegisterDialog(loginVm);

        var loginWindow = new LoginWindow(loginVm);
        loginWindow.Show();
    }

    private void ShowRegisterDialog(LoginViewModel loginVm)
    {
        var usernameBox = new TextBox { Margin = new Thickness(0, 4, 0, 8), Padding = new Thickness(8) };
        var nicknameBox = new TextBox { Margin = new Thickness(0, 4, 0, 8), Padding = new Thickness(8) };
        var passwordBox = new PasswordBox { Margin = new Thickness(0, 4, 0, 12), Padding = new Thickness(8) };
        var errorText = new TextBlock { Foreground = System.Windows.Media.Brushes.Red, Margin = new Thickness(0, 0, 0, 8) };

        var okBtn = new Button
        {
            Content = "注册",
            Style = (Style)FindResource("PrimaryBtn"),
            HorizontalAlignment = HorizontalAlignment.Right,
            Padding = new Thickness(24, 8, 24, 8)
        };

        var stack = new StackPanel { Margin = new Thickness(16) };
        stack.Children.Add(new TextBlock { Text = "用户名", FontSize = 13 });
        stack.Children.Add(usernameBox);
        stack.Children.Add(new TextBlock { Text = "昵称", FontSize = 13 });
        stack.Children.Add(nicknameBox);
        stack.Children.Add(new TextBlock { Text = "密码", FontSize = 13 });
        stack.Children.Add(passwordBox);
        stack.Children.Add(errorText);
        stack.Children.Add(okBtn);

        var dialog = new Window
        {
            Title = "注册新账号",
            Content = stack,
            Width = 320,
            Height = 320,
            WindowStartupLocation = WindowStartupLocation.CenterScreen,
            ResizeMode = ResizeMode.NoResize,
            ShowInTaskbar = false,
            Topmost = true
        };

        okBtn.Click += async (_, _) =>
        {
            if (string.IsNullOrWhiteSpace(usernameBox.Text) || string.IsNullOrWhiteSpace(passwordBox.Password))
            {
                errorText.Text = "用户名和密码不能为空";
                return;
            }
            okBtn.IsEnabled = false;
            var result = await _api!.RegisterAsync(usernameBox.Text, passwordBox.Password,
                string.IsNullOrWhiteSpace(nicknameBox.Text) ? usernameBox.Text : nicknameBox.Text);
            if (result.Success)
            {
                MessageBox.Show("注册成功，请等待管理员审核", "提示");
                dialog.Close();
            }
            else
            {
                errorText.Text = result.Message;
                okBtn.IsEnabled = true;
            }
        };

        dialog.ShowDialog();
    }

    private async void ShowMainWindow()
    {
        try
        {
            // 连接 SignalR（如果已连接则跳过）
            try { await _signalR!.ConnectAsync(ServerUrl); } catch { }

            var mainVm = new MainViewModel(_api!, _signalR!);
            mainVm.OnLogout += () =>
            {
                // 退出登录：先弹出登录窗口，再关掉主窗口（防止应用自动退出）
                ShowLoginWindow();
                var windows = Current.Windows.Cast<Window>().ToList();
                foreach (Window w in windows)
                    if (w is not LoginWindow && w.IsVisible)
                        w.Close();
            };

            var mainWindow = new MainWindow(mainVm);
            mainWindow.Show();

            // 窗口已成功显示，再关掉登录窗口
            var loginWindows = Current.Windows.Cast<Window>()
                .Where(w => w is LoginWindow).ToList();
            foreach (var w in loginWindows)
                w.Close();

            _ = mainVm.LoadFriendsAsync();
            _ = mainVm.LoadPendingRequestsAsync();
        }
        catch (Exception ex)
        {
            MessageBox.Show("打开主窗口失败: " + ex.Message, "错误",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    public static new App Current => (App)Application.Current;
}
