using Microsoft.EntityFrameworkCore;
using Resgatos.Models;
using Resgatos.Services;

namespace Resgatos.Data;

// Cria o usuário administrador padrão automaticamente ao iniciar a aplicação (sem pedir nada no terminal).
// Se o e-mail já existir no banco, não faz nada.
public static class AdminSeed
{
    private const string Nome = "Junin";
    private const string Email = "junin@gmail.com";
    private const string Senha = "junin123";

    public static async Task CriarAsync(AppDbContext context, AuthService auth)
    {
        if (await context.UsuariosAdmin.AnyAsync(u => u.Email == Email)) return;

        context.UsuariosAdmin.Add(new UsuarioAdmin
        {
            Nome = Nome,
            Email = Email,
            SenhaHash = auth.GerarHash(Senha),
            Ativo = true
        });
        await context.SaveChangesAsync();

        Console.WriteLine($"Administrador '{Nome}' ({Email}) criado.");
    }
}