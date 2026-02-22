using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VetRandevu.Api.Data;
using VetRandevu.Api.Models;
using VetRandevu.Api.Security;
using VetRandevu.Api.Services;

namespace VetRandevu.Api.Controllers;

[Authorize(AuthenticationSchemes = "AdminCookie", Roles = Roles.Admin)]
[Route("admin/vaccinations")]
public class VaccinationsAdminController : Controller
{
    private readonly VetRandevuDbContext _db;
    private readonly IEmailSender _emailSender;
    private readonly IConfiguration _configuration;
    private readonly ILogger<VaccinationsAdminController> _logger;

    public VaccinationsAdminController(
        VetRandevuDbContext db,
        IEmailSender emailSender,
        IConfiguration configuration,
        ILogger<VaccinationsAdminController> logger)
    {
        _db = db;
        _emailSender = emailSender;
        _configuration = configuration;
        _logger = logger;
    }

    [HttpGet("")]
    public async Task<IActionResult> Index()
    {
        var clinicsQuery = _db.Clinics.AsNoTracking();
        if (User.IsInRole(Roles.ClinicAdmin) && !User.IsInRole(Roles.Admin))
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            clinicsQuery = clinicsQuery.Where(c => c.OwnerUserId == userId);
        }

        var clinics = await clinicsQuery.ToListAsync();
        var clinicIds = clinics.Select(c => c.Id).ToList();

        IQueryable<VaccinationRecord> recordsQuery = _db.VaccinationRecords.AsNoTracking()
            .OrderByDescending(v => v.AdministeredUtc);
        if (clinicIds.Count > 0)
        {
            recordsQuery = recordsQuery.Where(v => clinicIds.Contains(v.ClinicId));
        }

        var records = await recordsQuery.ToListAsync();
        var pets = await _db.Pets.AsNoTracking().ToListAsync();

        ViewBag.Clinics = clinics.ToDictionary(c => c.Id, c => c.Name);
        ViewBag.Pets = pets.ToDictionary(p => p.Id, p => p.Name);
        return View(records);
    }

    [HttpGet("create")]
    public async Task<IActionResult> Create()
    {
        await LoadLookupsAsync();
        return View(new VaccinationRecord
        {
            AdministeredUtc = DateTime.UtcNow,
            NextDueUtc = DateTime.UtcNow.AddMonths(12)
        });
    }

    [HttpPost("create")]
    public async Task<IActionResult> Create(VaccinationRecord record)
    {
        if (!ModelState.IsValid)
        {
            await LoadLookupsAsync();
            return View(record);
        }

        var clinic = await _db.Clinics.AsNoTracking().FirstOrDefaultAsync(c => c.Id == record.ClinicId);
        if (clinic is null)
        {
            ModelState.AddModelError("", "Klinik bulunamadı.");
            await LoadLookupsAsync();
            return View(record);
        }

        if (!User.IsInRole(Roles.Admin))
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!string.Equals(clinic.OwnerUserId, userId, StringComparison.Ordinal))
            {
                return Forbid();
            }
        }

        var pet = await _db.Pets.AsNoTracking().FirstOrDefaultAsync(p => p.Id == record.PetId);
        if (pet is null)
        {
            ModelState.AddModelError("", "Pet bulunamadı.");
            await LoadLookupsAsync();
            return View(record);
        }

        record.Id = Guid.NewGuid();
        record.VaccineName = record.VaccineName?.Trim() ?? string.Empty;
        record.Notes = string.IsNullOrWhiteSpace(record.Notes) ? null : record.Notes.Trim();
        record.AdministeredUtc = DateTime.SpecifyKind(record.AdministeredUtc, DateTimeKind.Utc);
        record.NextDueUtc = record.NextDueUtc.HasValue
            ? DateTime.SpecifyKind(record.NextDueUtc.Value, DateTimeKind.Utc)
            : null;
        record.CreatedUtc = DateTime.UtcNow;

        _db.VaccinationRecords.Add(record);
        await _db.SaveChangesAsync();
        await TrySendReminderAsync(record);
        return RedirectToAction(nameof(Index));
    }

    private async Task TrySendReminderAsync(VaccinationRecord record)
    {
        if (!record.NextDueUtc.HasValue)
        {
            return;
        }

        var now = DateTime.UtcNow;
        var daysBefore = _configuration.GetValue<int?>("VaccinationReminder:DaysBefore") ?? 7;
        var dueUntil = now.AddDays(daysBefore);

        if (record.NextDueUtc.Value < now || record.NextDueUtc.Value > dueUntil)
        {
            return;
        }

        var exists = await _db.VaccinationReminders.AsNoTracking()
            .AnyAsync(r => r.VaccinationRecordId == record.Id);
        if (exists)
        {
            return;
        }

        var pet = await _db.Pets.AsNoTracking().FirstOrDefaultAsync(p => p.Id == record.PetId);
        if (pet is null)
        {
            return;
        }

        var user = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == pet.OwnerUserId);
        if (user is null || string.IsNullOrWhiteSpace(user.Email))
        {
            return;
        }

        var clinic = await _db.Clinics.AsNoTracking().FirstOrDefaultAsync(c => c.Id == record.ClinicId);
        var clinicName = clinic?.Name ?? "Klinik";
        var dueText = record.NextDueUtc.Value.ToLocalTime().ToString("dd.MM.yyyy");
        var body = $"Yaklasan asi hatirlatmasi:{Environment.NewLine}{pet.Name} • {record.VaccineName} • {dueText} • {clinicName}";

        try
        {
            await _emailSender.SendAsync(user.Email, "Yaklasan Asi Hatirlatmasi", body);
            _db.VaccinationReminders.Add(new VaccinationReminder
            {
                Id = Guid.NewGuid(),
                VaccinationRecordId = record.Id,
                SentUtc = DateTime.UtcNow
            });
            await _db.SaveChangesAsync();
            _logger.LogInformation("VaccinationReminder: sent for record {RecordId}.", record.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "VaccinationReminder: send failed for record {RecordId}.", record.Id);
        }
    }

    private async Task LoadLookupsAsync()
    {
        IQueryable<Clinic> clinicsQuery = _db.Clinics.AsNoTracking().OrderBy(c => c.Name);
        if (User.IsInRole(Roles.ClinicAdmin) && !User.IsInRole(Roles.Admin))
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            clinicsQuery = clinicsQuery.Where(c => c.OwnerUserId == userId);
        }

        ViewBag.Clinics = await clinicsQuery.ToListAsync();
        ViewBag.Pets = await _db.Pets.AsNoTracking().OrderBy(p => p.Name).ToListAsync();
    }
}
