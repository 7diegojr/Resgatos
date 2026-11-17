using Microsoft.AspNetCore.Mvc;

namespace Resgatos.Areas.Admin.Controllers;

// Página em branco por enquanto, só para o menu lateral funcionar. Itens excluídos (a definir junto com o CRUD).
public class LixeiraController : AdminBaseController
{
    public IActionResult Index()
    {
        return View();
    }
}
