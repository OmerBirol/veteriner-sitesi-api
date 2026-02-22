using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VetRandevu.Api.Data;
using VetRandevu.Api.Models;
using VetRandevu.Api.Security;
using VetRandevu.Api.Services;

namespace VetRandevu.Api.Controllers;

[Authorize(AuthenticationSchemes = "AdminCookie", Roles = $"{Roles.Admin},{Roles.ClinicAdmin}")]
[Route("clinic-admin/vaccinations")]
public class ClinicVaccinationsController : Controller
{
    private readonly VetRandevuDbContext _db;
    private readonly IEmailSender _emailSender;
    private readonly IConfiguration _configuration;
    private readonly ILogger<ClinicVaccinationsController> _logger;

    public ClinicVaccinationsController(
        VetRandevuDbContext db,
        IEmailSender emailSender,
        IConfiguration configuration,
        ILogger<ClinicVaccinationsController> logger)
    {
        _db = db;
        _emailSender = emailSender;
        _configuration = configuration;
        _logger = logger;
    }

    private async Task<List<Clinic>> GetClinicsAsync()
    {
        if (User.IsInRole(Roles.Admin))
        {
            return await _db.Clinics.AsNoTracking().OrderBy(c => c.Name).ToListAsync();
        }

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return await _db.Clinics.AsNoTracking()
            .Where(c => c.OwnerUserId == userId)
            .OrderBy(c => c.Name)
            .ToListAsync();
    }

    [HttpGet("")]
    public async Task<IActionResult> Index()
    {
        var clinics = await GetClinicsAsync();
        var clinicIds = clinics.Select(c => c.Id).ToList();

        IQueryable<VaccinationRecord> recordsQuery = _db.VaccinationRecords.AsNoTracking()
            .OrderByDescending(v => v.AdministeredUtc);
        recordsQuery = recordsQuery.Where(v => clinicIds.Contains(v.ClinicId));

        var records = await recordsQuery.ToListAsync();
        var petIds = records.Select(r => r.PetId).Distinct().ToList();
        var pets = await _db.Pets.AsNoTracking()
            .Where(p => petIds.Contains(p.Id))
            .ToListAsync();

        ViewBag.Clinics = clinics.ToDictionary(c => c.Id, c => c.Name);
        ViewBag.Pets = pets.ToDictionary(p => p.Id, p => p.Name);
        ViewBag.Layout = "~/Views/ClinicAdminLayout/Index.cshtml";
        ViewBag.BasePath = "/clinic-admin";
        return View("~/Views/VaccinationsAdmin/Index.cshtml", records);
    }

    [HttpGet("create")]
    public async Task<IActionResult> Create()
    {
        await LoadLookupsAsync();
        ViewBag.Layout = "~/Views/ClinicAdminLayout/Index.cshtml";
        ViewBag.BasePath = "/clinic-admin";
        var clinics = ViewBag.Clinics as List<Clinic> ?? new List<Clinic>();
        var clinic = clinics.FirstOrDefault();
        if (clinic is null)
        {
            return NotFound("Klinik bulunamadı.");
        }
        ViewBag.ClinicName = clinic.Name;
        return View("~/Views/VaccinationsAdmin/Create.cshtml", new VaccinationRecord
        {
            ClinicId = clinic.Id,
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
            ViewBag.Layout = "~/Views/ClinicAdminLayout/Index.cshtml";
            ViewBag.BasePath = "/clinic-admin";
            return View("~/Views/VaccinationsAdmin/Create.cshtml", record);
        }

        var clinics = await GetClinicsAsync();
        var clinic = clinics.FirstOrDefault(c => c.Id == record.ClinicId);
        if (clinic is null)
        {
            ModelState.AddModelError("", "Klinik bulunamadı.");
            await LoadLookupsAsync();
            ViewBag.Layout = "~/Views/ClinicAdminLayout/Index.cshtml";
            ViewBag.BasePath = "/clinic-admin";
            return View("~/Views/VaccinationsAdmin/Create.cshtml", record);
        }

        record.ClinicId = clinic.Id;

        var pet = await _db.Pets.AsNoTracking().FirstOrDefaultAsync(p => p.Id == record.PetId);
        if (pet is null)
        {
            ModelState.AddModelError("", "Pet bulunamadı.");
            await LoadLookupsAsync();
            ViewBag.Layout = "~/Views/ClinicAdminLayout/Index.cshtml";
            ViewBag.BasePath = "/clinic-admin";
            return View("~/Views/VaccinationsAdmin/Create.cshtml", record);
        }
        var hasAppointment = await _db.Appointments.AsNoTracking()
            .AnyAsync(a => a.ClinicId == record.ClinicId && a.PetId == record.PetId);
        if (!hasAppointment)
        {
            ModelState.AddModelError("", "Bu pet bu klinigin hastasi degil.");
            await LoadLookupsAsync();
            ViewBag.Layout = "~/Views/ClinicAdminLayout/Index.cshtml";
            ViewBag.BasePath = "/clinic-admin";
            return View("~/Views/VaccinationsAdmin/Create.cshtml", record);
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

        var email = string.Empty;
        if (!string.IsNullOrWhiteSpace(pet.OwnerUserId))
        {
            var user = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == pet.OwnerUserId);
            email = user?.Email ?? string.Empty;
        }
        if (string.IsNullOrWhiteSpace(email))
        {
            email = pet.OwnerEmail ?? string.Empty;
        }
        if (string.IsNullOrWhiteSpace(email))
        {
            return;
        }

        var clinic = await _db.Clinics.AsNoTracking().FirstOrDefaultAsync(c => c.Id == record.ClinicId);
        var clinicName = clinic?.Name ?? "Klinik";
        var dueText = record.NextDueUtc.Value.ToLocalTime().ToString("dd.MM.yyyy");
        var body = $"Yaklasan asi hatirlatmasi:{Environment.NewLine}{pet.Name} • {record.VaccineName} • {dueText} • {clinicName}";

        try
        {
            await _emailSender.SendAsync(email, "Yaklasan Asi Hatirlatmasi", body);
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
        var clinics = await GetClinicsAsync();
        var clinicIds = clinics.Select(c => c.Id).ToList();
        IQueryable<Pet> petsQuery = _db.Pets.AsNoTracking().OrderBy(p => p.Name);

        var customerPetIds = await _db.Appointments.AsNoTracking()
            .Where(a => clinicIds.Contains(a.ClinicId))
            .Select(a => a.PetId)
            .Distinct()
            .ToListAsync();

        petsQuery = petsQuery.Where(p => customerPetIds.Contains(p.Id));

        ViewBag.Clinics = clinics;
        ViewBag.Pets = await petsQuery.ToListAsync();
    }
}
