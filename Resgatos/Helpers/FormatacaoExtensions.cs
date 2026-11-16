using System.Globalization;

namespace Resgatos.Helpers;

// Formatação em português do Brasil, independente da cultura configurada no servidor.
public static class FormatacaoExtensions
{
    private static readonly CultureInfo PtBr = new("pt-BR");

    // 1234.5 -> "R$ 1.234,50"
    public static string ToReais(this decimal valor) => valor.ToString("C2", PtBr);

    // 2026-03-10 -> "10/03/2026"
    public static string ToDataBr(this DateTime data) => data.ToString("dd/MM/yyyy", PtBr);

    // 2026-03-10 -> "março"
    public static string ToMes(this DateTime data) => data.ToString("MMMM", PtBr);

    // 2026-03-10 -> "março de 2026"
    public static string ToMesAno(this DateTime data) => data.ToString("MMMM 'de' yyyy", PtBr);
}
