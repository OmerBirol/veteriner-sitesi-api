namespace VetRandevu.Api.Models;

public class Pet
{
    public Guid Id { get; set; }
    public string? OwnerUserId { get; set; }
    public string OwnerName { get; set; } = string.Empty;
    public string? OwnerEmail { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Species { get; set; } = string.Empty;
    public int Age { get; set; }
}
