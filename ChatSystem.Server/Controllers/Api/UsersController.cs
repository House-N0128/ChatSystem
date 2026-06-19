using ChatSystem.Core.DTOs;
using ChatSystem.Core.Enums;
using ChatSystem.Data.Repositories;
using ChatSystem.Server.Helpers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ChatSystem.Server.Controllers.Api;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class UsersController : ControllerBase
{
    private readonly IUserRepository _userRepo;

    public UsersController(IUserRepository userRepo)
    {
        _userRepo = userRepo;
    }

    [HttpGet("search")]
    public async Task<IActionResult> Search([FromQuery] string keyword = "")
    {
        var (userId, _, _, _) = JwtHelper.ParseToken(User);
        var users = await _userRepo.SearchUsersAsync(keyword, userId);
        var dtos = users.Select(u => new UserDTO
        {
            Id = u.Id,
            Username = u.Username,
            Nickname = u.Nickname,
            Status = u.Status,
            CreatedAt = u.CreatedAt
        }).ToList();

        return Ok(ApiResponse<List<UserDTO>>.Ok(dtos));
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var user = await _userRepo.GetByIdAsync(id);
        if (user == null)
            return Ok(ApiResponse.Fail("用户不存在"));

        return Ok(ApiResponse<UserDTO>.Ok(new UserDTO
        {
            Id = user.Id,
            Username = user.Username,
            Nickname = user.Nickname,
            Status = user.Status,
            CreatedAt = user.CreatedAt
        }));
    }

    [HttpPut("profile")]
    public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileDTO dto)
    {
        var (userId, _, _, _) = JwtHelper.ParseToken(User);
        var user = await _userRepo.GetByIdAsync(userId);
        if (user == null)
            return Ok(ApiResponse.Fail("用户不存在"));

        user.Nickname = dto.Nickname;
        _userRepo.Update(user);
        await _userRepo.SaveChangesAsync();

        return Ok(ApiResponse.Ok("昵称已更新"));
    }

    [HttpPut("password")]
    public async Task<IActionResult> UpdatePassword([FromBody] UpdatePasswordDTO dto)
    {
        var (userId, _, _, _) = JwtHelper.ParseToken(User);
        var user = await _userRepo.GetByIdAsync(userId);
        if (user == null)
            return Ok(ApiResponse.Fail("用户不存在"));

        if (!BCrypt.Net.BCrypt.Verify(dto.OldPassword, user.PasswordHash))
            return Ok(ApiResponse.Fail("原密码错误"));

        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.NewPassword);
        _userRepo.Update(user);
        await _userRepo.SaveChangesAsync();

        return Ok(ApiResponse.Ok("密码已更新"));
    }
}
