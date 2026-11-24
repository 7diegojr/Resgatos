using System.Globalization;
using Resgatos.Models;

namespace Resgatos.Helpers;

// Detalhes visuais da Transparência usados nas telas do admin.
public static class TransparenciaExtensions
{
    private static readonly CultureInfo PtBr = new("pt-BR");

    // Ícone do FontAwesome de cada categoria (os mesmos da página pública de Transparência)
    public static string GetIcone(this CategoriaTransparencia categoria) => categoria switch
    {
        CategoriaTransparencia.Alimentacao => "fa-utensils",
        CategoriaTransparencia.Castracao => "fa-stethoscope",
        CategoriaTransparencia.Medicamentos => "fa-pills",
        CategoriaTransparencia.Estrutura => "fa-house-chimney",
        _ => "fa-box-open"
    };

    // 2026-03-10 -> "Terça-feira"
    public static string ToDiaSemana(this DateTime data)
    {
        var texto = data.ToString("dddd", PtBr);
        return char.ToUpper(texto[0]) + texto[1..];
    }

    // 1234.5 -> "1.234,50" (sem o "R$": é o formato que o campo de valor do formulário usa)
    public static string ToValorCampo(this decimal valor) => valor.ToString("N2", PtBr);

    // true se o arquivo anexado (caminho relativo) é um PDF
    public static bool EhPdf(string caminho) =>
        !string.IsNullOrEmpty(caminho) && caminho.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase);
}

// Frases dos avisos e do modal de exclusão de um gasto.
public static class TransparenciaTexto
{
    // "Tem certeza que deseja" + quebra de linha + "excluir este gasto?" (o confirmacao.js transforma o \n em quebra de linha)
    public const string PerguntaExcluir = "Tem certeza que deseja\nexcluir este gasto?";

    // "O gasto "Ração premium" (R$ 189,90) será movido para a lixeira e deixará de aparecer na página de Transparência do site."
    public static string AvisoExcluir(string titulo, decimal valor) =>
        $"O gasto \"{titulo}\" ({valor.ToReais()}) será movido para a lixeira e deixará de aparecer na página de Transparência do site.";

    public static string MensagemExcluido(string titulo) => $"O gasto \"{titulo}\" foi movido para a lixeira.";

    public static string MensagemCadastrado(string titulo) => $"O gasto \"{titulo}\" foi cadastrado com sucesso.";

    public static string MensagemEditado(string titulo) => $"As alterações do gasto \"{titulo}\" foram salvas.";
}
