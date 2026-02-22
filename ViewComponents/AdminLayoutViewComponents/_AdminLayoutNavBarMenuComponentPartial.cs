using Microsoft.AspNetCore.Mvc;

namespace VetRandevu.Api.ViewComponents.AdminLayoutViewComponents;

public class _AdminLayoutNavBarMenuComponentPartial : ViewComponent
{
    public IViewComponentResult Invoke()
    {
        return View();
    }
}
