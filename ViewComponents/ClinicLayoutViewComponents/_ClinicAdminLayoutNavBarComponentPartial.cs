using Microsoft.AspNetCore.Mvc;

namespace VetRandevu.Api.ViewComponents.ClinicLayoutViewComponents;

public class _ClinicAdminLayoutNavBarComponentPartial : ViewComponent
{
    public IViewComponentResult Invoke()
    {
        return View();
    }
}
