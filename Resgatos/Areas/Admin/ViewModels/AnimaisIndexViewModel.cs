using Resgatos.Helpers;
using Resgatos.Models;

namespace Resgatos.Areas.Admin.ViewModels;

// Ordens da lista. Os nomes vão na URL (?ordem=NomeAZ), então a página filtrada pode ser copiada.
// As duas primeiras olham o tempo no Resgatos (data de chegada): quem está há menos tempo ou há mais tempo.
// Animais sem data de chegada ficam sempre no fim dessas duas ordens.
public enum OrdenacaoAnimaisAdmin
{
    MenosTempo = 0,     // padrão: quem chegou por último aparece primeiro
    MaisTempo = 1,      // quem está há mais tempo no Resgatos aparece primeiro
    NomeAZ = 2,
    NomeZA = 3
}

// Filtro de saúde: "Completa" = nada pendente; "ComPendencias" = falta algo (mesma regra da coluna Saúde da tabela)
public enum FiltroSaudeAdmin
{
    Completa = 1,
    ComPendencias = 2
}

// Filtros da lista de animais do admin. Vêm da query string (?busca=mel&especie=Gato&status=Disponivel...).
// Nada preenchido = "todos".
public class AnimaisFiltroAdminViewModel
{
    public string Busca { get; set; }
    public StatusAnimal? Status { get; set; }
    public EspecieAnimal? Especie { get; set; }
    public SexoAnimal? Sexo { get; set; }
    public PorteAnimal? Porte { get; set; }
    public FaixaEtariaAnimal? FaixaEtaria { get; set; }
    public FiltroSaudeAdmin? Saude { get; set; }
    public OrdenacaoAnimaisAdmin Ordem { get; set; } = OrdenacaoAnimaisAdmin.MenosTempo;
    public int Pagina { get; set; } = 1;

    // Quantos filtros estão aplicados (a busca não conta: ela tem a própria caixa). Decide se aparece o "Limpar filtros".
    public int QuantidadeFiltrosAtivos =>
        (Status.HasValue ? 1 : 0) +
        (Especie.HasValue ? 1 : 0) +
        (Sexo.HasValue ? 1 : 0) +
        (Porte.HasValue ? 1 : 0) +
        (FaixaEtaria.HasValue ? 1 : 0) +
        (Saude.HasValue ? 1 : 0) +
        (Ordem != OrdenacaoAnimaisAdmin.MenosTempo ? 1 : 0);

    // Quantos filtros aplicados só existem no modal (status, espécie e ordenação já aparecem na barra).
    // É o número que aparece no botão "Filtrar": mostra que há filtros "escondidos" valendo.
    public int QuantidadeFiltrosAvancados =>
        (Sexo.HasValue ? 1 : 0) +
        (Porte.HasValue ? 1 : 0) +
        (FaixaEtaria.HasValue ? 1 : 0) +
        (Saude.HasValue ? 1 : 0);

    // Parâmetros da URL com tudo o que está aplicado agora (menos a página, que volta para a 1).
    // "sem" tira alguns deles: é assim que o "x" de cada filtro aplicado, as abas de status e os
    // formulários (que só carregam o que não está nos próprios campos) funcionam.
    public Dictionary<string, string> ParaRota(params string[] sem)
    {
        var d = new Dictionary<string, string>();

        void Adicionar(string nome, string valor)
        {
            if (!string.IsNullOrEmpty(valor) && !sem.Contains(nome)) d[nome] = valor;
        }

        Adicionar("busca", Busca);
        Adicionar("status", Status?.ToString());
        Adicionar("especie", Especie?.ToString());
        Adicionar("sexo", Sexo?.ToString());
        Adicionar("porte", Porte?.ToString());
        Adicionar("faixaEtaria", FaixaEtaria?.ToString());
        Adicionar("saude", Saude?.ToString());
        if (Ordem != OrdenacaoAnimaisAdmin.MenosTempo) Adicionar("ordem", Ordem.ToString());

        return d;
    }
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
    public int QtdFotos { get; set; }

    // Mês/ano de chegada ao Resgatos (sempre o dia 1). null = não informada.
    public DateTime? DataChegada { get; set; }

    // "2 meses", "1 ano e 3 meses"... calculado na hora de mostrar, então se atualiza sozinho com o tempo
    public string TempoNoResgatos
    {
        get
        {
            if (!DataChegada.HasValue) return null;
            var tempo = DataChegada.Value.ToTempoDecorrido();
            return char.ToUpper(tempo[0]) + tempo[1..];
        }
    }

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

    // Quantidade de animais por status (alimenta as abas; não depende dos filtros)
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
