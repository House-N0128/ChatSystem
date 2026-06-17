using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using ChatSystem.Core.DTOs;

namespace ChatSystem.Wpf.Services;

public class ApiService
{
    private readonly HttpClient _http;
    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public ApiService(string baseUrl = "https://localhost:5000")
    {
        _http = new HttpClient { BaseAddress = new Uri(baseUrl) };
        _http.DefaultRequestHeaders.Accept.Add(
            new MediaTypeWithQualityHeaderValue("application/json"));
    }

    private void SetAuth()
    {
        if (!string.IsNullOrEmpty(AuthService.AccessToken))
            _http.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", AuthService.AccessToken);
    }

    // ===== Auth =====
    public async Task<ApiResponse<LoginResponseDTO>> LoginAsync(string username, string password)
    {
        var dto = new { username, password };
        var resp = await _http.PostAsJsonAsync("/api/auth/login", dto);
        var json = await resp.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<ApiResponse<LoginResponseDTO>>(json, JsonOpts)!;
    }

    public async Task<ApiResponse> RegisterAsync(string username, string password, string nickname)
    {
        var dto = new { username, password, nickname };
        var resp = await _http.PostAsJsonAsync("/api/auth/register", dto);
        var json = await resp.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<ApiResponse>(json, JsonOpts)!;
    }

    // ===== Friends =====
    public async Task<ApiResponse<List<FriendDTO>>> GetFriendsAsync()
    {
        SetAuth();
        var json = await _http.GetStringAsync("/api/friends");
        return JsonSerializer.Deserialize<ApiResponse<List<FriendDTO>>>(json, JsonOpts)!;
    }

    public async Task<ApiResponse> SendFriendRequestAsync(int toUserId)
    {
        SetAuth();
        var resp = await _http.PostAsJsonAsync("/api/friends/request", new { toUserId });
        var json = await resp.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<ApiResponse>(json, JsonOpts)!;
    }

    public async Task<ApiResponse<List<FriendRequestDTO>>> GetPendingRequestsAsync()
    {
        SetAuth();
        var json = await _http.GetStringAsync("/api/friends/requests/pending");
        return JsonSerializer.Deserialize<ApiResponse<List<FriendRequestDTO>>>(json, JsonOpts)!;
    }

    public async Task<ApiResponse> AcceptRequestAsync(int requestId)
    {
        SetAuth();
        var resp = await _http.PostAsync($"/api/friends/requests/{requestId}/accept", null);
        var json = await resp.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<ApiResponse>(json, JsonOpts)!;
    }

    public async Task<ApiResponse> RejectRequestAsync(int requestId)
    {
        SetAuth();
        var resp = await _http.PostAsync($"/api/friends/requests/{requestId}/reject", null);
        var json = await resp.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<ApiResponse>(json, JsonOpts)!;
    }

    public async Task<ApiResponse> RemoveFriendAsync(int friendId)
    {
        SetAuth();
        var resp = await _http.DeleteAsync($"/api/friends/{friendId}");
        var json = await resp.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<ApiResponse>(json, JsonOpts)!;
    }

    // ===== Messages =====
    public async Task<ApiResponse<PagedResult<MessageDTO>>> GetMessagesAsync(int userId, int page = 1, int pageSize = 50)
    {
        SetAuth();
        var json = await _http.GetStringAsync($"/api/messages/private/{userId}?page={page}&pageSize={pageSize}");
        return JsonSerializer.Deserialize<ApiResponse<PagedResult<MessageDTO>>>(json, JsonOpts)!;
    }

    // ===== Users =====
    public async Task<ApiResponse<List<UserDTO>>> SearchUsersAsync(string keyword)
    {
        SetAuth();
        var json = await _http.GetStringAsync($"/api/users/search?keyword={Uri.EscapeDataString(keyword)}");
        return JsonSerializer.Deserialize<ApiResponse<List<UserDTO>>>(json, JsonOpts)!;
    }

    // ===== Admin =====
    public async Task<ApiResponse<List<UserDTO>>> GetPendingUsersAsync()
    {
        SetAuth();
        var json = await _http.GetStringAsync("/api/admin/users/pending");
        return JsonSerializer.Deserialize<ApiResponse<List<UserDTO>>>(json, JsonOpts)!;
    }

    public async Task<ApiResponse> ApproveUserAsync(int userId)
    {
        SetAuth();
        var resp = await _http.PostAsync($"/api/admin/users/{userId}/approve", null);
        var json = await resp.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<ApiResponse>(json, JsonOpts)!;
    }

    public async Task<ApiResponse> BanUserAsync(int userId)
    {
        SetAuth();
        var resp = await _http.PostAsync($"/api/admin/users/{userId}/ban", null);
        var json = await resp.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<ApiResponse>(json, JsonOpts)!;
    }

    public async Task<ApiResponse> UnbanUserAsync(int userId)
    {
        SetAuth();
        var resp = await _http.PostAsync($"/api/admin/users/{userId}/unban", null);
        var json = await resp.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<ApiResponse>(json, JsonOpts)!;
    }

    public async Task<ApiResponse<PagedResult<MessageDTO>>> SearchMessagesAsync(
        string? keyword = null, DateTime? from = null, DateTime? to = null, int page = 1, int pageSize = 20)
    {
        SetAuth();
        var url = $"/api/admin/messages?page={page}&pageSize={pageSize}";
        if (!string.IsNullOrWhiteSpace(keyword)) url += $"&keyword={Uri.EscapeDataString(keyword)}";
        if (from.HasValue) url += $"&from={from.Value:yyyy-MM-dd}";
        if (to.HasValue) url += $"&to={to.Value:yyyy-MM-dd}";

        var json = await _http.GetStringAsync(url);
        return JsonSerializer.Deserialize<ApiResponse<PagedResult<MessageDTO>>>(json, JsonOpts)!;
    }

    public async Task<ApiResponse> ForceDeleteMessageAsync(long messageId)
    {
        SetAuth();
        var resp = await _http.DeleteAsync($"/api/admin/messages/{messageId}");
        var json = await resp.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<ApiResponse>(json, JsonOpts)!;
    }
}
