using Microsoft.AspNetCore.Mvc;

namespace Resgatos.Areas.Admin.Controllers;

// Página em branco por enquanto, só para o menu lateral funcionar. Sprint 14: gestão de adotados.
public class AdotadosController : AdminBaseController
{
    public IActionResult Index()
    {
        return View();
    }
}
