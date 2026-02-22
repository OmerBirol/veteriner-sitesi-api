using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VetRandevu.Api.Data;
using VetRandevu.Api.Models;
using VetRandevu.Api.Security;

namespace VetRandevu.Api.Controllers;

[Authorize(AuthenticationSchemes = "AdminCookie", Roles = $"{Roles.Admin},{Roles.ClinicAdmin}")]
[Route("clinic-admin/pets")]
public class ClinicPetsController : Controller
{
    private readonly VetRandevuDbContext _db;

    public ClinicPetsController(VetRandevuDbContext db)
    {
        _db = db;
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
        if (clinicIds.Count == 0)
        {
            return NotFound("Klinik bulunamadı.");
        }

        var petIds = await _db.Appointments.AsNoTracking()
            .Where(a => clinicIds.Contains(a.ClinicId))
            .Select(a => a.PetId)
            .Distinct()
            .ToListAsync();

        var pets = await _db.Pets.AsNoTracking()
            .Where(p => petIds.Contains(p.Id))
            .OrderBy(p => p.Name)
            .ToListAsync();

        var appointments = await _db.Appointments.AsNoTracking()
            .Where(a => clinicIds.Contains(a.ClinicId) && petIds.Contains(a.PetId))
            .OrderByDescending(a => a.StartUtc)
            .ToListAsync();

        var vaccinations = await _db.VaccinationRecords.AsNoTracking()
            .Where(v => clinicIds.Contains(v.ClinicId) && petIds.Contains(v.PetId))
            .OrderByDescending(v => v.AdministeredUtc)
            .ToListAsync();

        ViewBag.Appointments = appointments;
        ViewBag.Vaccinations = vaccinations;
        ViewBag.Layout = "~/Views/ClinicAdminLayout/Index.cshtml";
        ViewBag.BasePath = "/clinic-admin";
        return View("~/Views/ClinicAdmin/Pets.cshtml", pets);
    }

    [HttpGet("create")]
    public IActionResult Create()
    {
        ViewBag.Layout = "~/Views/ClinicAdminLayout/Index.cshtml";
        ViewBag.BasePath = "/clinic-admin";
        return View("~/Views/ClinicAdmin/PetCreate.cshtml", new Pet());
    }

    [HttpPost("create")]
    public async Task<IActionResult> Create(Pet pet)
    {
        if (!ModelState.IsValid)
        {
            ViewBag.Layout = "~/Views/ClinicAdminLayout/Index.cshtml";
            ViewBag.BasePath = "/clinic-admin";
            return View("~/Views/ClinicAdmin/PetCreate.cshtml", pet);
        }

        var newPet = new Pet
        {
            Id = Guid.NewGuid(),
            OwnerName = pet.OwnerName.Trim(),
            OwnerEmail = string.IsNullOrWhiteSpace(pet.OwnerEmail) ? null : pet.OwnerEmail.Trim(),
            Name = pet.Name.Trim(),
            Species = pet.Species.Trim(),
            Age = pet.Age,
            OwnerUserId = null
        };

        _db.Pets.Add(newPet);
        await _db.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }
}
