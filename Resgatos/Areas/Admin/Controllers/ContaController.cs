using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Resgatos.Areas.Admin.ViewModels;
using Resgatos.Services;

namespace Resgatos.Areas.Admin.Controllers;

// Login e logout do admin. Herda de Controller (e não de AdminBaseController)
// porque o login precisa ser acessível por quem ainda NÃO está logado.
[Area("Admin")]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public class ContaController : Controller
{
    private readonly AuthService _auth;

    public ContaController(AuthService auth)
    {
        _auth = auth;
    }

    [HttpGet]
    [AllowAnonymous]
    public IActionResult Login(string returnUrl = null)
    {
        // Já está logado: não faz sentido ver a tela de login
        if (User.Identity?.IsAuthenticated == true)
            return RedirecionarPosLogin(returnUrl);

        return View(new LoginViewModel { ReturnUrl = returnUrl });
    }

    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model)
    {
        if (!ModelState.IsValid)
            return View(model);

        var usuario = await _auth.ValidarCredenciaisAsync(model.Email, model.Senha);

        if (usuario == null)
        {
            // Mensagem única de propósito: não revela se o erro foi no e-mail ou na senha
            ModelState.AddModelError(string.Empty, "E-mail ou senha incorretos.");
            model.Senha = string.Empty;
            return View(model);
        }

        // "Crachá" do usuário que fica dentro do cookie
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, usuario.IdUsuarioAdmin.ToString()),
            new Claim(ClaimTypes.Name, usuario.Nome),
            new Claim(ClaimTypes.Email, usuario.Email)
        };
        var identidade = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);

        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            new ClaimsPrincipal(identidade));

        return RedirecionarPosLogin(model.ReturnUrl);
    }

    // POST (e não GET) para que um link malicioso não consiga deslogar a pessoa
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return RedirectToAction(nameof(Login));
    }

    // Só aceita voltar para endereços do próprio site (evita "open redirect" para sites externos)
    private IActionResult RedirecionarPosLogin(string returnUrl)
    {
        if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
            return LocalRedirect(returnUrl);

        return RedirectToAction("Index", "Dashboard");
    }
}
