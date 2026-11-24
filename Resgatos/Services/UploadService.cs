namespace Resgatos.Services;

// Upload e exclusão de arquivos enviados pelo admin.
// Os arquivos ficam em wwwroot/uploads/animais (fotos dos animais) e wwwroot/uploads/transparencia (foto e comprovante dos gastos);
// no banco guardamos só o caminho relativo ("uploads/animais/xxxx.jpg"), o mesmo formato dos arquivos de exemplo
// que o site público já usa (~/ + caminho).
public class UploadService
{
    public const int MaxFotosPorAnimal = 4;
    public const long TamanhoMaximoBytes = 5 * 1024 * 1024; // 5 MB por arquivo

    private const string PastaAnimais = "uploads/animais";
    private const string PastaTransparencia = "uploads/transparencia";

    // Só estas pastas podem ter arquivos apagados por aqui
    private static readonly string[] PastasPermitidas = { PastaAnimais, PastaTransparencia };

    private readonly IWebHostEnvironment _env;

    public UploadService(IWebHostEnvironment env)
    {
        _env = env;
    }

    // ---------- Validação ----------

    // Devolve null se a foto é válida; senão, a mensagem de erro para mostrar na tela.
    // Não confia no nome nem no Content-Type (o usuário controla os dois): confere os primeiros bytes do arquivo.
    public async Task<string> ValidarAsync(IFormFile arquivo)
    {
        if (arquivo.Length == 0) return "o arquivo está vazio.";
        if (arquivo.Length > TamanhoMaximoBytes) return "passa de 5 MB.";
        if (await DetectarExtensaoAsync(arquivo, permitirPdf: false) == null) return "precisa ser uma imagem JPG, PNG ou WebP.";
        return null;
    }

    // Igual ao ValidarAsync, mas o comprovante também pode ser um PDF.
    public async Task<string> ValidarComprovanteAsync(IFormFile arquivo)
    {
        if (arquivo.Length == 0) return "o arquivo está vazio.";
        if (arquivo.Length > TamanhoMaximoBytes) return "passa de 5 MB.";
        if (await DetectarExtensaoAsync(arquivo, permitirPdf: true) == null) return "precisa ser uma imagem JPG, PNG ou WebP, ou um PDF.";
        return null;
    }

    // ---------- Salvar ----------

    // Salva a foto com um nome novo (GUID) e devolve o caminho relativo para gravar no banco.
    public Task<string> SalvarFotoAnimalAsync(IFormFile arquivo) =>
        SalvarAsync(arquivo, PastaAnimais, permitirPdf: false);

    // Foto do gasto (aparece no card da página de Transparência)
    public Task<string> SalvarImagemTransparenciaAsync(IFormFile arquivo) =>
        SalvarAsync(arquivo, PastaTransparencia, permitirPdf: false);

    // Comprovante do gasto (imagem ou PDF)
    public Task<string> SalvarComprovanteTransparenciaAsync(IFormFile arquivo) =>
        SalvarAsync(arquivo, PastaTransparencia, permitirPdf: true);

    private async Task<string> SalvarAsync(IFormFile arquivo, string pastaRelativa, bool permitirPdf)
    {
        var extensao = await DetectarExtensaoAsync(arquivo, permitirPdf)
            ?? throw new InvalidOperationException("Arquivo inválido.");

        var pasta = Path.Combine(_env.WebRootPath, pastaRelativa.Replace('/', Path.DirectorySeparatorChar));
        Directory.CreateDirectory(pasta);

        var nomeArquivo = Guid.NewGuid().ToString("N") + extensao;
        await using (var destino = File.Create(Path.Combine(pasta, nomeArquivo)))
        {
            await arquivo.CopyToAsync(destino);
        }

        return $"{pastaRelativa}/{nomeArquivo}";
    }

    // ---------- Excluir ----------

    // Apaga o arquivo do disco. Só mexe em arquivos de uploads/animais e uploads/transparencia:
    // os arquivos de exemplo (img/adocoes/...) nunca são apagados por aqui.
    public void ExcluirArquivo(string caminhoRelativo)
    {
        if (string.IsNullOrWhiteSpace(caminhoRelativo)) return;

        var pastaRelativa = PastasPermitidas.FirstOrDefault(p =>
            caminhoRelativo.StartsWith(p + "/", StringComparison.OrdinalIgnoreCase));
        if (pastaRelativa == null) return;

        var pastaUploads = Path.GetFullPath(Path.Combine(_env.WebRootPath, pastaRelativa.Replace('/', Path.DirectorySeparatorChar)));
        var completo = Path.GetFullPath(Path.Combine(_env.WebRootPath, caminhoRelativo.Replace('/', Path.DirectorySeparatorChar)));

        // Garante que o caminho (mesmo com "..") continua dentro da pasta de uploads
        if (!completo.StartsWith(pastaUploads + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase)) return;

        try
        {
            if (File.Exists(completo)) File.Delete(completo);
        }
        catch (IOException)
        {
            // Arquivo em uso ou sem permissão: o registro já saiu do banco, então não vale derrubar a página por isso.
        }
    }

    // Descobre o tipo real pela "assinatura" dos primeiros bytes. null = tipo não aceito.
    // Aceita JPG, PNG e WebP (e PDF, quando permitirPdf é true).
    private static async Task<string> DetectarExtensaoAsync(IFormFile arquivo, bool permitirPdf)
    {
        var inicio = new byte[12];
        await using var fluxo = arquivo.OpenReadStream();
        var lidos = await fluxo.ReadAtLeastAsync(inicio.AsMemory(), inicio.Length, throwOnEndOfStream: false);
        if (lidos < 12) return null;

        // JPEG: FF D8 FF
        if (inicio[0] == 0xFF && inicio[1] == 0xD8 && inicio[2] == 0xFF) return ".jpg";

        // PNG: 89 50 4E 47 0D 0A 1A 0A
        if (inicio[0] == 0x89 && inicio[1] == 0x50 && inicio[2] == 0x4E && inicio[3] == 0x47 &&
            inicio[4] == 0x0D && inicio[5] == 0x0A && inicio[6] == 0x1A && inicio[7] == 0x0A) return ".png";

        // WebP: "RIFF" .... "WEBP"
        if (inicio[0] == 0x52 && inicio[1] == 0x49 && inicio[2] == 0x46 && inicio[3] == 0x46 &&
            inicio[8] == 0x57 && inicio[9] == 0x45 && inicio[10] == 0x42 && inicio[11] == 0x50) return ".webp";

        // PDF: "%PDF-"
        if (permitirPdf &&
            inicio[0] == 0x25 && inicio[1] == 0x50 && inicio[2] == 0x44 && inicio[3] == 0x46 && inicio[4] == 0x2D) return ".pdf";

        return null;
    }
}
