using Microsoft.AspNetCore.Mvc;

namespace Resgatos.Areas.Admin.Controllers;

// Página inicial do admin (/admin). Por enquanto em branco; os atalhos entram nas próximas sprints.
public class DashboardController : AdminBaseController
{
    public IActionResult Index()
    {
        return View();
    }
}
