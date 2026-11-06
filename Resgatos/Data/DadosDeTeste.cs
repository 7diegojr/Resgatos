using Microsoft.EntityFrameworkCore;
using Resgatos.Models;

namespace Resgatos.Data;

// TEMPORÁRIO: animais de teste para desenvolver o catálogo antes do CRUD de animais (Sprint 13).
// Roda só com `dotnet run --seed-teste` e só se a tabela estiver vazia.
// Apague este arquivo (e o `if` no Program.cs) quando o admin existir.
public static class DadosDeTeste
{
    public static async Task PopularAnimaisAsync(AppDbContext context)
    {
        if (await context.Animais.AnyAsync()) return;

        context.Animais.AddRange(
            // Mel: 3 fotos (para testar a galeria), história com mais de um parágrafo e castração agendada
            Novo("Mel",      EspecieAnimal.Gato,     SexoAnimal.Femea, PorteAnimal.Pequeno, FaixaEtariaAnimal.Filhote, "2 meses",  "Caramelo", fotos: 3, castrado: false,
                 historia: "A Mel foi encontrada ainda bebê, miando embaixo de um carro num estacionamento do Centro.\n\nChegou magrinha, mas se recuperou rápido e hoje é pura bagunça: adora brincar, dormir no colo e esperar a hora da ração."),
            Novo("Tigrão",   EspecieAnimal.Gato,     SexoAnimal.Macho, PorteAnimal.Medio,   FaixaEtariaAnimal.Adulto,  "3 anos",   "Rajado",   fotos: 2,
                 historia: "Tigrão vivia na rua do bairro Portão e foi resgatado depois de uma briga. Tratou as feridas, ganhou peso e descobriu o sofá. Calmo, carinhoso e ótimo para apartamento."),
            Novo("Luna",     EspecieAnimal.Gato,     SexoAnimal.Femea, PorteAnimal.Pequeno, FaixaEtariaAnimal.Adulto,  "1 ano",    "Preta"),
            Novo("Pipoca",   EspecieAnimal.Gato,     SexoAnimal.Macho, PorteAnimal.Pequeno, FaixaEtariaAnimal.Filhote, "4 meses",  "Branco",   castrado: false),
            Novo("Dona Nina",EspecieAnimal.Gato,     SexoAnimal.Femea, PorteAnimal.Medio,   FaixaEtariaAnimal.Idoso,   "12 anos",  "Tricolor"),
            Novo("Thor",     EspecieAnimal.Gato,     SexoAnimal.Macho, PorteAnimal.Grande,  FaixaEtariaAnimal.Adulto,  "5 anos",   "Cinza"),
            Novo("Sansão",   EspecieAnimal.Cachorro, SexoAnimal.Macho, PorteAnimal.Grande,  FaixaEtariaAnimal.Adulto,  "4 anos",   "Caramelo", fotos: 3,
                 historia: "Sansão foi deixado amarrado na frente de um mercado. Grandão e desengonçado, tem coração de filhote e adora passear."),
            Novo("Bolota",   EspecieAnimal.Cachorro, SexoAnimal.Macho, PorteAnimal.Pequeno, FaixaEtariaAnimal.Filhote, "3 meses",  "Preto",    castrado: false),
            Novo("Frida",    EspecieAnimal.Cachorro, SexoAnimal.Femea, PorteAnimal.Medio,   FaixaEtariaAnimal.Adulto,  "2 anos",   "Marrom"),
            Novo("Vovó Dita",EspecieAnimal.Cachorro, SexoAnimal.Femea, PorteAnimal.Pequeno, FaixaEtariaAnimal.Idoso,   "11 anos",  "Branca"),
            // Estes dois NÃO devem aparecer no catálogo (status diferente de "disponível"):
            Novo("Reservada",EspecieAnimal.Gato,     SexoAnimal.Femea, PorteAnimal.Pequeno, FaixaEtariaAnimal.Adulto,  "2 anos",   "Preta",    status: StatusAnimal.Reservado),
            Novo("Tratando", EspecieAnimal.Cachorro, SexoAnimal.Macho, PorteAnimal.Medio,   FaixaEtariaAnimal.Adulto,  "3 anos",   "Marrom",   status: StatusAnimal.EmTratamento)
        );

        await context.SaveChangesAsync();
    }

    private static Animal Novo(string nome, EspecieAnimal especie, SexoAnimal sexo, PorteAnimal porte,
        FaixaEtariaAnimal faixa, string idade, string cor,
        StatusAnimal status = StatusAnimal.Disponivel, string historia = null, int fotos = 1, bool castrado = true)
    {
        var animal = new Animal
        {
            Nome = nome,
            Especie = especie,
            Sexo = sexo,
            Porte = porte,
            FaixaEtaria = faixa,
            IdadeEstimada = idade,
            Cor = cor,
            Status = status,
            Historia = historia,

            // Saúde: tudo em dia, menos a castração dos que vieram marcados com castrado: false
            AntipulgasOk = true,
            VermifugadoOk = true,
            VacinadoOk = true,
            CastradoOk = castrado,
            DataCastracao = castrado ? new DateTime(2026, 3, 10) : null,

            // FIV/FeLV só para gatos
            FivTestado = especie == EspecieAnimal.Gato,
            ResultadoFiv = especie == EspecieAnimal.Gato ? ResultadoFiv.Negativo : null,
            FelvTestado = especie == EspecieAnimal.Gato,
            ResultadoFelv = especie == EspecieAnimal.Gato ? ResultadoFelv.Negativo : null,

            Fotos = new List<AnimalFoto>()
        };

        // Foto de teste: caminho a partir da wwwroot (mesmo formato que o upload da Sprint 13 deve salvar).
        // Só existe uma imagem de teste, então "fotos: 3" repete a mesma 3 vezes, o bastante para testar a galeria.
        for (var i = 0; i < fotos; i++)
            animal.Fotos.Add(new AnimalFoto { Foto = "img/adocoes/gato-teste.png", Capa = i == 0 });

        return animal;
    }
}
