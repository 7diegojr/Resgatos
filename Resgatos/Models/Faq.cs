using System.ComponentModel.DataAnnotations;

namespace Resgatos.Models;

public class Faq
{
    [Key]
    public int IdFaq { get; set; }

    [Required]
    [StringLength(255)]
    public string Pergunta { get; set; } = string.Empty;

    [Required]
    public string Resposta { get; set; } = string.Empty;

    public bool Ativo { get; set; } = true;
}