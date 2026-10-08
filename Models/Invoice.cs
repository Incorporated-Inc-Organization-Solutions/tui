using System.ComponentModel.DataAnnotations;

namespace tui.Models;

public class Invoice
{
    public int Id { get; set; }

    [MaxLength(32)]
    public required string InternalReference { get; set; }

    public int RecipientId { get; set; }

    public Recipient Recipient { get; set; } = null!;

    public DateOnly InvoiceDate { get; set; }

    [MaxLength(1_000)]
    public required string Description { get; set; }

    public decimal TotalAmount { get; set; }

    public InvoicePaymentStatus PaymentStatus { get; set; } = InvoicePaymentStatus.Openstaand;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
