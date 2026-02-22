using Microsoft.AspNetCore.Mvc;

namespace VetRandevu.Api.ViewComponents.AdminLayoutViewComponents;

public class _AdminLayoutNavBarComponentPartial : ViewComponent
{
    public IViewComponentResult Invoke()
    {
        return View();
    }
}
