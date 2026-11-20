using Resgatos.Models;

namespace Resgatos.Helpers;

// Frases que concordam com o sexo do animal: "a Mel" / "o Tigrão", "movida" / "movido".
// O artigo ("a" ou "o") é também a letra final da palavra (movid-a, movid-o, cadastrad-a, cadastrad-o).
public static class AnimalTextoExtensions
{
    // "a" para fêmea, "o" para macho
    public static string ArtigoDefinido(this SexoAnimal sexo) => sexo == SexoAnimal.Femea ? "a" : "o";

    // "Tem certeza que deseja excluir a Mel?"
    public static string PerguntaExcluir(this SexoAnimal sexo, string nome) =>
        $"Tem certeza que deseja excluir {sexo.ArtigoDefinido()} {nome}?";

    // "A Mel será movida para a lixeira e deixará de aparecer no site e na lista de animais."
    public static string AvisoExcluir(this SexoAnimal sexo, string nome)
    {
        var artigo = sexo.ArtigoDefinido();
        return $"{artigo.ToUpper()} {nome} será movid{artigo} para a lixeira e deixará de aparecer no site e na lista de animais.";
    }

    // "A Mel foi movida para a lixeira."
    public static string MensagemExcluido(this SexoAnimal sexo, string nome)
    {
        var artigo = sexo.ArtigoDefinido();
        return $"{artigo.ToUpper()} {nome} foi movid{artigo} para a lixeira.";
    }

    // "A Mel foi cadastrada com sucesso."
    public static string MensagemCadastrado(this SexoAnimal sexo, string nome)
    {
        var artigo = sexo.ArtigoDefinido();
        return $"{artigo.ToUpper()} {nome} foi cadastrad{artigo} com sucesso.";
    }
}
