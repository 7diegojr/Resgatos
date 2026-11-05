namespace Resgatos.ViewModels;

public class AdocoesViewModel
{
    public AnimalFiltroViewModel Filtro { get; set; } = new();
    public List<AnimalCardViewModel> Animais { get; set; } = new();
}
