using System.ComponentModel.DataAnnotations;

namespace Resgatos.Areas.Admin.ViewModels;

public class LoginViewModel
{
    [Required(ErrorMessage = "Informe o e-mail")]
    [EmailAddress(ErrorMessage = "E-mail inválido")]
    [Display(Name = "E-mail")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Informe a senha")]
    [DataType(DataType.Password)]
    [Display(Name = "Senha")]
    public string Senha { get; set; } = string.Empty;

    // Para onde voltar depois do login (preenchido quando o usuário tentou abrir uma página protegida)
    public string ReturnUrl { get; set; }
}
