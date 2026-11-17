using Microsoft.AspNetCore.Mvc;

namespace Resgatos.Areas.Admin.Controllers;

// Página em branco por enquanto, só para o menu lateral funcionar. Sprint 13: CRUD de animais e fotos.
public class AnimaisController : AdminBaseController
{
    public IActionResult Index()
    {
        return View();
    }
}
