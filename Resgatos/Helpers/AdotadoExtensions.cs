using System.Globalization;
using System.Text.RegularExpressions;
using Resgatos.Models;

namespace Resgatos.Helpers;

// Textos e conversões da adoção (Sprint 14).
public static class AdotadoExtensions
{
    // "A Mel foi marcada como adotada por Maria Silva." / "O Tigrão foi marcado como adotado por João."
    public static string MensagemAdotado(this SexoAnimal sexo, string nomeAnimal, string nomeAdotante)
    {
        var artigo = sexo.ArtigoDefinido();
        return $"{artigo.ToUpper()} {nomeAnimal} foi marcad{artigo} como adotad{artigo} por {nomeAdotante}.";
    }

    // Aceita "189,90", "1.234,50", "R$ 150"... Mesma regra do campo de valor da Transparência.
    public static bool TentarLerValor(string texto, out decimal valor)
    {
        valor = 0;
        if (string.IsNullOrWhiteSpace(texto)) return false;

        var limpo = texto.Replace("R$", "", StringComparison.OrdinalIgnoreCase).Replace(" ", "");
        if (limpo.Length == 0) return false;

        if (limpo.Contains(','))
            limpo = limpo.Replace(".", "").Replace(",", ".");
        else if (Regex.IsMatch(limpo, @"^\d{1,3}(\.\d{3})+$"))
            limpo = limpo.Replace(".", "");

        return decimal.TryParse(limpo, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out valor);
    }

    // Só os dígitos do telefone. 10 (fixo) ou 11 (celular) com DDD são válidos.
    public static string SoDigitos(string texto) => new string((texto ?? "").Where(char.IsDigit).ToArray());

    // "41984972309" -> "(41) 98497-2309" | "4133334444" -> "(41) 3333-4444"
    public static string FormatarTelefone(string digitos) => digitos.Length switch
    {
        11 => $"({digitos[..2]}) {digitos[2..7]}-{digitos[7..]}",
        10 => $"({digitos[..2]}) {digitos[2..6]}-{digitos[6..]}",
        _ => digitos
    };
}
