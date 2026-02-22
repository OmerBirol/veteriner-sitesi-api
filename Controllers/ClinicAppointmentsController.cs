using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VetRandevu.Api.Data;
using VetRandevu.Api.Models;
using VetRandevu.Api.Security;

namespace VetRandevu.Api.Controllers;

[Authorize(AuthenticationSchemes = "AdminCookie", Roles = $"{Roles.Admin},{Roles.ClinicAdmin}")]
[Route("clinic-admin/appointments")]
public class ClinicAppointmentsController : Controller
{
    private readonly VetRandevuDbContext _db;

    public ClinicAppointmentsController(VetRandevuDbContext db)
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

        IQueryable<Appointment> appointmentsQuery = _db.Appointments.AsNoTracking()
            .OrderByDescending(a => a.StartUtc);
        appointmentsQuery = appointmentsQuery.Where(a => clinicIds.Contains(a.ClinicId));

        var appointments = await appointmentsQuery.ToListAsync();
        var pets = await _db.Pets.AsNoTracking().ToListAsync();
        var services = await _db.Services.AsNoTracking().ToListAsync();

        ViewBag.Clinics = clinics.ToDictionary(c => c.Id, c => c.Name);
        ViewBag.Pets = pets.ToDictionary(p => p.Id, p => p.Name);
        ViewBag.Services = services.ToDictionary(s => s.Id, s => s.Name);
        ViewBag.Layout = "~/Views/ClinicAdminLayout/Index.cshtml";
        ViewBag.BasePath = "/clinic-admin";
        return View("~/Views/AppointmentsAdmin/Index.cshtml", appointments);
    }

    [HttpGet("agenda")]
    public async Task<IActionResult> Agenda()
    {
        var clinics = await GetClinicsAsync();
        var clinicIds = clinics.Select(c => c.Id).ToList();

        IQueryable<Appointment> appointmentsQuery = _db.Appointments.AsNoTracking()
            .OrderBy(a => a.StartUtc);
        appointmentsQuery = appointmentsQuery.Where(a => clinicIds.Contains(a.ClinicId));

        var appointments = await appointmentsQuery.ToListAsync();
        var pets = await _db.Pets.AsNoTracking().ToListAsync();
        var services = await _db.Services.AsNoTracking().ToListAsync();

        ViewBag.Clinics = clinics.ToDictionary(c => c.Id, c => c.Name);
        ViewBag.Pets = pets.ToDictionary(p => p.Id, p => p.Name);
        ViewBag.Services = services.ToDictionary(s => s.Id, s => s.Name);
        ViewBag.Layout = "~/Views/ClinicAdminLayout/Index.cshtml";
        ViewBag.BasePath = "/clinic-admin";
        return View("~/Views/ClinicAdmin/Agenda.cshtml", appointments);
    }

    [HttpGet("edit/{id:guid}")]
    public async Task<IActionResult> Edit(Guid id)
    {
        var appointment = await _db.Appointments.FindAsync(id);
        if (appointment is null)
        {
            return NotFound();
        }
        var clinics = await GetClinicsAsync();
        if (!clinics.Any(c => c.Id == appointment.ClinicId))
        {
            return Forbid();
        }

        ViewBag.Statuses = Enum.GetValues<AppointmentStatus>();
        ViewBag.Layout = "~/Views/ClinicAdminLayout/Index.cshtml";
        ViewBag.BasePath = "/clinic-admin";
        return View("~/Views/AppointmentsAdmin/Edit.cshtml", appointment);
    }

    [HttpPost("edit/{id:guid}")]
    public async Task<IActionResult> Edit(Guid id, Appointment appointment)
    {
        var existing = await _db.Appointments.FindAsync(id);
        if (existing is null)
        {
            return NotFound();
        }
        var clinics = await GetClinicsAsync();
        if (!clinics.Any(c => c.Id == existing.ClinicId))
        {
            return Forbid();
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
        var clinics = await GetClinicsAsync();
        if (!clinics.Any(c => c.Id == appointment.ClinicId))
        {
            return Forbid();
        }

        _db.Appointments.Remove(appointment);
        await _db.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }
}
