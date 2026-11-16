using Microsoft.EntityFrameworkCore;
using Resgatos.Models;

namespace Resgatos.Data;

// TEMPORÁRIO: animais de teste para desenvolver o catálogo antes do CRUD de animais (Sprint 13).
// Roda só com `dotnet run --seed-teste` e só se a tabela estiver vazia.
// Apague este arquivo (e o `if` no Program.cs) quando o admin existir.
public static class DadosDeTeste
{
    // Apaga TODOS os animais (com fotos e adotados) e cria a seed de novo.
    // Use quando mudar esta seed: o PopularAnimaisAsync abaixo não faz nada se já houver animais.
    // Rode com:  dotnet run --refazer-seed-teste
    public static async Task RefazerAnimaisAsync(AppDbContext context)
    {
        // Ordem importa: primeiro quem depende do animal, por último o animal
        context.Adotados.RemoveRange(await context.Adotados.ToListAsync());
        context.AnimalFotos.RemoveRange(await context.AnimalFotos.ToListAsync());
        context.Animais.RemoveRange(await context.Animais.ToListAsync());
        await context.SaveChangesAsync();

        await PopularAnimaisAsync(context);
    }

    public static async Task PopularAnimaisAsync(AppDbContext context)
    {
        if (await context.Animais.AnyAsync()) return;

        // Todos os animais têm 3 fotos (gato-teste.png, gato-teste-2.png e gato-teste-3.png).
        // A saúde segue a realidade do projeto: quase todos têm tudo feito; alguns têm 1 ou 2 itens pendentes.
        // Nos gatos, fiv/felv: ResultadoX.Negativo (padrão), Positivo, Inconclusivo ou null (= não testado).
        context.Animais.AddRange(
            // Mel: filhote, castração ainda agendada (pendente)
            Novo("Mel",      EspecieAnimal.Gato,     SexoAnimal.Femea, PorteAnimal.Pequeno, FaixaEtariaAnimal.Filhote, "2 meses",  "Caramelo", castrado: false,
                 historia: "A Mel foi encontrada ainda bebê, miando embaixo de um carro num estacionamento do Centro.\n\nChegou magrinha, mas se recuperou rápido e hoje é pura bagunça: adora brincar, dormir no colo e esperar a hora da ração."),
            // Tigrão: tudo feito, FIV positivo
            Novo("Tigrão",   EspecieAnimal.Gato,     SexoAnimal.Macho, PorteAnimal.Medio,   FaixaEtariaAnimal.Adulto,  "3 anos",   "Rajado",   fiv: ResultadoFiv.Positivo,
                 historia: "Tigrão vivia na rua do bairro Portão e foi resgatado depois de uma briga. Tratou as feridas, ganhou peso e descobriu o sofá. Calmo, carinhoso e ótimo para apartamento."),
            // Luna: tudo feito, FeLV inconclusivo
            Novo("Luna",     EspecieAnimal.Gato,     SexoAnimal.Femea, PorteAnimal.Pequeno, FaixaEtariaAnimal.Adulto,  "1 ano",    "Preta",    felv: ResultadoFelv.Inconclusivo),
            // Pipoca: filhote, falta vacina e castração
            Novo("Pipoca",   EspecieAnimal.Gato,     SexoAnimal.Macho, PorteAnimal.Pequeno, FaixaEtariaAnimal.Filhote, "4 meses",  "Branco",   vacinado: false, castrado: false),
            // Dona Nina: falta só a vermifugação
            Novo("Dona Nina",EspecieAnimal.Gato,     SexoAnimal.Femea, PorteAnimal.Medio,   FaixaEtariaAnimal.Idoso,   "12 anos",  "Tricolor", vermifugado: false),
            // Thor: FIV inconclusivo e FeLV ainda não testado
            Novo("Thor",     EspecieAnimal.Gato,     SexoAnimal.Macho, PorteAnimal.Grande,  FaixaEtariaAnimal.Adulto,  "5 anos",   "Cinza",    fiv: ResultadoFiv.Inconclusivo, felv: null),
            // Cachorros (sem FIV/FeLV)
            Novo("Sansão",   EspecieAnimal.Cachorro, SexoAnimal.Macho, PorteAnimal.Grande,  FaixaEtariaAnimal.Adulto,  "4 anos",   "Caramelo",
                 historia: "Sansão foi deixado amarrado na frente de um mercado. Grandão e desengonçado, tem coração de filhote e adora passear."),
            Novo("Bolota",   EspecieAnimal.Cachorro, SexoAnimal.Macho, PorteAnimal.Pequeno, FaixaEtariaAnimal.Filhote, "3 meses",  "Preto",    vacinado: false, castrado: false),
            Novo("Frida",    EspecieAnimal.Cachorro, SexoAnimal.Femea, PorteAnimal.Medio,   FaixaEtariaAnimal.Adulto,  "2 anos",   "Marrom",   antipulgas: false),
            Novo("Vovó Dita",EspecieAnimal.Cachorro, SexoAnimal.Femea, PorteAnimal.Pequeno, FaixaEtariaAnimal.Idoso,   "11 anos",  "Branca"),
            // Estes dois NÃO devem aparecer no catálogo (status diferente de "disponível"):
            Novo("Reservada",EspecieAnimal.Gato,     SexoAnimal.Femea, PorteAnimal.Pequeno, FaixaEtariaAnimal.Adulto,  "2 anos",   "Preta",    status: StatusAnimal.Reservado),
            Novo("Tratando", EspecieAnimal.Cachorro, SexoAnimal.Macho, PorteAnimal.Medio,   FaixaEtariaAnimal.Adulto,  "3 anos",   "Marrom",   status: StatusAnimal.EmTratamento)
        );

        await context.SaveChangesAsync();
    }

    // ---------- Transparência ----------

    // Apaga TODOS os lançamentos e cria a seed de novo.
    // Rode com:  dotnet run --refazer-seed-transparencia
    public static async Task RefazerTransparenciaAsync(AppDbContext context)
    {
        context.Transparencias.RemoveRange(await context.Transparencias.ToListAsync());
        await context.SaveChangesAsync();

        await PopularTransparenciaAsync(context);
    }

    // Lançamentos de gastos de teste (só se a tabela estiver vazia).
    // Rode com:  dotnet run --seed-transparencia
    public static async Task PopularTransparenciaAsync(AppDbContext context)
    {
        if (await context.Transparencias.AnyAsync()) return;

        // Cobre os casos da página: com foto + comprovante, só comprovante, sem nada anexado,
        // e todas as categorias (as imagens são as de teste dos animais, só para ver o layout).
        // As datas são relativas a hoje, para testar os filtros de período (semana, mês, 3 meses).
        var hoje = DateTime.Today;

        const string imagem = "img/adocoes/gato-teste.png";
        const string comprovante = "img/adocoes/gato-teste-2.png";

        context.Transparencias.AddRange(
            Gasto(CategoriaTransparencia.Alimentacao,  "Ração premium para gatos filhotes (10 kg)", 189.90m, hoje.AddDays(-2), comprovante, imagem),
            Gasto(CategoriaTransparencia.Castracao,    "Castração de 3 gatas",                     540.00m, hoje.AddDays(-4), comprovante),
            Gasto(CategoriaTransparencia.Medicamentos, "Antipulgas e vermífugo (lote de 6)",       214.50m, hoje.AddDays(-9),  comprovante),
            Gasto(CategoriaTransparencia.Estrutura,    "Caixas de areia e kit de caixas de transporte", 326.80m, hoje.AddDays(-15), comprovante, imagem),
            Gasto(CategoriaTransparencia.Alimentacao,  "Ração úmida (sachês) para recuperação",    132.40m, hoje.AddDays(-24),  comprovante),
            Gasto(CategoriaTransparencia.Castracao,    "Castração de 2 machos adultos",            420.00m, hoje.AddDays(-33), comprovante),
            Gasto(CategoriaTransparencia.Medicamentos, "Vacina V4 (4 doses)",                      360.00m, hoje.AddDays(-47),  comprovante),
            Gasto(CategoriaTransparencia.Outros,       "Transporte de gato resgatado até a clínica", 48.00m, hoje.AddDays(-62)),
            Gasto(CategoriaTransparencia.Alimentacao,  "Ração adulto (20 kg)",                     279.90m, hoje.AddDays(-80),  comprovante, imagem),
            Gasto(CategoriaTransparencia.Estrutura,    "Areia higiênica (sacos de 4 kg)",           96.00m, hoje.AddDays(-110))
        );

        await context.SaveChangesAsync();
    }

    private static Transparencia Gasto(CategoriaTransparencia categoria, string titulo, decimal valor,
        DateTime data, string comprovante = null, string imagem = null)
    {
        return new Transparencia
        {
            Categoria = categoria,
            Titulo = titulo,
            Valor = valor,
            Data = data,
            Comprovante = comprovante,
            Imagem = imagem
        };
    }

    private static Animal Novo(string nome, EspecieAnimal especie, SexoAnimal sexo, PorteAnimal porte,
        FaixaEtariaAnimal faixa, string idade, string cor,
        StatusAnimal status = StatusAnimal.Disponivel, string historia = null,
        bool antipulgas = true, bool vermifugado = true, bool vacinado = true, bool castrado = true,
        ResultadoFiv? fiv = ResultadoFiv.Negativo, ResultadoFelv? felv = ResultadoFelv.Negativo)
    {
        var ehGato = especie == EspecieAnimal.Gato;

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

            AntipulgasOk = antipulgas,
            VermifugadoOk = vermifugado,
            VacinadoOk = vacinado,
            CastradoOk = castrado,
            DataCastracao = castrado ? new DateTime(2026, 3, 10) : null,

            // FIV/FeLV só para gatos; resultado null = ainda não testado
            FivTestado = ehGato && fiv.HasValue,
            ResultadoFiv = ehGato ? fiv : null,
            FelvTestado = ehGato && felv.HasValue,
            ResultadoFelv = ehGato ? felv : null,

            Fotos = new List<AnimalFoto>()
        };

        // Fotos de teste: caminho a partir da wwwroot (mesmo formato que o upload da Sprint 13 deve salvar)
        var fotos = new[] { "img/adocoes/gato-teste.png", "img/adocoes/gato-teste-2.png", "img/adocoes/gato-teste-3.png" };
        for (var i = 0; i < fotos.Length; i++)
            animal.Fotos.Add(new AnimalFoto { Foto = fotos[i], Capa = i == 0 });

        return animal;
    }
}
