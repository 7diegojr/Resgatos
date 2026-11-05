using Resgatos.Models;

namespace Resgatos.ViewModels;

// Só o que o card precisa. Evita trazer do banco a história, os dados de saúde etc.
public class AnimalCardViewModel
{
    public int IdAnimal { get; set; }
    public string Nome { get; set; }
    public EspecieAnimal Especie { get; set; }
    public SexoAnimal Sexo { get; set; }
    public PorteAnimal Porte { get; set; }
    public string IdadeEstimada { get; set; }

    // Caminho da foto de capa a partir da wwwroot, ex.: "uploads/animais/sansao.jpg"
    // (null se o animal ainda não tem foto)
    public string FotoCapa { get; set; }

    // Link do WhatsApp com a mensagem pronta (preenchido no controller)
    public string LinkWhatsApp { get; set; }
}
