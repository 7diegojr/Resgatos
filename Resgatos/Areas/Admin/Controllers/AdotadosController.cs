using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Resgatos.Areas.Admin.ViewModels;
using Resgatos.Data;
using Resgatos.Helpers;
using Resgatos.Models;
using Resgatos.Services;

namespace Resgatos.Areas.Admin.Controllers;

// Sprint 14: gestão de adotados.
// Index (cards), Registrar (modal "Marcar como adotado" da lista de Animais), Termo (abre o termo anexado, só com login),
// Editar (modal de detalhes: altera os dados, anexa/troca/remove o termo e as observações) e Excluir (desfaz a adoção).
public class AdotadosController : AdminBaseController
{
    private const int TamanhoPagina = 12;
    private const int MaxParcelas = 24;
    private const int MaxObservacoes = 500;

    private readonly AppDbContext _context;
    private readonly UploadService _upload;

    public AdotadosController(AppDbContext context, UploadService upload)
    {
        _context = context;
        _upload = upload;
    }

    // =====================================================================
    // LISTA (cards)
    // =====================================================================
    public async Task<IActionResult> Index(AdotadosFiltroViewModel filtro)
    {
        filtro.Busca = filtro.Busca?.Trim();

        var todos = _context.Adotados.AsNoTracking();
        var totalGeral = await todos.CountAsync();

        // Busca e filtros rápidos (Espécie e Termo)
        IQueryable<Adotado> Filtrar()
        {
            var q = todos;
            if (!string.IsNullOrWhiteSpace(filtro.Busca))
            {
                q = q.Where(a =>
                    a.NomeAdotante.Contains(filtro.Busca) || a.Animal.Nome.Contains(filtro.Busca));
            }

            if (filtro.Especie.HasValue)
                q = q.Where(a => a.Animal.Especie == filtro.Especie.Value);

            if (filtro.Termo == FiltroTermoAdotado.Pendente)
                q = q.Where(a => a.TermoAssinado == null);
            else if (filtro.Termo == FiltroTermoAdotado.Anexado)
                q = q.Where(a => a.TermoAssinado != null);

            return q;
        }

        // ?abrir= (depois de salvar no modal, ou voltando das telas do animal): a adoção precisa aparecer mesmo que
        // a edição a tenha tirado do filtro (ex.: filtro "Termo pendente" e o termo acabou de ser anexado). Se ela não
        // passa mais nos filtros, eles são limpos.
        if (filtro.Abrir.HasValue && !await Filtrar().AnyAsync(a => a.IdAdotado == filtro.Abrir.Value))
        {
            filtro.Busca = null;
            filtro.Especie = null;
            filtro.Termo = null;
        }

        var consulta = Filtrar();

        // Veio de "Voltar para a adoção" (detalhes ou edição do animal): vai direto para a página em que essa adoção está
        // (a conta usa a mesma ordem da lista: quantas adoções vêm antes dela, dividido pelo tamanho da página).
        if (filtro.Abrir.HasValue)
        {
            var alvo = await consulta
                .Where(a => a.IdAdotado == filtro.Abrir.Value)
                .Select(a => new { a.IdAdotado, a.DataAdocao })
                .FirstOrDefaultAsync();

            if (alvo != null)
            {
                var antes = await consulta.CountAsync(a =>
                    a.DataAdocao > alvo.DataAdocao || (a.DataAdocao == alvo.DataAdocao && a.IdAdotado > alvo.IdAdotado));
                filtro.Pagina = antes / TamanhoPagina + 1;
            }
        }

        // Adoção mais recente primeiro
        var ordenada = consulta.OrderByDescending(a => a.DataAdocao).ThenByDescending(a => a.IdAdotado);

        var totalFiltrado = await ordenada.CountAsync();
        var totalPaginas = Math.Max(1, (int)Math.Ceiling(totalFiltrado / (double)TamanhoPagina));
        var pagina = Math.Clamp(filtro.Pagina, 1, totalPaginas);
        filtro.Pagina = pagina;

        var cards = await ordenada
            .Skip((pagina - 1) * TamanhoPagina)
            .Take(TamanhoPagina)
            .Select(a => new AdotadoCardViewModel
            {
                IdAdotado = a.IdAdotado,
                IdAnimal = a.IdAnimal,
                NomeAnimal = a.Animal.Nome,
                Especie = a.Animal.Especie,
                Sexo = a.Animal.Sexo,
                FotoCapa = a.Animal.Fotos
                    .OrderByDescending(f => f.Capa)
                    .ThenBy(f => f.IdAnimalFoto)
                    .Select(f => f.Foto)
                    .FirstOrDefault(),
                NomeAdotante = a.NomeAdotante,
                TelefoneAdotante = a.TelefoneAdotante,
                EmailAdotante = a.EmailAdotante,
                EnderecoAdotante = a.EnderecoAdotante,
                DataAdocao = a.DataAdocao,
                ValorTaxa = a.ValorTaxa,
                TaxaParcelada = a.TaxaParcelada,
                QtdParcelas = a.QtdParcelas,
                TermoAnexado = a.TermoAssinado != null,
                Observacoes = a.Observacoes,
                CriadoEm = a.CriadoEm,

                // Do animal: aparecem só no modal de detalhes
                Cor = a.Animal.Cor,
                Porte = a.Animal.Porte,
                FaixaEtaria = a.Animal.FaixaEtaria,
                IdadeEstimada = a.Animal.IdadeEstimada,
                DataChegada = a.Animal.DataChegada,
                AntipulgasOk = a.Animal.AntipulgasOk,
                VermifugadoOk = a.Animal.VermifugadoOk,
                VacinadoOk = a.Animal.VacinadoOk,
                CastradoOk = a.Animal.CastradoOk,
                FivTestado = a.Animal.FivTestado,
                FelvTestado = a.Animal.FelvTestado
            })
            .ToListAsync();

        return View(new AdotadosIndexViewModel
        {
            Filtro = filtro,
            Adotados = cards,
            TotalGeral = totalGeral,
            TotalFiltrado = totalFiltrado,
            Pagina = pagina,
            TamanhoPagina = TamanhoPagina,
            AbrirId = filtro.Abrir.HasValue && cards.Any(c => c.IdAdotado == filtro.Abrir.Value) ? filtro.Abrir : null
        });
    }

    // =====================================================================
    // TERMO (abre o arquivo anexado)
    // =====================================================================
    // O termo fica fora da wwwroot (tem dados pessoais): só chega ao navegador por aqui, e só com login (AdminBaseController).
    public async Task<IActionResult> Termo(int id)
    {
        var nomeArquivo = await _context.Adotados
            .AsNoTracking()
            .Where(a => a.IdAdotado == id)
            .Select(a => a.TermoAssinado)
            .FirstOrDefaultAsync();

        var caminho = _upload.CaminhoFisicoTermo(nomeArquivo);
        if (caminho == null) return NotFound();

        var tipo = Path.GetExtension(caminho).ToLowerInvariant() switch
        {
            ".pdf" => "application/pdf",
            ".png" => "image/png",
            ".webp" => "image/webp",
            _ => "image/jpeg"
        };

        return PhysicalFile(caminho, tipo);   // abre na aba (inline), não baixa
    }

    // =====================================================================
    // REGISTRAR (modal da lista de Animais)
    // =====================================================================
    // O modal envia por fetch (sem recarregar a página) para os erros aparecerem nos campos com tudo preenchido.
    // Resposta: { ok: true, redirect } ou { ok: false, erros: { Campo: "mensagem" }, mensagem }.
    // Criar o registro e mudar o status do animal acontecem no mesmo SaveChanges (uma transação só).
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Registrar(AdotadoFormViewModel form)
    {
        var animal = await _context.Animais.FirstOrDefaultAsync(a => a.IdAnimal == form.IdAnimal);
        if (animal == null)
            return Json(new { ok = false, mensagem = "Animal não encontrado. Atualize a página e tente de novo." });

        if (await _context.Adotados.AnyAsync(a => a.IdAnimal == animal.IdAnimal))
            return Json(new { ok = false, mensagem = "Este animal já tem uma adoção registrada." });

        var erros = Validar(form, out var valor, out var telefone);

        // Termo (opcional): só confere se veio um arquivo
        var termo = form.Termo != null && form.Termo.Length > 0 ? form.Termo : null;
        if (termo != null)
        {
            var erroTermo = await _upload.ValidarComprovanteAsync(termo);
            if (erroTermo != null) erros["Termo"] = $"\"{termo.FileName}\": {erroTermo}";
        }

        if (erros.Count > 0)
            return Json(new { ok = false, erros });

        var adotado = new Adotado
        {
            IdAnimal = animal.IdAnimal,
            NomeAdotante = form.NomeAdotante.Trim(),
            TelefoneAdotante = telefone,
            EmailAdotante = form.Email.Trim(),
            EnderecoAdotante = form.Endereco.Trim(),
            DataAdocao = form.DataAdocao.Value.Date,
            ValorTaxa = valor,
            TaxaParcelada = form.TaxaParcelada,
            QtdParcelas = form.TaxaParcelada ? form.QtdParcelas : null,
            Observacoes = string.IsNullOrWhiteSpace(form.Observacoes) ? null : form.Observacoes.Replace("\r\n", "\n").Trim(),
            CriadoEm = DateTime.Now
        };

        string arquivoTermo = null;
        try
        {
            if (termo != null)
            {
                arquivoTermo = await _upload.SalvarTermoAdotadoAsync(termo);
                adotado.TermoAssinado = arquivoTermo;
            }

            _context.Adotados.Add(adotado);
            animal.Status = StatusAnimal.Adotado;
            await _context.SaveChangesAsync();
        }
        catch
        {
            // Não deixa o termo órfão no disco se o banco falhar
            if (arquivoTermo != null) _upload.ExcluirTermo(arquivoTermo);
            throw;
        }

        TempData["Sucesso"] = animal.Sexo.MensagemAdotado(animal.Nome, adotado.NomeAdotante);
        return Json(new { ok = true, redirect = Url.Action(nameof(Index), "Adotados", new { area = "Admin" }) });
    }

    // =====================================================================
    // EDITAR (modal de detalhes)
    // =====================================================================
    // Mesmo formato do Registrar: o modal envia por fetch e recebe { ok: true } ou { ok: false, erros, mensagem }.
    // Quando dá certo, o modal recarrega a página (o aviso de sucesso vem do TempData).
    // Termo: se veio um arquivo novo, ele substitui o atual; se veio RemoverTermo, o atual é apagado; senão, nada muda.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Editar(AdotadoEdicaoViewModel form)
    {
        var adotado = await _context.Adotados
            .Include(a => a.Animal)
            .FirstOrDefaultAsync(a => a.IdAdotado == form.IdAdotado);
        if (adotado == null)
            return Json(new { ok = false, mensagem = "Adoção não encontrada. Atualize a página e tente de novo." });

        var erros = Validar(form, out var valor, out var telefone);

        var termo = form.Termo != null && form.Termo.Length > 0 ? form.Termo : null;
        if (termo != null)
        {
            var erroTermo = await _upload.ValidarComprovanteAsync(termo);
            if (erroTermo != null) erros["Termo"] = $"\"{termo.FileName}\": {erroTermo}";
        }

        if (erros.Count > 0)
            return Json(new { ok = false, erros });

        adotado.NomeAdotante = form.NomeAdotante.Trim();
        adotado.TelefoneAdotante = telefone;
        adotado.EmailAdotante = form.Email.Trim();
        adotado.EnderecoAdotante = form.Endereco.Trim();
        adotado.DataAdocao = form.DataAdocao.Value.Date;
        adotado.ValorTaxa = valor;
        adotado.TaxaParcelada = form.TaxaParcelada;
        adotado.QtdParcelas = form.TaxaParcelada ? form.QtdParcelas : null;
        adotado.Observacoes = string.IsNullOrWhiteSpace(form.Observacoes) ? null : form.Observacoes.Replace("\r\n", "\n").Trim();

        var termoAntigo = adotado.TermoAssinado;
        string termoNovo = null;
        try
        {
            if (termo != null)
            {
                termoNovo = await _upload.SalvarTermoAdotadoAsync(termo);
                adotado.TermoAssinado = termoNovo;
            }
            else if (form.RemoverTermo)
            {
                adotado.TermoAssinado = null;
            }

            await _context.SaveChangesAsync();
        }
        catch
        {
            // Não deixa o termo novo órfão no disco se o banco falhar
            if (termoNovo != null) _upload.ExcluirTermo(termoNovo);
            throw;
        }

        // O banco já está certo: agora sim apaga o arquivo que saiu (trocado ou removido)
        if (termoAntigo != null && termoAntigo != adotado.TermoAssinado)
            _upload.ExcluirTermo(termoAntigo);

        TempData["Sucesso"] = adotado.Animal.Sexo.MensagemAdocaoEditada(adotado.Animal.Nome);
        return Json(new { ok = true });
    }

    // =====================================================================
    // EXCLUIR (desfazer a adoção)
    // =====================================================================
    // Sem lixeira: o registro e o termo anexado somem de vez. O animal volta para a lista de Animais como Disponível
    // (se a adoção foi um engano, é o certo; se foi uma devolução, dá para trocar o status na edição do animal).
    // Apagar o registro e voltar o status acontecem no mesmo SaveChanges (uma transação só).
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Excluir(int id)
    {
        var adotado = await _context.Adotados
            .Include(a => a.Animal)
            .FirstOrDefaultAsync(a => a.IdAdotado == id);
        if (adotado == null)
        {
            TempData["Erro"] = "Adoção não encontrada. Ela pode já ter sido excluída.";
            return RedirectToAction(nameof(Index));
        }

        var animal = adotado.Animal;
        var termo = adotado.TermoAssinado;

        _context.Adotados.Remove(adotado);
        if (animal.Status == StatusAnimal.Adotado) animal.Status = StatusAnimal.Disponivel;
        await _context.SaveChangesAsync();

        _upload.ExcluirTermo(termo);   // só depois do banco: se ele falhar, o termo continua no disco

        TempData["Sucesso"] = animal.Sexo.MensagemAdocaoExcluida(animal.Nome);
        return RedirectToAction(nameof(Index));
    }

    // Regras que o modal também confere no navegador (o servidor sempre manda).
    // Os nomes das chaves são os mesmos dos campos do formulário.
    private static Dictionary<string, string> Validar(AdotadoFormViewModel form, out decimal valor, out string telefone)
    {
        var erros = new Dictionary<string, string>();
        valor = 0;
        telefone = null;

        var nome = form.NomeAdotante?.Trim() ?? "";
        if (nome.Length < 3) erros["NomeAdotante"] = "Informe o nome completo (mínimo 3 caracteres).";
        else if (nome.Length > 150) erros["NomeAdotante"] = "O nome pode ter no máximo 150 caracteres.";

        var digitos = AdotadoExtensions.SoDigitos(form.Telefone);
        if (digitos.Length is not (10 or 11)) erros["Telefone"] = "Informe um telefone com DDD, ex.: (41) 99999-9999.";
        else telefone = AdotadoExtensions.FormatarTelefone(digitos);

        var email = form.Email?.Trim() ?? "";
        if (email.Length == 0 || email.Length > 150 || !new System.ComponentModel.DataAnnotations.EmailAddressAttribute().IsValid(email))
            erros["Email"] = "Informe um e-mail válido.";

        var endereco = form.Endereco?.Trim() ?? "";
        if (endereco.Length < 8) erros["Endereco"] = "Informe o endereço completo (rua, número e bairro).";
        else if (endereco.Length > 255) erros["Endereco"] = "O endereço pode ter no máximo 255 caracteres.";

        if (!form.DataAdocao.HasValue) erros["DataAdocao"] = "Informe a data da adoção.";
        else if (form.DataAdocao.Value.Date > DateTime.Today) erros["DataAdocao"] = "A data da adoção não pode ser no futuro.";
        else if (form.DataAdocao.Value.Date < new DateTime(2000, 1, 1)) erros["DataAdocao"] = "Data inválida.";

        if (!AdotadoExtensions.TentarLerValor(form.Valor, out valor)) erros["Valor"] = "Informe um valor válido (ex.: 150,00).";
        else if (valor < 0.01m) erros["Valor"] = "O valor deve ser maior que R$ 0,00.";
        else if (valor > 99999999.99m) erros["Valor"] = "O valor é alto demais. Confira o que foi digitado.";
        else if (decimal.Round(valor, 2) != valor) erros["Valor"] = "O valor pode ter no máximo 2 casas depois da vírgula.";

        if (form.TaxaParcelada && (form.QtdParcelas is null or < 2 or > MaxParcelas))
            erros["QtdParcelas"] = $"Informe de 2 a {MaxParcelas} parcelas.";

        if (form.Observacoes != null && form.Observacoes.Replace("\r\n", "\n").Trim().Length > MaxObservacoes)
            erros["Observacoes"] = $"As observações podem ter no máximo {MaxObservacoes} caracteres.";

        return erros;
    }
}
