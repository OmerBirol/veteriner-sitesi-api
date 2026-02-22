using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VetRandevu.Api.Data;
using VetRandevu.Api.Models;
using VetRandevu.Api.Security;

namespace VetRandevu.Api.Controllers;

[Authorize(AuthenticationSchemes = "AdminCookie", Roles = Roles.Admin)]
[Route("admin/reviews")]
public class ReviewsAdminController : Controller
{
    private readonly VetRandevuDbContext _db;
    private const string RemovedPlaceholder = "Yorum hakaret/küfür vb. yüzünden kaldırıldı.";

    public ReviewsAdminController(VetRandevuDbContext db)
    {
        _db = db;
    }

    [HttpGet("")]
    public async Task<IActionResult> Index()
    {
        var reviews = await _db.Reviews.AsNoTracking()
            .OrderByDescending(r => r.CreatedAtUtc)
            .ToListAsync();
        var reports = await _db.ReviewReports.AsNoTracking()
            .OrderByDescending(r => r.CreatedAtUtc)
            .ToListAsync();

        ViewBag.Reports = reports;
        return View(reviews);
    }

    [HttpPost("delete/{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var review = await _db.Reviews.FindAsync(id);
        if (review is null)
        {
            return NotFound();
        }

        _db.Reviews.Remove(review);
        await _db.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    [HttpPost("remove/{id:guid}")]
    public async Task<IActionResult> Remove(Guid id)
    {
        var review = await _db.Reviews.FindAsync(id);
        if (review is null)
        {
            return NotFound();
        }

        if (!review.IsRemoved)
        {
            review.OriginalComment = review.Comment;
        }
        review.IsRemoved = true;
        review.RemovedReason = "Admin moderasyon";
        review.ModeratedAtUtc = DateTime.UtcNow;
        review.Comment = RemovedPlaceholder;
        await _db.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    [HttpPost("restore/{id:guid}")]
    public async Task<IActionResult> Restore(Guid id)
    {
        var review = await _db.Reviews.FindAsync(id);
        if (review is null)
        {
            return NotFound();
        }

        review.IsRemoved = false;
        review.RemovedReason = null;
        review.ModeratedAtUtc = DateTime.UtcNow;
        if (!string.IsNullOrWhiteSpace(review.OriginalComment))
        {
            review.Comment = review.OriginalComment;
        }
        await _db.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    [HttpPost("reports/{id:guid}/resolve")]
    public async Task<IActionResult> ResolveReport(Guid id)
    {
        var report = await _db.ReviewReports.FindAsync(id);
        if (report is null)
        {
            return NotFound();
        }

        report.Status = ReviewReportStatus.Resolved;
        await _db.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }
}
