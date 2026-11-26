using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Resgatos.Areas.Admin.ViewModels;
using Resgatos.Data;
using Resgatos.Helpers;
using Resgatos.Models;
using Resgatos.Services;

namespace Resgatos.Areas.Admin.Controllers;

// Sprint 14: gestão de adotados.
// Index (cards), Registrar (modal "Marcar como adotado" da lista de Animais) e Termo (abre o termo anexado, só com login).
// Detalhamento (pagamentos, trocar o termo) e desfazer adoção ficam para o próximo passo.
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

        var consulta = todos;
        if (!string.IsNullOrWhiteSpace(filtro.Busca))
        {
            consulta = consulta.Where(a =>
                a.NomeAdotante.Contains(filtro.Busca) || a.Animal.Nome.Contains(filtro.Busca));
        }

        if (filtro.Especie.HasValue)
            consulta = consulta.Where(a => a.Animal.Especie == filtro.Especie.Value);

        if (filtro.Termo == FiltroTermoAdotado.Pendente)
            consulta = consulta.Where(a => a.TermoAssinado == null);
        else if (filtro.Termo == FiltroTermoAdotado.Anexado)
            consulta = consulta.Where(a => a.TermoAssinado != null);

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
                EnderecoAdotante = a.EnderecoAdotante,
                DataAdocao = a.DataAdocao,
                ValorTaxa = a.ValorTaxa,
                TaxaParcelada = a.TaxaParcelada,
                QtdParcelas = a.QtdParcelas,
                TermoAnexado = a.TermoAssinado != null
            })
            .ToListAsync();

        return View(new AdotadosIndexViewModel
        {
            Filtro = filtro,
            Adotados = cards,
            TotalGeral = totalGeral,
            TotalFiltrado = totalFiltrado,
            Pagina = pagina,
            TamanhoPagina = TamanhoPagina
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
