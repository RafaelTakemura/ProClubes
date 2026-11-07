using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using ProClubesWeb.Models;

namespace ProClubesWeb.Services;

public class WebSessionState
{
    public string? AccessToken { get; private set; }
    public string? RefreshToken { get; private set; }
    public PerfilUsuarioDto? Usuario { get; private set; }
    public int? UnidadeContextoId { get; private set; }
    public int NotificacoesNaoLidas { get; set; }

    public bool IsAuthenticated => !string.IsNullOrWhiteSpace(AccessToken) && Usuario is not null;
    public bool IsSuperAdmin => Usuario?.RoleAdministrativa == RoleAdministrativa.SuperAdmin;
    public bool IsAdmin => Usuario?.RoleAdministrativa == RoleAdministrativa.Admin;
    public bool IsTesoureiro => Usuario?.RoleAdministrativa == RoleAdministrativa.Tesoureiro;

    public event Action? OnChange;

    public void SetSession(LoginResponse response)
    {
        AccessToken = response.AccessToken;
        RefreshToken = response.RefreshToken;
        Usuario = response.Usuario;
        UnidadeContextoId = response.Usuario.RoleAdministrativa == RoleAdministrativa.SuperAdmin
            ? null
            : response.Usuario.UnidadeAdministrativaId;
        NotifyStateChanged();
    }

    public void SetUnidadeContexto(int? unidadeId)
    {
        if (!IsSuperAdmin)
            return;
        UnidadeContextoId = unidadeId;
        NotifyStateChanged();
    }

    public void Clear()
    {
        AccessToken = null;
        RefreshToken = null;
        Usuario = null;
        UnidadeContextoId = null;
        NotificacoesNaoLidas = 0;
        NotifyStateChanged();
    }

    public void NotifyStateChanged() => OnChange?.Invoke();
}

public class ProClubesApiClient
{
    private readonly HttpClient _http;
    private readonly WebSessionState _session;
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters = { new JsonStringEnumConverter() }
    };

    public ProClubesApiClient(HttpClient http, WebSessionState session)
    {
        _http = http;
        _session = session;
    }

    public async Task<(bool Ok, string? ErrorMessage)> LoginAsync(string identificador, string senha)
    {
        try
        {
            var resp = await _http.PostAsJsonAsync("api/v1/auth/login", new LoginRequest(identificador, senha), JsonOptions);
            if (!resp.IsSuccessStatusCode)
                return (false, await ExtractProblemDetailAsync(resp));

            var login = await resp.Content.ReadFromJsonAsync<LoginResponse>(JsonOptions);
            if (login is null)
                return (false, "Resposta inválida da API.");

            if (login.Usuario.RoleAdministrativa == RoleAdministrativa.Nenhuma)
                return (false, "Acesso negado: o painel ProClubesWeb é exclusivo para membros da diretoria (Super Admin, Admin e Tesoureiro). Utilize o aplicativo ProClubesApp.");

            _session.SetSession(login);
            await AtualizarContadorNotificacoesAsync();
            return (true, null);
        }
        catch (Exception ex)
        {
            return (false, $"Falha de conexão com a API: {ex.Message}");
        }
    }

    public async Task LogoutAsync()
    {
        try
        {
            if (_session.IsAuthenticated)
            {
                using var req = CreateRequest(HttpMethod.Post, "api/v1/auth/logout", new { refreshToken = _session.RefreshToken });
                await _http.SendAsync(req);
            }
        }
        catch
        {
            // Ignora erro de rede no logout
        }
        finally
        {
            _session.Clear();
        }
    }

    public async Task AtualizarContadorNotificacoesAsync()
    {
        var page = await GetAsync<PagedResponse<NotificacaoDto>>("api/v1/notificacoes?apenasNaoLidas=true&page=1&pageSize=1");
        if (page.Data is not null)
        {
            _session.NotificacoesNaoLidas = page.Data.TotalCount;
            _session.NotifyStateChanged();
        }
    }

    public async Task<(T? Data, string? Error)> GetAsync<T>(string relativeUrl)
    {
        try
        {
            using var req = CreateRequest(HttpMethod.Get, relativeUrl);
            var resp = await _http.SendAsync(req);
            if (resp.StatusCode == System.Net.HttpStatusCode.Unauthorized && await TryRefreshTokenAsync())
            {
                using var retryReq = CreateRequest(HttpMethod.Get, relativeUrl);
                resp = await _http.SendAsync(retryReq);
            }
            if (!resp.IsSuccessStatusCode)
                return (default, await ExtractProblemDetailAsync(resp));

            var data = await resp.Content.ReadFromJsonAsync<T>(JsonOptions);
            return (data, null);
        }
        catch (Exception ex)
        {
            return (default, $"Erro ao comunicar com a API: {ex.Message}");
        }
    }

    public async Task<(T? Data, string? Error)> PostAsync<T>(string relativeUrl, object? body = null)
    {
        try
        {
            using var req = CreateRequest(HttpMethod.Post, relativeUrl, body);
            var resp = await _http.SendAsync(req);
            if (resp.StatusCode == System.Net.HttpStatusCode.Unauthorized && await TryRefreshTokenAsync())
            {
                using var retryReq = CreateRequest(HttpMethod.Post, relativeUrl, body);
                resp = await _http.SendAsync(retryReq);
            }
            if (!resp.IsSuccessStatusCode)
                return (default, await ExtractProblemDetailAsync(resp));

            var data = await resp.Content.ReadFromJsonAsync<T>(JsonOptions);
            return (data, null);
        }
        catch (Exception ex)
        {
            return (default, $"Erro ao comunicar com a API: {ex.Message}");
        }
    }

    public async Task<(T? Data, string? Error)> PutAsync<T>(string relativeUrl, object? body = null)
    {
        try
        {
            using var req = CreateRequest(HttpMethod.Put, relativeUrl, body);
            var resp = await _http.SendAsync(req);
            if (resp.StatusCode == System.Net.HttpStatusCode.Unauthorized && await TryRefreshTokenAsync())
            {
                using var retryReq = CreateRequest(HttpMethod.Put, relativeUrl, body);
                resp = await _http.SendAsync(retryReq);
            }
            if (!resp.IsSuccessStatusCode)
                return (default, await ExtractProblemDetailAsync(resp));

            var data = await resp.Content.ReadFromJsonAsync<T>(JsonOptions);
            return (data, null);
        }
        catch (Exception ex)
        {
            return (default, $"Erro ao comunicar com a API: {ex.Message}");
        }
    }

    public async Task<(T? Data, string? Error)> PatchAsync<T>(string relativeUrl, object? body = null)
    {
        try
        {
            using var req = CreateRequest(HttpMethod.Patch, relativeUrl, body);
            var resp = await _http.SendAsync(req);
            if (resp.StatusCode == System.Net.HttpStatusCode.Unauthorized && await TryRefreshTokenAsync())
            {
                using var retryReq = CreateRequest(HttpMethod.Patch, relativeUrl, body);
                resp = await _http.SendAsync(retryReq);
            }
            if (!resp.IsSuccessStatusCode)
                return (default, await ExtractProblemDetailAsync(resp));

            var data = await resp.Content.ReadFromJsonAsync<T>(JsonOptions);
            return (data, null);
        }
        catch (Exception ex)
        {
            return (default, $"Erro ao comunicar com a API: {ex.Message}");
        }
    }

    private async Task<bool> TryRefreshTokenAsync()
    {
        if (string.IsNullOrWhiteSpace(_session.RefreshToken))
            return false;

        try
        {
            var resp = await _http.PostAsJsonAsync("api/v1/auth/refresh", new { refreshToken = _session.RefreshToken }, JsonOptions);
            if (!resp.IsSuccessStatusCode)
                return false;

            var login = await resp.Content.ReadFromJsonAsync<LoginResponse>(JsonOptions);
            if (login is null)
                return false;

            _session.SetSession(login);
            return true;
        }
        catch
        {
            return false;
        }
    }

    private HttpRequestMessage CreateRequest(HttpMethod method, string relativeUrl, object? body = null)
    {
        var req = new HttpRequestMessage(method, relativeUrl);
        if (!string.IsNullOrWhiteSpace(_session.AccessToken))
            req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _session.AccessToken);

        if (body is not null)
            req.Content = JsonContent.Create(body, options: JsonOptions);

        return req;
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
            // Fallback
        }
        return $"Operação recusada pela API (HTTP {(int)resp.StatusCode}).";
    }
}
