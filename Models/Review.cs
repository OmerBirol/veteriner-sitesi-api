namespace VetRandevu.Api.Models;

public class Review
{
    public Guid Id { get; set; }
    public Guid ClinicId { get; set; }
    public string UserId { get; set; } = string.Empty;
    public int Rating { get; set; }
    public string Comment { get; set; } = string.Empty;
    public DateTime CreatedAtUtc { get; set; }
    public bool IsRemoved { get; set; }
    public string? RemovedReason { get; set; }
    public string? OriginalComment { get; set; }
    public DateTime? ModeratedAtUtc { get; set; }
}
