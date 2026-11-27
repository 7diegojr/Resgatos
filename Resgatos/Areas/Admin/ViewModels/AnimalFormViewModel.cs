using System.ComponentModel.DataAnnotations;
using Resgatos.Models;

namespace Resgatos.Areas.Admin.ViewModels;

// Formulário de criar/editar animal. É um modelo próprio (e não a entidade Animal) de propósito:
// só entra o que a tela envia, e o que não pode vir do navegador (Id, CriadoEm, ExcluidoEm) fica de fora.
// Os enums são "?" para o "Selecione" vazio virar erro de obrigatório (em vez de assumir o primeiro valor).
public class AnimalFormViewModel
{
    public int IdAnimal { get; set; }
    public bool EhEdicao => IdAnimal > 0;

    // Preenchido pelo controller (nunca vem do formulário) quando o animal está adotado: é o id da adoção dele.
    // Com isso a edição fixa o status em "Adotado" e o "Cancelar" volta para o modal da adoção.
    public int? IdAdotado { get; set; }
    public bool EstaAdotado => IdAdotado.HasValue;

    // ---------- Dados ----------
    [Required(ErrorMessage = "Selecione a espécie.")]
    public EspecieAnimal? Especie { get; set; }

    [Required(ErrorMessage = "Informe o nome do animal.")]
    [StringLength(100, ErrorMessage = "O nome pode ter no máximo 100 caracteres.")]
    public string Nome { get; set; }

    [Required(ErrorMessage = "Selecione o sexo.")]
    public SexoAnimal? Sexo { get; set; }

    [Required(ErrorMessage = "Selecione o porte.")]
    public PorteAnimal? Porte { get; set; }

    [Required(ErrorMessage = "Informe a cor do animal.")]
    [StringLength(100, ErrorMessage = "A cor pode ter no máximo 100 caracteres.")]
    public string Cor { get; set; }

    [Required(ErrorMessage = "Selecione a faixa etária.")]
    public FaixaEtariaAnimal? FaixaEtaria { get; set; }

    [Required(ErrorMessage = "Informe a idade estimada (ex.: 2 meses).")]
    [StringLength(50, ErrorMessage = "A idade pode ter no máximo 50 caracteres.")]
    public string IdadeEstimada { get; set; }

    [Required(ErrorMessage = "Selecione o status.")]
    public StatusAnimal? Status { get; set; }

    // Data de chegada ao Resgatos: só mês e ano (os dois juntos ou nenhum). Vira o dia 1 daquele mês no banco.
    public int? MesChegada { get; set; }
    public int? AnoChegada { get; set; }

    // ---------- Textos (aparecem no site) ----------
    // Limite de 200 caracteres conferido no AnimaisController (ele conta cada quebra de linha como 1 caractere,
    // enquanto o navegador envia as quebras como 2 e o [StringLength] contaria a mais).
    public string Historia { get; set; }

    [StringLength(4000, ErrorMessage = "O texto pode ter no máximo 4000 caracteres.")]
    public string Observacoes { get; set; }

    // ---------- Saúde ----------
    public bool AntipulgasOk { get; set; }
    public bool VermifugadoOk { get; set; }
    public bool VacinadoOk { get; set; }
    public bool CastradoOk { get; set; }

    // FIV e FeLV só existem para gatos (as regras ficam no controller)
    public bool FivTestado { get; set; }
    public ResultadoFiv? ResultadoFiv { get; set; }
    public bool FelvTestado { get; set; }
    public ResultadoFelv? ResultadoFelv { get; set; }

    // ---------- Fotos ----------
    // Fotos que o animal já tem (só na edição; carregadas do banco, não vêm do formulário)
    public List<FotoExistenteViewModel> FotosExistentes { get; set; } = new();

    // Ids das fotos existentes marcadas para remover
    public List<int> FotosRemover { get; set; } = new();

    // Qual foto é a capa: "foto-12" (já existente, id 12) ou "nova-0" (primeira das fotos enviadas agora)
    public string CapaSelecionada { get; set; }

    // Fotos novas enviadas neste envio
    public List<IFormFile> NovasFotos { get; set; } = new();
}

public class FotoExistenteViewModel
{
    public int IdAnimalFoto { get; set; }
    public string Caminho { get; set; }
    public bool Capa { get; set; }
}

// Dados de uma caixa de seleção (usado pelo partial _CampoSelect)
public class CampoSelectViewModel
{
    public string Nome { get; set; }              // name do <select> (= nome da propriedade do formulário) e, se Id não for informado, também o id
    public string Id { get; set; }                // id do <select>, quando precisa ser diferente do name (ex.: a mesma caixa aparece duas vezes na página)
    public string Rotulo { get; set; }
    public bool Obrigatorio { get; set; } = true;
    public string MensagemObrigatorio { get; set; }
    public string Erro { get; set; }             // mensagem de erro vinda do servidor (null = sem erro)
    public string Placeholder { get; set; } = "Selecione";

    // false = sem a opção vazia (o campo sempre tem uma opção escolhida, ex.: "Ordenar por")
    public bool PermiteVazio { get; set; } = true;

    // true = a opção vazia é uma resposta de verdade ("Todos") e não um "Selecione": não usa a cor de placeholder
    public bool OpcaoVaziaEhResposta { get; set; }
    public List<OpcaoSelectViewModel> Opcoes { get; set; } = new();
}

public record OpcaoSelectViewModel(string Valor, string Texto, bool Selecionada);
