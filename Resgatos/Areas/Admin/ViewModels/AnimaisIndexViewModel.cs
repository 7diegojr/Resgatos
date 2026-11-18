using Resgatos.Models;

namespace Resgatos.Areas.Admin.ViewModels;

// Ordens da lista. Os nomes vão na URL (?ordem=NomeAZ), então a página filtrada pode ser copiada.
public enum OrdenacaoAnimaisAdmin
{
    MaisRecentes = 0,   // padrão
    MaisAntigos = 1,
    NomeAZ = 2,
    NomeZA = 3
}

// Filtros da lista de animais do admin. Vêm da query string (?busca=mel&especie=Gato&status=Disponivel...).
// Nada preenchido = "todos".
public class AnimaisFiltroAdminViewModel
{
    public string Busca { get; set; }
    public EspecieAnimal? Especie { get; set; }
    public StatusAnimal? Status { get; set; }
    public OrdenacaoAnimaisAdmin Ordem { get; set; } = OrdenacaoAnimaisAdmin.MaisRecentes;
    public int Pagina { get; set; } = 1;

    // Status não conta: ele tem as suas próprias abas, e o "Limpar filtros" só mexe na busca/espécie/ordem
    public bool TemFiltroAtivo =>
        !string.IsNullOrWhiteSpace(Busca) || Especie.HasValue || Ordem != OrdenacaoAnimaisAdmin.MaisRecentes;
}

// Uma linha da tabela. Só o que a lista mostra (não traz história, observações etc.).
public class AnimalAdminItemViewModel
{
    public int IdAnimal { get; set; }
    public string Nome { get; set; }
    public string Cor { get; set; }
    public EspecieAnimal Especie { get; set; }
    public SexoAnimal Sexo { get; set; }
    public FaixaEtariaAnimal FaixaEtaria { get; set; }
    public string IdadeEstimada { get; set; }
    public StatusAnimal Status { get; set; }
    public DateTime CriadoEm { get; set; }
    public int QtdFotos { get; set; }

    // Caminho da foto de capa a partir da wwwroot (null = sem foto)
    public string FotoCapa { get; set; }

    // Saúde (para mostrar o que ainda está pendente)
    public bool AntipulgasOk { get; set; }
    public bool VermifugadoOk { get; set; }
    public bool VacinadoOk { get; set; }
    public bool CastradoOk { get; set; }
    public bool FivTestado { get; set; }
    public bool FelvTestado { get; set; }

    // O que ainda falta fazer. FIV/FeLV só entram para gatos.
    public List<string> Pendencias
    {
        get
        {
            var lista = new List<string>();
            if (!AntipulgasOk) lista.Add("Antipulgas");
            if (!VermifugadoOk) lista.Add("Vermífugo");
            if (!VacinadoOk) lista.Add("Vacina");
            if (!CastradoOk) lista.Add("Castração");
            if (Especie == EspecieAnimal.Gato)
            {
                if (!FivTestado) lista.Add("FIV");
                if (!FelvTestado) lista.Add("FeLV");
            }
            return lista;
        }
    }
}

public class AnimaisIndexViewModel
{
    public AnimaisFiltroAdminViewModel Filtro { get; set; } = new();
    public List<AnimalAdminItemViewModel> Animais { get; set; } = new();

    // Quantidade de animais por status (alimenta as abas; não depende da busca)
    public Dictionary<StatusAnimal, int> ContagemPorStatus { get; set; } = new();
    public int TotalGeral { get; set; }

    // Paginação (sobre o resultado já filtrado)
    public int TotalFiltrado { get; set; }
    public int Pagina { get; set; } = 1;
    public int TamanhoPagina { get; set; } = 10;
    public int TotalPaginas => Math.Max(1, (int)Math.Ceiling(TotalFiltrado / (double)TamanhoPagina));
    public int PrimeiroItem => TotalFiltrado == 0 ? 0 : (Pagina - 1) * TamanhoPagina + 1;
    public int UltimoItem => Math.Min(Pagina * TamanhoPagina, TotalFiltrado);
}
