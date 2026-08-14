using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Resgatos.Models;

public class Adotado
{
    [Key]
    public int IdAdotado { get; set; }

    [Required]
    public int IdAnimal { get; set; }

    [ForeignKey(nameof(IdAnimal))]
    public Animal? Animal { get; set; }

    [Required(ErrorMessage = "O nome é obrigatório")]
    [StringLength(150)]
    [Display(Name = "Nome")]
    public string NomeAdotante { get; set; } = string.Empty;

    [Required(ErrorMessage = "O telefone é obrigatório")]
    [StringLength(20)]
    [Phone]
    [Display(Name = "Telefone")]
    public string TelefoneAdotante { get; set; } = string.Empty;

    [Required(ErrorMessage = "O email é obrigatório")]
    [StringLength(150)]
    [EmailAddress]
    [Display(Name = "Email")]
    public string EmailAdotante { get; set; } = string.Empty;

    [Required(ErrorMessage = "O endereço é obrigatório")]
    [StringLength(255)]
    [Display(Name = "Endereço completo")]
    public string EnderecoAdotante { get; set; } = string.Empty;

    [Required(ErrorMessage = "A data de adoção é obrigatória")]
    [DataType(DataType.Date)]
    [Display(Name = "Data da adoção")]
    public DateTime DataAdocao { get; set; }

    [Required(ErrorMessage = "O valor da taxa é obrigatório")]
    [Range(0.01, 99999999.99, ErrorMessage = "O valor deve ser maior que R$0,00")]
    [Column(TypeName = "decimal(10,2)")]
    [Display(Name = "Valor da taxa")]
    public decimal ValorTaxa { get; set; }

    [Display(Name = "A taxa foi parcelada?")]
    public bool TaxaParcelada { get; set; } = false;

    [Range(1, 999, ErrorMessage = "A quantidade de parcelas deve ser maior que zero")]
    [Display(Name = "Em quantas parcelas?")]
    public int? QtdParcelas { get; set; }

    [Display(Name = "Anexo do termo assinado")]
    public string? TermoAssinado { get; set; }

    [Display(Name = "Observações")]
    public string? Observacoes { get; set; }

    [Required]
    [DataType(DataType.DateTime)]
    [Display(Name = "Criado em")]
    public DateTime CriadoEm { get; set; } = DateTime.Now;
}