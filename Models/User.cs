using System.ComponentModel.DataAnnotations;

namespace tui.Models;

public class User
{
    public int Id { get; set; }

    [MaxLength(50)]
    public required string Username { get; set; }

    [MaxLength(254)]
    public required string Email { get; set; }

    [MaxLength(512)]
    public required string PasswordHash { get; set; }

    public UserRole Role { get; set; } = UserRole.User;

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
