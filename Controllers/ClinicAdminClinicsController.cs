using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VetRandevu.Api.Data;
using VetRandevu.Api.Models;
using VetRandevu.Api.Security;

namespace VetRandevu.Api.Controllers;

[Authorize(AuthenticationSchemes = "AdminCookie", Roles = $"{Roles.Admin},{Roles.ClinicAdmin}")]
[Route("clinic-admin/clinics")]
public class ClinicAdminClinicsController : Controller
{
    private readonly VetRandevuDbContext _db;

    public ClinicAdminClinicsController(VetRandevuDbContext db)
    {
        _db = db;
    }

    [HttpGet("")]
    public async Task<IActionResult> Index()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var clinics = await _db.Clinics.AsNoTracking()
            .Where(c => c.OwnerUserId == userId)
            .OrderBy(c => c.Name)
            .ToListAsync();

        ViewBag.Layout = "~/Views/ClinicAdminLayout/Index.cshtml";
        ViewBag.BasePath = "/clinic-admin";
        return View("~/Views/ClinicAdmin/Clinics.cshtml", clinics);
    }

    [HttpGet("edit/{id:guid}")]
    public async Task<IActionResult> Edit(Guid id)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var clinic = await _db.Clinics.FirstOrDefaultAsync(c => c.Id == id && c.OwnerUserId == userId);
        if (clinic is null)
        {
            return Forbid();
        }

        ViewBag.Layout = "~/Views/ClinicAdminLayout/Index.cshtml";
        ViewBag.BasePath = "/clinic-admin";
        return View("~/Views/ClinicAdmin/ClinicEdit.cshtml", clinic);
    }

    [HttpPost("edit/{id:guid}")]
    public async Task<IActionResult> Edit(Guid id, Clinic clinic)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var existing = await _db.Clinics.FirstOrDefaultAsync(c => c.Id == id && c.OwnerUserId == userId);
        if (existing is null)
        {
            return Forbid();
        }

        if (!ModelState.IsValid)
        {
            ViewBag.Layout = "~/Views/ClinicAdminLayout/Index.cshtml";
            ViewBag.BasePath = "/clinic-admin";
            return View("~/Views/ClinicAdmin/ClinicEdit.cshtml", clinic);
        }

        existing.Name = clinic.Name.Trim();
        existing.City = clinic.City.Trim();
        existing.Address = clinic.Address.Trim();
        existing.Phone = clinic.Phone.Trim();
        existing.Description = clinic.Description.Trim();
        existing.ImageUrl = string.IsNullOrWhiteSpace(clinic.ImageUrl) ? null : clinic.ImageUrl.Trim();
        existing.Latitude = clinic.Latitude;
        existing.Longitude = clinic.Longitude;

        await _db.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }
}
