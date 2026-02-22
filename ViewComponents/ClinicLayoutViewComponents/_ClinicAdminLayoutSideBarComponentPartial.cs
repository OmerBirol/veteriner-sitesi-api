using Microsoft.AspNetCore.Mvc;

namespace VetRandevu.Api.ViewComponents.ClinicLayoutViewComponents;

public class _ClinicAdminLayoutSideBarComponentPartial : ViewComponent
{
    public IViewComponentResult Invoke()
    {
        return View();
    }
}
