using System.ComponentModel.DataAnnotations;
using tui.Models;

namespace tui.Contracts;

public sealed class RegisterRequest
{
    [Required(ErrorMessage = "Gebruikersnaam is verplicht.")]
    [StringLength(50, MinimumLength = 3, ErrorMessage = "Gebruikersnaam moet tussen 3 en 50 tekens lang zijn.")]
    public string? Username { get; init; }

    [Required(ErrorMessage = "E-mailadres is verplicht.")]
    [EmailAddress(ErrorMessage = "Vul een geldig e-mailadres in.")]
    [StringLength(254)]
    public string? Email { get; init; }

    [Required(ErrorMessage = "Wachtwoord is verplicht.")]
    [StringLength(100, MinimumLength = 8, ErrorMessage = "Wachtwoord moet minimaal 8 tekens lang zijn.")]
    public string? Password { get; init; }

    [Required(ErrorMessage = "Herhaal het wachtwoord.")]
    public string? ConfirmPassword { get; init; }
}

public sealed class LoginRequest
{
    [Required(ErrorMessage = "E-mailadres of gebruikersnaam is verplicht.")]
    public string? Identifier { get; init; }

    [Required(ErrorMessage = "Wachtwoord is verplicht.")]
    public string? Password { get; init; }
}

public sealed record AuthUserResponse(int Id, string Username, string Email, UserRole Role);

public sealed record UserResponse(int Id, string Username, string Email, UserRole Role, bool IsActive, DateTime CreatedAt);

public sealed class ChangeUserRoleRequest
{
    [Required(ErrorMessage = "Rol is verplicht.")]
    public string? Role { get; init; }
}
