using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Resgatos.Areas.Admin.ViewModels;
using Resgatos.Data;
using Resgatos.Helpers;
using Resgatos.Models;
using Resgatos.Services;

namespace Resgatos.Areas.Admin.Controllers;

// Sprint 13: CRUD de animais e fotos.
// Index (lista), Detalhes (visualizar), Criar, Editar e Excluir (vai para a lixeira).
public class AnimaisController : AdminBaseController
{
    private const int TamanhoPagina = 10;
    private const int MaxCaracteresHistoria = 225;

    // Até 10 fotos de 5 MB = 50 MB. O limite padrão do ASP.NET é ~30 MB, então os formulários com foto o aumentam.
    private const long LimiteEnvio = 60 * 1024 * 1024;

    private readonly AppDbContext _context;
    private readonly UploadService _upload;

    public AnimaisController(AppDbContext context, UploadService upload)
    {
        _context = context;
        _upload = upload;
    }

    // =====================================================================
    // LISTA
    // =====================================================================
    public async Task<IActionResult> Index(AnimaisFiltroAdminViewModel filtro)
    {
        filtro.Busca = filtro.Busca?.Trim();

        // Animal adotado sai desta lista e passa a aparecer só na página Adotados (Sprint 14)
        var todos = _context.Animais.AsNoTracking().Where(a => a.Status != StatusAnimal.Adotado);

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

        if (filtro.Sexo.HasValue)
            consulta = consulta.Where(a => a.Sexo == filtro.Sexo.Value);

        if (filtro.Porte.HasValue)
            consulta = consulta.Where(a => a.Porte == filtro.Porte.Value);

        if (filtro.FaixaEtaria.HasValue)
            consulta = consulta.Where(a => a.FaixaEtaria == filtro.FaixaEtaria.Value);

        // Saúde: a mesma regra da coluna "Saúde" da tabela (AnimalAdminItemViewModel.Pendencias).
        // FIV e FeLV só contam para gatos.
        if (filtro.Saude == FiltroSaudeAdmin.ComPendencias)
        {
            consulta = consulta.Where(a =>
                !a.AntipulgasOk || !a.VermifugadoOk || !a.VacinadoOk || !a.CastradoOk ||
                (a.Especie == EspecieAnimal.Gato && (!a.FivTestado || !a.FelvTestado)));
        }
        else if (filtro.Saude == FiltroSaudeAdmin.Completa)
        {
            consulta = consulta.Where(a =>
                a.AntipulgasOk && a.VermifugadoOk && a.VacinadoOk && a.CastradoOk &&
                (a.Especie != EspecieAnimal.Gato || (a.FivTestado && a.FelvTestado)));
        }

        // "Menos tempo" / "Mais tempo" olham a data de chegada ao Resgatos (o tempo que o animal está aqui).
        // Quem não tem data de chegada vai para o fim (o CASE coloca os preenchidos primeiro).
        IQueryable<Animal> ordenada = filtro.Ordem switch
        {
            OrdenacaoAnimaisAdmin.MaisTempo  => consulta.OrderBy(a => a.DataChegada == null ? 1 : 0).ThenBy(a => a.DataChegada).ThenBy(a => a.IdAnimal),
            OrdenacaoAnimaisAdmin.NomeAZ      => consulta.OrderBy(a => a.Nome).ThenBy(a => a.IdAnimal),
            OrdenacaoAnimaisAdmin.NomeZA      => consulta.OrderByDescending(a => a.Nome).ThenBy(a => a.IdAnimal),
            _                                 => consulta.OrderBy(a => a.DataChegada == null ? 1 : 0).ThenByDescending(a => a.DataChegada).ThenByDescending(a => a.IdAnimal)
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
                Porte = a.Porte,
                FaixaEtaria = a.FaixaEtaria,
                IdadeEstimada = a.IdadeEstimada,
                Status = a.Status,
                DataChegada = a.DataChegada,
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

    // =====================================================================
    // VISUALIZAR
    // =====================================================================
    public async Task<IActionResult> Detalhes(int id)
    {
        var animal = await _context.Animais
            .AsNoTracking()
            .Include(a => a.Fotos)
            .FirstOrDefaultAsync(a => a.IdAnimal == id);

        if (animal == null) return NotFound();

        // Animal adotado só aparece na página Adotados (Sprint 14)
        if (animal.Status == StatusAnimal.Adotado) return RedirectToAction("Index", "Adotados", new { area = "Admin", busca = animal.Nome });

        return View(animal);
    }

    // =====================================================================
    // CRIAR
    // =====================================================================
    [HttpGet]
    public IActionResult Criar()
    {
        return View(new AnimalFormViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequestSizeLimit(LimiteEnvio)]
    [RequestFormLimits(MultipartBodyLengthLimit = LimiteEnvio)]
    public async Task<IActionResult> Criar(AnimalFormViewModel form)
    {
        var novasFotos = form.NovasFotos.Where(f => f.Length > 0).ToList();

        await ValidarRegrasAsync(form, novasFotos, fotosMantidas: 0);
        if (!ModelState.IsValid) return View(form);

        var animal = new Animal { CriadoEm = DateTime.Now };
        AplicarCampos(animal, form);

        var arquivosSalvos = new List<string>();
        try
        {
            var fotosCriadas = await SalvarNovasFotosAsync(novasFotos, arquivosSalvos);
            foreach (var foto in fotosCriadas) animal.Fotos.Add(foto);
            DefinirCapa(fotosCriadas, fotosCriadas, form.CapaSelecionada);

            _context.Animais.Add(animal);
            await _context.SaveChangesAsync();
        }
        catch
        {
            // Não deixa arquivo órfão no disco se o banco falhar
            arquivosSalvos.ForEach(_upload.ExcluirArquivo);
            throw;
        }

        TempData["Sucesso"] = animal.Sexo.MensagemCadastrado(animal.Nome);
        return RedirectToAction(nameof(Detalhes), new { id = animal.IdAnimal });
    }

    // =====================================================================
    // EDITAR
    // =====================================================================
    [HttpGet]
    public async Task<IActionResult> Editar(int id)
    {
        var animal = await _context.Animais
            .AsNoTracking()
            .Include(a => a.Fotos)
            .FirstOrDefaultAsync(a => a.IdAnimal == id);

        if (animal == null) return NotFound();

        // Animal adotado só aparece na página Adotados (Sprint 14)
        if (animal.Status == StatusAnimal.Adotado) return RedirectToAction("Index", "Adotados", new { area = "Admin", busca = animal.Nome });

        var form = new AnimalFormViewModel
        {
            IdAnimal = animal.IdAnimal,
            Especie = animal.Especie,
            Nome = animal.Nome,
            Sexo = animal.Sexo,
            Porte = animal.Porte,
            Cor = animal.Cor,
            FaixaEtaria = animal.FaixaEtaria,
            IdadeEstimada = animal.IdadeEstimada,
            Status = animal.Status,
            MesChegada = animal.DataChegada?.Month,
            AnoChegada = animal.DataChegada?.Year,
            Historia = animal.Historia,
            Observacoes = animal.Observacoes,
            AntipulgasOk = animal.AntipulgasOk,
            VermifugadoOk = animal.VermifugadoOk,
            VacinadoOk = animal.VacinadoOk,
            CastradoOk = animal.CastradoOk,
            FivTestado = animal.FivTestado,
            ResultadoFiv = animal.ResultadoFiv,
            FelvTestado = animal.FelvTestado,
            ResultadoFelv = animal.ResultadoFelv
        };
        CarregarFotosExistentes(form, animal);

        return View(form);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequestSizeLimit(LimiteEnvio)]
    [RequestFormLimits(MultipartBodyLengthLimit = LimiteEnvio)]
    public async Task<IActionResult> Editar(int id, AnimalFormViewModel form)
    {
        var animal = await _context.Animais
            .Include(a => a.Fotos)
            .FirstOrDefaultAsync(a => a.IdAnimal == id);

        if (animal == null) return NotFound();

        // Animal adotado só aparece na página Adotados (Sprint 14)
        if (animal.Status == StatusAnimal.Adotado) return RedirectToAction("Index", "Adotados", new { area = "Admin", busca = animal.Nome });

        form.IdAnimal = id; // vale o id da URL, não o que veio no formulário

        var idsRemover = form.FotosRemover.ToHashSet();
        var fotosMantidas = animal.Fotos.Where(f => !idsRemover.Contains(f.IdAnimalFoto)).ToList();
        var novasFotos = form.NovasFotos.Where(f => f.Length > 0).ToList();

        await ValidarRegrasAsync(form, novasFotos, fotosMantidas.Count);
        if (!ModelState.IsValid)
        {
            CarregarFotosExistentes(form, animal);
            return View(form);
        }

        AplicarCampos(animal, form);

        // Fotos marcadas para remover: saem do banco agora e do disco só depois que o banco confirmar
        var fotosRemovidas = animal.Fotos.Where(f => idsRemover.Contains(f.IdAnimalFoto)).ToList();
        _context.AnimalFotos.RemoveRange(fotosRemovidas);

        var arquivosSalvos = new List<string>();
        try
        {
            var fotosCriadas = await SalvarNovasFotosAsync(novasFotos, arquivosSalvos);
            foreach (var foto in fotosCriadas) animal.Fotos.Add(foto);

            DefinirCapa(fotosMantidas.Concat(fotosCriadas).ToList(), fotosCriadas, form.CapaSelecionada);

            await _context.SaveChangesAsync();
        }
        catch
        {
            arquivosSalvos.ForEach(_upload.ExcluirArquivo);
            throw;
        }

        fotosRemovidas.ForEach(f => _upload.ExcluirArquivo(f.Foto));

        TempData["Sucesso"] = $"As alterações de {animal.Nome} foram salvas.";
        return RedirectToAction(nameof(Index));
    }

    // =====================================================================
    // EXCLUIR (vai para a lixeira)
    // =====================================================================
    // Exclusão lógica: o animal e as fotos continuam no banco e no disco, só ficam marcados (ExcluidoEm)
    // e somem do site e das listas. A confirmação é um modal (partial _ModalConfirmacao), por isso só existe POST.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Excluir(int id)
    {
        var animal = await _context.Animais.FirstOrDefaultAsync(a => a.IdAnimal == id);
        if (animal == null) return NotFound();

        // Animal adotado só aparece na página Adotados (Sprint 14)
        if (animal.Status == StatusAnimal.Adotado) return RedirectToAction("Index", "Adotados", new { area = "Admin", busca = animal.Nome });

        animal.ExcluidoEm = DateTime.Now;
        await _context.SaveChangesAsync();

        TempData["Sucesso"] = animal.Sexo.MensagemExcluido(animal.Nome);
        return RedirectToAction(nameof(Index));
    }

    // =====================================================================
    // AUXILIARES
    // =====================================================================

    // Regras que as anotações do formulário não cobrem. Os erros usam o nome do campo,
    // então aparecem logo abaixo dele na tela.
    private async Task ValidarRegrasAsync(AnimalFormViewModel form, List<IFormFile> novasFotos, int fotosMantidas)
    {
        // "Adotado" só é definido pelo botão de adoção da lista (que também registra quem adotou)
        if (form.Status == StatusAnimal.Adotado)
            ModelState.AddModelError(nameof(form.Status), "Para marcar como adotado, use o botão de adoção na lista de animais.");

        form.Nome = form.Nome?.Trim();
        form.Cor = form.Cor?.Trim();
        form.IdadeEstimada = form.IdadeEstimada?.Trim();

        // O navegador envia a quebra de linha como 2 caracteres (\r\n); contamos e guardamos como 1 (\n)
        form.Historia = form.Historia?.Replace("\r\n", "\n").Trim();
        if (form.Historia != null && form.Historia.Length > MaxCaracteresHistoria)
            ModelState.AddModelError(nameof(form.Historia), $"A história pode ter no máximo {MaxCaracteresHistoria} caracteres.");

        // FIV e FeLV só valem para gatos; e o resultado só vale se o teste foi feito
        if (form.Especie != EspecieAnimal.Gato)
        {
            form.FivTestado = false;
            form.FelvTestado = false;
        }
        if (!form.FivTestado) form.ResultadoFiv = null;
        if (!form.FelvTestado) form.ResultadoFelv = null;

        if (form.FivTestado && form.ResultadoFiv == null)
            ModelState.AddModelError(nameof(form.ResultadoFiv), "Informe o resultado do teste de FIV.");
        if (form.FelvTestado && form.ResultadoFelv == null)
            ModelState.AddModelError(nameof(form.ResultadoFelv), "Informe o resultado do teste de FeLV.");

        // Data de chegada: mês e ano juntos (ou nenhum), e não pode ser depois do mês atual
        if (form.MesChegada.HasValue != form.AnoChegada.HasValue)
        {
            ModelState.AddModelError("DataChegada", "Informe o mês e o ano da chegada.");
        }
        else if (form.MesChegada.HasValue)
        {
            var mes = form.MesChegada.Value;
            var ano = form.AnoChegada.Value;
            var hoje = DateTime.Today;

            if (mes < 1 || mes > 12 || ano < 2000 || ano > hoje.Year)
                ModelState.AddModelError("DataChegada", "Data de chegada inválida.");
            else if (new DateTime(ano, mes, 1) > new DateTime(hoje.Year, hoje.Month, 1))
                ModelState.AddModelError("DataChegada", "A data de chegada não pode ser no futuro.");
        }

        // Fotos
        foreach (var foto in novasFotos)
        {
            var erro = await _upload.ValidarAsync(foto);
            if (erro != null)
                ModelState.AddModelError(nameof(form.NovasFotos), $"\"{foto.FileName}\": {erro}");
        }

        var totalFotos = fotosMantidas + novasFotos.Count;
        if (totalFotos > UploadService.MaxFotosPorAnimal)
            ModelState.AddModelError(nameof(form.NovasFotos), $"Cada animal pode ter no máximo {UploadService.MaxFotosPorAnimal} fotos.");

        // Só aparece no site quem está disponível, e o card do site precisa de foto
        if (form.Status == StatusAnimal.Disponivel && totalFotos == 0)
            ModelState.AddModelError(nameof(form.Status), "Para ficar disponível, o animal precisa de pelo menos uma foto.");
    }

    // Copia os campos do formulário para o animal (só o que a pessoa pode alterar)
    private static void AplicarCampos(Animal animal, AnimalFormViewModel form)
    {
        animal.Especie = form.Especie.Value;
        animal.Nome = form.Nome;
        animal.Sexo = form.Sexo.Value;
        animal.Porte = form.Porte.Value;
        animal.Cor = form.Cor;
        animal.FaixaEtaria = form.FaixaEtaria.Value;
        animal.IdadeEstimada = form.IdadeEstimada;
        animal.Status = form.Status.Value;
        animal.DataChegada = form.MesChegada.HasValue && form.AnoChegada.HasValue
            ? new DateTime(form.AnoChegada.Value, form.MesChegada.Value, 1)
            : null;

        animal.Historia = string.IsNullOrWhiteSpace(form.Historia) ? null : form.Historia.Trim();
        animal.Observacoes = string.IsNullOrWhiteSpace(form.Observacoes) ? null : form.Observacoes.Trim();

        animal.AntipulgasOk = form.AntipulgasOk;
        animal.VermifugadoOk = form.VermifugadoOk;
        animal.VacinadoOk = form.VacinadoOk;
        animal.CastradoOk = form.CastradoOk;

        animal.FivTestado = form.FivTestado;
        animal.ResultadoFiv = form.ResultadoFiv;
        animal.FelvTestado = form.FelvTestado;
        animal.ResultadoFelv = form.ResultadoFelv;
    }

    // Salva as fotos no disco e devolve os registros (AnimalFoto) na mesma ordem em que foram enviadas.
    // Os caminhos salvos entram em "arquivosSalvos" para o chamador poder desfazer se o banco falhar.
    private async Task<List<AnimalFoto>> SalvarNovasFotosAsync(List<IFormFile> arquivos, List<string> arquivosSalvos)
    {
        var fotos = new List<AnimalFoto>();
        foreach (var arquivo in arquivos)
        {
            var caminho = await _upload.SalvarFotoAnimalAsync(arquivo);
            arquivosSalvos.Add(caminho);
            fotos.Add(new AnimalFoto { Foto = caminho });
        }
        return fotos;
    }

    // Garante UMA capa entre as fotos que o animal vai ter.
    // "selecao" vem do formulário: "foto-12" (existente) ou "nova-0" (a primeira das fotos enviadas agora).
    // Se não veio nada válido, mantém a capa atual; se ela foi removida (ou não havia), usa a primeira foto.
    private static void DefinirCapa(List<AnimalFoto> todas, List<AnimalFoto> novas, string selecao)
    {
        AnimalFoto capa = null;

        if (!string.IsNullOrEmpty(selecao))
        {
            if (selecao.StartsWith("nova-") && int.TryParse(selecao[5..], out var indice) && indice >= 0 && indice < novas.Count)
                capa = novas[indice];
            else if (selecao.StartsWith("foto-") && int.TryParse(selecao[5..], out var idFoto))
                capa = todas.FirstOrDefault(f => f.IdAnimalFoto == idFoto);
        }

        capa ??= todas.FirstOrDefault(f => f.Capa);
        capa ??= todas.FirstOrDefault();

        foreach (var foto in todas) foto.Capa = foto == capa;
    }

    // Preenche as fotos que o animal já tem (capa primeiro). Também usado quando a edição volta com erro.
    private static void CarregarFotosExistentes(AnimalFormViewModel form, Animal animal)
    {
        form.FotosExistentes = animal.Fotos
            .OrderByDescending(f => f.Capa)
            .ThenBy(f => f.IdAnimalFoto)
            .Select(f => new FotoExistenteViewModel { IdAnimalFoto = f.IdAnimalFoto, Caminho = f.Foto, Capa = f.Capa })
            .ToList();
    }
}
