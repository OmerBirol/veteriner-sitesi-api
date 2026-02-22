namespace VetRandevu.Api.Dtos;

public class CreatePetRequest
{
    public string OwnerName { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Species { get; set; } = string.Empty;
    public int Age { get; set; }
}
