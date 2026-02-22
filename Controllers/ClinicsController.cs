using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VetRandevu.Api.Data;
using VetRandevu.Api.Dtos;
using VetRandevu.Api.Models;
using VetRandevu.Api.Security;

namespace VetRandevu.Api.Controllers;

[ApiController]
[Route("clinics")]
public class ClinicsController : ControllerBase
{
    private readonly VetRandevuDbContext _db;
    private readonly UserManager<ApplicationUser> _userManager;

    public ClinicsController(VetRandevuDbContext db, UserManager<ApplicationUser> userManager)
    {
        _db = db;
        _userManager = userManager;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<Clinic>>> GetClinics([FromQuery] bool includeUnapproved = false)
    {
        var query = _db.Clinics.AsQueryable();

        if (!includeUnapproved || !User.IsInRole(Roles.Admin))
        {
            query = query.Where(c => c.IsApproved);
        }

        var clinics = await query.AsNoTracking().ToListAsync();
        return Ok(clinics);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<Clinic>> GetClinic(Guid id)
    {
        var clinic = await _db.Clinics.AsNoTracking().FirstOrDefaultAsync(c => c.Id == id);
        if (clinic is null)
        {
            return NotFound();
        }

        if (!clinic.IsApproved && !User.IsInRole(Roles.Admin))
        {
            return NotFound();
        }

        return Ok(clinic);
    }

    [AllowAnonymous]
    [HttpPost("apply")]
    public async Task<ActionResult<Clinic>> ApplyClinic([FromBody] CreateClinicApplicationRequest request)
    {
        var email = request.OwnerEmail.Trim().ToLowerInvariant();
        var existingUser = await _userManager.FindByEmailAsync(email);
        if (existingUser is not null)
        {
            return BadRequest("Bu email zaten kayıtlı.");
        }

        var existingClinic = await _db.Clinics.AsNoTracking()
            .AnyAsync(c => c.Name == request.Name.Trim());
        if (existingClinic)
        {
            return BadRequest("Bu isimle bir klinik zaten var.");
        }

        var user = new ApplicationUser
        {
            UserName = email,
            Email = email,
            FullName = request.OwnerName.Trim()
        };

        var createResult = await _userManager.CreateAsync(user, request.Password);
        if (!createResult.Succeeded)
        {
            var errors = string.Join(", ", createResult.Errors.Select(e => e.Description));
            return BadRequest(errors);
        }

        await _userManager.AddToRoleAsync(user, Roles.ClinicAdmin);

        var clinic = new Clinic
        {
            Id = Guid.NewGuid(),
            Name = request.Name.Trim(),
            City = request.City.Trim(),
            Address = request.Address.Trim(),
            Phone = request.Phone.Trim(),
            Description = request.Description.Trim(),
            ImageUrl = string.IsNullOrWhiteSpace(request.ImageUrl) ? null : request.ImageUrl.Trim(),
            Rating = 0,
            IsApproved = false,
            OwnerUserId = user.Id,
            Latitude = request.Latitude,
            Longitude = request.Longitude
        };

        _db.Clinics.Add(clinic);
        await _db.SaveChangesAsync();

        return Ok(clinic);
    }

    [HttpGet("{id:guid}/services")]
    public async Task<ActionResult<IEnumerable<Service>>> GetServices(Guid id)
    {
        var services = await _db.Services.AsNoTracking()
            .Where(s => s.ClinicId == id)
            .ToListAsync();
        return Ok(services);
    }

    [Authorize(Roles = $"{Roles.ClinicAdmin},{Roles.Admin}")]
    [HttpPost]
    public async Task<ActionResult<Clinic>> CreateClinic([FromBody] CreateClinicRequest request)
    {
        var ownerId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var clinic = new Clinic
        {
            Id = Guid.NewGuid(),
            Name = request.Name.Trim(),
            City = request.City.Trim(),
            Address = request.Address.Trim(),
            Phone = request.Phone.Trim(),
            Description = request.Description.Trim(),
            ImageUrl = string.IsNullOrWhiteSpace(request.ImageUrl) ? null : request.ImageUrl.Trim(),
            Rating = 0,
            IsApproved = false,
            OwnerUserId = ownerId
        };

        _db.Clinics.Add(clinic);

        foreach (var serviceRequest in request.Services)
        {
            var service = new Service
            {
                Id = Guid.NewGuid(),
                ClinicId = clinic.Id,
                Name = serviceRequest.Name.Trim(),
                Price = serviceRequest.Price,
                DurationMinutes = serviceRequest.DurationMinutes
            };

            _db.Services.Add(service);
        }

        await _db.SaveChangesAsync();

        return CreatedAtAction(nameof(GetClinic), new { id = clinic.Id }, clinic);
    }

    [Authorize(Roles = $"{Roles.ClinicAdmin},{Roles.Admin}")]
    [HttpPost("{id:guid}/slots")]
    public async Task<ActionResult<AvailabilitySlot>> CreateSlot(Guid id, [FromBody] CreateSlotRequest request)
    {
        if (request.EndUtc <= request.StartUtc)
        {
            return BadRequest("EndUtc must be greater than StartUtc.");
        }

        var clinic = await _db.Clinics.AsNoTracking().FirstOrDefaultAsync(c => c.Id == id);
        if (clinic is null)
        {
            return NotFound();
        }

        if (!User.IsInRole(Roles.Admin))
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!string.Equals(clinic.OwnerUserId, userId, StringComparison.Ordinal))
            {
                return Forbid();
            }
        }

        var slot = new AvailabilitySlot
        {
            Id = Guid.NewGuid(),
            ClinicId = id,
            StartUtc = DateTime.SpecifyKind(request.StartUtc, DateTimeKind.Utc),
            EndUtc = DateTime.SpecifyKind(request.EndUtc, DateTimeKind.Utc),
            IsBooked = false
        };

        _db.Slots.Add(slot);
        await _db.SaveChangesAsync();
        return Ok(slot);
    }

    [HttpGet("{id:guid}/slots")]
    public async Task<ActionResult<IEnumerable<AvailabilitySlot>>> GetSlots(
        Guid id,
        [FromQuery] DateTime? fromUtc,
        [FromQuery] DateTime? toUtc)
    {
        var nowUtc = DateTime.UtcNow;
        var hasUpcoming = await _db.Slots.AsNoTracking()
            .AnyAsync(s => s.ClinicId == id && s.EndUtc > nowUtc);

        if (!hasUpcoming)
        {
            await EnsureUpcomingSlotsAsync(id, nowUtc);
        }

        var query = _db.Slots.AsNoTracking().Where(s => s.ClinicId == id);

        if (fromUtc is not null)
        {
            query = query.Where(s => s.EndUtc > fromUtc.Value);
        }

        if (toUtc is not null)
        {
            query = query.Where(s => s.StartUtc < toUtc.Value);
        }

        var slots = await query.ToListAsync();
        return Ok(slots);
    }

    private async Task EnsureUpcomingSlotsAsync(Guid clinicId, DateTime nowUtc)
    {
        var startDay = nowUtc.Date.AddDays(1);
        var endDay = startDay.AddDays(7);
        var existing = await _db.Slots.AsNoTracking()
            .Where(s => s.ClinicId == clinicId && s.StartUtc >= startDay && s.StartUtc < endDay)
            .Select(s => s.StartUtc)
            .ToListAsync();

        var existingStarts = new HashSet<DateTime>(existing);
        var newSlots = new List<AvailabilitySlot>();
        var hours = new[] { 9, 11, 14, 16 };

        for (var dayOffset = 0; dayOffset < 7; dayOffset++)
        {
            var day = startDay.AddDays(dayOffset);
            foreach (var hour in hours)
            {
                var start = DateTime.SpecifyKind(day.AddHours(hour), DateTimeKind.Utc);
                var end = start.AddHours(1);
                if (existingStarts.Contains(start))
                {
                    continue;
                }

                newSlots.Add(new AvailabilitySlot
                {
                    Id = Guid.NewGuid(),
                    ClinicId = clinicId,
                    StartUtc = start,
                    EndUtc = end,
                    IsBooked = false
                });
            }
        }

        if (newSlots.Count > 0)
        {
            _db.Slots.AddRange(newSlots);
            await _db.SaveChangesAsync();
        }
    }

    [Authorize(Roles = Roles.Admin)]
    [HttpPost("{id:guid}/approve")]
    public async Task<ActionResult<Clinic>> ApproveClinic(Guid id)
    {
        var clinic = await _db.Clinics.FirstOrDefaultAsync(c => c.Id == id);
        if (clinic is null)
        {
            return NotFound();
        }

        clinic.IsApproved = true;
        await _db.SaveChangesAsync();
        return Ok(clinic);
    }
}
