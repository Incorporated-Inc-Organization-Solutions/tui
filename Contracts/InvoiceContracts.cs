using System.ComponentModel.DataAnnotations;
using tui.Models;

namespace tui.Contracts;

public sealed class CreateInvoiceRequest
{
    [Range(1, int.MaxValue, ErrorMessage = "Kies een bestaande ontvanger.")]
    public int RecipientId { get; init; }

    [Required(ErrorMessage = "Factuurdatum is verplicht.")]
    public DateOnly? InvoiceDate { get; init; }

    [Required(ErrorMessage = "Omschrijving is verplicht.")]
    [StringLength(1_000, ErrorMessage = "Omschrijving mag maximaal 1000 tekens bevatten.")]
    public string? Description { get; init; }

    public decimal TotalAmount { get; init; }
}

public sealed record RecipientResponse(int Id, string Name);

public sealed record InvoiceResponse(
    int Id,
    string InternalReference,
    int RecipientId,
    string RecipientName,
    DateOnly InvoiceDate,
    string Description,
    decimal TotalAmount,
    InvoicePaymentStatus PaymentStatus,
    DateTime CreatedAt);
