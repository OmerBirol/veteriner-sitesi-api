using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VetRandevu.Api.Security;

namespace VetRandevu.Api.Controllers;

[Authorize(AuthenticationSchemes = "AdminCookie", Roles = $"{Roles.Admin},{Roles.ClinicAdmin}")]
[Route("clinic-admin")]
public class ClinicAdminHomeController : Controller
{
    [HttpGet("")]
    public IActionResult Index()
    {
        return Redirect("/clinic-admin/appointments");
    }
}
