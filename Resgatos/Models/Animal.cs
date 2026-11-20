using System.ComponentModel.DataAnnotations;

namespace Resgatos.Models;

public class Animal
{
    [Key]
    public int IdAnimal { get; set; }

    [Required]
    [Display(Name = "Espécie")]
    public EspecieAnimal Especie { get; set; }

    [Required(ErrorMessage = "O nome é obrigatório")]
    [StringLength(100)]
    public string Nome { get; set; }

    [Required]
    public SexoAnimal Sexo { get; set; }

    [Required]
    public PorteAnimal Porte { get; set; }

    [Required(ErrorMessage = "A cor do animal é obrigatória")]
    [StringLength(100)]
    public string Cor { get; set; }

    [Required]
    [Display(Name = "Faixa etária")]
    public FaixaEtariaAnimal FaixaEtaria { get; set; }

    [Required(ErrorMessage = "A idade estimada é obrigatória")]
    [StringLength(50)]
    [Display(Name = "Idade estimada")]
    public string IdadeEstimada { get; set; }

    [Display(Name = "História")]
    public string? Historia { get; set; }

    [Required]
    public StatusAnimal Status { get; set; }

    // Só o mês e o ano importam: guardamos sempre o dia 1 do mês de chegada.
    // O tempo que o animal está no Resgatos é calculado a partir daqui (Helpers/FormatacaoExtensions.ToTempoDecorrido).
    [DataType(DataType.Date)]
    [Display(Name = "Data de chegada ao Resgatos")]
    public DateTime? DataChegada { get; set; }

    [Display(Name = "Antipulgas em dia")]
    public bool AntipulgasOk { get; set; } = false;

    [Display(Name = "Vermifugado")]
    public bool VermifugadoOk { get; set; } = false;

    [Display(Name = "Vacinado")]
    public bool VacinadoOk { get; set; } = false;

    [Display(Name = "FIV testado")]
    public bool FivTestado { get; set; } = false;

    [Display(Name = "Resultado da FIV")]
    public ResultadoFiv? ResultadoFiv { get; set; }

    [Display(Name = "FeLV testado")]
    public bool FelvTestado { get; set; } = false;

    [Display(Name = "Resultado da FeLV")]
    public ResultadoFelv? ResultadoFelv { get; set; }

    [Display(Name = "Castrado")]
    public bool CastradoOk { get; set; } = false;

    [Display(Name = "Observações")]
    public string? Observacoes { get; set; }

    [Required]
    [DataType(DataType.DateTime)]
    [Display(Name = "Criado em")]
    public DateTime CriadoEm { get; set; } = DateTime.Now;

    // Lixeira (exclusão lógica): preenchido = o animal está na lixeira e some do site e das listas do admin.
    [DataType(DataType.DateTime)]
    [Display(Name = "Excluído em")]
    public DateTime? ExcluidoEm { get; set; }

    public virtual ICollection<AnimalFoto> Fotos { get; set; } = new List<AnimalFoto>();
    public virtual Adotado? Adotado { get; set; }
}