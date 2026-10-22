using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace tui.Models;

public class TicketStatus
{
    [Key]
    public int StatusId { get; set; }

    [MaxLength(50)]
    public string Name { get; set; } = string.Empty;

    // Navigation properties
    public ICollection<ComplaintTicket> ComplaintTickets { get; set; } = [];
}

public class TicketPriority
{
    [Key]
    public int PriorityId { get; set; }

    [MaxLength(50)]
    public string Name { get; set; } = string.Empty;

    // Navigation properties
    public ICollection<ComplaintTicket> ComplaintTickets { get; set; } = [];
}

public class TicketSolution
{
    [Key]
    public int SolutionId { get; set; }

    [MaxLength(255)]
    public string Description { get; set; } = string.Empty;

    // Navigation properties
    public ICollection<ComplaintTicket> ComplaintTickets { get; set; } = [];
}

public class SLA
{
    [Key]
    public int SLAId { get; set; }

    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    public int DurationMinutes { get; set; }

    public int? WarningMinutes { get; set; }

    public bool Active { get; set; }

    // Navigation properties
    public ICollection<ComplaintTicket> ComplaintTickets { get; set; } = [];
    public ICollection<BookingSLA> BookingSLAs { get; set; } = [];
}

public class BookingSLA
{
    [Key]
    public int BookingSLAId { get; set; }

    public int BookingId { get; set; }

    public int SLAId { get; set; }

    public DateTime StartAt { get; set; }

    public DateTime DeadlineAt { get; set; }

    [MaxLength(30)]
    public string? Status { get; set; }

    public DateTime? CompletedAt { get; set; }

    // Navigation properties
    public Booking Booking { get; set; } = null!;
    public SLA SLA { get; set; } = null!;
}

public class ComplaintTicket
{
    [Key]
    public int TicketId { get; set; }

    public int ClientId { get; set; }

    public int? StaffUserId { get; set; }

    public int? BookingId { get; set; }

    public int StatusId { get; set; }

    public int PriorityId { get; set; }

    public int? SolutionId { get; set; }

    public int? SLAId { get; set; }

    [MaxLength(150)]
    public string Topic { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public DateTime TicketDate { get; set; }

    // Navigation properties
    public Client Client { get; set; } = null!;
    public Booking? Booking { get; set; }
    public TicketStatus Status { get; set; } = null!;
    public TicketPriority Priority { get; set; } = null!;
    public TicketSolution? Solution { get; set; }
    public SLA? SLA { get; set; }
    public AppUser? StaffUser { get; set; }
    public ICollection<TicketAction> TicketActions { get; set; } = [];
    public ICollection<Feedback> Feedbacks { get; set; } = [];
}

public class TicketAction
{
    [Key]
    public int TicketActionId { get; set; }

    public int TicketId { get; set; }

    [MaxLength(255)]
    public string Action { get; set; } = string.Empty;

    public DateTime ActionDate { get; set; }

    // Navigation properties
    public ComplaintTicket Ticket { get; set; } = null!;
}

public class Feedback
{
    [Key]
    public int FeedbackId { get; set; }

    public int TicketId { get; set; }

    // 1-5 stars (integer(1,5) in the first ERD version)
    [Range(1, 5)]
    public byte StarRating { get; set; }

    public string? Description { get; set; }

    // Navigation properties
    public ComplaintTicket Ticket { get; set; } = null!;
}
