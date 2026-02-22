using Microsoft.AspNetCore.Mvc;

namespace VetRandevu.Api.ViewComponents.ClinicLayoutViewComponents;

public class _ClinicAdminLayoutNavBarMenuComponentPartial : ViewComponent
{
    public IViewComponentResult Invoke()
    {
        return View();
    }
}
