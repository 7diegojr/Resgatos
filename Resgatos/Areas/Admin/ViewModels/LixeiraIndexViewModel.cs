using Resgatos.Models;

namespace Resgatos.Areas.Admin.ViewModels;

// O que a lixeira está mostrando. Os nomes vão na URL (?tipo=Transparencia), então a aba pode ser copiada.
public enum TipoLixeira
{
    Animais = 0,        // padrão
    Transparencia = 1
}

// Filtros da lixeira. Vêm da query string (?tipo=Animais&busca=mel&pagina=2).
public class LixeiraFiltroViewModel
{
    public TipoLixeira Tipo { get; set; } = TipoLixeira.Animais;
    public string Busca { get; set; }
    public int Pagina { get; set; } = 1;

    // Parâmetros da URL com tudo o que está aplicado agora (menos a página, que volta para a 1).
    // "sem" tira alguns deles: é assim que as abas e os formulários (que já carregam o próprio campo) funcionam.
    public Dictionary<string, string> ParaRota(params string[] sem)
    {
        var d = new Dictionary<string, string>();

        void Adicionar(string nome, string valor)
        {
            if (!string.IsNullOrEmpty(valor) && !sem.Contains(nome)) d[nome] = valor;
        }

        Adicionar("tipo", Tipo.ToString());
        Adicionar("busca", Busca);

        return d;
    }
}

// Uma linha da aba de animais. Só o que a lista mostra.
public class LixeiraAnimalItemViewModel
{
    public int IdAnimal { get; set; }
    public string Nome { get; set; }
    public string Cor { get; set; }
    public EspecieAnimal Especie { get; set; }
    public SexoAnimal Sexo { get; set; }
    public FaixaEtariaAnimal FaixaEtaria { get; set; }
    public string IdadeEstimada { get; set; }
    public StatusAnimal Status { get; set; }
    public int QtdFotos { get; set; }
    public DateTime ExcluidoEm { get; set; }

    // Caminho da foto de capa a partir da wwwroot (null = sem foto)
    public string FotoCapa { get; set; }
}

// Uma linha da aba de transparência. Só o que a lista mostra.
public class LixeiraGastoItemViewModel
{
    public int IdTransparencia { get; set; }
    public string Titulo { get; set; }
    public CategoriaTransparencia Categoria { get; set; }
    public decimal Valor { get; set; }
    public DateTime Data { get; set; }
    public DateTime ExcluidoEm { get; set; }

    // Caminhos a partir da wwwroot (null = não anexado)
    public string Imagem { get; set; }
    public string Comprovante { get; set; }
}

public class LixeiraIndexViewModel
{
    public LixeiraFiltroViewModel Filtro { get; set; } = new();
    public List<LixeiraAnimalItemViewModel> Animais { get; set; } = new();
    public List<LixeiraGastoItemViewModel> Gastos { get; set; } = new();

    // Quantos itens há na lixeira em cada aba (alimenta as abas; não depende da busca)
    public int QuantidadeAnimais { get; set; }
    public int QuantidadeGastos { get; set; }
    public int TotalGeral => QuantidadeAnimais + QuantidadeGastos;

    // Paginação (sobre o resultado já filtrado da aba atual)
    public int TotalFiltrado { get; set; }
    public int Pagina { get; set; } = 1;
    public int TamanhoPagina { get; set; } = 10;
    public int TotalPaginas => Math.Max(1, (int)Math.Ceiling(TotalFiltrado / (double)TamanhoPagina));
    public int PrimeiroItem => TotalFiltrado == 0 ? 0 : (Pagina - 1) * TamanhoPagina + 1;
    public int UltimoItem => Math.Min(Pagina * TamanhoPagina, TotalFiltrado);
}
