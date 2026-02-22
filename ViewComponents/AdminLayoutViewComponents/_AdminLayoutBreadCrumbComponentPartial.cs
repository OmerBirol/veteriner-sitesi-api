using Microsoft.AspNetCore.Mvc;

namespace VetRandevu.Api.ViewComponents.AdminLayoutViewComponents;

public class _AdminLayoutBreadCrumbComponentPartial : ViewComponent
{
    public IViewComponentResult Invoke()
    {
        return View();
    }
}
