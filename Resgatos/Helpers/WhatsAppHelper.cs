using Resgatos.ViewModels;

namespace Resgatos.Helpers;

public static class WhatsAppHelper
{
    // TODO: mover para o SiteConfig quando os dados fixos forem centralizados.
    private const string NumeroResgatos = "55419849723090";

    // Link do botão "Quero conhecer": abre o WhatsApp com a mensagem e a ficha do animal.
    public static string LinkConhecerAnimal(AnimalCardViewModel animal)
    {
        var mensagem =
            "Olá, ResGatos! Vim pelo site e gostaria de conhecer um animal que vi na página de adoção.\n\n" +
            $"🐾 *{animal.Nome}*\n" +
            $"• Espécie: {animal.Especie.GetDisplayName()}\n" +
            $"• Sexo: {animal.Sexo.GetDisplayName()}\n" +
            $"• Idade: {animal.IdadeEstimada}\n" +
            $"• Porte: {animal.Porte.GetDisplayName()}\n\n" +
            "Podemos conversar?";

        // EscapeDataString codifica acentos e emoji (UTF-8) para caber na URL
        return $"https://wa.me/{NumeroResgatos}?text={Uri.EscapeDataString(mensagem)}";
    }
}
