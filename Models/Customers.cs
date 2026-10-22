using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace tui.Models;

public class AppUser
{
    [Key]
    public int UserId { get; set; }

    [MaxLength(50)]
    public string FirstName { get; set; } = string.Empty;

    [MaxLength(50)]
    public string LastName { get; set; } = string.Empty;

    [MaxLength(200)]
    public string Email { get; set; } = string.Empty;

    [MaxLength(255)]
    public string PasswordHash { get; set; } = string.Empty;

    [MaxLength(20)]
    public string UserRole { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public Client? Client { get; set; }
    public ICollection<ComplaintTicket> AssignedTickets { get; set; } = [];
}

public class ClientType
{
    [Key]
    public int ClientTypeId { get; set; }

    [MaxLength(50)]
    public string Name { get; set; } = string.Empty;

    // Navigation properties
    public ICollection<Client> Clients { get; set; } = [];
}

public class Client
{
    [Key]
    public int ClientId { get; set; }

    public int? UserId { get; set; }

    public int? ClientTypeId { get; set; }

    [MaxLength(50)]
    public string FirstName { get; set; } = string.Empty;

    [MaxLength(50)]
    public string LastName { get; set; } = string.Empty;

    [MaxLength(255)]
    public string? Address { get; set; }

    [MaxLength(20)]
    public string? Phone { get; set; }

    // Navigation properties
    public AppUser? User { get; set; }
    public ClientType? ClientType { get; set; }
    public ICollection<Booking> Bookings { get; set; } = [];
    public ICollection<Traveler> Travelers { get; set; } = [];
    public ICollection<ComplaintTicket> ComplaintTickets { get; set; } = [];
    public Party? Party { get; set; }
}
