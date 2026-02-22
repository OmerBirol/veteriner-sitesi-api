namespace VetRandevu.Api.Dtos;

public class CreateVaccinationRecordRequest
{
    public Guid PetId { get; set; }
    public Guid ClinicId { get; set; }
    public string VaccineName { get; set; } = string.Empty;
    public DateTime AdministeredUtc { get; set; }
    public DateTime? NextDueUtc { get; set; }
    public string? Notes { get; set; }
}

public class VaccinationRecordResponse
{
    public Guid Id { get; set; }
    public Guid PetId { get; set; }
    public Guid ClinicId { get; set; }
    public string VaccineName { get; set; } = string.Empty;
    public DateTime AdministeredUtc { get; set; }
    public DateTime? NextDueUtc { get; set; }
    public string? Notes { get; set; }
    public DateTime CreatedUtc { get; set; }
}
