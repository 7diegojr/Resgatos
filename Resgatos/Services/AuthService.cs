using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Resgatos.Data;
using Resgatos.Models;

namespace Resgatos.Services;

// Regras de autenticação do admin: gerar hash de senha e validar o login.
// Usa só o PasswordHasher do Identity (PBKDF2 com salt automático), sem o ASP.NET Identity completo.
public class AuthService
{
    private readonly AppDbContext _context;
    private readonly PasswordHasher<UsuarioAdmin> _hasher = new();

    // Hash "de mentira" usado quando o e-mail não existe, para o tempo de resposta
    // ser parecido nos dois casos (senão dá para descobrir quais e-mails existem).
    private static readonly string _hashFalso = new PasswordHasher<UsuarioAdmin>()
        .HashPassword(new UsuarioAdmin(), Guid.NewGuid().ToString());

    public AuthService(AppDbContext context)
    {
        _context = context;
    }

    public string GerarHash(string senha)
    {
        return _hasher.HashPassword(new UsuarioAdmin(), senha);
    }

    // Retorna o usuário se e-mail e senha estiverem corretos (e o usuário estiver ativo); senão, null.
    public async Task<UsuarioAdmin> ValidarCredenciaisAsync(string email, string senha)
    {
        var emailNormalizado = email.Trim().ToLowerInvariant();

        var usuario = await _context.UsuariosAdmin
            .FirstOrDefaultAsync(u => u.Email == emailNormalizado);

        if (usuario == null)
        {
            _hasher.VerifyHashedPassword(new UsuarioAdmin(), _hashFalso, senha);
            return null;
        }

        var resultado = _hasher.VerifyHashedPassword(usuario, usuario.SenhaHash, senha);

        if (resultado == PasswordVerificationResult.Failed || !usuario.Ativo)
            return null;

        // Se o algoritmo do hash evoluiu desde que a senha foi salva, atualiza o hash agora
        if (resultado == PasswordVerificationResult.SuccessRehashNeeded)
        {
            usuario.SenhaHash = GerarHash(senha);
            await _context.SaveChangesAsync();
        }

        return usuario;
    }
}
