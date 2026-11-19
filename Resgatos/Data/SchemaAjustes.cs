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
    }

    private static async Task GarantirColunaAsync(AppDbContext context, string tabela, string coluna, string definicao)
    {
        var conexao = context.Database.GetDbConnection();
        var abriuAgora = conexao.State != ConnectionState.Open;
        if (abriuAgora) await conexao.OpenAsync();

        try
        {
            // LOWER nos dois lados: no Windows o MySQL guarda nomes de tabela em minúsculas
            using (var consulta = conexao.CreateCommand())
            {
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

                var existe = Convert.ToInt32(await consulta.ExecuteScalarAsync()) > 0;
                if (existe) return;
            }

            using var alteracao = conexao.CreateCommand();
            alteracao.CommandText = $"ALTER TABLE `{tabela}` ADD COLUMN `{coluna}` {definicao}";
            await alteracao.ExecuteNonQueryAsync();
        }
        finally
        {
            if (abriuAgora) await conexao.CloseAsync();
        }
    }
}
