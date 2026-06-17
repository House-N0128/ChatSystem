using ChatSystem.Core.DTOs;
using ChatSystem.Core.Enums;
using ChatSystem.Core.Models;
using ChatSystem.Data.Repositories;
using ChatSystem.Server.Helpers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ChatSystem.Server.Controllers.Api;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IUserRepository _userRepo;
    private readonly IConfiguration _config;

    public AuthController(IUserRepository userRepo, IConfiguration config)
    {
        _userRepo = userRepo;
        _config = config;
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterDTO dto)
    {
        var existing = await _userRepo.GetByUsernameAsync(dto.Username);
        if (existing != null)
            return Ok(ApiResponse.Fail("用户名已存在"));

        var user = new User
        {
            Username = dto.Username,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password),
            Nickname = dto.Nickname,
            Role = UserRole.User,
            Status = UserStatus.Pending,
            CreatedAt = DateTime.UtcNow
        };

        await _userRepo.AddAsync(user);
        await _userRepo.SaveChangesAsync();

        return Ok(ApiResponse.Ok("注册成功，请等待管理员审核"));
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginDTO dto)
    {
        var user = await _userRepo.GetByUsernameAsync(dto.Username);
        if (user == null)
            return Ok(ApiResponse.Fail("用户名或密码错误"));

        if (!BCrypt.Net.BCrypt.Verify(dto.Password, user.PasswordHash))
            return Ok(ApiResponse.Fail("用户名或密码错误"));

        if (user.Status == UserStatus.Pending)
            return Ok(ApiResponse.Fail("账号尚未通过审核，请等待管理员审批"));

        if (user.Status == UserStatus.Banned)
            return Ok(ApiResponse.Fail("账号已被封禁"));

        user.LastLoginAt = DateTime.UtcNow;
        _userRepo.Update(user);
        await _userRepo.SaveChangesAsync();

        var token = JwtHelper.GenerateToken(user, _config);
        var userDto = new UserDTO
        {
            Id = user.Id,
            Username = user.Username,
            Nickname = user.Nickname,
            Role = user.Role,
            Status = user.Status,
            CreatedAt = user.CreatedAt,
            LastLoginAt = user.LastLoginAt
        };

        return Ok(ApiResponse<LoginResponseDTO>.Ok(new LoginResponseDTO
        {
            Token = token,
            User = userDto
        }, "登录成功"));
    }

    [HttpGet("me")]
    [Authorize]
    public async Task<IActionResult> Me()
    {
        var (userId, _, _, _) = JwtHelper.ParseToken(User);
        var user = await _userRepo.GetByIdAsync(userId);
        if (user == null)
            return Ok(ApiResponse.Fail("用户不存在"));

        return Ok(ApiResponse<UserDTO>.Ok(new UserDTO
        {
            Id = user.Id,
            Username = user.Username,
            Nickname = user.Nickname,
            Role = user.Role,
            Status = user.Status,
            CreatedAt = user.CreatedAt,
            LastLoginAt = user.LastLoginAt
        }));
    }
}
