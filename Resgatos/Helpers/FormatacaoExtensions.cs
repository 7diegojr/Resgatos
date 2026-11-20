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

    // Tempo desde um mês/ano até hoje, em linguagem simples: "menos de 1 mês", "2 meses", "1 ano", "1 ano e 3 meses".
    // Conta meses fechados (quem chegou em março e estamos em maio está há "2 meses") e se atualiza sozinho com o tempo.
    // "hoje" existe só para poder testar com outra data.
    public static string ToTempoDecorrido(this DateTime data, DateTime? hoje = null)
    {
        var agora = hoje ?? DateTime.Today;
        var meses = (agora.Year - data.Year) * 12 + agora.Month - data.Month;
        if (meses < 1) return "menos de 1 mês";

        var anos = meses / 12;
        var resto = meses % 12;

        var partes = new List<string>();
        if (anos > 0) partes.Add(anos == 1 ? "1 ano" : $"{anos} anos");
        if (resto > 0) partes.Add(resto == 1 ? "1 mês" : $"{resto} meses");
        return string.Join(" e ", partes);
    }
}
