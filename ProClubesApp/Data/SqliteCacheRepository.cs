using System.Text.Json;
using ProClubesApp.Models;
using SQLite;

namespace ProClubesApp.Data;

// Cache local SQLite somente leitura de resumos sincronizados (Seção 13.2 do Plano v1.2)
// NUNCA armazena tokens, senhas ou fila de operações offline.
[Table("CacheResumoSincronizado")]
public class CacheResumoSincronizadoRow
{
    [PrimaryKey]
    public int Id { get; set; } = 1;
    public int UsuarioId { get; set; }
    public string SincronizadoEmIso { get; set; } = string.Empty;
    public string PayloadResumoJson { get; set; } = string.Empty;
}

public class SqliteCacheRepository
{
    private SQLiteAsyncConnection? _connection;

    private async Task<SQLiteAsyncConnection> GetConnectionAsync()
    {
        if (_connection is not null)
            return _connection;

        var dbPath = Path.Combine(FileSystem.AppDataDirectory, "proclubes_read_cache.db3");
        _connection = new SQLiteAsyncConnection(dbPath, SQLiteOpenFlags.ReadWrite | SQLiteOpenFlags.Create | SQLiteOpenFlags.SharedCache);
        await _connection.CreateTableAsync<CacheResumoSincronizadoRow>();
        return _connection;
    }

    public async Task SalvarResumoSincronizadoAsync(ResumoSincronizacaoAppDto resumo)
    {
        var conn = await GetConnectionAsync();
        var row = new CacheResumoSincronizadoRow
        {
            Id = 1,
            UsuarioId = resumo.Perfil.UsuarioId,
            SincronizadoEmIso = resumo.SincronizadoEm.ToString("O"),
            PayloadResumoJson = JsonSerializer.Serialize(resumo)
        };
        await conn.InsertOrReplaceAsync(row);
    }

    public async Task<ResumoSincronizacaoAppDto?> ObterResumoEmCacheAsync()
    {
        var conn = await GetConnectionAsync();
        var row = await conn.Table<CacheResumoSincronizadoRow>().FirstOrDefaultAsync(r => r.Id == 1);
        if (row is null || string.IsNullOrWhiteSpace(row.PayloadResumoJson))
            return null;

        try
        {
            return JsonSerializer.Deserialize<ResumoSincronizacaoAppDto>(row.PayloadResumoJson);
        }
        catch
        {
            return null;
        }
    }

    public async Task LimparCachePessoalNoLogoutAsync()
    {
        var conn = await GetConnectionAsync();
        await conn.DeleteAllAsync<CacheResumoSincronizadoRow>();
    }
}
