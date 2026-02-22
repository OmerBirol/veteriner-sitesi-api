using Microsoft.AspNetCore.Mvc;

namespace VetRandevu.Api.ViewComponents.AdminLayoutViewComponents;

public class _AdminLayoutScriptComponentPartial : ViewComponent
{
    public IViewComponentResult Invoke()
    {
        return View();
    }
}
