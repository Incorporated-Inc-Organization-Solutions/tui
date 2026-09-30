using System.ComponentModel.DataAnnotations;

namespace tui.Contracts;

public sealed class CreateTestRecordRequest
{
    [Required(ErrorMessage = "Naam is verplicht.")]
    [StringLength(200, ErrorMessage = "Naam mag maximaal 200 tekens bevatten.")]
    public string? Name { get; init; }
}

public sealed record TestRecordResponse(int Id, string Name, DateTime CreatedAt);
