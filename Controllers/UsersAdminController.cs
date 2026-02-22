using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using VetRandevu.Api.Models;
using VetRandevu.Api.Security;

namespace VetRandevu.Api.Controllers;

[Authorize(AuthenticationSchemes = "AdminCookie", Roles = Roles.Admin)]
[Route("admin/users")]
public class UsersAdminController : Controller
{
    private readonly UserManager<ApplicationUser> _userManager;

    public UsersAdminController(UserManager<ApplicationUser> userManager)
    {
        _userManager = userManager;
    }

    [HttpGet("")]
    public async Task<IActionResult> Index()
    {
        var users = _userManager.Users.ToList();
        var roleMap = new Dictionary<string, IList<string>>();
        foreach (var user in users)
        {
            roleMap[user.Id] = await _userManager.GetRolesAsync(user);
        }

        ViewBag.RoleMap = roleMap;
        return View(users);
    }

    [HttpPost("role/{id}")]
    public async Task<IActionResult> UpdateRole(string id, [FromForm] string role)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user is null)
        {
            return NotFound();
        }

        var validRoles = new[] { Roles.Admin, Roles.ClinicAdmin, Roles.User };
        if (!validRoles.Contains(role))
        {
            return BadRequest("Geçersiz rol.");
        }

        var existingRoles = await _userManager.GetRolesAsync(user);
        var toRemove = existingRoles.Where(r => validRoles.Contains(r)).ToList();
        if (toRemove.Count > 0)
        {
            await _userManager.RemoveFromRolesAsync(user, toRemove);
        }
        await _userManager.AddToRoleAsync(user, role);

        return RedirectToAction(nameof(Index));
    }
}
