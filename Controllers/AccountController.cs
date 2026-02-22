using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using VetRandevu.Api.Models;
using VetRandevu.Api.Security;

namespace VetRandevu.Api.Controllers;

[AllowAnonymous]
[Route("account")]
public class AccountController : Controller
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;

    public AccountController(
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager)
    {
        _userManager = userManager;
        _signInManager = signInManager;
    }

    [HttpGet("login")]
    public IActionResult Login(string? returnUrl = null)
    {
        ViewBag.ReturnUrl = returnUrl;
        return View(new LoginViewModel());
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login(LoginViewModel model, string? returnUrl = null)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var user = await _userManager.FindByEmailAsync(model.Email.Trim().ToLowerInvariant());
        if (user is null)
        {
            ModelState.AddModelError(string.Empty, "Geçersiz giriş bilgileri.");
            return View(model);
        }

        var result = await _signInManager.CheckPasswordSignInAsync(user, model.Password, lockoutOnFailure: false);
        if (!result.Succeeded)
        {
            ModelState.AddModelError(string.Empty, "Geçersiz giriş bilgileri.");
            return View(model);
        }

        var isAdmin = await _userManager.IsInRoleAsync(user, Roles.Admin);
        var isClinicAdmin = await _userManager.IsInRoleAsync(user, Roles.ClinicAdmin);
        if (!isAdmin && !isClinicAdmin)
        {
            ModelState.AddModelError(string.Empty, "Bu panel için yetkiniz yok.");
            return View(model);
        }

        var principal = await _signInManager.CreateUserPrincipalAsync(user);
        await HttpContext.SignInAsync("AdminCookie", principal);

        var adminTarget = "/admin/clinics";
        var clinicTarget = "/clinic-admin/clinics";
        var target = isAdmin ? adminTarget : clinicTarget;

        if (!string.IsNullOrWhiteSpace(returnUrl))
        {
            if (isAdmin && returnUrl.StartsWith("/admin", StringComparison.OrdinalIgnoreCase))
            {
                return Redirect(returnUrl);
            }

            if (isClinicAdmin && returnUrl.StartsWith("/clinic-admin", StringComparison.OrdinalIgnoreCase))
            {
                return Redirect(returnUrl);
            }
        }

        return Redirect(target);
    }

    [HttpGet("register")]
    public IActionResult Register()
    {
        return View(new RegisterViewModel());
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register(RegisterViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var email = model.Email.Trim().ToLowerInvariant();
        var user = new ApplicationUser
        {
            UserName = email,
            Email = email,
            FullName = model.FullName.Trim()
        };

        var result = await _userManager.CreateAsync(user, model.Password);
        if (!result.Succeeded)
        {
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }
            return View(model);
        }

        await _userManager.AddToRoleAsync(user, Roles.User);
        return Redirect("/account/login");
    }

    [Authorize]
    [HttpGet("logout")]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync("AdminCookie");
        return Redirect("/account/login");
    }
}
