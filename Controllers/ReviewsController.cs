using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VetRandevu.Api.Data;
using VetRandevu.Api.Dtos;
using VetRandevu.Api.Models;
using VetRandevu.Api.Security;
using VetRandevu.Api.Services;

namespace VetRandevu.Api.Controllers;

[ApiController]
[Route("clinics/{clinicId:guid}/reviews")]
public class ReviewsController : ControllerBase
{
    private readonly VetRandevuDbContext _db;
    private readonly IReviewModerationService _moderationService;
    private const string RemovedPlaceholder = "Yorum hakaret/küfür vb. yüzünden kaldırıldı.";

    public ReviewsController(VetRandevuDbContext db, IReviewModerationService moderationService)
    {
        _db = db;
        _moderationService = moderationService;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<Review>>> GetReviews(Guid clinicId)
    {
        var reviews = await _db.Reviews.AsNoTracking()
            .Where(r => r.ClinicId == clinicId)
            .OrderByDescending(r => r.CreatedAtUtc)
            .ToListAsync();

        foreach (var review in reviews)
        {
            if (review.IsRemoved)
            {
                review.Comment = RemovedPlaceholder;
            }
        }

        return Ok(reviews);
    }

    [Authorize(Roles = $"{Roles.User},{Roles.Admin}")]
    [HttpPost]
    public async Task<ActionResult<Review>> CreateReview(Guid clinicId, [FromBody] CreateReviewRequest request)
    {
        if (request.Rating is < 1 or > 5)
        {
            return BadRequest("Rating must be between 1 and 5.");
        }

        var clinic = await _db.Clinics.FirstOrDefaultAsync(c => c.Id == clinicId);
        if (clinic is null)
        {
            return NotFound("Clinic not found.");
        }

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userId))
        {
            return Unauthorized();
        }

        var review = new Review
        {
            Id = Guid.NewGuid(),
            ClinicId = clinicId,
            UserId = userId,
            Rating = request.Rating,
            Comment = (request.Comment ?? string.Empty).Trim(),
            CreatedAtUtc = DateTime.UtcNow
        };

        var moderationResult = await _moderationService.ModerateAsync(review.Comment);
        if (moderationResult.IsBlocked)
        {
            review.IsRemoved = true;
            review.RemovedReason = string.IsNullOrWhiteSpace(moderationResult.Reason) ? "Hakaret/küfür" : moderationResult.Reason;
            review.OriginalComment = review.Comment;
            review.Comment = RemovedPlaceholder;
            review.ModeratedAtUtc = DateTime.UtcNow;
        }

        _db.Reviews.Add(review);

        var ratings = await _db.Reviews.AsNoTracking()
            .Where(r => r.ClinicId == clinicId)
            .Select(r => r.Rating)
            .ToListAsync();
        ratings.Add(request.Rating);
        var newAverage = ratings.Count == 0 ? 0 : ratings.Average();

        clinic.Rating = Math.Round(newAverage, 2);
        await _db.SaveChangesAsync();

        return Ok(review);
    }

    [Authorize]
    [HttpPut("{reviewId:guid}")]
    public async Task<ActionResult<Review>> UpdateReview(Guid clinicId, Guid reviewId, [FromBody] UpdateReviewRequest request)
    {
        if (request.Rating is < 1 or > 5)
        {
            return BadRequest("Rating must be between 1 and 5.");
        }

        var review = await _db.Reviews.FirstOrDefaultAsync(r => r.Id == reviewId && r.ClinicId == clinicId);
        if (review is null)
        {
            return NotFound();
        }

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!User.IsInRole(Roles.Admin) && !string.Equals(review.UserId, userId, StringComparison.Ordinal))
        {
            return Forbid();
        }

        review.Rating = request.Rating;
        review.Comment = (request.Comment ?? string.Empty).Trim();

        var moderationResult = await _moderationService.ModerateAsync(review.Comment);
        if (moderationResult.IsBlocked)
        {
            review.IsRemoved = true;
            review.RemovedReason = string.IsNullOrWhiteSpace(moderationResult.Reason) ? "Hakaret/küfür" : moderationResult.Reason;
            review.OriginalComment = review.Comment;
            review.Comment = RemovedPlaceholder;
            review.ModeratedAtUtc = DateTime.UtcNow;
        }
        else
        {
            review.IsRemoved = false;
            review.RemovedReason = null;
            review.ModeratedAtUtc = DateTime.UtcNow;
        }

        var clinic = await _db.Clinics.FirstOrDefaultAsync(c => c.Id == clinicId);
        if (clinic is not null)
        {
            var ratings = await _db.Reviews.AsNoTracking()
                .Where(r => r.ClinicId == clinicId)
                .Select(r => r.Rating)
                .ToListAsync();
            var newAverage = ratings.Count == 0 ? 0 : ratings.Average();
            clinic.Rating = Math.Round(newAverage, 2);
        }

        await _db.SaveChangesAsync();
        return Ok(review);
    }

    [Authorize]
    [HttpPost("{reviewId:guid}/report")]
    public async Task<IActionResult> ReportReview(Guid clinicId, Guid reviewId, [FromBody] CreateReviewReportRequest request)
    {
        var review = await _db.Reviews.AsNoTracking().FirstOrDefaultAsync(r => r.Id == reviewId && r.ClinicId == clinicId);
        if (review is null)
        {
            return NotFound();
        }

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userId))
        {
            return Unauthorized();
        }

        var existing = await _db.ReviewReports.AsNoTracking()
            .AnyAsync(r => r.ReviewId == reviewId && r.ReporterUserId == userId && r.Status == ReviewReportStatus.Pending);
        if (existing)
        {
            return Ok();
        }

        var report = new ReviewReport
        {
            Id = Guid.NewGuid(),
            ReviewId = reviewId,
            ReporterUserId = userId,
            Reason = (request.Reason ?? string.Empty).Trim(),
            Status = ReviewReportStatus.Pending,
            CreatedAtUtc = DateTime.UtcNow
        };

        _db.ReviewReports.Add(report);
        await _db.SaveChangesAsync();
        return Ok();
    }
}
