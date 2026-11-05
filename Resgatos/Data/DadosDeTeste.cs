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
            Novo("Mel",      EspecieAnimal.Gato,     SexoAnimal.Femea, PorteAnimal.Pequeno, FaixaEtariaAnimal.Filhote, "2 meses",  "Caramelo"),
            Novo("Tigrão",   EspecieAnimal.Gato,     SexoAnimal.Macho, PorteAnimal.Medio,   FaixaEtariaAnimal.Adulto,  "3 anos",   "Rajado"),
            Novo("Luna",     EspecieAnimal.Gato,     SexoAnimal.Femea, PorteAnimal.Pequeno, FaixaEtariaAnimal.Adulto,  "1 ano",    "Preta"),
            Novo("Pipoca",   EspecieAnimal.Gato,     SexoAnimal.Macho, PorteAnimal.Pequeno, FaixaEtariaAnimal.Filhote, "4 meses",  "Branco"),
            Novo("Dona Nina",EspecieAnimal.Gato,     SexoAnimal.Femea, PorteAnimal.Medio,   FaixaEtariaAnimal.Idoso,   "12 anos",  "Tricolor"),
            Novo("Thor",     EspecieAnimal.Gato,     SexoAnimal.Macho, PorteAnimal.Grande,  FaixaEtariaAnimal.Adulto,  "5 anos",   "Cinza"),
            Novo("Sansão",   EspecieAnimal.Cachorro, SexoAnimal.Macho, PorteAnimal.Grande,  FaixaEtariaAnimal.Adulto,  "4 anos",   "Caramelo"),
            Novo("Bolota",   EspecieAnimal.Cachorro, SexoAnimal.Macho, PorteAnimal.Pequeno, FaixaEtariaAnimal.Filhote, "3 meses",  "Preto"),
            Novo("Frida",    EspecieAnimal.Cachorro, SexoAnimal.Femea, PorteAnimal.Medio,   FaixaEtariaAnimal.Adulto,  "2 anos",   "Marrom"),
            Novo("Vovó Dita",EspecieAnimal.Cachorro, SexoAnimal.Femea, PorteAnimal.Pequeno, FaixaEtariaAnimal.Idoso,   "11 anos",  "Branca"),
            // Estes dois NÃO devem aparecer no catálogo (status diferente de "disponível"):
            Novo("Reservada",EspecieAnimal.Gato,     SexoAnimal.Femea, PorteAnimal.Pequeno, FaixaEtariaAnimal.Adulto,  "2 anos",   "Preta",   StatusAnimal.Reservado),
            Novo("Tratando", EspecieAnimal.Cachorro, SexoAnimal.Macho, PorteAnimal.Medio,   FaixaEtariaAnimal.Adulto,  "3 anos",   "Marrom",  StatusAnimal.EmTratamento)
        );

        await context.SaveChangesAsync();
    }

    private static Animal Novo(string nome, EspecieAnimal especie, SexoAnimal sexo, PorteAnimal porte,
        FaixaEtariaAnimal faixa, string idade, string cor, StatusAnimal status = StatusAnimal.Disponivel)
    {
        return new Animal
        {
            Nome = nome,
            Especie = especie,
            Sexo = sexo,
            Porte = porte,
            FaixaEtaria = faixa,
            IdadeEstimada = idade,
            Cor = cor,
            Status = status,
            // Foto de teste: caminho a partir da wwwroot (mesmo formato que o upload da Sprint 13 deve salvar)
            Fotos = new List<AnimalFoto>
            {
                new AnimalFoto { Foto = "img/adocoes/gato-teste.png", Capa = true }
            }
        };
    }
}
