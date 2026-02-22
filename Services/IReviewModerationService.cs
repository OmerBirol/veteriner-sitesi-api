namespace VetRandevu.Api.Services;

public interface IReviewModerationService
{
    Task<ReviewModerationResult> ModerateAsync(string text);
}

public record ReviewModerationResult(bool IsBlocked, string? Reason);
