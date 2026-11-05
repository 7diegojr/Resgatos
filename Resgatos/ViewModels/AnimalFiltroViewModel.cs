using Resgatos.Models;

namespace Resgatos.ViewModels;

// Filtros da página de Adoções. Vêm da query string (?especies=Gato&sexos=Femea...),
// então a URL filtrada pode ser copiada e compartilhada.
// Dentro de um mesmo grupo vale "OU" (Gato ou Cachorro); entre grupos vale "E".
public class AnimalFiltroViewModel
{
    public List<EspecieAnimal> Especies { get; set; } = new();
    public List<SexoAnimal> Sexos { get; set; } = new();
    public List<FaixaEtariaAnimal> FaixasEtarias { get; set; } = new();
    public List<PorteAnimal> Portes { get; set; } = new();

    public bool TemFiltroAtivo =>
        Especies.Count > 0 || Sexos.Count > 0 || FaixasEtarias.Count > 0 || Portes.Count > 0;
}
