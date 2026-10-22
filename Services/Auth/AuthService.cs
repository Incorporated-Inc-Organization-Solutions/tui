using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using tui.Data;
using tui.Models;

namespace tui.Services;

public record AuthResult(bool Success, string? Error = null);

public class AuthService(
    IDbContextFactory<ApplicationDbContext> dbFactory,
    IHttpContextAccessor httpContextAccessor,
    IPasswordHasher<User> passwordHasher,
    AuthenticationStateProvider authStateProvider)
{
    private const string AdminEmail = "ADMIN@ADMIN.COM";

    public async Task<AuthResult> RegisterAsync(string firstname, string lastname, string email, string password)
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        email = email.Trim().ToUpperInvariant();

        if (await db.Users.AnyAsync(u => u.Email == email))
        {
            return new AuthResult(false, "Email already in use.");
        }

        var user = new User
        {
            FirstName = firstname, LastName = lastname, Email = email, PasswordHash = "", CreatedAt = DateTime.UtcNow
        };
        user.PasswordHash = passwordHasher.HashPassword(user, password);

        db.Users.Add(user);
        await db.SaveChangesAsync();

        return new AuthResult(true);
    }

    public async Task<AuthResult> LoginAsync(string email, string password)
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        email = email.Trim().ToUpperInvariant();

        var user = await db.Users.SingleOrDefaultAsync(u => u.Email == email);

        if (user is null)
        {
            return new AuthResult(false, "Invalid email or password.");
        }

        var result = passwordHasher.VerifyHashedPassword(user, user.PasswordHash, password);

        if (result == PasswordVerificationResult.Failed)
        {
            return new AuthResult(false, "Invalid email or password.");
        }

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Email, user.Email),
            new(ClaimTypes.GivenName, user.FirstName),
            new(ClaimTypes.Surname, user.LastName),
            new(ClaimTypes.Name, $"{user.FirstName} {user.LastName}")
        };

        if (user.Email == AdminEmail)
        {
            claims.Add(new Claim(ClaimTypes.Role, "Admin"));
        }

        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        var principal = new ClaimsPrincipal(identity);

        if (httpContextAccessor.HttpContext is { } httpContext)
        {
            await httpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal);
        }

        return new AuthResult(true);
    }

    public async Task LogoutAsync()
    {
        if (httpContextAccessor.HttpContext is { } httpContext)
        {
            await httpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        }
    }
}