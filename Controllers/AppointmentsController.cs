using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VetRandevu.Api.Data;
using VetRandevu.Api.Dtos;
using VetRandevu.Api.Models;
using VetRandevu.Api.Security;

namespace VetRandevu.Api.Controllers;

[ApiController]
[Route("appointments")]
public class AppointmentsController : ControllerBase
{
    private readonly VetRandevuDbContext _db;

    public AppointmentsController(VetRandevuDbContext db)
    {
        _db = db;
    }

    [Authorize]
    [HttpGet]
    public async Task<ActionResult<IEnumerable<Appointment>>> GetAppointments()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var query = _db.Appointments.AsNoTracking();

        if (!User.IsInRole(Roles.Admin))
        {
            query = query.Where(a => a.UserId == userId);
        }

        var appointments = await query.ToListAsync();
        return Ok(appointments);
    }

    [Authorize]
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<Appointment>> GetAppointment(Guid id)
    {
        var appointment = await _db.Appointments.AsNoTracking().FirstOrDefaultAsync(a => a.Id == id);
        if (appointment is null)
        {
            return NotFound();
        }

        if (!User.IsInRole(Roles.Admin))
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!string.Equals(appointment.UserId, userId, StringComparison.Ordinal))
            {
                return NotFound();
            }
        }

        return Ok(appointment);
    }

    [Authorize(Roles = $"{Roles.User},{Roles.Admin}")]
    [HttpPost]
    public async Task<ActionResult<Appointment>> CreateAppointment([FromBody] CreateAppointmentRequest request)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var clinic = await _db.Clinics.AsNoTracking().FirstOrDefaultAsync(c => c.Id == request.ClinicId);
        if (clinic is null)
        {
            return NotFound("Clinic not found.");
        }

        var pet = await _db.Pets.AsNoTracking().FirstOrDefaultAsync(p => p.Id == request.PetId);
        if (pet is null)
        {
            return NotFound("Pet not found.");
        }

        if (!User.IsInRole(Roles.Admin) && !string.Equals(pet.OwnerUserId, userId, StringComparison.Ordinal))
        {
            return BadRequest("Pet does not belong to current user.");
        }

        var service = await _db.Services.AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == request.ServiceId && s.ClinicId == request.ClinicId);
        if (service is null)
        {
            return NotFound("Service not found.");
        }

        var startUtc = DateTime.SpecifyKind(request.StartUtc, DateTimeKind.Utc);
        var endUtc = startUtc.AddMinutes(service.DurationMinutes);

        var slot = await _db.Slots.FirstOrDefaultAsync(s =>
            s.ClinicId == request.ClinicId &&
            !s.IsBooked &&
            s.StartUtc <= startUtc &&
            s.EndUtc >= endUtc);

        if (slot is null)
        {
            return BadRequest("No available slot for this time.");
        }

        slot.IsBooked = true;

        var appointment = new Appointment
        {
            Id = Guid.NewGuid(),
            UserId = userId ?? string.Empty,
            ClinicId = request.ClinicId,
            PetId = request.PetId,
            ServiceId = request.ServiceId,
            StartUtc = startUtc,
            EndUtc = endUtc,
            Status = AppointmentStatus.Confirmed
        };

        _db.Appointments.Add(appointment);
        await _db.SaveChangesAsync();

        return CreatedAtAction(nameof(GetAppointment), new { id = appointment.Id }, appointment);
    }

    [Authorize]
    [HttpPatch("{id:guid}/cancel")]
    public async Task<ActionResult<Appointment>> CancelAppointment(Guid id)
    {
        var appointment = await _db.Appointments.FirstOrDefaultAsync(a => a.Id == id);
        if (appointment is null)
        {
            return NotFound();
        }

        if (!User.IsInRole(Roles.Admin))
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!string.Equals(appointment.UserId, userId, StringComparison.Ordinal))
            {
                return NotFound();
            }
        }

        if (appointment.Status == AppointmentStatus.Cancelled)
        {
            return Ok(appointment);
        }

        var slot = await _db.Slots.FirstOrDefaultAsync(s =>
            s.ClinicId == appointment.ClinicId &&
            s.StartUtc <= appointment.StartUtc &&
            s.EndUtc >= appointment.EndUtc);
        if (slot is not null)
        {
            slot.IsBooked = false;
        }

        appointment.Status = AppointmentStatus.Cancelled;
        await _db.SaveChangesAsync();
        return Ok(appointment);
    }

    [Authorize]
    [HttpPatch("{id:guid}/reschedule")]
    public async Task<ActionResult<Appointment>> RescheduleAppointment(Guid id, [FromBody] RescheduleAppointmentRequest request)
    {
        var appointment = await _db.Appointments.FirstOrDefaultAsync(a => a.Id == id);
        if (appointment is null)
        {
            return NotFound();
        }

        if (!User.IsInRole(Roles.Admin))
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!string.Equals(appointment.UserId, userId, StringComparison.Ordinal))
            {
                return NotFound();
            }
        }

        if (appointment.Status == AppointmentStatus.Cancelled)
        {
            return BadRequest("Cancelled appointment cannot be rescheduled.");
        }

        var newSlot = await _db.Slots.FirstOrDefaultAsync(s =>
            s.Id == request.SlotId &&
            s.ClinicId == appointment.ClinicId &&
            !s.IsBooked);
        if (newSlot is null)
        {
            return BadRequest("Slot not available.");
        }

        var oldSlot = await _db.Slots.FirstOrDefaultAsync(s =>
            s.ClinicId == appointment.ClinicId &&
            s.StartUtc <= appointment.StartUtc &&
            s.EndUtc >= appointment.EndUtc);
        if (oldSlot is not null)
        {
            oldSlot.IsBooked = false;
        }

        newSlot.IsBooked = true;
        appointment.StartUtc = newSlot.StartUtc;
        appointment.EndUtc = newSlot.EndUtc;
        appointment.Status = AppointmentStatus.Confirmed;

        await _db.SaveChangesAsync();
        return Ok(appointment);
    }
}
