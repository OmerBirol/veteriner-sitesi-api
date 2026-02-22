using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VetRandevu.Api.Data;
using VetRandevu.Api.Models;
using VetRandevu.Api.Security;

namespace VetRandevu.Api.Controllers;

[Authorize(AuthenticationSchemes = "AdminCookie", Roles = $"{Roles.Admin},{Roles.ClinicAdmin}")]
[Route("clinic-admin/services")]
public class ClinicServicesController : Controller
{
    private readonly VetRandevuDbContext _db;
    private const string ClinicName = "Trabzon Pet Klinik";

    public ClinicServicesController(VetRandevuDbContext db)
    {
        _db = db;
    }

    private async Task<List<Clinic>> GetAllowedClinicsAsync()
    {
        if (User.IsInRole(Roles.Admin))
        {
            return await _db.Clinics.AsNoTracking().OrderBy(c => c.Name).ToListAsync();
        }

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var owned = await _db.Clinics.AsNoTracking()
            .Where(c => c.OwnerUserId == userId)
            .OrderBy(c => c.Name)
            .ToListAsync();

        return owned;
    }

    [HttpGet("")]
    public async Task<IActionResult> Index()
    {
        var clinics = await GetAllowedClinicsAsync();
        var clinicIds = clinics.Select(c => c.Id).ToList();
        var services = await _db.Services.AsNoTracking()
            .Where(s => clinicIds.Contains(s.ClinicId))
            .OrderBy(s => s.Name)
            .ToListAsync();

        ViewBag.Clinics = clinics.ToDictionary(c => c.Id, c => c.Name);
        ViewBag.NoClinic = clinics.Count == 0 && !User.IsInRole(Roles.Admin);
        ViewBag.Layout = "~/Views/ClinicAdminLayout/Index.cshtml";
        ViewBag.BasePath = "/clinic-admin";
        return View("~/Views/ServicesAdmin/Index.cshtml", services);
    }

    [HttpGet("create")]
    public async Task<IActionResult> Create()
    {
        var clinics = await GetAllowedClinicsAsync();
        ViewBag.Clinics = clinics;
        ViewBag.NoClinic = clinics.Count == 0 && !User.IsInRole(Roles.Admin);
        ViewBag.Layout = "~/Views/ClinicAdminLayout/Index.cshtml";
        ViewBag.BasePath = "/clinic-admin";
        return View("~/Views/ServicesAdmin/Create.cshtml", new Service());
    }

    [HttpPost("create")]
    public async Task<IActionResult> Create(Service service)
    {
        var clinics = await GetAllowedClinicsAsync();
        var clinicIds = clinics.Select(c => c.Id).ToList();
        if (!ModelState.IsValid || !clinicIds.Contains(service.ClinicId))
        {
            ViewBag.Clinics = clinics;
            ViewBag.NoClinic = clinics.Count == 0 && !User.IsInRole(Roles.Admin);
            ViewBag.Layout = "~/Views/ClinicAdminLayout/Index.cshtml";
            ViewBag.BasePath = "/clinic-admin";
            return View("~/Views/ServicesAdmin/Create.cshtml", service);
        }

        service.Id = Guid.NewGuid();
        service.Name = service.Name.Trim();
        _db.Services.Add(service);
        await _db.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    [HttpGet("edit/{id:guid}")]
    public async Task<IActionResult> Edit(Guid id)
    {
        var service = await _db.Services.FindAsync(id);
        if (service is null)
        {
            return NotFound();
        }

        var clinics = await GetAllowedClinicsAsync();
        if (!clinics.Any(c => c.Id == service.ClinicId))
        {
            return Forbid();
        }

        ViewBag.Clinics = clinics;
        ViewBag.NoClinic = clinics.Count == 0 && !User.IsInRole(Roles.Admin);
        ViewBag.Layout = "~/Views/ClinicAdminLayout/Index.cshtml";
        ViewBag.BasePath = "/clinic-admin";
        return View("~/Views/ServicesAdmin/Edit.cshtml", service);
    }

    [HttpPost("edit/{id:guid}")]
    public async Task<IActionResult> Edit(Guid id, Service service)
    {
        var existing = await _db.Services.FindAsync(id);
        if (existing is null)
        {
            return NotFound();
        }

        var clinics = await GetAllowedClinicsAsync();
        var clinicIds = clinics.Select(c => c.Id).ToList();
        if (!ModelState.IsValid || !clinicIds.Contains(service.ClinicId) || !clinicIds.Contains(existing.ClinicId))
        {
            ViewBag.Clinics = clinics;
            ViewBag.NoClinic = clinics.Count == 0 && !User.IsInRole(Roles.Admin);
            ViewBag.Layout = "~/Views/ClinicAdminLayout/Index.cshtml";
            ViewBag.BasePath = "/clinic-admin";
            return View("~/Views/ServicesAdmin/Edit.cshtml", service);
        }

        existing.Name = service.Name.Trim();
        existing.Price = service.Price;
        existing.DurationMinutes = service.DurationMinutes;
        existing.ClinicId = service.ClinicId;

        await _db.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    [HttpPost("delete/{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var service = await _db.Services.FindAsync(id);
        if (service is null)
        {
            return NotFound();
        }

        var clinics = await GetAllowedClinicsAsync();
        if (!clinics.Any(c => c.Id == service.ClinicId))
        {
            return Forbid();
        }

        _db.Services.Remove(service);
        await _db.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }
}
