using System.IO;
using System.Text.Json;
using ChatSystem.Core.DTOs;

namespace ChatSystem.Wpf.Services;

public static class AuthService
{
    private static readonly string TokenFile =
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                     "ChatSystem", "auth.json");

    public static string? AccessToken { get; private set; }
    public static UserDTO? CurrentUser { get; private set; }

    public static void SaveAuth(string token, UserDTO user)
    {
        AccessToken = token;
        CurrentUser = user;

        var dir = Path.GetDirectoryName(TokenFile)!;
        if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);

        var data = new { Token = token, User = user };
        File.WriteAllText(TokenFile, JsonSerializer.Serialize(data));
    }

    public static bool LoadAuth()
    {
        if (!File.Exists(TokenFile)) return false;
        try
        {
            var json = File.ReadAllText(TokenFile);
            var data = JsonSerializer.Deserialize<AuthData>(json);
            if (data == null || string.IsNullOrEmpty(data.Token)) return false;

            AccessToken = data.Token;
            CurrentUser = data.User;
            return true;
        }
        catch { return false; }
    }

    public static void Clear()
    {
        AccessToken = null;
        CurrentUser = null;
        if (File.Exists(TokenFile)) File.Delete(TokenFile);
    }

    private class AuthData
    {
        public string Token { get; set; } = string.Empty;
        public UserDTO? User { get; set; }
    }
}
