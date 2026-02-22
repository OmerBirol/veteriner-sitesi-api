namespace VetRandevu.Api.Dtos;

public class CreateAppointmentRequest
{
    public Guid ClinicId { get; set; }
    public Guid PetId { get; set; }
    public Guid ServiceId { get; set; }
    public DateTime StartUtc { get; set; }
}
