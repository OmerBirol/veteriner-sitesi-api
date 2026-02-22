using System.Text;
using System.Text.Json;

namespace VetRandevu.Api.Services;

public class GeminiReviewModerationService : IReviewModerationService
{
    private const string ModelName = "gemini-2.5-flash";
    private readonly IConfiguration _configuration;

    public GeminiReviewModerationService(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public async Task<ReviewModerationResult> ModerateAsync(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return new ReviewModerationResult(false, null);
        }

        var apiKey = _configuration["Gemini:ApiKey"] ?? Environment.GetEnvironmentVariable("GEMINI_API_KEY");
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            return new ReviewModerationResult(false, "AI anahtari yok");
        }

        var prompt =
            "You are a Turkish content moderation system. " +
            "Decide if the text contains insults, hate, harassment, profanity, or abuse. " +
            "Reply ONLY with JSON: {\"blocked\":true|false,\"reason\":\"short Turkish reason or empty\"}. " +
            $"Text: {text}";

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
            return new ReviewModerationResult(false, $"AI hata: {response.StatusCode}");
        }

        using var jsonDoc = JsonDocument.Parse(responseJson);
        var textResult = jsonDoc.RootElement
            .GetProperty("candidates")[0]
            .GetProperty("content")
            .GetProperty("parts")[0]
            .GetProperty("text")
            .GetString();

        if (string.IsNullOrWhiteSpace(textResult))
        {
            return new ReviewModerationResult(false, null);
        }

        var parsed = TryParseResult(textResult);
        return parsed ?? new ReviewModerationResult(false, null);
    }

    private static ReviewModerationResult? TryParseResult(string raw)
    {
        var start = raw.IndexOf('{');
        var end = raw.LastIndexOf('}');
        if (start < 0 || end <= start)
        {
            return null;
        }

        var json = raw.Substring(start, end - start + 1);
        try
        {
            using var doc = JsonDocument.Parse(json);
            var blocked = doc.RootElement.GetProperty("blocked").GetBoolean();
            var reason = doc.RootElement.TryGetProperty("reason", out var reasonProp)
                ? reasonProp.GetString()
                : null;
            return new ReviewModerationResult(blocked, reason);
        }
        catch
        {
            return null;
        }
    }
}
