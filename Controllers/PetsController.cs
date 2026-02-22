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
[Route("pets")]
public class PetsController : ControllerBase
{
    private readonly VetRandevuDbContext _db;

    public PetsController(VetRandevuDbContext db)
    {
        _db = db;
    }

    [Authorize]
    [HttpGet]
    public async Task<ActionResult<IEnumerable<Pet>>> GetPets()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var query = _db.Pets.AsNoTracking();

        if (!User.IsInRole(Roles.Admin))
        {
            query = query.Where(p => p.OwnerUserId == userId);
        }

        var pets = await query.ToListAsync();
        return Ok(pets);
    }

    [Authorize]
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<Pet>> GetPet(Guid id)
    {
        var pet = await _db.Pets.AsNoTracking().FirstOrDefaultAsync(p => p.Id == id);
        if (pet is null)
        {
            return NotFound();
        }

        if (!User.IsInRole(Roles.Admin))
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (pet.OwnerUserId != userId)
            {
                return NotFound();
            }
        }

        return Ok(pet);
    }

    [Authorize(Roles = Roles.User)]
    [HttpPost]
    public async Task<ActionResult<Pet>> CreatePet([FromBody] CreatePetRequest request)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var pet = new Pet
        {
            Id = Guid.NewGuid(),
            OwnerUserId = userId ?? string.Empty,
            OwnerName = request.OwnerName.Trim(),
            Name = request.Name.Trim(),
            Species = request.Species.Trim(),
            Age = request.Age
        };

        _db.Pets.Add(pet);
        await _db.SaveChangesAsync();
        return CreatedAtAction(nameof(GetPet), new { id = pet.Id }, pet);
    }

    [Authorize]
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeletePet(Guid id)
    {
        var pet = await _db.Pets.FindAsync(id);
        if (pet is null)
        {
            return NotFound();
        }

        if (!User.IsInRole(Roles.Admin))
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (pet.OwnerUserId != userId)
            {
                return NotFound();
            }
        }

        _db.Pets.Remove(pet);
        await _db.SaveChangesAsync();
        return NoContent();
    }
}
