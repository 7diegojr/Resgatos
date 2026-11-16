using Resgatos.Models;

namespace Resgatos.ViewModels;

// Período do filtro. "Mes" = mês atual do calendário; "Meses" = últimos 3 meses.
public enum PeriodoTransparencia
{
    Todos = 0,
    Semana = 1,
    Mes = 2,
    Meses = 3
}

public enum OrdenacaoTransparencia
{
    MaisRecentes = 0,
    MaisAntigos = 1,
    MaiorValor = 2,
    MenorValor = 3
}

// Filtros da página de Transparência. Vêm da query string
// (?categorias=Alimentacao&periodo=Mes&ordem=MaiorValor), então a URL filtrada pode ser copiada e compartilhada.
// Dentro das categorias vale "OU"; entre categoria e período vale "E".
public class TransparenciaFiltroViewModel
{
    public List<CategoriaTransparencia> Categorias { get; set; } = new();
    public PeriodoTransparencia Periodo { get; set; } = PeriodoTransparencia.Todos;
    public OrdenacaoTransparencia Ordem { get; set; } = OrdenacaoTransparencia.MaisRecentes;

    // "Algo diferente do padrão": é o que decide se o botão "Limpar" aparece
    public bool TemFiltroAtivo =>
        Categorias.Count > 0
        || Periodo != PeriodoTransparencia.Todos
        || Ordem != OrdenacaoTransparencia.MaisRecentes;
}

// Um lançamento (card) da lista.
public class TransparenciaItemViewModel
{
    public int IdTransparencia { get; set; }
    public CategoriaTransparencia Categoria { get; set; }
    public string Titulo { get; set; } = string.Empty;
    public decimal Valor { get; set; }
    public DateTime Data { get; set; }
    public string Comprovante { get; set; }   // caminho a partir da wwwroot (ou null)
    public string Imagem { get; set; }        // caminho a partir da wwwroot (ou null)
}

// Uma linha do "Em que usamos os recursos": quanto foi gasto em cada categoria.
public class TransparenciaCategoriaResumoViewModel
{
    public CategoriaTransparencia Categoria { get; set; }
    public decimal Total { get; set; }
    public int Quantidade { get; set; }
    public decimal Percentual { get; set; }   // 0 a 100, em relação ao total geral
}

public class TransparenciaViewModel
{
    public TransparenciaFiltroViewModel Filtro { get; set; } = new();

    // Lista (respeita o filtro)
    public List<TransparenciaItemViewModel> Lancamentos { get; set; } = new();
    public decimal TotalFiltrado { get; set; }

    // Cards do topo: total e quantidade do MÊS ATUAL (não mudam com o filtro)
    public DateTime MesReferencia { get; set; }
    public decimal TotalMes { get; set; }
    public int QuantidadeMes { get; set; }
    public DateTime? UltimaData { get; set; }   // data do último gasto lançado

    // Resumo por categoria (todos os gastos, não muda com o filtro)
    public decimal TotalGeral { get; set; }
    public int QuantidadeGeral { get; set; }
    public List<TransparenciaCategoriaResumoViewModel> PorCategoria { get; set; } = new();
}
