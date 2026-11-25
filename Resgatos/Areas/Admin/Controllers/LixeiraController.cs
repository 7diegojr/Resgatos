using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Resgatos.Areas.Admin.ViewModels;
using Resgatos.Data;
using Resgatos.Helpers;
using Resgatos.Models;
using Resgatos.Services;

namespace Resgatos.Areas.Admin.Controllers;

// Lixeira (exclusão lógica) de animais e gastos da Transparência.
// Index (lista das duas abas), Restaurar (volta para o site e para as listas) e Excluir definitivamente (apaga do banco e do disco).
// Cada ação existe para um item, para vários selecionados (int[] ids) e, na exclusão, para todos os itens da aba.
// Tudo o que está aqui tem ExcluidoEm preenchido; os query filters do AppDbContext escondem esses itens de todo o resto,
// por isso estas consultas usam .IgnoreQueryFilters() e sempre conferem ExcluidoEm != null (nada fora da lixeira é mexido por aqui).
public class LixeiraController : AdminBaseController
{
    private const int TamanhoPagina = 10;

    private readonly AppDbContext _context;
    private readonly UploadService _upload;

    public LixeiraController(AppDbContext context, UploadService upload)
    {
        _context = context;
        _upload = upload;
    }

    // =====================================================================
    // LISTA
    // =====================================================================
    public async Task<IActionResult> Index(LixeiraFiltroViewModel filtro)
    {
        filtro.Busca = filtro.Busca?.Trim();

        var animaisNaLixeira = _context.Animais.IgnoreQueryFilters().AsNoTracking().Where(a => a.ExcluidoEm != null);
        var gastosNaLixeira = _context.Transparencias.IgnoreQueryFilters().AsNoTracking().Where(t => t.ExcluidoEm != null);

        var modelo = new LixeiraIndexViewModel
        {
            Filtro = filtro,
            TamanhoPagina = TamanhoPagina,
            // Contagem das abas (não depende da busca)
            QuantidadeAnimais = await animaisNaLixeira.CountAsync(),
            QuantidadeGastos = await gastosNaLixeira.CountAsync()
        };

        if (filtro.Tipo == TipoLixeira.Animais)
        {
            var consulta = animaisNaLixeira;

            if (!string.IsNullOrWhiteSpace(filtro.Busca))
                consulta = consulta.Where(a => a.Nome.Contains(filtro.Busca));

            // O que foi excluído por último aparece primeiro
            var ordenada = consulta.OrderByDescending(a => a.ExcluidoEm).ThenByDescending(a => a.IdAnimal);

            modelo.TotalFiltrado = await ordenada.CountAsync();
            modelo.Pagina = PaginaValida(filtro, modelo.TotalFiltrado);

            modelo.Animais = await ordenada
                .Skip((modelo.Pagina - 1) * TamanhoPagina)
                .Take(TamanhoPagina)
                .Select(a => new LixeiraAnimalItemViewModel
                {
                    IdAnimal = a.IdAnimal,
                    Nome = a.Nome,
                    Cor = a.Cor,
                    Especie = a.Especie,
                    Sexo = a.Sexo,
                    FaixaEtaria = a.FaixaEtaria,
                    IdadeEstimada = a.IdadeEstimada,
                    Status = a.Status,
                    ExcluidoEm = a.ExcluidoEm.Value,
                    QtdFotos = a.Fotos.Count(),
                    // Foto de capa; se nenhuma foi marcada como capa, usa a primeira (mesma regra do site público)
                    FotoCapa = a.Fotos
                        .OrderByDescending(f => f.Capa)
                        .ThenBy(f => f.IdAnimalFoto)
                        .Select(f => f.Foto)
                        .FirstOrDefault()
                })
                .ToListAsync();
        }
        else
        {
            var consulta = gastosNaLixeira;

            if (!string.IsNullOrWhiteSpace(filtro.Busca))
                consulta = consulta.Where(t => t.Titulo.Contains(filtro.Busca));

            var ordenada = consulta.OrderByDescending(t => t.ExcluidoEm).ThenByDescending(t => t.IdTransparencia);

            modelo.TotalFiltrado = await ordenada.CountAsync();
            modelo.Pagina = PaginaValida(filtro, modelo.TotalFiltrado);

            modelo.Gastos = await ordenada
                .Skip((modelo.Pagina - 1) * TamanhoPagina)
                .Take(TamanhoPagina)
                .Select(t => new LixeiraGastoItemViewModel
                {
                    IdTransparencia = t.IdTransparencia,
                    Titulo = t.Titulo,
                    Categoria = t.Categoria,
                    Valor = t.Valor,
                    Data = t.Data,
                    ExcluidoEm = t.ExcluidoEm.Value,
                    Imagem = t.Imagem,
                    Comprovante = t.Comprovante
                })
                .ToListAsync();
        }

        return View(modelo);
    }

    // =====================================================================
    // ANIMAIS
    // =====================================================================

    // Restaurar: tira o animal da lixeira. Ele volta para a lista do admin e, se estiver Disponível, para o site.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RestaurarAnimal(int id)
    {
        var animal = await _context.Animais
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(a => a.IdAnimal == id && a.ExcluidoEm != null);

        if (animal == null) return NotFound();

        animal.ExcluidoEm = null;
        await _context.SaveChangesAsync();

        TempData["Sucesso"] = animal.Sexo.MensagemRestaurado(animal.Nome);
        return RedirectToAction(nameof(Index), new { tipo = TipoLixeira.Animais });
    }

    // Excluir definitivamente: apaga o animal e as fotos do banco e do disco. Não tem volta; por isso a tela pede confirmação (modal).
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ExcluirAnimalDefinitivamente(int id)
    {
        var animal = await _context.Animais
            .IgnoreQueryFilters()
            .Include(a => a.Fotos)
            .Include(a => a.Adotado)
            .FirstOrDefaultAsync(a => a.IdAnimal == id && a.ExcluidoEm != null);

        if (animal == null) return NotFound();

        // O registro de adoção é o histórico de quem adotou (e do termo assinado): não é apagado junto com o animal
        if (animal.Adotado != null)
        {
            TempData["Erro"] = $"{animal.Nome} tem um registro de adoção e não pode ser excluíd{animal.Sexo.ArtigoDefinido()} definitivamente. Restaure para manter o histórico.";
            return RedirectToAction(nameof(Index), new { tipo = TipoLixeira.Animais });
        }

        var arquivos = animal.Fotos.Select(f => f.Foto).ToList();

        // As fotos saem do banco junto com o animal (exclusão em cascata)
        _context.Animais.Remove(animal);
        await _context.SaveChangesAsync();

        // Do disco só depois que o banco confirmar (os arquivos de exemplo, fora de uploads/, nunca são apagados)
        arquivos.ForEach(_upload.ExcluirArquivo);

        TempData["Sucesso"] = animal.Sexo.MensagemExcluidoDefinitivo(animal.Nome);
        return RedirectToAction(nameof(Index), new { tipo = TipoLixeira.Animais });
    }

    // Restaurar os selecionados (a tela só marca itens da página atual)
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RestaurarAnimais(int[] ids)
    {
        ids = IdsValidos(ids);
        if (ids.Length == 0) return Erro(TipoLixeira.Animais, LixeiraLoteTexto.NenhumSelecionado);

        var animais = await _context.Animais
            .IgnoreQueryFilters()
            .Where(a => a.ExcluidoEm != null && ids.Contains(a.IdAnimal))
            .ToListAsync();

        if (animais.Count == 0) return Erro(TipoLixeira.Animais, LixeiraLoteTexto.NenhumAnimalNaLixeira);

        animais.ForEach(a => a.ExcluidoEm = null);
        await _context.SaveChangesAsync();

        TempData["Sucesso"] = LixeiraLoteTexto.AnimaisRestaurados(animais.Count);
        return RedirectToAction(nameof(Index), new { tipo = TipoLixeira.Animais });
    }

    // Excluir definitivamente os selecionados
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ExcluirAnimaisDefinitivamente(int[] ids)
    {
        ids = IdsValidos(ids);
        if (ids.Length == 0) return Erro(TipoLixeira.Animais, LixeiraLoteTexto.NenhumSelecionado);

        return await ExcluirAnimaisAsync(
            _context.Animais.IgnoreQueryFilters().Where(a => a.ExcluidoEm != null && ids.Contains(a.IdAnimal)));
    }

    // Excluir todos os animais da lixeira (a aba inteira, não só a página ou a busca atual)
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ExcluirTodosAnimais()
    {
        return await ExcluirAnimaisAsync(
            _context.Animais.IgnoreQueryFilters().Where(a => a.ExcluidoEm != null));
    }

    // =====================================================================
    // TRANSPARÊNCIA
    // =====================================================================

    // Restaurar: o gasto volta para a lista do admin e para a página pública de Transparência.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RestaurarGasto(int id)
    {
        var gasto = await _context.Transparencias
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(t => t.IdTransparencia == id && t.ExcluidoEm != null);

        if (gasto == null) return NotFound();

        gasto.ExcluidoEm = null;
        await _context.SaveChangesAsync();

        TempData["Sucesso"] = LixeiraTransparenciaTexto.MensagemRestaurado(gasto.Titulo);
        return RedirectToAction(nameof(Index), new { tipo = TipoLixeira.Transparencia });
    }

    // Excluir definitivamente: apaga o gasto, a foto e o comprovante do banco e do disco. Não tem volta.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ExcluirGastoDefinitivamente(int id)
    {
        var gasto = await _context.Transparencias
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(t => t.IdTransparencia == id && t.ExcluidoEm != null);

        if (gasto == null) return NotFound();

        var arquivos = new[] { gasto.Imagem, gasto.Comprovante };

        _context.Transparencias.Remove(gasto);
        await _context.SaveChangesAsync();

        // Do disco só depois que o banco confirmar
        foreach (var arquivo in arquivos) _upload.ExcluirArquivo(arquivo);

        TempData["Sucesso"] = LixeiraTransparenciaTexto.MensagemExcluidoDefinitivo(gasto.Titulo);
        return RedirectToAction(nameof(Index), new { tipo = TipoLixeira.Transparencia });
    }

    // Restaurar os selecionados (a tela só marca itens da página atual)
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RestaurarGastos(int[] ids)
    {
        ids = IdsValidos(ids);
        if (ids.Length == 0) return Erro(TipoLixeira.Transparencia, LixeiraLoteTexto.NenhumSelecionado);

        var gastos = await _context.Transparencias
            .IgnoreQueryFilters()
            .Where(t => t.ExcluidoEm != null && ids.Contains(t.IdTransparencia))
            .ToListAsync();

        if (gastos.Count == 0) return Erro(TipoLixeira.Transparencia, LixeiraLoteTexto.NenhumGastoNaLixeira);

        gastos.ForEach(t => t.ExcluidoEm = null);
        await _context.SaveChangesAsync();

        TempData["Sucesso"] = LixeiraLoteTexto.GastosRestaurados(gastos.Count);
        return RedirectToAction(nameof(Index), new { tipo = TipoLixeira.Transparencia });
    }

    // Excluir definitivamente os selecionados
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ExcluirGastosDefinitivamente(int[] ids)
    {
        ids = IdsValidos(ids);
        if (ids.Length == 0) return Erro(TipoLixeira.Transparencia, LixeiraLoteTexto.NenhumSelecionado);

        return await ExcluirGastosAsync(
            _context.Transparencias.IgnoreQueryFilters().Where(t => t.ExcluidoEm != null && ids.Contains(t.IdTransparencia)));
    }

    // Excluir todos os gastos da lixeira (a aba inteira, não só a página ou a busca atual)
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ExcluirTodosGastos()
    {
        return await ExcluirGastosAsync(
            _context.Transparencias.IgnoreQueryFilters().Where(t => t.ExcluidoEm != null));
    }

    // =====================================================================
    // AUXILIARES
    // =====================================================================

    // Ids que vieram da tela: sem repetidos e sem valores inválidos
    private static int[] IdsValidos(int[] ids) =>
        (ids ?? Array.Empty<int>()).Where(id => id > 0).Distinct().ToArray();

    // Volta para a aba com um aviso de erro
    private IActionResult Erro(TipoLixeira tipo, string mensagem)
    {
        TempData["Erro"] = mensagem;
        return RedirectToAction(nameof(Index), new { tipo });
    }

    // Exclusão definitiva em lote (vários selecionados ou todos). Animais com registro de adoção são mantidos na lixeira:
    // o registro é o histórico de quem adotou (mesma regra da exclusão de um animal só).
    private async Task<IActionResult> ExcluirAnimaisAsync(IQueryable<Animal> consulta)
    {
        var animais = await consulta
            .Include(a => a.Fotos)
            .Include(a => a.Adotado)
            .ToListAsync();

        if (animais.Count == 0) return Erro(TipoLixeira.Animais, LixeiraLoteTexto.NenhumAnimalNaLixeira);

        var excluiveis = animais.Where(a => a.Adotado == null).ToList();
        var mantidos = animais.Count - excluiveis.Count;

        if (excluiveis.Count == 0) return Erro(TipoLixeira.Animais, LixeiraLoteTexto.AnimaisMantidos(mantidos));

        var arquivos = excluiveis.SelectMany(a => a.Fotos.Select(f => f.Foto)).ToList();

        // As fotos saem do banco junto com os animais (exclusão em cascata)
        _context.Animais.RemoveRange(excluiveis);
        await _context.SaveChangesAsync();

        // Do disco só depois que o banco confirmar (os arquivos de exemplo, fora de uploads/, nunca são apagados)
        arquivos.ForEach(_upload.ExcluirArquivo);

        TempData["Sucesso"] = LixeiraLoteTexto.AnimaisExcluidos(excluiveis.Count, mantidos);
        return RedirectToAction(nameof(Index), new { tipo = TipoLixeira.Animais });
    }

    private async Task<IActionResult> ExcluirGastosAsync(IQueryable<Transparencia> consulta)
    {
        var gastos = await consulta.ToListAsync();

        if (gastos.Count == 0) return Erro(TipoLixeira.Transparencia, LixeiraLoteTexto.NenhumGastoNaLixeira);

        var arquivos = gastos.SelectMany(t => new[] { t.Imagem, t.Comprovante }).ToList();

        _context.Transparencias.RemoveRange(gastos);
        await _context.SaveChangesAsync();

        // Do disco só depois que o banco confirmar
        arquivos.ForEach(_upload.ExcluirArquivo);

        TempData["Sucesso"] = LixeiraLoteTexto.GastosExcluidos(gastos.Count);
        return RedirectToAction(nameof(Index), new { tipo = TipoLixeira.Transparencia });
    }

    // Página fora do intervalo (ex.: ?pagina=99, ou a última página esvaziou) volta para a última existente
    private static int PaginaValida(LixeiraFiltroViewModel filtro, int totalFiltrado)
    {
        var totalPaginas = Math.Max(1, (int)Math.Ceiling(totalFiltrado / (double)TamanhoPagina));
        filtro.Pagina = Math.Clamp(filtro.Pagina, 1, totalPaginas);
        return filtro.Pagina;
    }
}
