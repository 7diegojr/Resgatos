using Resgatos.Models;

namespace Resgatos.Helpers;

// Frases da lixeira de animais que concordam com o sexo do animal: "a Mel" / "o Tigrão", "restaurada" / "restaurado".
// O artigo ("a" ou "o") é também a letra final da palavra (restaurad-a, restaurad-o, excluíd-a, excluíd-o).
public static class LixeiraAnimalTextoExtensions
{
    // "A Mel foi restaurada e voltou para a lista de animais."
    public static string MensagemRestaurado(this SexoAnimal sexo, string nome)
    {
        var artigo = sexo.ArtigoDefinido();
        return $"{artigo.ToUpper()} {nome} foi restaurad{artigo} e voltou para a lista de animais.";
    }

    // "Tem certeza que deseja excluir definitivamente a Mel?"
    public static string PerguntaExcluirDefinitivo(this SexoAnimal sexo, string nome) =>
        $"Tem certeza que deseja excluir definitivamente {sexo.ArtigoDefinido()} {nome}?";

    // "A Mel e as fotos dela serão apagadas para sempre. Essa ação não pode ser desfeita."
    public static string AvisoExcluirDefinitivo(this SexoAnimal sexo, string nome)
    {
        var artigo = sexo.ArtigoDefinido();
        var dele = sexo == SexoAnimal.Femea ? "dela" : "dele";
        return $"{artigo.ToUpper()} {nome} e as fotos {dele} serão apagad{artigo}s para sempre. Essa ação não pode ser desfeita.";
    }

    // "A Mel foi excluída definitivamente."
    public static string MensagemExcluidoDefinitivo(this SexoAnimal sexo, string nome)
    {
        var artigo = sexo.ArtigoDefinido();
        return $"{artigo.ToUpper()} {nome} foi excluíd{artigo} definitivamente.";
    }
}

// Frases da lixeira de gastos da Transparência.
public static class LixeiraTransparenciaTexto
{
    // "Tem certeza que deseja" + quebra de linha + "excluir este gasto definitivamente?" (o confirmacao.js transforma o \n em quebra de linha)
    public const string PerguntaExcluirDefinitivo = "Tem certeza que deseja\nexcluir este gasto definitivamente?";

    // "O gasto "Ração premium" (R$ 189,90) e os arquivos anexados serão apagados para sempre. Essa ação não pode ser desfeita."
    public static string AvisoExcluirDefinitivo(string titulo, decimal valor) =>
        $"O gasto \"{titulo}\" ({valor.ToReais()}) e os arquivos anexados serão apagados para sempre. Essa ação não pode ser desfeita.";

    public static string MensagemRestaurado(string titulo) =>
        $"O gasto \"{titulo}\" foi restaurado e voltou para a página de Transparência.";

    public static string MensagemExcluidoDefinitivo(string titulo) =>
        $"O gasto \"{titulo}\" foi excluído definitivamente.";
}

// Frases das ações em lote da lixeira (excluir todos, restaurar e excluir os selecionados).
// Animais e gastos têm o mesmo molde; os textos com {n} são preenchidos pelo lixeira.js com a quantidade marcada na hora.
public static class LixeiraLoteTexto
{
    // ---------- Excluir todos (a quantidade já é conhecida quando a página é montada) ----------

    public const string PerguntaExcluirTodosAnimais = "Tem certeza que deseja\nexcluir todos os animais?";
    public const string PerguntaExcluirTodosGastos = "Tem certeza que deseja\nexcluir todos os gastos?";

    // "Os 5 animais da lixeira e as fotos deles serão apagados para sempre. Essa ação não pode ser desfeita. Os animais com registro de adoção são mantidos."
    public static string AvisoExcluirTodosAnimais(int quantidade) =>
        (quantidade == 1
            ? "O animal da lixeira e as fotos dele serão apagados para sempre."
            : $"Os {quantidade} animais da lixeira e as fotos deles serão apagados para sempre.")
        + " Essa ação não pode ser desfeita. Os animais com registro de adoção são mantidos.";

    // "Os 3 gastos da lixeira e os arquivos anexados serão apagados para sempre. Essa ação não pode ser desfeita."
    public static string AvisoExcluirTodosGastos(int quantidade) =>
        (quantidade == 1
            ? "O gasto da lixeira e os arquivos anexados serão apagados para sempre."
            : $"Os {quantidade} gastos da lixeira e os arquivos anexados serão apagados para sempre.")
        + " Essa ação não pode ser desfeita.";

    // ---------- Excluir os selecionados (a quantidade só é conhecida na hora do clique: o lixeira.js troca o {n}) ----------

    public const string PerguntaExcluirSelecionadoAnimal = "Tem certeza que deseja\nexcluir o animal selecionado?";
    public const string PerguntaExcluirSelecionadosAnimais = "Tem certeza que deseja\nexcluir os {n} animais selecionados?";
    public const string AvisoExcluirSelecionadoAnimal = "O animal selecionado e as fotos dele serão apagados para sempre. Essa ação não pode ser desfeita.";
    public const string AvisoExcluirSelecionadosAnimais = "Os {n} animais selecionados e as fotos deles serão apagados para sempre. Essa ação não pode ser desfeita. Os animais com registro de adoção são mantidos.";

    public const string PerguntaExcluirSelecionadoGasto = "Tem certeza que deseja\nexcluir o gasto selecionado?";
    public const string PerguntaExcluirSelecionadosGastos = "Tem certeza que deseja\nexcluir os {n} gastos selecionados?";
    public const string AvisoExcluirSelecionadoGasto = "O gasto selecionado e os arquivos anexados serão apagados para sempre. Essa ação não pode ser desfeita.";
    public const string AvisoExcluirSelecionadosGastos = "Os {n} gastos selecionados e os arquivos anexados serão apagados para sempre. Essa ação não pode ser desfeita.";

    // ---------- Avisos depois da ação ----------

    public const string NenhumSelecionado = "Selecione pelo menos um item.";
    public const string NenhumAnimalNaLixeira = "Não há animais na lixeira.";
    public const string NenhumGastoNaLixeira = "Não há gastos na lixeira.";

    // "1 animal foi restaurado e voltou para a lista de animais." / "3 animais foram restaurados e voltaram para a lista de animais."
    public static string AnimaisRestaurados(int quantidade) => quantidade == 1
        ? "1 animal foi restaurado e voltou para a lista de animais."
        : $"{quantidade} animais foram restaurados e voltaram para a lista de animais.";

    // "1 gasto foi restaurado e voltou para a página de Transparência." / "3 gastos foram restaurados e voltaram para a página de Transparência."
    public static string GastosRestaurados(int quantidade) => quantidade == 1
        ? "1 gasto foi restaurado e voltou para a página de Transparência."
        : $"{quantidade} gastos foram restaurados e voltaram para a página de Transparência.";

    // "3 animais foram excluídos definitivamente." (+ aviso dos que foram mantidos por terem registro de adoção)
    public static string AnimaisExcluidos(int excluidos, int mantidos)
    {
        var texto = excluidos == 1
            ? "1 animal foi excluído definitivamente."
            : $"{excluidos} animais foram excluídos definitivamente.";

        if (mantidos > 0)
            texto += mantidos == 1
                ? " 1 animal foi mantido na lixeira por ter registro de adoção."
                : $" {mantidos} animais foram mantidos na lixeira por terem registro de adoção.";

        return texto;
    }

    // Nada foi excluído: todos os escolhidos têm registro de adoção
    public static string AnimaisMantidos(int mantidos) => mantidos == 1
        ? "O animal tem um registro de adoção e não pode ser excluído definitivamente. Restaure para manter o histórico."
        : "Os animais têm registro de adoção e não podem ser excluídos definitivamente. Restaure para manter o histórico.";

    public static string GastosExcluidos(int quantidade) => quantidade == 1
        ? "1 gasto foi excluído definitivamente."
        : $"{quantidade} gastos foram excluídos definitivamente.";
}

// Detalhes visuais da lixeira usados na tela.
public static class LixeiraExtensions
{
    // Há quanto tempo o item foi para a lixeira, em linguagem simples: "Hoje", "Ontem", "Há 3 dias", "Há 2 meses", "Há 1 ano".
    // Conta dias fechados e se atualiza sozinho com o tempo. "hoje" existe só para poder testar com outra data.
    public static string ToHaQuantoTempo(this DateTime data, DateTime? hoje = null)
    {
        var dias = ((hoje ?? DateTime.Today).Date - data.Date).Days;

        if (dias <= 0) return "Hoje";
        if (dias == 1) return "Ontem";
        if (dias < 30) return $"Há {dias} dias";

        var meses = dias / 30;
        if (meses < 12) return meses == 1 ? "Há 1 mês" : $"Há {meses} meses";

        var anos = dias / 365;
        return anos <= 1 ? "Há 1 ano" : $"Há {anos} anos";
    }
}
