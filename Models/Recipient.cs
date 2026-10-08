using System.ComponentModel.DataAnnotations;

namespace tui.Models;

public class Recipient
{
    public int Id { get; set; }

    [MaxLength(200)]
    public required string Name { get; set; }

    public ICollection<Invoice> Invoices { get; set; } = [];
}
