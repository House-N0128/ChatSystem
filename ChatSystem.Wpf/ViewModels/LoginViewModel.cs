using System.Windows;
using System.Windows.Input;
using ChatSystem.Wpf.Services;

namespace ChatSystem.Wpf.ViewModels;

public class LoginViewModel : BaseViewModel
{
    private readonly ApiService _api;
    private readonly SignalRService _signalR;
    private readonly string _serverUrl;

    public LoginViewModel(ApiService api, SignalRService signalR, string serverUrl)
    {
        _api = api;
        _signalR = signalR;
        _serverUrl = serverUrl;
        LoginCommand = new RelayCommand(async _ => await LoginAsync(), _ => CanLogin);
        GoRegisterCommand = new RelayCommand(_ => OnNavigateToRegister?.Invoke());
    }

    private string _username = "";
    public string Username { get => _username; set { SetField(ref _username, value); OnPropertyChanged(nameof(CanLogin)); } }

    private string _password = "";
    public string Password { get => _password; set { SetField(ref _password, value); OnPropertyChanged(nameof(CanLogin)); } }

    private string _errorMessage = "";
    public string ErrorMessage { get => _errorMessage; set => SetField(ref _errorMessage, value); }

    public bool CanLogin => !string.IsNullOrWhiteSpace(Username) && !string.IsNullOrWhiteSpace(Password);

    public ICommand LoginCommand { get; }
    public ICommand GoRegisterCommand { get; }
    public event Action? OnNavigateToRegister;

    private bool _isLoggingIn;
    public bool IsLoggingIn { get => _isLoggingIn; set => SetField(ref _isLoggingIn, value); }

    private async Task LoginAsync()
    {
        ErrorMessage = "";
        IsLoggingIn = true;
        try
        {
            var result = await _api.LoginAsync(Username, Password);
            if (result.Success && result.Data != null)
            {
                AuthService.SaveAuth(result.Data.Token, result.Data.User);
                await _signalR.ConnectAsync(_serverUrl);
                OnLoginSuccess?.Invoke();
            }
            else
            {
                ErrorMessage = result.Message;
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = "连接服务器失败: " + ex.Message;
        }
        finally { IsLoggingIn = false; }
    }

    public event Action? OnLoginSuccess;
}

public class RelayCommand : ICommand
{
    private readonly Action<object?> _execute;
    private readonly Func<object?, bool>? _canExecute;

    public RelayCommand(Action<object?> execute, Func<object?, bool>? canExecute = null)
    {
        _execute = execute;
        _canExecute = canExecute;
    }

    public event EventHandler? CanExecuteChanged;
    public bool CanExecute(object? param) => _canExecute?.Invoke(param) ?? true;
    public void Execute(object? param) => _execute(param);
    public void RaiseCanExecuteChanged() => CanExecuteChanged?.Invoke(this, EventArgs.Empty);
}
