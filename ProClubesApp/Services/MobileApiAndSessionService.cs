using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using ProClubesApp.Data;
using ProClubesApp.Models;

namespace ProClubesApp.Services;

public class MobileSessionAndApiService
{
    private const string KeyAccessToken = "pc_access_token";
    private const string KeyRefreshToken = "pc_refresh_token";

    private readonly HttpClient _http;
    private readonly SqliteCacheRepository _cache;
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public PerfilUsuarioDto? UsuarioAtual { get; private set; }
    public bool IsOffline { get; private set; }
    public DateTimeOffset? LastSyncAt { get; private set; }

    public bool IsAuthenticated => UsuarioAtual is not null;
    public bool IsRestritaCadastro => UsuarioAtual?.EstadoConta == "RestritaCadastro";
    public bool IsDependente => UsuarioAtual?.EhDependenteAtivo == true;
    public bool IsSimpatizante => UsuarioAtual?.CategoriaCodigo == "SIMPATIZANTE" && !IsDependente;
    public bool IsTitularPagante => UsuarioAtual?.AssociadoId.HasValue == true && !IsSimpatizante && !IsDependente;
    public bool TemDireitoVoto => UsuarioAtual?.CategoriaCodigo is "ASSOCIADO" or "FUNDADOR";

    public event Action? StateChanged;

    public MobileSessionAndApiService(HttpClient http, SqliteCacheRepository cache)
    {
        _http = http;
        _cache = cache;
    }

    public async Task<(bool Ok, string? Error)> LoginAsync(string identificador, string senha)
    {
        try
        {
            var resp = await _http.PostAsJsonAsync("api/v1/auth/login", new LoginRequest(identificador, senha), JsonOptions);
            if (!resp.IsSuccessStatusCode)
                return (false, await ExtractProblemDetailAsync(resp));

            var login = await resp.Content.ReadFromJsonAsync<LoginResponse>(JsonOptions);
            if (login is null)
                return (false, "Resposta inválida da API.");

            await SecureStorage.Default.SetAsync(KeyAccessToken, login.AccessToken);
            await SecureStorage.Default.SetAsync(KeyRefreshToken, login.RefreshToken);

            UsuarioAtual = login.Usuario;
            IsOffline = false;
            StateChanged?.Invoke();
            return (true, null);
        }
        catch (Exception ex)
        {
            return (false, $"Sem conexão com a API para realizar login: {ex.Message}");
        }
    }

    public async Task LogoutAsync()
    {
        try
        {
            var token = await SecureStorage.Default.GetAsync(KeyAccessToken);
            var refresh = await SecureStorage.Default.GetAsync(KeyRefreshToken);
            if (!string.IsNullOrWhiteSpace(token))
            {
                using var req = new HttpRequestMessage(HttpMethod.Post, "api/v1/auth/logout");
                req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
                req.Content = JsonContent.Create(new { refreshToken = refresh }, options: JsonOptions);
                await _http.SendAsync(req);
            }
        }
        catch
        {
            // Ignora falha de rede no logout
        }
        finally
        {
            SecureStorage.Default.Remove(KeyAccessToken);
            SecureStorage.Default.Remove(KeyRefreshToken);
            await _cache.LimparCachePessoalNoLogoutAsync();
            UsuarioAtual = null;
            LastSyncAt = null;
            StateChanged?.Invoke();
        }
    }

    public async Task<(ResumoSincronizacaoAppDto? Resumo, bool VeioDoCacheOffline, string? Error)> SincronizarOuLerCacheAsync()
    {
        var (online, err) = await GetAsync<ResumoSincronizacaoAppDto>("api/v1/me/sincronizacao");
        if (online is not null)
        {
            IsOffline = false;
            LastSyncAt = online.SincronizadoEm;
            UsuarioAtual = online.Perfil;
            await _cache.SalvarResumoSincronizadoAsync(online);
            StateChanged?.Invoke();
            return (online, false, null);
        }

        // Modo offline somente leitura a partir do SQLite (Regra 13.2 e UI09)
        var cached = await _cache.ObterResumoEmCacheAsync();
        if (cached is not null)
        {
            IsOffline = true;
            LastSyncAt = cached.SincronizadoEm;
            UsuarioAtual ??= cached.Perfil;
            StateChanged?.Invoke();
            return (cached, true, null);
        }

        return (null, false, err ?? "Sem conexão e sem dados sincronizados no cache local.");
    }

    public async Task<(T? Data, string? Error)> GetAsync<T>(string relativeUrl, bool anonymous = false)
    {
        try
        {
            using var req = new HttpRequestMessage(HttpMethod.Get, relativeUrl);
            if (!anonymous)
            {
                var token = await SecureStorage.Default.GetAsync(KeyAccessToken);
                if (!string.IsNullOrWhiteSpace(token))
                    req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            }

            var resp = await _http.SendAsync(req);
            if (!resp.IsSuccessStatusCode)
                return (default, await ExtractProblemDetailAsync(resp));

            IsOffline = false;
            var data = await resp.Content.ReadFromJsonAsync<T>(JsonOptions);
            return (data, null);
        }
        catch (Exception ex)
        {
            IsOffline = true;
            return (default, $"Modo offline / sem conexão com o servidor ({ex.Message}).");
        }
    }

    public async Task<(T? Data, string? Error)> PostAsync<T>(string relativeUrl, object? body = null, bool anonymous = false)
    {
        if (Connectivity.Current.NetworkAccess != NetworkAccess.Internet && Connectivity.Current.NetworkAccess != NetworkAccess.ConstrainedInternet && Connectivity.Current.NetworkAccess != NetworkAccess.Local)
        {
            IsOffline = true;
            return (default, "Operação bloqueada: escritas (reservas, manifestações, votos e cadastros) exigem conexão ativa com a internet.");
        }

        try
        {
            using var req = new HttpRequestMessage(HttpMethod.Post, relativeUrl);
            if (!anonymous)
            {
                var token = await SecureStorage.Default.GetAsync(KeyAccessToken);
                if (!string.IsNullOrWhiteSpace(token))
                    req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            }
            if (body is not null)
                req.Content = JsonContent.Create(body, options: JsonOptions);

            var resp = await _http.SendAsync(req);
            if (!resp.IsSuccessStatusCode)
                return (default, await ExtractProblemDetailAsync(resp));

            IsOffline = false;
            var data = await resp.Content.ReadFromJsonAsync<T>(JsonOptions);
            return (data, null);
        }
        catch (Exception ex)
        {
            IsOffline = true;
            return (default, $"Não foi possível concluir a operação sem conexão com a API: {ex.Message}");
        }
    }

    public async Task<(T? Data, string? Error)> PutAsync<T>(string relativeUrl, object? body = null)
    {
        try
        {
            using var req = new HttpRequestMessage(HttpMethod.Put, relativeUrl);
            var token = await SecureStorage.Default.GetAsync(KeyAccessToken);
            if (!string.IsNullOrWhiteSpace(token))
                req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            if (body is not null)
                req.Content = JsonContent.Create(body, options: JsonOptions);

            var resp = await _http.SendAsync(req);
            if (!resp.IsSuccessStatusCode)
                return (default, await ExtractProblemDetailAsync(resp));

            var data = await resp.Content.ReadFromJsonAsync<T>(JsonOptions);
            return (data, null);
        }
        catch (Exception ex)
        {
            return (default, $"Sem conexão para enviar dados: {ex.Message}");
        }
    }

    private static async Task<string> ExtractProblemDetailAsync(HttpResponseMessage resp)
    {
        try
        {
            using var doc = await JsonDocument.ParseAsync(await resp.Content.ReadAsStreamAsync());
            var root = doc.RootElement;
            var detail = root.TryGetProperty("detail", out var d) ? d.GetString() : null;
            var code = root.TryGetProperty("errorCode", out var c) ? c.GetString() : null;
            if (!string.IsNullOrWhiteSpace(detail) && !string.IsNullOrWhiteSpace(code))
                return $"{detail} [{code}]";
            if (!string.IsNullOrWhiteSpace(detail))
                return detail!;
        }
        catch
        {
            // Ignora
        }
        return $"Operação não concluída (HTTP {(int)resp.StatusCode}).";
    }
}
