using System.ComponentModel.DataAnnotations;
using Resgatos.Models;

namespace Resgatos.Areas.Admin.ViewModels;

// Dados enviados pelo modal "Marcar como adotado" (Animais > Ações).
// Valor é texto ("150,00") para a vírgula do Brasil funcionar em qualquer servidor; o controller converte.
public class AdotadoFormViewModel
{
    public int IdAnimal { get; set; }

    [Required(ErrorMessage = "Informe o nome de quem está adotando.")]
    public string NomeAdotante { get; set; }

    [Required(ErrorMessage = "Informe o telefone.")]
    public string Telefone { get; set; }

    [Required(ErrorMessage = "Informe o e-mail.")]
    public string Email { get; set; }

    [Required(ErrorMessage = "Informe o endereço completo.")]
    public string Endereco { get; set; }

    [Required(ErrorMessage = "Informe a data da adoção.")]
    [DataType(DataType.Date)]
    public DateTime? DataAdocao { get; set; }

    [Required(ErrorMessage = "Informe o valor da taxa.")]
    public string Valor { get; set; }

    public bool TaxaParcelada { get; set; }
    public int? QtdParcelas { get; set; }
    public string Observacoes { get; set; }

    // Termo assinado (opcional): imagem ou PDF
    public IFormFile Termo { get; set; }
}

// Filtro rápido do termo
public enum FiltroTermoAdotado
{
    Pendente = 1,
    Anexado = 2
}

// Filtros da página Adotados (?busca=mel&especie=Gato&termo=Pendente)
public class AdotadosFiltroViewModel
{
    public string Busca { get; set; }
    public EspecieAnimal? Especie { get; set; }
    public FiltroTermoAdotado? Termo { get; set; }
    public int Pagina { get; set; } = 1;

    // Parâmetros da URL com o que está aplicado (a página volta para a 1)
    public Dictionary<string, string> ParaRota()
    {
        var d = new Dictionary<string, string>();
        if (!string.IsNullOrWhiteSpace(Busca)) d["busca"] = Busca;
        if (Especie.HasValue) d["especie"] = Especie.Value.ToString();
        if (Termo.HasValue) d["termo"] = Termo.Value.ToString();
        return d;
    }
}

// Um card da página Adotados
public class AdotadoCardViewModel
{
    public int IdAdotado { get; set; }
    public int IdAnimal { get; set; }
    public string NomeAnimal { get; set; }
    public EspecieAnimal Especie { get; set; }
    public SexoAnimal Sexo { get; set; }
    public string FotoCapa { get; set; }

    public string NomeAdotante { get; set; }
    public string TelefoneAdotante { get; set; }
    public string EnderecoAdotante { get; set; }
    public DateTime DataAdocao { get; set; }

    public decimal ValorTaxa { get; set; }
    public bool TaxaParcelada { get; set; }
    public int? QtdParcelas { get; set; }

    public bool TermoAnexado { get; set; }
}

public class AdotadosIndexViewModel
{
    public AdotadosFiltroViewModel Filtro { get; set; } = new();
    public List<AdotadoCardViewModel> Adotados { get; set; } = new();

    public int TotalGeral { get; set; }
    public int TotalFiltrado { get; set; }
    public int Pagina { get; set; } = 1;
    public int TamanhoPagina { get; set; } = 12;
    public int TotalPaginas => Math.Max(1, (int)Math.Ceiling(TotalFiltrado / (double)TamanhoPagina));
    public int PrimeiroItem => TotalFiltrado == 0 ? 0 : (Pagina - 1) * TamanhoPagina + 1;
    public int UltimoItem => Math.Min(Pagina * TamanhoPagina, TotalFiltrado);
}
