using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VetRandevu.Api.Data;
using VetRandevu.Api.Models;
using VetRandevu.Api.Security;

namespace VetRandevu.Api.Controllers;

[Authorize(AuthenticationSchemes = "AdminCookie", Roles = Roles.Admin)]
[Route("admin/appointments")]
public class AppointmentsAdminController : Controller
{
    private readonly VetRandevuDbContext _db;

    public AppointmentsAdminController(VetRandevuDbContext db)
    {
        _db = db;
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

        IQueryable<Appointment> appointmentsQuery = _db.Appointments.AsNoTracking()
            .OrderByDescending(a => a.StartUtc);
        if (clinicIds.Count > 0)
        {
            appointmentsQuery = appointmentsQuery.Where(a => clinicIds.Contains(a.ClinicId));
        }

        var appointments = await appointmentsQuery.ToListAsync();
        var pets = await _db.Pets.AsNoTracking().ToListAsync();
        var services = await _db.Services.AsNoTracking().ToListAsync();

        ViewBag.Clinics = clinics.ToDictionary(c => c.Id, c => c.Name);
        ViewBag.Pets = pets.ToDictionary(p => p.Id, p => p.Name);
        ViewBag.Services = services.ToDictionary(s => s.Id, s => s.Name);
        return View(appointments);
    }

    [HttpGet("edit/{id:guid}")]
    public async Task<IActionResult> Edit(Guid id)
    {
        var appointment = await _db.Appointments.FindAsync(id);
        if (appointment is null)
        {
            return NotFound();
        }

        ViewBag.Statuses = Enum.GetValues<AppointmentStatus>();
        return View(appointment);
    }

    [HttpPost("edit/{id:guid}")]
    public async Task<IActionResult> Edit(Guid id, Appointment appointment)
    {
        var existing = await _db.Appointments.FindAsync(id);
        if (existing is null)
        {
            return NotFound();
        }

        existing.Status = appointment.Status;
        await _db.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    [HttpPost("delete/{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var appointment = await _db.Appointments.FindAsync(id);
        if (appointment is null)
        {
            return NotFound();
        }

        _db.Appointments.Remove(appointment);
        await _db.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }
}
