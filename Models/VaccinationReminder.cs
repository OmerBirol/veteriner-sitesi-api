namespace VetRandevu.Api.Models;

public class VaccinationReminder
{
    public Guid Id { get; set; }
    public Guid VaccinationRecordId { get; set; }
    public DateTime SentUtc { get; set; }
}
