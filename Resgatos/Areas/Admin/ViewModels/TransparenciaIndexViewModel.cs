using Resgatos.Models;

namespace Resgatos.Areas.Admin.ViewModels;

// Ordens da lista. Os nomes vão na URL (?ordem=MaiorValor), então a página filtrada pode ser copiada.
public enum OrdenacaoTransparenciaAdmin
{
    MaisRecentes = 0,   // padrão: o gasto mais recente aparece primeiro
    MaisAntigos = 1,
    MaiorValor = 2,
    MenorValor = 3,
    TituloAZ = 4,
    TituloZA = 5
}

// Período do filtro (mesmas regras da página pública). Sem período = "todo o período".
// "Mes" = mês atual do calendário; "Meses" = últimos 3 meses.
public enum PeriodoTransparenciaAdmin
{
    Semana = 1,
    Mes = 2,
    Meses = 3
}

// Filtro de "tem ou não tem" (comprovante e foto do gasto)
public enum FiltroAnexoAdmin
{
    Com = 1,
    Sem = 2
}

// Filtros da lista de gastos do admin. Vêm da query string (?busca=racao&categoria=Alimentacao&periodo=Mes...).
// Nada preenchido = "todos".
public class TransparenciaFiltroAdminViewModel
{
    public string Busca { get; set; }
    public CategoriaTransparencia? Categoria { get; set; }
    public PeriodoTransparenciaAdmin? Periodo { get; set; }
    public FiltroAnexoAdmin? Comprovante { get; set; }
    public FiltroAnexoAdmin? Foto { get; set; }
    public OrdenacaoTransparenciaAdmin Ordem { get; set; } = OrdenacaoTransparenciaAdmin.MaisRecentes;
    public int Pagina { get; set; } = 1;

    // Quantos filtros estão aplicados (a busca não conta: ela tem a própria caixa). Decide se aparece o "Limpar filtros".
    public int QuantidadeFiltrosAtivos =>
        (Categoria.HasValue ? 1 : 0) +
        (Periodo.HasValue ? 1 : 0) +
        (Comprovante.HasValue ? 1 : 0) +
        (Foto.HasValue ? 1 : 0) +
        (Ordem != OrdenacaoTransparenciaAdmin.MaisRecentes ? 1 : 0);

    // Quantos filtros aplicados só existem no modal (categoria, período e ordenação já aparecem na barra).
    // É o número que aparece no botão "Filtrar": mostra que há filtros "escondidos" valendo.
    public int QuantidadeFiltrosAvancados =>
        (Comprovante.HasValue ? 1 : 0) +
        (Foto.HasValue ? 1 : 0);

    // Parâmetros da URL com tudo o que está aplicado agora (menos a página, que volta para a 1).
    // "sem" tira alguns deles: é assim que o "x" de cada filtro aplicado, as abas de categoria e os
    // formulários (que só carregam o que não está nos próprios campos) funcionam.
    public Dictionary<string, string> ParaRota(params string[] sem)
    {
        var d = new Dictionary<string, string>();

        void Adicionar(string nome, string valor)
        {
            if (!string.IsNullOrEmpty(valor) && !sem.Contains(nome)) d[nome] = valor;
        }

        Adicionar("busca", Busca);
        Adicionar("categoria", Categoria?.ToString());
        Adicionar("periodo", Periodo?.ToString());
        Adicionar("comprovante", Comprovante?.ToString());
        Adicionar("foto", Foto?.ToString());
        if (Ordem != OrdenacaoTransparenciaAdmin.MaisRecentes) Adicionar("ordem", Ordem.ToString());

        return d;
    }
}

// Uma linha da tabela. Só o que a lista mostra.
public class TransparenciaAdminItemViewModel
{
    public int IdTransparencia { get; set; }
    public string Titulo { get; set; }
    public CategoriaTransparencia Categoria { get; set; }
    public decimal Valor { get; set; }
    public DateTime Data { get; set; }

    // Caminhos a partir da wwwroot (null = não anexado)
    public string Comprovante { get; set; }
    public string Imagem { get; set; }
}

public class TransparenciaIndexViewModel
{
    public TransparenciaFiltroAdminViewModel Filtro { get; set; } = new();
    public List<TransparenciaAdminItemViewModel> Lancamentos { get; set; } = new();

    // Quantidade de gastos por categoria (alimenta as abas; não depende dos filtros)
    public Dictionary<CategoriaTransparencia, int> ContagemPorCategoria { get; set; } = new();
    public int TotalGeral { get; set; }
    public decimal ValorTotalGeral { get; set; }

    // Paginação (sobre o resultado já filtrado)
    public int TotalFiltrado { get; set; }
    public decimal ValorTotalFiltrado { get; set; }
    public int Pagina { get; set; } = 1;
    public int TamanhoPagina { get; set; } = 10;
    public int TotalPaginas => Math.Max(1, (int)Math.Ceiling(TotalFiltrado / (double)TamanhoPagina));
    public int PrimeiroItem => TotalFiltrado == 0 ? 0 : (Pagina - 1) * TamanhoPagina + 1;
    public int UltimoItem => Math.Min(Pagina * TamanhoPagina, TotalFiltrado);
}
