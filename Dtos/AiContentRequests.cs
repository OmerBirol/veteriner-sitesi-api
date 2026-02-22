namespace VetRandevu.Api.Dtos;

public class AiClinicDescriptionRequest
{
    public string ClinicName { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string? FocusAreas { get; set; }
    public string? Tone { get; set; }
}

public class AiClinicHighlightsRequest
{
    public string ClinicName { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
}

public class AiServicesRequest
{
    public string ClinicName { get; set; } = string.Empty;
    public string? SpeciesFocus { get; set; }
}

public class AiTestimonialsRequest
{
    public string ClinicName { get; set; } = string.Empty;
}

public class AiAppointmentMessageRequest
{
    public string ClinicName { get; set; } = string.Empty;
    public string OwnerName { get; set; } = string.Empty;
    public string PetName { get; set; } = string.Empty;
    public DateTime AppointmentUtc { get; set; }
    public string? Notes { get; set; }
}

public class AiContentResponse
{
    public string Content { get; set; } = string.Empty;
    public List<string>? Items { get; set; }
}
