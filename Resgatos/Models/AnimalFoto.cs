using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Resgatos.Models;

public class AnimalFoto
{
    [Key]
    public int IdAnimalFoto { get; set; }

    [Required]
    public int IdAnimal { get; set; }

    [ForeignKey(nameof(IdAnimal))]
    public Animal Animal { get; set; } = null!;

    [Required(ErrorMessage = "A foto é obrigatória")]
    public string Foto { get; set; } = string.Empty;

    [Display(Name = "Essa foto deve ser a capa?")]
    public bool Capa { get; set; } = false;
}