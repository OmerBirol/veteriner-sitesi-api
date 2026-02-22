using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VetRandevu.Api.Data;
using VetRandevu.Api.Models;
using VetRandevu.Api.Security;

namespace VetRandevu.Api.Controllers;

[Authorize(AuthenticationSchemes = "AdminCookie", Roles = Roles.Admin)]
[Route("admin/services")]
public class ServicesAdminController : Controller
{
    private readonly VetRandevuDbContext _db;

    public ServicesAdminController(VetRandevuDbContext db)
    {
        _db = db;
    }

    [HttpGet("")]
    public async Task<IActionResult> Index()
    {
        var services = await _db.Services.AsNoTracking()
            .OrderBy(s => s.Name)
            .ToListAsync();
        var clinics = await _db.Clinics.AsNoTracking().ToListAsync();
        ViewBag.Clinics = clinics.ToDictionary(c => c.Id, c => c.Name);
        ViewBag.BasePath = "/admin";
        return View(services);
    }

    [HttpGet("create")]
    public async Task<IActionResult> Create()
    {
        ViewBag.Clinics = await _db.Clinics.AsNoTracking().OrderBy(c => c.Name).ToListAsync();
        ViewBag.BasePath = "/admin";
        return View(new Service());
    }

    [HttpPost("create")]
    public async Task<IActionResult> Create(Service service)
    {
        if (!ModelState.IsValid)
        {
            ViewBag.Clinics = await _db.Clinics.AsNoTracking().OrderBy(c => c.Name).ToListAsync();
            ViewBag.BasePath = "/admin";
            return View(service);
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

        ViewBag.Clinics = await _db.Clinics.AsNoTracking().OrderBy(c => c.Name).ToListAsync();
        ViewBag.BasePath = "/admin";
        return View(service);
    }

    [HttpPost("edit/{id:guid}")]
    public async Task<IActionResult> Edit(Guid id, Service service)
    {
        if (!ModelState.IsValid)
        {
            ViewBag.Clinics = await _db.Clinics.AsNoTracking().OrderBy(c => c.Name).ToListAsync();
            ViewBag.BasePath = "/admin";
            return View(service);
        }

        var existing = await _db.Services.FindAsync(id);
        if (existing is null)
        {
            return NotFound();
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

        _db.Services.Remove(service);
        await _db.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }
}
