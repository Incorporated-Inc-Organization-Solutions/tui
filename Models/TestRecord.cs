using System.ComponentModel.DataAnnotations;

namespace tui.Models;

public class TestRecord
{
    public int Id { get; set; }

    [MaxLength(200)]
    public required string Name { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
