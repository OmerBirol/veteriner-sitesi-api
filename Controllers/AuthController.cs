using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VetRandevu.Api.Data;
using VetRandevu.Api.Dtos;
using VetRandevu.Api.Models;
using VetRandevu.Api.Security;
using VetRandevu.Api.Services;

namespace VetRandevu.Api.Controllers;

[ApiController]
[Route("auth")]
public class AuthController : ControllerBase
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly VetRandevuDbContext _db;
    private readonly IEmailSender _emailSender;
    private readonly IConfiguration _configuration;
    private readonly ILogger<AuthController> _logger;

    public AuthController(
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        VetRandevuDbContext db,
        IEmailSender emailSender,
        IConfiguration configuration,
        ILogger<AuthController> logger)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _db = db;
        _emailSender = emailSender;
        _configuration = configuration;
        _logger = logger;
    }

    [HttpPost("register")]
    public async Task<ActionResult<AuthResponse>> Register([FromBody] RegisterRequest request)
    {
        var email = request.Email.Trim().ToLowerInvariant();
        var user = new ApplicationUser
        {
            UserName = email,
            Email = email,
            FullName = request.FullName?.Trim()
        };

        var result = await _userManager.CreateAsync(user, request.Password);
        if (!result.Succeeded)
        {
            return BadRequest(result.Errors.Select(e => e.Description));
        }

        var roleToAssign = string.IsNullOrWhiteSpace(request.Role) ? Roles.User : request.Role.Trim();
        if (roleToAssign == Roles.Admin)
        {
            roleToAssign = Roles.User;
        }

        if (roleToAssign is Roles.ClinicAdmin or Roles.User)
        {
            await _userManager.AddToRoleAsync(user, roleToAssign);
        }

        return Ok(new AuthResponse
        {
            Token = string.Empty,
            ExpiresAtUtc = DateTime.UtcNow.AddHours(8),
            UserId = user.Id,
            Email = user.Email ?? string.Empty,
            Roles = new List<string> { roleToAssign }
        });
    }

    [HttpPost("login")]
    public async Task<ActionResult<AuthResponse>> Login([FromBody] LoginRequest request)
    {
        var email = request.Email.Trim().ToLowerInvariant();
        var user = await _userManager.FindByEmailAsync(email);
        if (user is null)
        {
            return Unauthorized("Invalid credentials.");
        }

        var passwordValid = await _userManager.CheckPasswordAsync(user, request.Password);
        if (!passwordValid)
        {
            return Unauthorized("Invalid credentials.");
        }

        await _signInManager.SignInAsync(user, isPersistent: false);
        await TrySendVaccinationRemindersAsync(user);
        var roles = await _userManager.GetRolesAsync(user);
        return Ok(new AuthResponse
        {
            Token = string.Empty,
            ExpiresAtUtc = DateTime.UtcNow.AddHours(8),
            UserId = user.Id,
            Email = user.Email ?? string.Empty,
            Roles = roles.ToList()
        });
    }

    [Authorize]
    [HttpGet("me")]
    public async Task<ActionResult<object>> Me()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userId))
        {
            return Unauthorized();
        }

        var user = await _userManager.FindByIdAsync(userId);
        if (user is null)
        {
            return Unauthorized();
        }

        return Ok(new
        {
            user.Id,
            user.Email,
            user.FullName
        });
    }

    [Authorize]
    [HttpPost("logout")]
    public async Task<IActionResult> Logout()
    {
        await _signInManager.SignOutAsync();
        return Ok();
    }

    private async Task TrySendVaccinationRemindersAsync(ApplicationUser user)
    {
        if (string.IsNullOrWhiteSpace(user.Id) || string.IsNullOrWhiteSpace(user.Email))
        {
            return;
        }

        var daysBefore = _configuration.GetValue<int?>("VaccinationReminder:DaysBefore") ?? 7;
        var now = DateTime.UtcNow;
        var dueUntil = now.AddDays(daysBefore);

        var petIds = await _db.Pets.AsNoTracking()
            .Where(p => p.OwnerUserId == user.Id)
            .Select(p => p.Id)
            .ToListAsync();

        if (petIds.Count == 0)
        {
            _logger.LogInformation("VaccinationReminder: no pets for user {UserId}.", user.Id);
            return;
        }

        var dueRecords = await _db.VaccinationRecords.AsNoTracking()
            .Where(v =>
                petIds.Contains(v.PetId) &&
                v.NextDueUtc.HasValue &&
                v.NextDueUtc.Value >= now &&
                v.NextDueUtc.Value <= dueUntil)
            .ToListAsync();

        if (dueRecords.Count == 0)
        {
            _logger.LogInformation("VaccinationReminder: no due records for user {UserId}.", user.Id);
            return;
        }

        var recordIds = dueRecords.Select(r => r.Id).ToList();
        var remindedIds = await _db.VaccinationReminders.AsNoTracking()
            .Where(r => recordIds.Contains(r.VaccinationRecordId))
            .Select(r => r.VaccinationRecordId)
            .ToListAsync();

        var pending = dueRecords.Where(r => !remindedIds.Contains(r.Id)).ToList();
        if (pending.Count == 0)
        {
            _logger.LogInformation("VaccinationReminder: no pending reminders for user {UserId}.", user.Id);
            return;
        }

        var pets = await _db.Pets.AsNoTracking()
            .Where(p => petIds.Contains(p.Id))
            .ToDictionaryAsync(p => p.Id);

        var clinicIds = pending.Select(r => r.ClinicId).Distinct().ToList();
        var clinics = await _db.Clinics.AsNoTracking()
            .Where(c => clinicIds.Contains(c.Id))
            .ToDictionaryAsync(c => c.Id);

        var lines = new List<string>
        {
            "Yaklasan asi hatirlatmalari:",
            string.Empty
        };

        foreach (var record in pending.OrderBy(r => r.NextDueUtc))
        {
            var petName = pets.TryGetValue(record.PetId, out var pet) ? pet.Name : "Pet";
            var clinicName = clinics.TryGetValue(record.ClinicId, out var clinic) ? clinic.Name : "Klinik";
            var dueText = record.NextDueUtc?.ToLocalTime().ToString("dd.MM.yyyy") ?? "-";
            lines.Add($"{petName} • {record.VaccineName} • {dueText} • {clinicName}");
        }

        try
        {
            await _emailSender.SendAsync(user.Email, "Yaklasan Asi Hatirlatmalari", string.Join(Environment.NewLine, lines));
            _logger.LogInformation("VaccinationReminder: email sent to {Email}.", user.Email);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "VaccinationReminder: email send failed for {Email}.", user.Email);
            return;
        }

        var reminders = pending.Select(r => new VaccinationReminder
        {
            Id = Guid.NewGuid(),
            VaccinationRecordId = r.Id,
            SentUtc = DateTime.UtcNow
        });

        _db.VaccinationReminders.AddRange(reminders);
        await _db.SaveChangesAsync();
    }
}
