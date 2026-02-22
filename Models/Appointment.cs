namespace VetRandevu.Api.Models;

public class Appointment
{
    public Guid Id { get; set; }
    public string UserId { get; set; } = string.Empty;
    public Guid ClinicId { get; set; }
    public Guid PetId { get; set; }
    public Guid ServiceId { get; set; }
    public DateTime StartUtc { get; set; }
    public DateTime EndUtc { get; set; }
    public AppointmentStatus Status { get; set; }
}
