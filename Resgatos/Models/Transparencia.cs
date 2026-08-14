using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Resgatos.Models;

public class Transparencia
{
    [Key]
    public int IdTransparencia { get; set; }

    [Required]
    public CategoriaTransparencia Categoria { get; set; }

    [Required(ErrorMessage = "O título é obrigatório")]
    [StringLength(255, MinimumLength = 3, ErrorMessage = "O título deve ter entre 3 e 255 caracteres")]
    [Display(Name = "Título")]
    public string Titulo { get; set; } = string.Empty;

    [Required(ErrorMessage = "O valor é obrigatório")]
    [Range(0.01, 99999999.99, ErrorMessage = "O valor deve ser maior que R$0,00")]
    [DataType(DataType.Currency)]
    [Column(TypeName = "decimal(10,2)")]
    public decimal Valor { get; set; }

    [Required(ErrorMessage = "A data é obrigatória")]
    [DataType(DataType.Date)]
    public DateTime Data { get; set; }

    public string? Comprovante { get; set; }

    public string? Imagem { get; set; }

    [Required]
    [DataType(DataType.DateTime)]
    [Display(Name = "Criado em")]
    public DateTime CriadoEm { get; set; } = DateTime.Now;
}