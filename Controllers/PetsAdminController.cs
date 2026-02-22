using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VetRandevu.Api.Data;
using VetRandevu.Api.Models;
using VetRandevu.Api.Security;

namespace VetRandevu.Api.Controllers;

[Authorize(AuthenticationSchemes = "AdminCookie", Roles = Roles.Admin)]
[Route("admin/pets")]
public class PetsAdminController : Controller
{
    private readonly VetRandevuDbContext _db;

    public PetsAdminController(VetRandevuDbContext db)
    {
        _db = db;
    }

    [HttpGet("")]
    public async Task<IActionResult> Index()
    {
        var pets = await _db.Pets.AsNoTracking()
            .OrderBy(p => p.Name)
            .ToListAsync();
        return View(pets);
    }

    [HttpGet("edit/{id:guid}")]
    public async Task<IActionResult> Edit(Guid id)
    {
        var pet = await _db.Pets.FindAsync(id);
        if (pet is null)
        {
            return NotFound();
        }

        return View(pet);
    }

    [HttpPost("edit/{id:guid}")]
    public async Task<IActionResult> Edit(Guid id, Pet pet)
    {
        var existing = await _db.Pets.FindAsync(id);
        if (existing is null)
        {
            return NotFound();
        }

        existing.OwnerName = pet.OwnerName.Trim();
        existing.Name = pet.Name.Trim();
        existing.Species = pet.Species.Trim();
        existing.Age = pet.Age;

        await _db.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    [HttpPost("delete/{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var pet = await _db.Pets.FindAsync(id);
        if (pet is null)
        {
            return NotFound();
        }

        _db.Pets.Remove(pet);
        await _db.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }
}
