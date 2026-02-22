namespace VetRandevu.Api.Dtos;

public class CreateSlotRequest
{
    public DateTime StartUtc { get; set; }
    public DateTime EndUtc { get; set; }
}
