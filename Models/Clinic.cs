namespace VetRandevu.Api.Models;

public class Clinic
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string? ImageUrl { get; set; }
    public double Rating { get; set; }
    public bool IsApproved { get; set; }
    public string? OwnerUserId { get; set; }
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
}
