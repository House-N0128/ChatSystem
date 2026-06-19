using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using ChatSystem.Core.Models;
using Microsoft.IdentityModel.Tokens;

namespace ChatSystem.Server.Helpers;

public static class JwtHelper
{
    public static string GenerateToken(User user, IConfiguration config)
    {
        var key = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(config["Jwt:Key"]!));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Name, user.Username),
            new Claim("nickname", user.Nickname),
            new Claim(ClaimTypes.Role, ((int)user.Role).ToString())
        };

        var expireHours = double.Parse(config["Jwt:ExpireHours"] ?? "24");
        var token = new JwtSecurityToken(
            issuer: config["Jwt:Issuer"],
            audience: config["Jwt:Audience"],
            claims: claims,
            expires: DateTime.Now.AddHours(expireHours),
            signingCredentials: creds);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public static (int userId, string username, string nickname, int role) ParseToken(
        ClaimsPrincipal principal)
    {
        var userId = int.Parse(principal.FindFirst(ClaimTypes.NameIdentifier)!.Value);
        var username = principal.FindFirst(ClaimTypes.Name)!.Value;
        var nickname = principal.FindFirst("nickname")?.Value ?? username;
        var role = int.Parse(principal.FindFirst(ClaimTypes.Role)!.Value);
        return (userId, username, nickname, role);
    }
}
