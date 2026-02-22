namespace VetRandevu.Api.Models;

public class ReviewReport
{
    public Guid Id { get; set; }
    public Guid ReviewId { get; set; }
    public string ReporterUserId { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;
    public ReviewReportStatus Status { get; set; }
    public DateTime CreatedAtUtc { get; set; }
}
