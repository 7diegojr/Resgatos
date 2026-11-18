using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Resgatos.Areas.Admin.ViewModels;
using Resgatos.Data;
using Resgatos.Models;

namespace Resgatos.Areas.Admin.Controllers;

// Sprint 13: CRUD de animais e fotos.
// Por enquanto só a listagem (Index). Criar, Editar, Detalhes e Excluir entram nos próximos passos.
public class AnimaisController : AdminBaseController
{
    private const int TamanhoPagina = 10;

    private readonly AppDbContext _context;

    public AnimaisController(AppDbContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index(AnimaisFiltroAdminViewModel filtro)
    {
        filtro.Busca = filtro.Busca?.Trim();

        var todos = _context.Animais.AsNoTracking();

        // ---------- Abas de status: contagem de cada um (não depende da busca) ----------
        var contagens = await todos
            .GroupBy(a => a.Status)
            .Select(g => new { Status = g.Key, Quantidade = g.Count() })
            .ToListAsync();

        // ---------- Lista (respeita os filtros) ----------
        var consulta = todos;

        if (!string.IsNullOrWhiteSpace(filtro.Busca))
            consulta = consulta.Where(a => a.Nome.Contains(filtro.Busca));

        if (filtro.Especie.HasValue)
            consulta = consulta.Where(a => a.Especie == filtro.Especie.Value);

        if (filtro.Status.HasValue)
            consulta = consulta.Where(a => a.Status == filtro.Status.Value);

        // Em empate, o cadastrado por último vem primeiro
        IQueryable<Animal> ordenada = filtro.Ordem switch
        {
            OrdenacaoAnimaisAdmin.MaisAntigos => consulta.OrderBy(a => a.CriadoEm).ThenBy(a => a.IdAnimal),
            OrdenacaoAnimaisAdmin.NomeAZ      => consulta.OrderBy(a => a.Nome).ThenBy(a => a.IdAnimal),
            OrdenacaoAnimaisAdmin.NomeZA      => consulta.OrderByDescending(a => a.Nome).ThenBy(a => a.IdAnimal),
            _                                 => consulta.OrderByDescending(a => a.CriadoEm).ThenByDescending(a => a.IdAnimal)
        };

        var totalFiltrado = await ordenada.CountAsync();

        // Página fora do intervalo (ex.: ?pagina=99) volta para a última existente
        var totalPaginas = Math.Max(1, (int)Math.Ceiling(totalFiltrado / (double)TamanhoPagina));
        var pagina = Math.Clamp(filtro.Pagina, 1, totalPaginas);
        filtro.Pagina = pagina;

        var animais = await ordenada
            .Skip((pagina - 1) * TamanhoPagina)
            .Take(TamanhoPagina)
            .Select(a => new AnimalAdminItemViewModel
            {
                IdAnimal = a.IdAnimal,
                Nome = a.Nome,
                Cor = a.Cor,
                Especie = a.Especie,
                Sexo = a.Sexo,
                FaixaEtaria = a.FaixaEtaria,
                IdadeEstimada = a.IdadeEstimada,
                Status = a.Status,
                CriadoEm = a.CriadoEm,
                QtdFotos = a.Fotos.Count(),
                // Foto de capa; se nenhuma foi marcada como capa, usa a primeira (mesma regra do site público)
                FotoCapa = a.Fotos
                    .OrderByDescending(f => f.Capa)
                    .ThenBy(f => f.IdAnimalFoto)
                    .Select(f => f.Foto)
                    .FirstOrDefault(),
                AntipulgasOk = a.AntipulgasOk,
                VermifugadoOk = a.VermifugadoOk,
                VacinadoOk = a.VacinadoOk,
                CastradoOk = a.CastradoOk,
                FivTestado = a.FivTestado,
                FelvTestado = a.FelvTestado
            })
            .ToListAsync();

        var modelo = new AnimaisIndexViewModel
        {
            Filtro = filtro,
            Animais = animais,
            ContagemPorStatus = contagens.ToDictionary(c => c.Status, c => c.Quantidade),
            TotalGeral = contagens.Sum(c => c.Quantidade),
            TotalFiltrado = totalFiltrado,
            Pagina = pagina,
            TamanhoPagina = TamanhoPagina
        };

        return View(modelo);
    }
}
