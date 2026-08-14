using System.ComponentModel.DataAnnotations;

namespace Resgatos.Models;
public class UsuarioAdmin
{
    [Key]
    public int IdUsuarioAdmin { get; set; }
    
    [Required(ErrorMessage = "O nome é obrigatório")]
    [StringLength(150)]
    public string Nome { get; set; } = string.Empty;

    [Required(ErrorMessage = "O email é obrigatório")]
    [StringLength(150)]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "A senha é obrigatória")]
    [StringLength(255)]
    public string SenhaHash { get; set; } = string.Empty;

    public bool Ativo { get; set; } = true;
}