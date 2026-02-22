using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VetRandevu.Api.Data;
using VetRandevu.Api.Models;
using VetRandevu.Api.Security;

namespace VetRandevu.Api.Controllers;

[Authorize(AuthenticationSchemes = "AdminCookie", Roles = Roles.Admin)]
[Route("admin/clinics")]
public class ClinicsAdminController : Controller
{
    private readonly VetRandevuDbContext _db;
    private readonly UserManager<ApplicationUser> _userManager;

    public ClinicsAdminController(VetRandevuDbContext db, UserManager<ApplicationUser> userManager)
    {
        _db = db;
        _userManager = userManager;
    }

    [HttpGet("")]
    public async Task<IActionResult> Index()
    {
        var clinics = await _db.Clinics.AsNoTracking().OrderBy(c => c.Name).ToListAsync();
        var owners = await _db.Users.AsNoTracking()
            .Where(u => clinics.Select(c => c.OwnerUserId).Contains(u.Id))
            .ToListAsync();
        ViewBag.Owners = owners.ToDictionary(o => o.Id, o => o.Email ?? o.UserName ?? o.Id);
        return View(clinics);
    }

    [HttpGet("create")]
    public async Task<IActionResult> Create()
    {
        ViewBag.ClinicAdmins = await _userManager.GetUsersInRoleAsync(Roles.ClinicAdmin);
        return View(new Clinic());
    }

    [HttpPost("create")]
    public async Task<IActionResult> Create(Clinic clinic)
    {
        if (!ModelState.IsValid)
        {
            ViewBag.ClinicAdmins = await _userManager.GetUsersInRoleAsync(Roles.ClinicAdmin);
            return View(clinic);
        }

        clinic.Id = Guid.NewGuid();
        clinic.Name = clinic.Name.Trim();
        clinic.City = clinic.City.Trim();
        clinic.Address = clinic.Address.Trim();
        clinic.Phone = clinic.Phone.Trim();
        clinic.Description = clinic.Description.Trim();
        clinic.ImageUrl = string.IsNullOrWhiteSpace(clinic.ImageUrl) ? null : clinic.ImageUrl.Trim();
        clinic.OwnerUserId = string.IsNullOrWhiteSpace(clinic.OwnerUserId) ? null : clinic.OwnerUserId.Trim();

        _db.Clinics.Add(clinic);
        await _db.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    [HttpGet("edit/{id:guid}")]
    public async Task<IActionResult> Edit(Guid id)
    {
        var clinic = await _db.Clinics.FindAsync(id);
        if (clinic is null)
        {
            return NotFound();
        }

        ViewBag.ClinicAdmins = await _userManager.GetUsersInRoleAsync(Roles.ClinicAdmin);
        return View(clinic);
    }

    [HttpPost("edit/{id:guid}")]
    public async Task<IActionResult> Edit(Guid id, Clinic clinic)
    {
        if (!ModelState.IsValid)
        {
            ViewBag.ClinicAdmins = await _userManager.GetUsersInRoleAsync(Roles.ClinicAdmin);
            return View(clinic);
        }

        var existing = await _db.Clinics.FindAsync(id);
        if (existing is null)
        {
            return NotFound();
        }

        existing.Name = clinic.Name.Trim();
        existing.City = clinic.City.Trim();
        existing.Address = clinic.Address.Trim();
        existing.Phone = clinic.Phone.Trim();
        existing.Description = clinic.Description.Trim();
        existing.ImageUrl = string.IsNullOrWhiteSpace(clinic.ImageUrl) ? null : clinic.ImageUrl.Trim();
        existing.IsApproved = clinic.IsApproved;
        existing.OwnerUserId = string.IsNullOrWhiteSpace(clinic.OwnerUserId) ? null : clinic.OwnerUserId.Trim();
        existing.Latitude = clinic.Latitude;
        existing.Longitude = clinic.Longitude;

        await _db.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    [HttpPost("delete/{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var clinic = await _db.Clinics.FindAsync(id);
        if (clinic is null)
        {
            return NotFound();
        }

        _db.Clinics.Remove(clinic);
        await _db.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    [HttpPost("approve/{id:guid}")]
    public async Task<IActionResult> Approve(Guid id)
    {
        var clinic = await _db.Clinics.FindAsync(id);
        if (clinic is null)
        {
            return NotFound();
        }

        clinic.IsApproved = true;
        await _db.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }
}
