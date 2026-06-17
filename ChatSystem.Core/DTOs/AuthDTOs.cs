using System.ComponentModel.DataAnnotations;

namespace ChatSystem.Core.DTOs;

public class RegisterDTO
{
    [Required, MinLength(3), MaxLength(50)]
    public string Username { get; set; } = string.Empty;

    [Required, MinLength(6), MaxLength(100)]
    public string Password { get; set; } = string.Empty;

    [Required, MaxLength(50)]
    public string Nickname { get; set; } = string.Empty;
}

public class LoginDTO
{
    [Required]
    public string Username { get; set; } = string.Empty;

    [Required]
    public string Password { get; set; } = string.Empty;
}

public class LoginResponseDTO
{
    public string Token { get; set; } = string.Empty;
    public UserDTO User { get; set; } = null!;
}
