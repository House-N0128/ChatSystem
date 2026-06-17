using System.Collections.ObjectModel;
using System.Windows.Input;
using ChatSystem.Core.DTOs;
using ChatSystem.Wpf.Services;

namespace ChatSystem.Wpf.ViewModels;

public class AdminViewModel : BaseViewModel
{
    private readonly ApiService _api;

    public AdminViewModel(ApiService api)
    {
        _api = api;
        ApproveCommand = new RelayCommand(async p => await ApproveAsync((int)p!));
        BanCommand = new RelayCommand(async p => await BanAsync((int)p!));
        UnbanCommand = new RelayCommand(async p => await UnbanAsync((int)p!));
        DeleteMessageCommand = new RelayCommand(async p => await DeleteMessageAsync((long)p!));
        SearchMessagesCommand = new RelayCommand(async _ => await SearchMessagesAsync());
        LoadPendingCommand = new RelayCommand(async _ => await LoadPendingAsync());
    }

    public ObservableCollection<UserDTO> PendingUsers { get; } = new();
    public ObservableCollection<MessageDTO> SearchMessageResults { get; } = new();

    private string _msgKeyword = "";
    public string MsgKeyword { get => _msgKeyword; set => SetField(ref _msgKeyword, value); }

    public ICommand ApproveCommand { get; }
    public ICommand BanCommand { get; }
    public ICommand UnbanCommand { get; }
    public ICommand DeleteMessageCommand { get; }
    public ICommand SearchMessagesCommand { get; }
    public ICommand LoadPendingCommand { get; }

    public async Task LoadPendingAsync()
    {
        var result = await _api.GetPendingUsersAsync();
        PendingUsers.Clear();
        if (result.Success && result.Data != null)
            foreach (var u in result.Data) PendingUsers.Add(u);
    }

    private async Task ApproveAsync(int userId)
    {
        await _api.ApproveUserAsync(userId);
        await LoadPendingAsync();
    }

    private async Task BanAsync(int userId)
    {
        await _api.BanUserAsync(userId);
    }

    private async Task UnbanAsync(int userId)
    {
        await _api.UnbanUserAsync(userId);
    }

    private async Task DeleteMessageAsync(long msgId)
    {
        await _api.ForceDeleteMessageAsync(msgId);
    }

    private async Task SearchMessagesAsync()
    {
        var result = await _api.SearchMessagesAsync(MsgKeyword);
        SearchMessageResults.Clear();
        if (result.Success && result.Data != null)
            foreach (var m in result.Data.Items) SearchMessageResults.Add(m);
    }
}
