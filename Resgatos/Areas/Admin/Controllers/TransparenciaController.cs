using System.Globalization;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Resgatos.Areas.Admin.ViewModels;
using Resgatos.Data;
using Resgatos.Helpers;
using Resgatos.Models;
using Resgatos.Services;

namespace Resgatos.Areas.Admin.Controllers;

// Sprint 15: CRUD de transparência (gastos do projeto, com foto e comprovante).
// Index (lista), Detalhes (visualizar), Criar, Editar e Excluir.
// Tudo o que é cadastrado aqui aparece na página pública de Transparência.
// Excluir move o gasto para a lixeira (exclusão lógica).
public class TransparenciaController : AdminBaseController
{
    private const int TamanhoPagina = 10;
    private const decimal ValorMaximo = 99999999.99m; // mesmo limite da coluna decimal(10,2)

    private readonly AppDbContext _context;
    private readonly UploadService _upload;

    public TransparenciaController(AppDbContext context, UploadService upload)
    {
        _context = context;
        _upload = upload;
    }

    // =====================================================================
    // LISTA
    // =====================================================================
    public async Task<IActionResult> Index(TransparenciaFiltroAdminViewModel filtro)
    {
        filtro.Busca = filtro.Busca?.Trim();

        var hoje = DateTime.Today;
        var inicioMes = new DateTime(hoje.Year, hoje.Month, 1);
        var inicioProximoMes = inicioMes.AddMonths(1);

        var todos = _context.Transparencias.AsNoTracking();

        // ---------- Abas de categoria: contagem de cada uma (não depende da busca) ----------
        var contagens = await todos
            .GroupBy(t => t.Categoria)
            .Select(g => new { Categoria = g.Key, Quantidade = g.Count(), Total = g.Sum(t => t.Valor) })
            .ToListAsync();

        // ---------- Lista (respeita os filtros) ----------
        var consulta = todos;

        if (!string.IsNullOrWhiteSpace(filtro.Busca))
            consulta = consulta.Where(t => t.Titulo.Contains(filtro.Busca));

        if (filtro.Categoria.HasValue)
            consulta = consulta.Where(t => t.Categoria == filtro.Categoria.Value);

        switch (filtro.Periodo)
        {
            case PeriodoTransparenciaAdmin.Semana:   // últimos 7 dias (hoje + 6 anteriores)
                var seteDias = hoje.AddDays(-6);
                consulta = consulta.Where(t => t.Data >= seteDias);
                break;
            case PeriodoTransparenciaAdmin.Mes:      // mês atual do calendário
                consulta = consulta.Where(t => t.Data >= inicioMes && t.Data < inicioProximoMes);
                break;
            case PeriodoTransparenciaAdmin.Meses:    // últimos 3 meses
                var tresMeses = hoje.AddMonths(-3);
                consulta = consulta.Where(t => t.Data >= tresMeses);
                break;
        }

        if (filtro.Comprovante == FiltroAnexoAdmin.Com)
            consulta = consulta.Where(t => t.Comprovante != null && t.Comprovante != "");
        else if (filtro.Comprovante == FiltroAnexoAdmin.Sem)
            consulta = consulta.Where(t => t.Comprovante == null || t.Comprovante == "");

        if (filtro.Foto == FiltroAnexoAdmin.Com)
            consulta = consulta.Where(t => t.Imagem != null && t.Imagem != "");
        else if (filtro.Foto == FiltroAnexoAdmin.Sem)
            consulta = consulta.Where(t => t.Imagem == null || t.Imagem == "");

        // Em empate, o mais recente (e o lançado por último) vem primeiro
        IQueryable<Transparencia> ordenada = filtro.Ordem switch
        {
            OrdenacaoTransparenciaAdmin.MaisAntigos => consulta.OrderBy(t => t.Data).ThenBy(t => t.IdTransparencia),
            OrdenacaoTransparenciaAdmin.MaiorValor  => consulta.OrderByDescending(t => t.Valor).ThenByDescending(t => t.Data).ThenByDescending(t => t.IdTransparencia),
            OrdenacaoTransparenciaAdmin.MenorValor  => consulta.OrderBy(t => t.Valor).ThenByDescending(t => t.Data).ThenByDescending(t => t.IdTransparencia),
            OrdenacaoTransparenciaAdmin.TituloAZ    => consulta.OrderBy(t => t.Titulo).ThenBy(t => t.IdTransparencia),
            OrdenacaoTransparenciaAdmin.TituloZA    => consulta.OrderByDescending(t => t.Titulo).ThenBy(t => t.IdTransparencia),
            _                                       => consulta.OrderByDescending(t => t.Data).ThenByDescending(t => t.IdTransparencia)
        };

        var totalFiltrado = await ordenada.CountAsync();
        var valorFiltrado = await ordenada.SumAsync(t => (decimal?)t.Valor) ?? 0m;

        // Página fora do intervalo (ex.: ?pagina=99) volta para a última existente
        var totalPaginas = Math.Max(1, (int)Math.Ceiling(totalFiltrado / (double)TamanhoPagina));
        var pagina = Math.Clamp(filtro.Pagina, 1, totalPaginas);
        filtro.Pagina = pagina;

        var lancamentos = await ordenada
            .Skip((pagina - 1) * TamanhoPagina)
            .Take(TamanhoPagina)
            .Select(t => new TransparenciaAdminItemViewModel
            {
                IdTransparencia = t.IdTransparencia,
                Titulo = t.Titulo,
                Categoria = t.Categoria,
                Valor = t.Valor,
                Data = t.Data,
                Comprovante = t.Comprovante,
                Imagem = t.Imagem
            })
            .ToListAsync();

        var modelo = new TransparenciaIndexViewModel
        {
            Filtro = filtro,
            Lancamentos = lancamentos,
            ContagemPorCategoria = contagens.ToDictionary(c => c.Categoria, c => c.Quantidade),
            TotalGeral = contagens.Sum(c => c.Quantidade),
            ValorTotalGeral = contagens.Sum(c => c.Total),
            TotalFiltrado = totalFiltrado,
            ValorTotalFiltrado = valorFiltrado,
            Pagina = pagina,
            TamanhoPagina = TamanhoPagina
        };

        return View(modelo);
    }

    // =====================================================================
    // VISUALIZAR
    // =====================================================================
    public async Task<IActionResult> Detalhes(int id)
    {
        var gasto = await _context.Transparencias
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.IdTransparencia == id);

        if (gasto == null) return NotFound();

        return View(gasto);
    }

    // =====================================================================
    // CRIAR
    // =====================================================================
    [HttpGet]
    public IActionResult Criar()
    {
        // A data já vem com hoje: quase sempre o gasto é lançado no mesmo dia
        return View(new TransparenciaFormViewModel { Data = DateTime.Today });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Criar(TransparenciaFormViewModel form)
    {
        // Os arquivos atuais nunca vêm do navegador
        form.ImagemAtual = null;
        form.ComprovanteAtual = null;

        var novaImagem = form.NovaImagem is { Length: > 0 } ? form.NovaImagem : null;
        var novoComprovante = form.NovoComprovante is { Length: > 0 } ? form.NovoComprovante : null;

        var valor = await ValidarRegrasAsync(form, novaImagem, novoComprovante);
        if (!ModelState.IsValid) return View(form);

        var gasto = new Transparencia { CriadoEm = DateTime.Now };
        AplicarCampos(gasto, form, valor.Value);

        var arquivosSalvos = new List<string>();
        try
        {
            if (novaImagem != null)
            {
                gasto.Imagem = await _upload.SalvarImagemTransparenciaAsync(novaImagem);
                arquivosSalvos.Add(gasto.Imagem);
            }
            if (novoComprovante != null)
            {
                gasto.Comprovante = await _upload.SalvarComprovanteTransparenciaAsync(novoComprovante);
                arquivosSalvos.Add(gasto.Comprovante);
            }

            _context.Transparencias.Add(gasto);
            await _context.SaveChangesAsync();
        }
        catch
        {
            // Não deixa arquivo órfão no disco se o banco falhar
            arquivosSalvos.ForEach(_upload.ExcluirArquivo);
            throw;
        }

        TempData["Sucesso"] = TransparenciaTexto.MensagemCadastrado(gasto.Titulo);
        return RedirectToAction(nameof(Detalhes), new { id = gasto.IdTransparencia });
    }

    // =====================================================================
    // EDITAR
    // =====================================================================
    [HttpGet]
    public async Task<IActionResult> Editar(int id)
    {
        var gasto = await _context.Transparencias
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.IdTransparencia == id);

        if (gasto == null) return NotFound();

        var form = new TransparenciaFormViewModel
        {
            IdTransparencia = gasto.IdTransparencia,
            Categoria = gasto.Categoria,
            Titulo = gasto.Titulo,
            Valor = gasto.Valor.ToValorCampo(),
            Data = gasto.Data.Date,
            ImagemAtual = gasto.Imagem,
            ComprovanteAtual = gasto.Comprovante
        };

        return View(form);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Editar(int id, TransparenciaFormViewModel form)
    {
        var gasto = await _context.Transparencias.FirstOrDefaultAsync(t => t.IdTransparencia == id);
        if (gasto == null) return NotFound();

        form.IdTransparencia = id; // vale o id da URL, não o que veio no formulário

        var novaImagem = form.NovaImagem is { Length: > 0 } ? form.NovaImagem : null;
        var novoComprovante = form.NovoComprovante is { Length: > 0 } ? form.NovoComprovante : null;

        var valor = await ValidarRegrasAsync(form, novaImagem, novoComprovante);

        // Os arquivos atuais vêm do banco (nunca do formulário), inclusive quando a tela volta com erro
        form.ImagemAtual = gasto.Imagem;
        form.ComprovanteAtual = gasto.Comprovante;

        if (!ModelState.IsValid) return View(form);

        AplicarCampos(gasto, form, valor.Value);

        // Arquivos substituídos ou removidos: saem do banco agora e do disco só depois que o banco confirmar
        var arquivosAntigos = new List<string>();
        var arquivosSalvos = new List<string>();
        try
        {
            if (novaImagem != null)
            {
                if (!string.IsNullOrEmpty(gasto.Imagem)) arquivosAntigos.Add(gasto.Imagem);
                gasto.Imagem = await _upload.SalvarImagemTransparenciaAsync(novaImagem);
                arquivosSalvos.Add(gasto.Imagem);
            }
            else if (form.RemoverImagem && !string.IsNullOrEmpty(gasto.Imagem))
            {
                arquivosAntigos.Add(gasto.Imagem);
                gasto.Imagem = null;
            }

            if (novoComprovante != null)
            {
                if (!string.IsNullOrEmpty(gasto.Comprovante)) arquivosAntigos.Add(gasto.Comprovante);
                gasto.Comprovante = await _upload.SalvarComprovanteTransparenciaAsync(novoComprovante);
                arquivosSalvos.Add(gasto.Comprovante);
            }
            else if (form.RemoverComprovante && !string.IsNullOrEmpty(gasto.Comprovante))
            {
                arquivosAntigos.Add(gasto.Comprovante);
                gasto.Comprovante = null;
            }

            await _context.SaveChangesAsync();
        }
        catch
        {
            arquivosSalvos.ForEach(_upload.ExcluirArquivo);
            throw;
        }

        arquivosAntigos.ForEach(_upload.ExcluirArquivo);

        TempData["Sucesso"] = TransparenciaTexto.MensagemEditado(gasto.Titulo);
        return RedirectToAction(nameof(Index));
    }

    // =====================================================================
    // EXCLUIR (vai para a lixeira)
    // =====================================================================
    // Exclusão lógica: o gasto e os arquivos dele continuam no banco e no disco, só ficam marcados (ExcluidoEm)
    // e somem do site e das listas. A confirmação é um modal (partial _ModalConfirmacao), por isso só existe POST.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Excluir(int id)
    {
        var gasto = await _context.Transparencias.FirstOrDefaultAsync(t => t.IdTransparencia == id);
        if (gasto == null) return NotFound();

        gasto.ExcluidoEm = DateTime.Now;
        await _context.SaveChangesAsync();

        TempData["Sucesso"] = TransparenciaTexto.MensagemExcluido(gasto.Titulo);
        return RedirectToAction(nameof(Index));
    }

    // =====================================================================
    // AUXILIARES
    // =====================================================================

    // Regras que as anotações do formulário não cobrem. Os erros usam o nome do campo,
    // então aparecem logo abaixo dele na tela. Devolve o valor já convertido (null se o valor tem erro).
    private async Task<decimal?> ValidarRegrasAsync(TransparenciaFormViewModel form, IFormFile novaImagem, IFormFile novoComprovante)
    {
        form.Titulo = form.Titulo?.Trim();
        form.Valor = form.Valor?.Trim();

        // O mínimo de 3 caracteres é conferido depois do Trim (espaços não contam)
        var tituloJaTemErro = ModelState.TryGetValue(nameof(form.Titulo), out var entradaTitulo) && entradaTitulo.Errors.Count > 0;
        if (!string.IsNullOrEmpty(form.Titulo) && form.Titulo.Length < 3 && !tituloJaTemErro)
            ModelState.AddModelError(nameof(form.Titulo), "O título deve ter pelo menos 3 caracteres.");

        // Valor: texto digitado no formato brasileiro ("189,90"), convertido aqui
        decimal? valor = null;
        if (!string.IsNullOrEmpty(form.Valor))
        {
            if (!TentarLerValor(form.Valor, out var lido))
                ModelState.AddModelError(nameof(form.Valor), "Informe um valor válido (ex.: 189,90).");
            else if (decimal.Round(lido, 2) != lido)
                ModelState.AddModelError(nameof(form.Valor), "O valor pode ter no máximo 2 casas depois da vírgula.");
            else if (lido < 0.01m)
                ModelState.AddModelError(nameof(form.Valor), "O valor deve ser maior que R$ 0,00.");
            else if (lido > ValorMaximo)
                ModelState.AddModelError(nameof(form.Valor), "O valor é alto demais. Confira o que foi digitado.");
            else
                valor = lido;
        }

        // Data: não pode ser no futuro
        if (form.Data.HasValue)
        {
            var data = form.Data.Value.Date;
            if (data < new DateTime(2000, 1, 1))
                ModelState.AddModelError(nameof(form.Data), "Data inválida.");
            else if (data > DateTime.Today)
                ModelState.AddModelError(nameof(form.Data), "A data do gasto não pode ser no futuro.");
        }

        // Arquivos (cada um é opcional)
        if (novaImagem != null)
        {
            var erro = await _upload.ValidarAsync(novaImagem);
            if (erro != null)
                ModelState.AddModelError(nameof(form.NovaImagem), $"\"{novaImagem.FileName}\": {erro}");
        }

        if (novoComprovante != null)
        {
            var erro = await _upload.ValidarComprovanteAsync(novoComprovante);
            if (erro != null)
                ModelState.AddModelError(nameof(form.NovoComprovante), $"\"{novoComprovante.FileName}\": {erro}");
        }

        return valor;
    }

    // Converte o texto do campo de valor. Aceita "189,90", "1.234,50", "R$ 189,90" e também "189.90" (ponto como decimal).
    // Só números positivos: "-5" e letras viram erro.
    private static bool TentarLerValor(string texto, out decimal valor)
    {
        valor = 0;

        var limpo = texto.Replace("R$", "", StringComparison.OrdinalIgnoreCase).Replace(" ", "");
        if (limpo.Length == 0) return false;

        if (limpo.Contains(','))
        {
            // Formato brasileiro: o ponto separa os milhares e a vírgula, os centavos
            limpo = limpo.Replace(".", "").Replace(",", ".");
        }
        else if (Regex.IsMatch(limpo, @"^\d{1,3}(\.\d{3})+$"))
        {
            // "1.234" ou "1.234.567": pontos de milhar, sem centavos
            limpo = limpo.Replace(".", "");
        }

        return decimal.TryParse(limpo, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out valor);
    }

    // Copia os campos do formulário para o gasto (só o que a pessoa pode alterar; os arquivos são tratados à parte)
    private static void AplicarCampos(Transparencia gasto, TransparenciaFormViewModel form, decimal valor)
    {
        gasto.Categoria = form.Categoria.Value;
        gasto.Titulo = form.Titulo;
        gasto.Valor = valor;
        gasto.Data = form.Data.Value.Date;
    }
}
