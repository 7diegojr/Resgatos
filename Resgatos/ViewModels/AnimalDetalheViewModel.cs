
using Resgatos.Models;

namespace Resgatos.ViewModels;

// Tudo o que o modal de detalhes mostra. Herda do card (nome, espécie, sexo, porte, idade,
// link do WhatsApp) e acrescenta o que só aparece quando a pessoa abre o animal.
// Herdar do card também permite reaproveitar o WhatsAppHelper sem mudar nada nele.
public class AnimalDetalheViewModel : AnimalCardViewModel
{
    public FaixaEtariaAnimal FaixaEtaria { get; set; }
    public string Cor { get; set; }
    public string Historia { get; set; }
    public string Observacoes { get; set; }

    // Caminhos das fotos a partir da wwwroot, com a capa sempre na primeira posição
    public List<string> Fotos { get; set; } = new();

    // Saúde
    public bool AntipulgasOk { get; set; }
    public bool VermifugadoOk { get; set; }
    public bool VacinadoOk { get; set; }
    public bool FivTestado { get; set; }
    public ResultadoFiv? ResultadoFiv { get; set; }
    public bool FelvTestado { get; set; }
    public ResultadoFelv? ResultadoFelv { get; set; }
    public bool CastradoOk { get; set; }
    public DateTime? DataCastracao { get; set; }
}
