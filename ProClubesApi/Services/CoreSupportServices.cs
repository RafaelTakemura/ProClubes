using Microsoft.EntityFrameworkCore;
using ProClubesApi.Contracts;
using ProClubesApi.Data;
using ProClubesApi.Entities;

namespace ProClubesApi.Services;

public static class TimeZoneHelper
{
    private static readonly TimeZoneInfo SaoPauloTz = ResolveSaoPauloTimeZone();

    private static TimeZoneInfo ResolveSaoPauloTimeZone()
    {
        try
        {
            return TimeZoneInfo.FindSystemTimeZoneById("America/Sao_Paulo");
        }
        catch
        {
            return TimeZoneInfo.FindSystemTimeZoneById("E. South America Standard Time");
        }
    }

    public static DateTime UtcNow() => DateTime.UtcNow;

    public static DateTimeOffset ToSaoPauloOffset(DateTime utcDateTime)
    {
        var specifiedUtc = DateTime.SpecifyKind(utcDateTime, DateTimeKind.Utc);
        return TimeZoneInfo.ConvertTime(new DateTimeOffset(specifiedUtc), SaoPauloTz);
    }

    public static DateOnly TodaySaoPaulo(DateTime? utcNow = null)
    {
        var offset = ToSaoPauloOffset(utcNow ?? DateTime.UtcNow);
        return DateOnly.FromDateTime(offset.DateTime);
    }

    public static DateOnly FirstDayOfMonth(DateOnly date)
        => new(date.Year, date.Month, 1);

    public static DateOnly FirstDayOfNextMonth(DateOnly date)
        => new DateOnly(date.Year, date.Month, 1).AddMonths(1);

    public static DateOnly ResolveDayInMonth(int year, int month, int desiredDay)
    {
        var daysInMonth = DateTime.DaysInMonth(year, month);
        var actualDay = Math.Clamp(desiredDay, 1, daysInMonth);
        return new DateOnly(year, month, actualDay);
    }

    public static int CalculateAge(DateOnly birthDate, DateOnly referenceDate)
    {
        var age = referenceDate.Year - birthDate.Year;
        if (referenceDate < birthDate.AddYears(age))
            age--;
        return age;
    }
}

public class AuditoriaService
{
    private readonly ProClubesDbContext _db;

    public AuditoriaService(ProClubesDbContext db)
    {
        _db = db;
    }

    public void Registrar(
        int? usuarioAutorId,
        int? unidadeId,
        string tipoOperacao,
        string entidade,
        int? entidadeId,
        string? descricaoSegura)
    {
        _db.RegistrosAuditoria.Add(new RegistroAuditoria
        {
            UsuarioAutorId = usuarioAutorId,
            UnidadeId = unidadeId,
            TipoOperacao = tipoOperacao,
            Entidade = entidade,
            EntidadeId = entidadeId,
            DescricaoSegura = descricaoSegura is { Length: > 600 } ? descricaoSegura[..600] : descricaoSegura,
            CriadaEmUtc = DateTime.UtcNow
        });
    }
}

public class NotificacaoService
{
    private readonly ProClubesDbContext _db;

    public NotificacaoService(ProClubesDbContext db)
    {
        _db = db;
    }

    public async Task CriarNotificacaoAsync(
        int destinatarioUsuarioId,
        TipoNotificacao tipo,
        string titulo,
        string mensagem,
        string? referenciaTipo = null,
        int? referenciaId = null,
        string? chaveIdempotencia = null,
        CancellationToken ct = default)
    {
        if (!string.IsNullOrWhiteSpace(chaveIdempotencia))
        {
            var jaExiste = await _db.Notificacoes
                .AnyAsync(n => n.DestinatarioUsuarioId == destinatarioUsuarioId && n.ChaveIdempotencia == chaveIdempotencia, ct);
            if (jaExiste)
                return;
        }

        _db.Notificacoes.Add(new Notificacao
        {
            DestinatarioUsuarioId = destinatarioUsuarioId,
            Tipo = tipo,
            Titulo = titulo.Length > 150 ? titulo[..150] : titulo,
            Mensagem = mensagem.Length > 800 ? mensagem[..800] : mensagem,
            ReferenciaTipo = referenciaTipo,
            ReferenciaId = referenciaId,
            ChaveIdempotencia = chaveIdempotencia,
            CriadaEmUtc = DateTime.UtcNow
        });
    }

    public async Task<PagedResponse<NotificacaoDto>> ListarNotificacoesUsuarioAsync(
        int usuarioId,
        bool? apenasNaoLidas,
        PaginationQuery pagination,
        CancellationToken ct = default)
    {
        var query = _db.Notificacoes
            .AsNoTracking()
            .Where(n => n.DestinatarioUsuarioId == usuarioId);

        if (apenasNaoLidas == true)
            query = query.Where(n => n.LidaEmUtc == null);
        else if (apenasNaoLidas == false)
            query = query.Where(n => n.LidaEmUtc != null);

        var total = await query.CountAsync(ct);
        var items = await query
            .OrderByDescending(n => n.CriadaEmUtc)
            .Skip(pagination.Skip)
            .Take(pagination.NormalizedPageSize)
            .ToListAsync(ct);

        var dtos = items.Select(ToDto).ToList();
        return new PagedResponse<NotificacaoDto>(dtos, total, pagination.NormalizedPage, pagination.NormalizedPageSize);
    }

    public async Task<NotificacaoDto> MarcarComoLidaAsync(int usuarioId, int notificacaoId, CancellationToken ct = default)
    {
        var notif = await _db.Notificacoes
            .FirstOrDefaultAsync(n => n.Id == notificacaoId, ct);

        if (notif is null)
            throw new ApiException(StatusCodes.Status404NotFound, CodigosErro.RecursoNaoEncontrado, "Notificação não encontrada.");

        if (notif.DestinatarioUsuarioId != usuarioId)
            throw new ApiException(StatusCodes.Status403Forbidden, CodigosErro.RecursoSemPermissao, "Você não tem permissão para acessar esta notificação.");

        if (notif.LidaEmUtc is null)
        {
            notif.LidaEmUtc = DateTime.UtcNow;
            await _db.SaveChangesAsync(ct);
        }

        return ToDto(notif);
    }

    public static NotificacaoDto ToDto(Notificacao n) => new(
        Id: n.Id,
        Tipo: n.Tipo,
        Titulo: n.Titulo,
        Mensagem: n.Mensagem,
        ReferenciaTipo: n.ReferenciaTipo,
        ReferenciaId: n.ReferenciaId,
        CriadaEm: TimeZoneHelper.ToSaoPauloOffset(n.CriadaEmUtc),
        LidaEm: n.LidaEmUtc.HasValue ? TimeZoneHelper.ToSaoPauloOffset(n.LidaEmUtc.Value) : null,
        Lida: n.LidaEmUtc.HasValue);
}
