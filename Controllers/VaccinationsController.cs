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
[Route("vaccinations")]
public class VaccinationsController : ControllerBase
{
    private readonly VetRandevuDbContext _db;

    public VaccinationsController(VetRandevuDbContext db)
    {
        _db = db;
    }

    [Authorize]
    [HttpGet("/pets/{petId:guid}/vaccinations")]
    public async Task<ActionResult<IEnumerable<VaccinationRecordResponse>>> GetPetVaccinations(Guid petId)
    {
        var pet = await _db.Pets.AsNoTracking().FirstOrDefaultAsync(p => p.Id == petId);
        if (pet is null)
        {
            return NotFound();
        }

        if (!User.IsInRole(Roles.Admin))
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!string.Equals(pet.OwnerUserId, userId, StringComparison.Ordinal))
            {
                return NotFound();
            }
        }

        var records = await _db.VaccinationRecords.AsNoTracking()
            .Where(v => v.PetId == petId)
            .OrderByDescending(v => v.AdministeredUtc)
            .ToListAsync();

        var response = records.Select(record => new VaccinationRecordResponse
        {
            Id = record.Id,
            PetId = record.PetId,
            ClinicId = record.ClinicId,
            VaccineName = record.VaccineName,
            AdministeredUtc = record.AdministeredUtc,
            NextDueUtc = record.NextDueUtc,
            Notes = record.Notes,
            CreatedUtc = record.CreatedUtc
        });

        return Ok(response);
    }

    [Authorize(Roles = $"{Roles.ClinicAdmin},{Roles.Admin}")]
    [HttpPost]
    public async Task<ActionResult<VaccinationRecordResponse>> CreateVaccination([FromBody] CreateVaccinationRecordRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.VaccineName))
        {
            return BadRequest("VaccineName is required.");
        }

        var pet = await _db.Pets.AsNoTracking().FirstOrDefaultAsync(p => p.Id == request.PetId);
        if (pet is null)
        {
            return NotFound("Pet not found.");
        }

        var clinic = await _db.Clinics.AsNoTracking().FirstOrDefaultAsync(c => c.Id == request.ClinicId);
        if (clinic is null)
        {
            return NotFound("Clinic not found.");
        }

        if (!User.IsInRole(Roles.Admin))
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!string.Equals(clinic.OwnerUserId, userId, StringComparison.Ordinal))
            {
                return Forbid();
            }
        }

        var record = new VaccinationRecord
        {
            Id = Guid.NewGuid(),
            PetId = request.PetId,
            ClinicId = request.ClinicId,
            VaccineName = request.VaccineName.Trim(),
            AdministeredUtc = DateTime.SpecifyKind(request.AdministeredUtc, DateTimeKind.Utc),
            NextDueUtc = request.NextDueUtc.HasValue
                ? DateTime.SpecifyKind(request.NextDueUtc.Value, DateTimeKind.Utc)
                : null,
            Notes = string.IsNullOrWhiteSpace(request.Notes) ? null : request.Notes.Trim(),
            CreatedUtc = DateTime.UtcNow
        };

        _db.VaccinationRecords.Add(record);
        await _db.SaveChangesAsync();

        return Ok(new VaccinationRecordResponse
        {
            Id = record.Id,
            PetId = record.PetId,
            ClinicId = record.ClinicId,
            VaccineName = record.VaccineName,
            AdministeredUtc = record.AdministeredUtc,
            NextDueUtc = record.NextDueUtc,
            Notes = record.Notes,
            CreatedUtc = record.CreatedUtc
        });
    }
}
