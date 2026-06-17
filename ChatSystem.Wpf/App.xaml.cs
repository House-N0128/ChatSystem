using System.Windows;
using ChatSystem.Wpf.Services;
using ChatSystem.Wpf.ViewModels;
using ChatSystem.Wpf.Views;

namespace ChatSystem.Wpf;

public partial class App : Application
{
    private const string ServerUrl = "https://localhost:5000";

    private ApiService? _api;
    private SignalRService? _signalR;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        _api = new ApiService(ServerUrl);
        _signalR = new SignalRService();

        // 尝试加载已保存的认证信息
        if (AuthService.LoadAuth())
        {
            // 已有Token，尝试连接SignalR并进入主窗口
            ShowMainWindow();
        }
        else
        {
            ShowLoginWindow();
        }
    }

    private void ShowLoginWindow()
    {
        var loginVm = new LoginViewModel(_api!, _signalR!, ServerUrl);
        loginVm.OnLoginSuccess += ShowMainWindow;

        var loginWindow = new LoginWindow(loginVm);
        loginWindow.Show();
    }

    private void ShowMainWindow()
    {
        // 关闭所有未关闭的登录窗口
        foreach (Window w in Current.Windows)
            if (w is LoginWindow) w.Close();

        var mainVm = new MainViewModel(_api!, _signalR!);
        mainVm.OnLogout += () =>
        {
            foreach (Window w in Current.Windows)
                if (w is not LoginWindow) w.Close();
            ShowLoginWindow();
        };

        var mainWindow = new MainWindow(mainVm);
        mainWindow.Show();
        _ = mainVm.LoadFriendsAsync();
        _ = mainVm.LoadPendingRequestsAsync();
    }

    public static new App Current => (App)Application.Current;
}
