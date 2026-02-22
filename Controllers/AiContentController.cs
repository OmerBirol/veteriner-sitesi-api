using System.Linq;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using VetRandevu.Api.Dtos;

namespace VetRandevu.Api.Controllers;

[ApiController]
[Route("ai")]
public class AiContentController : ControllerBase
{
    private const string ModelName = "gemini-2.5-flash";
    private readonly IConfiguration _configuration;

    public AiContentController(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    [HttpPost("clinic-description")]
    public async Task<ActionResult<AiContentResponse>> GenerateClinicDescription([FromBody] AiClinicDescriptionRequest request)
    {
        var prompt =
            $"Bir veteriner kliniği için profesyonel ve güven verici bir tanıtım yazısı oluştur.\n" +
            $"Klinik adı: {request.ClinicName}\n" +
            $"Şehir: {request.City}\n" +
            $"Adres: {request.Address}\n" +
            $"Odak alanları: {request.FocusAreas ?? "Genel veteriner hizmetleri"}\n" +
            $"Ton: {request.Tone ?? "Samimi, profesyonel"}\n" +
            $"Tek paragraf, 4-6 cümle.";

        return await GenerateSingleContent(prompt);
    }

    [HttpPost("clinic-highlights")]
    public async Task<ActionResult<AiContentResponse>> GenerateClinicHighlights([FromBody] AiClinicHighlightsRequest request)
    {
        var prompt =
            $"Bir veteriner kliniği için öne çıkan özellikler listesi üret.\n" +
            $"Klinik adı: {request.ClinicName}\n" +
            $"Şehir: {request.City}\n" +
            $"Her satır tek madde olsun, 6-8 madde üret.";

        return await GenerateListContent(prompt);
    }

    [HttpPost("services")]
    public async Task<ActionResult<AiContentResponse>> GenerateServices([FromBody] AiServicesRequest request)
    {
        var prompt =
            $"Bir veteriner kliniği için hizmetler listesi üret.\n" +
            $"Klinik adı: {request.ClinicName}\n" +
            $"Odak: {request.SpeciesFocus ?? "Kedi, köpek ve egzotik hayvanlar"}\n" +
            $"Her satır tek hizmet adı olsun, 8-12 madde üret.";

        return await GenerateListContent(prompt);
    }

    [HttpPost("testimonials")]
    public async Task<ActionResult<AiContentResponse>> GenerateTestimonials([FromBody] AiTestimonialsRequest request)
    {
        var prompt =
            $"Veteriner kliniği için müşteri yorumları üret.\n" +
            $"Klinik adı: {request.ClinicName}\n" +
            $"Her satır tek yorum olsun. Format: İsim - Yorum. En az 6 yorum.";

        return await GenerateListContent(prompt);
    }

    [HttpPost("appointment-message")]
    public async Task<ActionResult<AiContentResponse>> GenerateAppointmentMessage([FromBody] AiAppointmentMessageRequest request)
    {
        var prompt =
            $"Aşağıdaki bilgilerle randevu bilgilendirme mesajı yaz.\n" +
            $"Klinik: {request.ClinicName}\n" +
            $"Sahip: {request.OwnerName}\n" +
            $"Evcil hayvan: {request.PetName}\n" +
            $"Randevu (UTC): {request.AppointmentUtc:O}\n" +
            $"Notlar: {request.Notes ?? "Yok"}\n" +
            $"Kısa ve net bir mesaj olsun.";

        return await GenerateSingleContent(prompt);
    }

    private async Task<ActionResult<AiContentResponse>> GenerateSingleContent(string prompt)
    {
        var apiKey = GetApiKey();
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            return BadRequest("Gemini API anahtarı bulunamadı.");
        }

        var responseText = await CallGeminiAsync(apiKey, prompt);
        if (responseText.errorMessage != null)
        {
            return BadRequest(responseText.errorMessage);
        }

        return Ok(new AiContentResponse
        {
            Content = responseText.content ?? string.Empty
        });
    }

    private async Task<ActionResult<AiContentResponse>> GenerateListContent(string prompt)
    {
        var apiKey = GetApiKey();
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            return BadRequest("Gemini API anahtarı bulunamadı.");
        }

        var responseText = await CallGeminiAsync(apiKey, prompt);
        if (responseText.errorMessage != null)
        {
            return BadRequest(responseText.errorMessage);
        }

        var items = responseText.content?
            .Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries)
            .Select(line => line.Trim().TrimStart('-', '•', '*').Trim())
            .Where(line => !string.IsNullOrWhiteSpace(line))
            .ToList();

        return Ok(new AiContentResponse
        {
            Content = responseText.content ?? string.Empty,
            Items = items
        });
    }

    private string? GetApiKey()
    {
        return _configuration["Gemini:ApiKey"] ?? Environment.GetEnvironmentVariable("GEMINI_API_KEY");
    }

    private async Task<(string? content, string? errorMessage)> CallGeminiAsync(string apiKey, string prompt)
    {
        var url = $"https://generativelanguage.googleapis.com/v1beta/models/{ModelName}:generateContent?key={apiKey}";

        var requestBody = new
        {
            contents = new[]
            {
                new
                {
                    parts = new[]
                    {
                        new { text = prompt }
                    }
                }
            }
        };

        using var httpClient = new HttpClient();
        var content = new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json");
        var response = await httpClient.PostAsync(url, content);
        var responseJson = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
        {
            return (null, $"Bir hata oluştu: {response.StatusCode} {responseJson}");
        }

        using var jsonDoc = JsonDocument.Parse(responseJson);
        var text = jsonDoc.RootElement
            .GetProperty("candidates")[0]
            .GetProperty("content")
            .GetProperty("parts")[0]
            .GetProperty("text")
            .GetString();

        return (text, null);
    }
}
