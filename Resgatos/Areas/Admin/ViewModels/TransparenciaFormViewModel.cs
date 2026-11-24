using System.ComponentModel.DataAnnotations;
using Resgatos.Models;

namespace Resgatos.Areas.Admin.ViewModels;

// Formulário de criar/editar gasto. É um modelo próprio (e não a entidade Transparencia) de propósito:
// só entra o que a tela envia, e o que não pode vir do navegador (Id, CriadoEm, caminhos dos arquivos) fica de fora.
// Categoria e Data são "?" para o campo vazio virar erro de obrigatório (em vez de assumir o primeiro valor).
public class TransparenciaFormViewModel
{
    public int IdTransparencia { get; set; }
    public bool EhEdicao => IdTransparencia > 0;

    // ---------- Dados ----------
    [Required(ErrorMessage = "Selecione a categoria.")]
    public CategoriaTransparencia? Categoria { get; set; }

    [Required(ErrorMessage = "Informe o título do gasto.")]
    [StringLength(255, ErrorMessage = "O título pode ter no máximo 255 caracteres.")]
    public string Titulo { get; set; }

    // Texto digitado ("189,90" ou "1.234,50"): o controller converte. Assim a vírgula do Brasil funciona
    // em qualquer servidor, independente da cultura configurada nele.
    [Required(ErrorMessage = "Informe o valor do gasto.")]
    public string Valor { get; set; }

    [Required(ErrorMessage = "Informe a data do gasto.")]
    [DataType(DataType.Date)]
    public DateTime? Data { get; set; }

    // ---------- Arquivos ----------
    // O que o gasto já tem (só na edição; carregado do banco, não vem do formulário)
    public string ImagemAtual { get; set; }
    public string ComprovanteAtual { get; set; }

    // Marcados para remover (só ao salvar)
    public bool RemoverImagem { get; set; }
    public bool RemoverComprovante { get; set; }

    // Arquivos novos enviados neste envio (um novo substitui o atual)
    public IFormFile NovaImagem { get; set; }
    public IFormFile NovoComprovante { get; set; }
}

// Dados de um campo de arquivo (usado pelo partial _CampoArquivo): foto do gasto e comprovante.
public class CampoArquivoViewModel
{
    public string Nome { get; set; }              // name/id do <input type="file"> (= propriedade do formulário, ex.: NovaImagem)
    public string NomeRemover { get; set; }       // name do checkbox "Remover" (ex.: RemoverImagem)
    public string Titulo { get; set; }
    public string Dica { get; set; }
    public string TituloDropzone { get; set; }
    public string Erro { get; set; }              // mensagem de erro vinda do servidor (null = sem erro)
    public bool AceitaPdf { get; set; }
    public long TamanhoMaximoBytes { get; set; }

    // Arquivo que o gasto já tem (caminho a partir da wwwroot) e se está marcado para remover
    public string CaminhoAtual { get; set; }
    public bool Remover { get; set; }
    public string TextoAlternativo { get; set; }
}
