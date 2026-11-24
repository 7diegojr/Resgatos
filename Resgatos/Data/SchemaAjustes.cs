using System.Data;
using Microsoft.EntityFrameworkCore;

namespace Resgatos.Data;

// O projeto usa EnsureCreatedAsync (sem migrations). Ele cria o banco do zero, mas NÃO altera um banco que já existe.
// Aqui ficam as colunas adicionadas depois da primeira criação do banco: se a coluna não existir, ela é criada.
// Em um banco novo o EnsureCreated já cria tudo, e este ajuste não faz nada.
public static class SchemaAjustes
{
    public static async Task GarantirColunasAsync(AppDbContext context)
    {
        // Sprint 13: lixeira de animais (exclusão lógica)
        await GarantirColunaAsync(context, "Animais", "ExcluidoEm", "datetime(6) NULL");

        // Lixeira de gastos de transparência (exclusão lógica)
        await GarantirColunaAsync(context, "Transparencias", "ExcluidoEm", "datetime(6) NULL");

        // Sprint 13: "Data de chegada ao Resgatos" (só mês e ano) substitui "Data do resgate".
        // Se a coluna acabou de ser criada, aproveita a data do resgate que já existia (vira o dia 1 daquele mês).
        // As colunas antigas (DataResgate e DataCastracao) continuam no banco, sem uso; não são apagadas para não perder dados.
        var criouChegada = await GarantirColunaAsync(context, "Animais", "DataChegada", "datetime(6) NULL");
        if (criouChegada && await ExisteColunaAsync(context, "Animais", "DataResgate"))
        {
            await ExecutarAsync(context,
                "UPDATE `Animais` SET `DataChegada` = DATE_FORMAT(`DataResgate`, '%Y-%m-01') WHERE `DataResgate` IS NOT NULL");
        }
    }

    // Cria a coluna se ela não existe. Devolve true se criou agora.
    private static async Task<bool> GarantirColunaAsync(AppDbContext context, string tabela, string coluna, string definicao)
    {
        if (await ExisteColunaAsync(context, tabela, coluna)) return false;

        await ExecutarAsync(context, $"ALTER TABLE `{tabela}` ADD COLUMN `{coluna}` {definicao}");
        return true;
    }

    private static async Task<bool> ExisteColunaAsync(AppDbContext context, string tabela, string coluna)
    {
        var conexao = context.Database.GetDbConnection();
        var abriuAgora = conexao.State != ConnectionState.Open;
        if (abriuAgora) await conexao.OpenAsync();

        try
        {
            using var consulta = conexao.CreateCommand();
            // LOWER nos dois lados: no Windows o MySQL guarda nomes de tabela em minúsculas
            consulta.CommandText =
                "SELECT COUNT(*) FROM information_schema.COLUMNS " +
                "WHERE TABLE_SCHEMA = DATABASE() AND LOWER(TABLE_NAME) = LOWER(@tabela) AND LOWER(COLUMN_NAME) = LOWER(@coluna)";

            var pTabela = consulta.CreateParameter();
            pTabela.ParameterName = "@tabela";
            pTabela.Value = tabela;
            consulta.Parameters.Add(pTabela);

            var pColuna = consulta.CreateParameter();
            pColuna.ParameterName = "@coluna";
            pColuna.Value = coluna;
            consulta.Parameters.Add(pColuna);

            return Convert.ToInt32(await consulta.ExecuteScalarAsync()) > 0;
        }
        finally
        {
            if (abriuAgora) await conexao.CloseAsync();
        }
    }

    private static async Task ExecutarAsync(AppDbContext context, string sql)
    {
        var conexao = context.Database.GetDbConnection();
        var abriuAgora = conexao.State != ConnectionState.Open;
        if (abriuAgora) await conexao.OpenAsync();

        try
        {
            using var comando = conexao.CreateCommand();
            comando.CommandText = sql;
            await comando.ExecuteNonQueryAsync();
        }
        finally
        {
            if (abriuAgora) await conexao.CloseAsync();
        }
    }
}
