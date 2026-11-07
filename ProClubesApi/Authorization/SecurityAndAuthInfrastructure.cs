using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using ProClubesApi.Contracts;
using ProClubesApi.Data;
using ProClubesApi.Entities;

namespace ProClubesApi.Authorization;

public class JwtOptions
{
    public string Issuer { get; set; } = "ProClubesApi";
    public string Audience { get; set; } = "ProClubesClients";
    public string SigningKey { get; set; } = string.Empty;
    public int AccessTokenMinutes { get; set; } = 30;
    public int RefreshTokenDays { get; set; } = 14;
}

public static class ProClubesClaimTypes
{
    public const string UsuarioId = "pc_uid";
    public const string PessoaId = "pc_pid";
    public const string RoleAdministrativa = "pc_role";
    public const string UnidadeAdministrativaId = "pc_unid_adm";
    public const string EstadoConta = "pc_estado";
    public const string VersaoSeguranca = "pc_vsec";
}

public static class PasswordHasherHelper
{
    private static readonly PasswordHasher<string> Hasher = new();

    public static string HashPassword(string password)
        => Hasher.HashPassword("ProClubesUser", password);

    public static bool VerifyPassword(string hashedPassword, string providedPassword)
    {
        if (string.IsNullOrWhiteSpace(hashedPassword) || string.IsNullOrWhiteSpace(providedPassword))
            return false;
        var result = Hasher.VerifyHashedPassword("ProClubesUser", hashedPassword, providedPassword);
        return result == PasswordVerificationResult.Success || result == PasswordVerificationResult.SuccessRehashNeeded;
    }

    public static string ComputeSha256Hex(string input)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(input));
        return Convert.ToHexStringLower(bytes);
    }

    public static string GenerateSecureRandomToken()
    {
        var bytes = RandomNumberGenerator.GetBytes(32);
        return Convert.ToBase64String(bytes);
    }
}

public static class CpfHelper
{
    public static string Normalize(string? cpf)
    {
        if (string.IsNullOrWhiteSpace(cpf))
            return string.Empty;
        return new string(cpf.Where(char.IsDigit).ToArray());
    }

    public static bool IsValidFormat(string? cpf)
    {
        var digits = Normalize(cpf);
        if (digits.Length != 11)
            return false;
        if (digits.Distinct().Count() == 1)
            return false;
        return true;
    }

    public static string Mask(string? cpf)
    {
        var d = Normalize(cpf);
        if (d.Length != 11)
            return "***.***.***-**";
        return $"***.{d.Substring(3, 3)}.{d.Substring(6, 3)}-**";
    }
}

public record AuthenticatedUserInfo(
    int UsuarioId,
    int PessoaId,
    RoleAdministrativa Role,
    int? UnidadeAdministrativaId,
    EstadoConta EstadoConta,
    int? AssociadoId,
    int? UnidadeAssociadoId,
    string? CategoriaCodigo,
    StatusAssociado? StatusAssociado,
    bool EhDependenteAtivo,
    int? TitularAssociadoId,
    int? UnidadeTitularId);

public class CurrentUserService
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ProClubesDbContext _db;
    private AuthenticatedUserInfo? _cached;

    public CurrentUserService(IHttpContextAccessor httpContextAccessor, ProClubesDbContext db)
    {
        _httpContextAccessor = httpContextAccessor;
        _db = db;
    }

    public async Task<AuthenticatedUserInfo> GetRequiredUserAsync(bool allowRestritaCadastro = false, CancellationToken ct = default)
    {
        var user = await GetOptionalUserAsync(ct);
        if (user is null)
            throw new ApiException(StatusCodes.Status401Unauthorized, CodigosErro.SessaoInvalidaOuRevogada, "Autenticação necessária ou sessão revogada.");

        if (user.EstadoConta is EstadoConta.Bloqueada or EstadoConta.Desativada)
            throw new ApiException(StatusCodes.Status403Forbidden, CodigosErro.ContaBloqueadaOuDesativada, "Sua conta encontra-se desativada ou bloqueada.");

        if (!allowRestritaCadastro && user.EstadoConta == EstadoConta.RestritaCadastro)
            throw new ApiException(StatusCodes.Status403Forbidden, CodigosErro.CadastroPendente, "Seu cadastro ainda aguarda aprovação; apenas a consulta de status está liberada.");

        return user;
    }

    public async Task<AuthenticatedUserInfo?> GetOptionalUserAsync(CancellationToken ct = default)
    {
        if (_cached is not null)
            return _cached;

        var principal = _httpContextAccessor.HttpContext?.User;
        if (principal?.Identity?.IsAuthenticated != true)
            return null;

        var uidStr = principal.FindFirstValue(ProClubesClaimTypes.UsuarioId);
        var vsecStr = principal.FindFirstValue(ProClubesClaimTypes.VersaoSeguranca);
        if (!int.TryParse(uidStr, out var uid) || !int.TryParse(vsecStr, out var vsec))
            return null;

        var usuario = await _db.Usuarios
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == uid, ct);

        if (usuario is null || usuario.VersaoSeguranca != vsec)
            throw new ApiException(StatusCodes.Status401Unauthorized, CodigosErro.SessaoInvalidaOuRevogada, "Sessão expirada ou revogada.");

        if (usuario.EstadoConta is EstadoConta.Bloqueada or EstadoConta.Desativada)
            throw new ApiException(StatusCodes.Status403Forbidden, CodigosErro.ContaBloqueadaOuDesativada, "Conta desativada ou bloqueada.");

        var associado = await _db.Associados
            .AsNoTracking()
            .Include(a => a.CategoriaAssociado)
            .FirstOrDefaultAsync(a => a.PessoaId == usuario.PessoaId, ct);

        var dep = await _db.Dependencias
            .AsNoTracking()
            .Include(d => d.AssociadoTitular)
            .FirstOrDefaultAsync(d => d.PessoaDependenteId == usuario.PessoaId && d.DataFim == null, ct);

        // Regra D12: Desativação do associado bloqueia login quando não há outra condição válida de acesso
        if (usuario.EstadoConta == EstadoConta.Ativa
            && usuario.RoleAdministrativa == RoleAdministrativa.Nenhuma
            && associado is not null
            && associado.Status == StatusAssociado.Inativo
            && dep is null)
        {
            throw new ApiException(StatusCodes.Status403Forbidden, CodigosErro.ContaBloqueadaOuDesativada, "Vínculo associativo inativo.");
        }

        _cached = new AuthenticatedUserInfo(
            UsuarioId: usuario.Id,
            PessoaId: usuario.PessoaId,
            Role: usuario.RoleAdministrativa,
            UnidadeAdministrativaId: usuario.UnidadeAdministrativaId,
            EstadoConta: usuario.EstadoConta,
            AssociadoId: associado?.Status == StatusAssociado.Ativo ? associado.Id : null,
            UnidadeAssociadoId: associado?.Status == StatusAssociado.Ativo ? associado.UnidadeId : null,
            CategoriaCodigo: associado?.Status == StatusAssociado.Ativo ? associado.CategoriaAssociado.Codigo : null,
            StatusAssociado: associado?.Status,
            EhDependenteAtivo: dep is not null && dep.AssociadoTitular.Status == StatusAssociado.Ativo,
            TitularAssociadoId: dep?.AssociadoTitularId,
            UnidadeTitularId: dep?.AssociadoTitular.UnidadeId);

        return _cached;
    }

    public void EnsureSuperAdmin(AuthenticatedUserInfo user)
    {
        if (user.Role != RoleAdministrativa.SuperAdmin)
            throw new ApiException(StatusCodes.Status403Forbidden, CodigosErro.RecursoSemPermissao, "Operação exclusiva do Super Admin.");
    }

    public void EnsureAdminOrSuperAdminForUnit(AuthenticatedUserInfo user, int unidadeId)
    {
        if (user.Role == RoleAdministrativa.SuperAdmin)
            return;

        if (user.Role == RoleAdministrativa.Admin && user.UnidadeAdministrativaId == unidadeId)
            return;

        throw new ApiException(StatusCodes.Status403Forbidden, CodigosErro.UnidadeSemPermissao, "Você não possui permissão administrativa para esta unidade.");
    }

    public void EnsureFinancialAccessForUnit(AuthenticatedUserInfo user, int unidadeId)
    {
        if (user.Role == RoleAdministrativa.SuperAdmin)
            return;

        if ((user.Role == RoleAdministrativa.Admin || user.Role == RoleAdministrativa.Tesoureiro)
            && user.UnidadeAdministrativaId == unidadeId)
            return;

        throw new ApiException(StatusCodes.Status403Forbidden, CodigosErro.UnidadeSemPermissao, "Você não possui permissão financeira para esta unidade.");
    }
}

public class ProblemDetailsExceptionHandler : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        if (exception is ApiException apiEx)
        {
            var problem = new ProblemDetails
            {
                Status = apiEx.StatusCode,
                Title = apiEx.Title,
                Detail = apiEx.Message,
                Instance = httpContext.Request.Path
            };
            problem.Extensions["errorCode"] = apiEx.ErrorCode;

            httpContext.Response.StatusCode = apiEx.StatusCode;
            await httpContext.Response.WriteAsJsonAsync(problem, cancellationToken);
            return true;
        }

        if (exception is DbUpdateException dbEx)
        {
            var msg = dbEx.InnerException?.Message ?? dbEx.Message;
            var errorCode = CodigosErro.ConflitoConcorrencia;
            var detail = "Conflito de integridade ou operação concorrente no banco de dados.";

            if (msg.Contains("AdminAtivoUnidadeId", StringComparison.OrdinalIgnoreCase))
            {
                errorCode = CodigosErro.UnidadeJaPossuiAdminAtivo;
                detail = "Esta unidade já possui um Admin ativo.";
            }
            else if (msg.Contains("Cpf", StringComparison.OrdinalIgnoreCase))
            {
                errorCode = CodigosErro.CpfJaCadastrado;
                detail = "CPF já cadastrado no sistema.";
            }
            else if (msg.Contains("EleitorVotacaoId", StringComparison.OrdinalIgnoreCase))
            {
                errorCode = CodigosErro.VotoJaRegistrado;
                detail = "O eleitor já registrou voto nesta votação.";
            }
            else if (msg.Contains("MensalidadeId", StringComparison.OrdinalIgnoreCase))
            {
                errorCode = CodigosErro.MensalidadeJaPaga;
                detail = "Esta mensalidade já possui pagamento registrado.";
            }

            var problem = new ProblemDetails
            {
                Status = StatusCodes.Status409Conflict,
                Title = errorCode,
                Detail = detail,
                Instance = httpContext.Request.Path
            };
            problem.Extensions["errorCode"] = errorCode;

            httpContext.Response.StatusCode = StatusCodes.Status409Conflict;
            await httpContext.Response.WriteAsJsonAsync(problem, cancellationToken);
            return true;
        }

        return false;
    }
}
