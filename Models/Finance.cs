using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace tui.Models;

public class Party
{
    [Key]
    public int PartyId { get; set; }

    public int? ClientId { get; set; }

    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(20)]
    public string PartyType { get; set; } = string.Empty;

    [MaxLength(100)]
    public string? Email { get; set; }

    [MaxLength(20)]
    public string? Phone { get; set; }

    [MaxLength(255)]
    public string? Address { get; set; }

    [MaxLength(50)]
    public string? VatNumber { get; set; }

    // Navigation properties
    public Client? Client { get; set; }
    public ICollection<Invoice> Invoices { get; set; } = [];
}

public class Account
{
    [Key]
    public int AccountId { get; set; }

    [MaxLength(20)]
    public string AccountNumber { get; set; } = string.Empty;

    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(50)]
    public string AccountType { get; set; } = string.Empty;

    // Navigation properties
    public ICollection<InvoiceLine> InvoiceLines { get; set; } = [];
    public ICollection<JournalEntryLine> JournalEntryLines { get; set; } = [];
}

public class TaxRate
{
    [Key]
    public int TaxRateId { get; set; }

    [MaxLength(50)]
    public string Name { get; set; } = string.Empty;

    [Precision(5, 2)]
    public decimal Percentage { get; set; }

    // Navigation properties
    public ICollection<InvoiceLine> InvoiceLines { get; set; } = [];
}

public class Invoice
{
    [Key]
    public int InvoiceId { get; set; }

    public int PartyId { get; set; }

    public int? BookingId { get; set; }

    [MaxLength(50)]
    public string InvoiceNumber { get; set; } = string.Empty;

    [MaxLength(20)]
    public string InvoiceType { get; set; } = string.Empty;

    public DateOnly InvoiceDate { get; set; }

    public DateOnly? DueDate { get; set; }

    [MaxLength(30)]
    public string Status { get; set; } = string.Empty;

    [Precision(10, 2)]
    public decimal TotalAmount { get; set; }

    // Navigation properties
    public Party Party { get; set; } = null!;
    public Booking? Booking { get; set; }
    public ICollection<InvoiceLine> InvoiceLines { get; set; } = [];
    public ICollection<Payment> Payments { get; set; } = [];
    public ICollection<JournalEntry> JournalEntries { get; set; } = [];
}

public class InvoiceLine
{
    [Key]
    public int InvoiceLineId { get; set; }

    public int InvoiceId { get; set; }

    public int AccountId { get; set; }

    public int? TaxRateId { get; set; }

    [MaxLength(255)]
    public string Description { get; set; } = string.Empty;

    [Precision(10, 2)]
    public decimal Quantity { get; set; }

    [Precision(10, 2)]
    public decimal Price { get; set; }

    // Navigation properties
    public Invoice Invoice { get; set; } = null!;
    public Account Account { get; set; } = null!;
    public TaxRate? TaxRate { get; set; }
}

public class Payment
{
    [Key]
    public int PaymentId { get; set; }

    public int InvoiceId { get; set; }

    public DateOnly PaymentDate { get; set; }

    [Precision(10, 2)]
    public decimal Amount { get; set; }

    [MaxLength(50)]
    public string? Method { get; set; }

    [MaxLength(100)]
    public string? Reference { get; set; }

    // Navigation properties
    public Invoice Invoice { get; set; } = null!;
}

public class JournalEntry
{
    [Key]
    public int JournalEntryId { get; set; }

    public int? InvoiceId { get; set; }

    public DateOnly EntryDate { get; set; }

    [MaxLength(255)]
    public string? Description { get; set; }

    // Navigation properties
    public Invoice? Invoice { get; set; }
    public ICollection<JournalEntryLine> JournalEntryLines { get; set; } = [];
}

public class JournalEntryLine
{
    [Key]
    public int JournalEntryLineId { get; set; }

    public int JournalEntryId { get; set; }

    public int AccountId { get; set; }

    [Precision(10, 2)]
    public decimal Debit { get; set; }

    [Precision(10, 2)]
    public decimal Credit { get; set; }

    [MaxLength(255)]
    public string? Description { get; set; }

    // Navigation properties
    public JournalEntry JournalEntry { get; set; } = null!;
    public Account Account { get; set; } = null!;
}
