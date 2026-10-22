using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace tui.Models;

public class TravelerType
{
    [Key]
    public int TravelerTypeId { get; set; }

    [MaxLength(50)]
    public string Name { get; set; } = string.Empty;

    // Navigation properties
    public ICollection<Tariff> Tariffs { get; set; } = [];
    public ICollection<Traveler> Travelers { get; set; } = [];
}

public class Tariff
{
    [Key]
    public int TariffId { get; set; }

    public int TripId { get; set; }

    public int TravelerTypeId { get; set; }

    [Precision(10, 2)]
    public decimal Amount { get; set; }

    public DateOnly ValidFrom { get; set; }

    public DateOnly? ValidUntil { get; set; }

    // Navigation properties
    public Trip Trip { get; set; } = null!;
    public TravelerType TravelerType { get; set; } = null!;
    public ICollection<BookingItem> BookingItems { get; set; } = [];
}

public class Booking
{
    [Key]
    public int BookingId { get; set; }

    public int ClientId { get; set; }

    public int TripId { get; set; }

    public DateTime BookingDate { get; set; }

    [MaxLength(30)]
    public string Status { get; set; } = string.Empty;

    // Navigation properties
    public Client Client { get; set; } = null!;
    public Trip Trip { get; set; } = null!;
    public ICollection<BookingItem> BookingItems { get; set; } = [];
    public ICollection<Traveler> Travelers { get; set; } = [];
    public ICollection<ComplaintTicket> ComplaintTickets { get; set; } = [];
    public ICollection<BookingSLA> BookingSLAs { get; set; } = [];
    public ICollection<Invoice> Invoices { get; set; } = [];
}

public class BookingItem
{
    [Key]
    public int BookingItemId { get; set; }

    public int BookingId { get; set; }

    public int TariffId { get; set; }

    public int Quantity { get; set; }

    [Precision(10, 2)]
    public decimal Price { get; set; }

    // Navigation properties
    public Booking Booking { get; set; } = null!;
    public Tariff Tariff { get; set; } = null!;
}

public class Traveler
{
    [Key]
    public int TravelerId { get; set; }

    public int TravelerTypeId { get; set; }

    public int BookingId { get; set; }

    public int? ClientId { get; set; }

    [MaxLength(50)]
    public string FirstName { get; set; } = string.Empty;

    [MaxLength(50)]
    public string LastName { get; set; } = string.Empty;

    [MaxLength(20)]
    public string? Phone { get; set; }

    // Navigation properties
    public TravelerType TravelerType { get; set; } = null!;
    public Booking Booking { get; set; } = null!;
    public Client? Client { get; set; }
}
