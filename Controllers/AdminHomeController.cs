using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VetRandevu.Api.Security;

namespace VetRandevu.Api.Controllers;

[Authorize(AuthenticationSchemes = "AdminCookie", Roles = Roles.Admin)]
[Route("admin")]
public class AdminHomeController : Controller
{
    [HttpGet("")]
    public IActionResult Index()
    {
        return Redirect("/admin/clinics");
    }
}
