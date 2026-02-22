using Microsoft.AspNetCore.Mvc;

namespace VetRandevu.Api.ViewComponents.AdminLayoutViewComponents;

public class _AdminLayoutSideBarComponentPartial : ViewComponent
{
    public IViewComponentResult Invoke()
    {
        return View();
    }
}
