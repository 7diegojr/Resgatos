using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Text.Json;
using Resgatos.Helpers;
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

// Dados enviados pelo modal de detalhes da página Adotados (editar a adoção).
// Os campos são os mesmos do modal "Marcar como adotado" (IdAnimal não é usado aqui); o termo novo, se vier, substitui o atual.
public class AdotadoEdicaoViewModel : AdotadoFormViewModel
{
    public int IdAdotado { get; set; }

    // Marcado quando o termo atual deve ser removido (sem enviar outro no lugar)
    public bool RemoverTermo { get; set; }
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

    // Id da adoção cujo modal deve abrir sozinho (?abrir=5). Vem dos botões "voltar" das páginas do animal.
    // Não entra no ParaRota: depois de abrir, os links de página e de filtro não carregam mais isso.
    public int? Abrir { get; set; }

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

// Um card da página Adotados. Também leva tudo o que o modal de detalhes mostra (ver DetalhesJson).
public class AdotadoCardViewModel
{
    private static readonly CultureInfo PtBr = new("pt-BR");

    public int IdAdotado { get; set; }
    public int IdAnimal { get; set; }
    public string NomeAnimal { get; set; }
    public EspecieAnimal Especie { get; set; }
    public SexoAnimal Sexo { get; set; }
    public string FotoCapa { get; set; }

    public string NomeAdotante { get; set; }
    public string TelefoneAdotante { get; set; }
    public string EmailAdotante { get; set; }
    public string EnderecoAdotante { get; set; }
    public DateTime DataAdocao { get; set; }

    public decimal ValorTaxa { get; set; }
    public bool TaxaParcelada { get; set; }
    public int? QtdParcelas { get; set; }

    public bool TermoAnexado { get; set; }
    public string Observacoes { get; set; }
    public DateTime CriadoEm { get; set; }

    // ----- Dados do animal (só aparecem no modal) -----
    public string Cor { get; set; }
    public PorteAnimal Porte { get; set; }
    public FaixaEtariaAnimal FaixaEtaria { get; set; }
    public string IdadeEstimada { get; set; }
    public DateTime? DataChegada { get; set; }
    public bool AntipulgasOk { get; set; }
    public bool VermifugadoOk { get; set; }
    public bool VacinadoOk { get; set; }
    public bool CastradoOk { get; set; }
    public bool FivTestado { get; set; }
    public bool FelvTestado { get; set; }

    // Mesma regra da coluna Saúde da lista de Animais: FIV/FeLV só contam para gatos
    private int QtdPendencias
    {
        get
        {
            var n = 0;
            if (!AntipulgasOk) n++;
            if (!VermifugadoOk) n++;
            if (!VacinadoOk) n++;
            if (!CastradoOk) n++;
            if (Especie == EspecieAnimal.Gato)
            {
                if (!FivTestado) n++;
                if (!FelvTestado) n++;
            }
            return n;
        }
    }

    private string TempoNoResgatos
    {
        get
        {
            if (!DataChegada.HasValue) return "Não informado";
            var tempo = DataChegada.Value.ToTempoDecorrido();
            return char.ToUpper(tempo[0]) + tempo[1..];
        }
    }

    // Tudo o que o modal mostra e edita, num JSON só (vai no data-detalhes do card).
    // O JSON já sai com os textos prontos (datas, valores, "o/a" do animal), então o JavaScript só preenche os campos.
    // Os nomes ficam em camelCase porque é como o adotado-modal.js lê.
    public string DetalhesJson
    {
        get
        {
            var pendencias = QtdPendencias;
            var pagamento = TaxaParcelada
                ? (QtdParcelas.HasValue ? $"Parcelada em {QtdParcelas}x" : "Parcelada")
                : "À vista";

            return JsonSerializer.Serialize(new
            {
                id = IdAdotado,
                idAnimal = IdAnimal,
                titulo = $"Adoção d{Sexo.ArtigoDefinido()} {NomeAnimal}",
                registrada = CriadoEm.ToDataBr(),
                foto = FotoCapa,
                animal = new
                {
                    nome = NomeAnimal,
                    especie = Especie.GetDisplayName(),
                    sexo = Sexo.GetDisplayName(),
                    idade = $"{IdadeEstimada} ({FaixaEtaria.GetDisplayName()})",
                    porte = Porte.GetDisplayName(),
                    cor = Cor,
                    tempo = TempoNoResgatos,
                    castrado = CastradoOk ? "Sim" : "Não",
                    saude = pendencias == 0 ? "Completa" : pendencias == 1 ? "1 pendência" : $"{pendencias} pendências",
                    saudePendente = pendencias > 0
                },
                adotante = new
                {
                    nome = NomeAdotante,
                    telefone = TelefoneAdotante,
                    email = EmailAdotante,
                    endereco = EnderecoAdotante
                },
                adocao = new
                {
                    data = DataAdocao.ToDataBr(),
                    dataIso = DataAdocao.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                    valor = ValorTaxa.ToReais(),
                    valorCampo = ValorTaxa.ToString("N2", PtBr),    // "1.234,50": o jeito que o campo do formulário aceita
                    pagamento,
                    parcelada = TaxaParcelada,
                    parcelas = QtdParcelas,
                    observacoes = Observacoes,
                    termo = TermoAnexado
                },
                excluirTexto = Sexo.AvisoExcluirAdocao(NomeAnimal, NomeAdotante, TermoAnexado)
            });
        }
    }
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

    // Adoção que abre o modal ao carregar a página (null = nenhuma, ou ela não está nesta página)
    public int? AbrirId { get; set; }

    public int PrimeiroItem => TotalFiltrado == 0 ? 0 : (Pagina - 1) * TamanhoPagina + 1;
    public int UltimoItem => Math.Min(Pagina * TamanhoPagina, TotalFiltrado);
}
