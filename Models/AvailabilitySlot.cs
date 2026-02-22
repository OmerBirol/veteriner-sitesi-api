namespace VetRandevu.Api.Models;

public class AvailabilitySlot
{
    public Guid Id { get; set; }
    public Guid ClinicId { get; set; }
    public DateTime StartUtc { get; set; }
    public DateTime EndUtc { get; set; }
    public bool IsBooked { get; set; }
}
