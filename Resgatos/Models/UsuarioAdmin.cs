using Microsoft.AspNetCore.Identity;

namespace Resgatos.Models;
public class UsuarioAdmin :IdentityUser<int>
{
    public string Nome { get; set; } = string.Empty;

    public bool Ativo { get; set; } = true;
}