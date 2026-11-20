using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Resgatos.Data;
using Resgatos.Helpers;
using Resgatos.Models;
using Resgatos.ViewModels;

namespace Resgatos.Controllers;

public class HomeController : Controller
{
    private readonly AppDbContext _context;

    public HomeController(AppDbContext context)
    {
        _context = context;
    }

    public IActionResult Index()
    {
        return View();
    }

    public IActionResult Sobre()
    {
        return View();
    }

    public async Task<IActionResult> Adocoes(AnimalFiltroViewModel filtro)
    {
        // Só aparecem no site os animais disponíveis
        var consulta = _context.Animais
            .AsNoTracking()
            .Where(a => a.Status == StatusAnimal.Disponivel);

        // Cada grupo só filtra se tiver algo marcado (nada marcado = "todos")
        if (filtro.Especies.Count > 0)
            consulta = consulta.Where(a => filtro.Especies.Contains(a.Especie));

        if (filtro.Sexos.Count > 0)
            consulta = consulta.Where(a => filtro.Sexos.Contains(a.Sexo));

        if (filtro.FaixasEtarias.Count > 0)
            consulta = consulta.Where(a => filtro.FaixasEtarias.Contains(a.FaixaEtaria));

        if (filtro.Portes.Count > 0)
            consulta = consulta.Where(a => filtro.Portes.Contains(a.Porte));

        var animais = await consulta
            .OrderBy(a => a.Nome)
            .Select(a => new AnimalCardViewModel
            {
                IdAnimal = a.IdAnimal,
                Nome = a.Nome,
                Especie = a.Especie,
                Sexo = a.Sexo,
                Porte = a.Porte,
                IdadeEstimada = a.IdadeEstimada,
                // Foto de capa; se nenhuma foi marcada como capa, usa a primeira
                FotoCapa = a.Fotos
                    .OrderByDescending(f => f.Capa)
                    .ThenBy(f => f.IdAnimalFoto)
                    .Select(f => f.Foto)
                    .FirstOrDefault()
            })
            .ToListAsync();

        // O link do WhatsApp é montado em C# (não dá para fazer isso dentro da consulta SQL)
        foreach (var animal in animais)
            animal.LinkWhatsApp = WhatsAppHelper.LinkConhecerAnimal(animal);

        return View(new AdocoesViewModel { Filtro = filtro, Animais = animais });
    }

    // Detalhes de um animal. Devolve só o miolo do modal (partial), que o JavaScript
    // busca com fetch e encaixa dentro do <dialog> da página de Adoções.
    public async Task<IActionResult> DetalhesAnimal(int id)
    {
        // Mesma regra da listagem: só animais disponíveis aparecem no site.
        // Se o animal foi adotado/reservado enquanto a pessoa olhava a página, cai no NotFound.
        var animal = await _context.Animais
            .AsNoTracking()
            .Include(a => a.Fotos)
            .FirstOrDefaultAsync(a => a.IdAnimal == id && a.Status == StatusAnimal.Disponivel);

        if (animal == null)
            return NotFound();

        var detalhe = new AnimalDetalheViewModel
        {
            IdAnimal = animal.IdAnimal,
            Nome = animal.Nome,
            Especie = animal.Especie,
            Sexo = animal.Sexo,
            Porte = animal.Porte,
            FaixaEtaria = animal.FaixaEtaria,
            IdadeEstimada = animal.IdadeEstimada,
            Cor = animal.Cor,
            Historia = animal.Historia,
            Observacoes = animal.Observacoes,
            // Capa primeiro; as demais na ordem em que foram enviadas
            Fotos = animal.Fotos
                .OrderByDescending(f => f.Capa)
                .ThenBy(f => f.IdAnimalFoto)
                .Select(f => f.Foto)
                .ToList(),
            AntipulgasOk = animal.AntipulgasOk,
            VermifugadoOk = animal.VermifugadoOk,
            VacinadoOk = animal.VacinadoOk,
            FivTestado = animal.FivTestado,
            ResultadoFiv = animal.ResultadoFiv,
            FelvTestado = animal.FelvTestado,
            ResultadoFelv = animal.ResultadoFelv,
            CastradoOk = animal.CastradoOk
        };

        detalhe.FotoCapa = detalhe.Fotos.FirstOrDefault();
        detalhe.LinkWhatsApp = WhatsAppHelper.LinkConhecerAnimal(detalhe);

        return PartialView("_DetalhesAnimal", detalhe);
    }

    public IActionResult Doacoes()
    {
        return View();
    }

    public async Task<IActionResult> Transparencia(TransparenciaFiltroViewModel filtro)
    {
        var hoje = DateTime.Today;
        var inicioMes = new DateTime(hoje.Year, hoje.Month, 1);
        var inicioProximoMes = inicioMes.AddMonths(1);

        var todos = _context.Transparencias.AsNoTracking();

        // ---------- Cards do topo: mês atual (não dependem do filtro) ----------
        var doMes = todos.Where(t => t.Data >= inicioMes && t.Data < inicioProximoMes);
        var quantidadeMes = await doMes.CountAsync();
        var totalMes = await doMes.SumAsync(t => (decimal?)t.Valor) ?? 0m;
        var ultimaData = await todos.MaxAsync(t => (DateTime?)t.Data);

        // ---------- Resumo por categoria (todos os gastos, não depende do filtro) ----------
        var somas = await todos
            .GroupBy(t => t.Categoria)
            .Select(g => new { Categoria = g.Key, Total = g.Sum(t => t.Valor), Quantidade = g.Count() })
            .ToListAsync();

        var totalGeral = somas.Sum(s => s.Total);

        var porCategoria = somas
            .OrderByDescending(s => s.Total)
            .Select(s => new TransparenciaCategoriaResumoViewModel
            {
                Categoria = s.Categoria,
                Total = s.Total,
                Quantidade = s.Quantidade,
                Percentual = totalGeral > 0 ? Math.Round(s.Total / totalGeral * 100, 1) : 0
            })
            .ToList();

        // ---------- Lista (respeita o filtro) ----------
        var consulta = todos;

        // Nada marcado = "todas as categorias"
        if (filtro.Categorias.Count > 0)
            consulta = consulta.Where(t => filtro.Categorias.Contains(t.Categoria));

        switch (filtro.Periodo)
        {
            case PeriodoTransparencia.Semana:   // últimos 7 dias (hoje + 6 anteriores)
                var seteDias = hoje.AddDays(-6);
                consulta = consulta.Where(t => t.Data >= seteDias);
                break;
            case PeriodoTransparencia.Mes:      // mês atual do calendário
                consulta = consulta.Where(t => t.Data >= inicioMes && t.Data < inicioProximoMes);
                break;
            case PeriodoTransparencia.Meses:    // últimos 3 meses
                var tresMeses = hoje.AddMonths(-3);
                consulta = consulta.Where(t => t.Data >= tresMeses);
                break;
        }

        // Em empate, o mais recente (e o lançado por último) vem primeiro
        IQueryable<Transparencia> ordenada = filtro.Ordem switch
        {
            OrdenacaoTransparencia.MaisAntigos => consulta.OrderBy(t => t.Data).ThenBy(t => t.IdTransparencia),
            OrdenacaoTransparencia.MaiorValor  => consulta.OrderByDescending(t => t.Valor).ThenByDescending(t => t.Data),
            OrdenacaoTransparencia.MenorValor  => consulta.OrderBy(t => t.Valor).ThenByDescending(t => t.Data),
            _                                  => consulta.OrderByDescending(t => t.Data).ThenByDescending(t => t.IdTransparencia)
        };

        var lancamentos = await ordenada
            .Select(t => new TransparenciaItemViewModel
            {
                IdTransparencia = t.IdTransparencia,
                Categoria = t.Categoria,
                Titulo = t.Titulo,
                Valor = t.Valor,
                Data = t.Data,
                Comprovante = t.Comprovante,
                Imagem = t.Imagem
            })
            .ToListAsync();

        return View(new TransparenciaViewModel
        {
            Filtro = filtro,
            Lancamentos = lancamentos,
            TotalFiltrado = lancamentos.Sum(l => l.Valor),
            MesReferencia = inicioMes,
            TotalMes = totalMes,
            QuantidadeMes = quantidadeMes,
            UltimaData = ultimaData,
            TotalGeral = totalGeral,
            QuantidadeGeral = somas.Sum(s => s.Quantidade),
            PorCategoria = porCategoria
        });
    }

    public IActionResult Contatos()
    {
        return View();
    }

    public IActionResult Privacy()
    {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
