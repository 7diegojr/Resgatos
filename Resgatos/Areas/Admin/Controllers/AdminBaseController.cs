using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Resgatos.Areas.Admin.Controllers;

// Base de todos os controllers do admin: já nasce na Area "Admin" e exige login.
// Todo controller novo do admin herda daqui e fica protegido sem precisar lembrar do [Authorize].
// NoStore: impede o navegador de guardar a página em cache (depois do logout, o "voltar" não mostra o painel).
[Area("Admin")]
[Authorize]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public abstract class AdminBaseController : Controller
{
}
