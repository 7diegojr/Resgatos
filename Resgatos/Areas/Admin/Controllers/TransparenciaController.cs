using Microsoft.AspNetCore.Mvc;

namespace Resgatos.Areas.Admin.Controllers;

// Página em branco por enquanto, só para o menu lateral funcionar. Sprint 15: CRUD de transparência.
public class TransparenciaController : AdminBaseController
{
    public IActionResult Index()
    {
        return View();
    }
}
