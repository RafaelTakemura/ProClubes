using System.Collections.Concurrent;
using Microsoft.EntityFrameworkCore;
using ProClubesApi.Authorization;
using ProClubesApi.Contracts;
using ProClubesApi.Data;
using ProClubesApi.Entities;

namespace ProClubesApi.Services;

public class EventoEspacoReservaService
{
    private static readonly ConcurrentDictionary<int, SemaphoreSlim> EspacoLocks = new();

    private readonly ProClubesDbContext _db;
    private readonly CurrentUserService _currentUser;
    private readonly AuditoriaService _auditoria;
    private readonly NotificacaoService _notificacao;

    public EventoEspacoReservaService(
        ProClubesDbContext db,
        CurrentUserService currentUser,
        AuditoriaService auditoria,
        NotificacaoService notificacao)
    {
        _db = db;
        _currentUser = currentUser;
        _auditoria = auditoria;
        _notificacao = notificacao;
    }

    // ==================== EVENTOS ====================

    public async Task<IReadOnlyList<EventoDto>> ListarEventosPublicosAsync(int? unidadeId = null, CancellationToken ct = default)
    {
        var query = _db.Eventos
            .AsNoTracking()
            .Include(e => e.UnidadeOrganizadora)
            .Include(e => e.UnidadesParticipantes)
                .ThenInclude(eu => eu.Unidade)
            .Include(e => e.OcupacoesEspaco)
                .ThenInclude(o => o.Espaco)
            .Where(e => e.PublicoAlvo == PublicoAlvoEvento.Publico && e.Status == StatusEvento.Programado);

        if (unidadeId.HasValue)
        {
            query = query.Where(e =>
                e.UnidadeOrganizadoraId == unidadeId.Value
                || e.UnidadesParticipantes.Any(eu => eu.UnidadeId == unidadeId.Value));
        }

        var items = await query.OrderBy(e => e.DataHoraInicioUtc).ToListAsync(ct);
        return items.Select(ToEventoDto).ToList();
    }

    public async Task<IReadOnlyList<EventoDto>> ListarEventosElegiveisAsync(
        AuthenticatedUserInfo user,
        int? unidadeId = null,
        CancellationToken ct = default)
    {
        var query = _db.Eventos
            .AsNoTracking()
            .Include(e => e.UnidadeOrganizadora)
            .Include(e => e.UnidadesParticipantes)
                .ThenInclude(eu => eu.Unidade)
            .Include(e => e.OcupacoesEspaco)
                .ThenInclude(o => o.Espaco)
            .AsQueryable();

        if (user.Role == RoleAdministrativa.SuperAdmin)
        {
            if (unidadeId.HasValue)
            {
                query = query.Where(e =>
                    e.UnidadeOrganizadoraId == unidadeId.Value
                    || e.UnidadesParticipantes.Any(eu => eu.UnidadeId == unidadeId.Value));
            }
        }
        else if (user.Role is RoleAdministrativa.Admin or RoleAdministrativa.Tesoureiro)
        {
            var unid = user.UnidadeAdministrativaId!.Value;
            query = query.Where(e =>
                e.PublicoAlvo == PublicoAlvoEvento.Publico
                || e.UnidadeOrganizadoraId == unid
                || e.UnidadesParticipantes.Any(eu => eu.UnidadeId == unid));
        }
        else if (user.CategoriaCodigo == CodigosCategoriaAssociado.Simpatizante && !user.EhDependenteAtivo)
        {
            // Regra 5.2 e 8.1: Simpatizantes consultam somente eventos abertos ao público
            query = query.Where(e => e.PublicoAlvo == PublicoAlvoEvento.Publico && e.Status == StatusEvento.Programado);
        }
        else if (user.EhDependenteAtivo && user.UnidadeTitularId.HasValue)
        {
            // Regra 5.3 e 8.1: Dependente consulta eventos públicos e eventos restritos da unidade do titular
            var unidTitular = user.UnidadeTitularId.Value;
            query = query.Where(e =>
                e.Status == StatusEvento.Programado
                && (e.PublicoAlvo == PublicoAlvoEvento.Publico
                    || e.UnidadeOrganizadoraId == unidTitular
                    || e.UnidadesParticipantes.Any(eu => eu.UnidadeId == unidTitular)));
        }
        else if (user.UnidadeAssociadoId.HasValue)
        {
            var unidAssoc = user.UnidadeAssociadoId.Value;
            query = query.Where(e =>
                e.Status == StatusEvento.Programado
                && (e.PublicoAlvo == PublicoAlvoEvento.Publico
                    || e.UnidadeOrganizadoraId == unidAssoc
                    || e.UnidadesParticipantes.Any(eu => eu.UnidadeId == unidAssoc)));
        }
        else
        {
            query = query.Where(e => e.PublicoAlvo == PublicoAlvoEvento.Publico && e.Status == StatusEvento.Programado);
        }

        var items = await query.OrderBy(e => e.DataHoraInicioUtc).ToListAsync(ct);
        return items.Select(ToEventoDto).ToList();
    }

    public async Task<EventoDto> CriarEventoAsync(
        AuthenticatedUserInfo user,
        CriarEventoRequest req,
        CancellationToken ct = default)
    {
        _currentUser.EnsureAdminOrSuperAdminForUnit(user, req.UnidadeOrganizadoraId);

        if (string.IsNullOrWhiteSpace(req.Nome))
            throw new ApiException(StatusCodes.Status400BadRequest, CodigosErro.DadosInvalidos, "O nome do evento é obrigatório.");

        var inicioUtc = req.Inicio.UtcDateTime;
        var fimUtc = req.Fim.UtcDateTime;
        if (fimUtc <= inicioUtc)
            throw new ApiException(StatusCodes.Status400BadRequest, CodigosErro.DadosInvalidos, "A data/hora final do evento deve ser posterior ao início.");

        if (req.TaxaParticipacao < 0 || (req.LimiteParticipantes.HasValue && req.LimiteParticipantes.Value <= 0))
            throw new ApiException(StatusCodes.Status400BadRequest, CodigosErro.DadosInvalidos, "Taxa não pode ser negativa e o limite de participantes, quando informado, deve ser positivo.");

        var unidadesIds = new HashSet<int> { req.UnidadeOrganizadoraId };
        if (req.UnidadesCompartilhadasIds is { Count: > 0 })
        {
            foreach (var uId in req.UnidadesCompartilhadasIds)
                unidadesIds.Add(uId);
        }

        // Regra D6: Somente o Super Admin define compartilhamento de eventos entre unidades
        if (unidadesIds.Any(id => id != req.UnidadeOrganizadoraId) && user.Role != RoleAdministrativa.SuperAdmin)
        {
            throw new ApiException(StatusCodes.Status403Forbidden, CodigosErro.UnidadeSemPermissao,
                "Somente o Super Admin pode compartilhar eventos entre diferentes unidades.");
        }

        var espacosIds = req.EspacosIds?.Distinct().OrderBy(x => x).ToList() ?? new List<int>();
        var acquiredLocks = new List<SemaphoreSlim>();

        try
        {
            foreach (var espId in espacosIds)
            {
                var sem = EspacoLocks.GetOrAdd(espId, _ => new SemaphoreSlim(1, 1));
                await sem.WaitAsync(ct);
                acquiredLocks.Add(sem);
            }

            await using var tx = await _db.Database.BeginTransactionAsync(ct);

            foreach (var espId in espacosIds)
            {
                await LockEspacoRowIfMySqlAsync(espId, ct);
                var espaco = await _db.Espacos.FirstOrDefaultAsync(e => e.Id == espId && e.Ativo, ct)
                    ?? throw new ApiException(StatusCodes.Status404NotFound, CodigosErro.RecursoNaoEncontrado, $"Espaço #{espId} não encontrado ou inativo.");

                var conflito = await _db.OcupacoesEspaco
                    .AnyAsync(o => o.EspacoId == espId && o.Ativa && o.InicioUtc < fimUtc && o.FimUtc > inicioUtc, ct);
                if (conflito)
                    throw new ApiException(StatusCodes.Status409Conflict, CodigosErro.ReservaHorarioOcupado, $"O espaço '{espaco.Nome}' já possui ocupação no período selecionado.");
            }

            var evento = new Evento
            {
                UnidadeOrganizadoraId = req.UnidadeOrganizadoraId,
                Nome = req.Nome.Trim(),
                Descricao = req.Descricao?.Trim(),
                PublicoAlvo = req.PublicoAlvo,
                DataHoraInicioUtc = inicioUtc,
                DataHoraFimUtc = fimUtc,
                LimiteParticipantes = req.LimiteParticipantes,
                TaxaParticipacao = Math.Round(req.TaxaParticipacao, 2),
                Status = StatusEvento.Programado,
                CriadoPorUsuarioId = user.UsuarioId,
                CriadoEmUtc = DateTime.UtcNow
            };

            _db.Eventos.Add(evento);
            await _db.SaveChangesAsync(ct);

            foreach (var uId in unidadesIds)
            {
                _db.EventosUnidades.Add(new EventoUnidade
                {
                    EventoId = evento.Id,
                    UnidadeId = uId
                });
            }

            foreach (var espId in espacosIds)
            {
                _db.OcupacoesEspaco.Add(new OcupacaoEspaco
                {
                    EspacoId = espId,
                    InicioUtc = inicioUtc,
                    FimUtc = fimUtc,
                    Origem = OrigemOcupacaoEspaco.Evento,
                    EventoId = evento.Id,
                    Ativa = true,
                    CriadaPorUsuarioId = user.UsuarioId
                });
            }

            _auditoria.Registrar(user.UsuarioId, req.UnidadeOrganizadoraId, "CRIAR_EVENTO", nameof(Evento), evento.Id,
                $"Evento '{evento.Nome}' criado ({evento.PublicoAlvo}).");

            await _db.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);

            var completo = await _db.Eventos
                .AsNoTracking()
                .Include(e => e.UnidadeOrganizadora)
                .Include(e => e.UnidadesParticipantes)
                    .ThenInclude(eu => eu.Unidade)
                .Include(e => e.OcupacoesEspaco)
                    .ThenInclude(o => o.Espaco)
                .FirstAsync(e => e.Id == evento.Id, ct);

            return ToEventoDto(completo);
        }
        finally
        {
            foreach (var sem in acquiredLocks)
                sem.Release();
        }
    }

    public async Task<PagamentoEventoDto> RegistrarPagamentoEventoAsync(
        AuthenticatedUserInfo user,
        int eventoId,
        RegistrarPagamentoEventoRequest req,
        CancellationToken ct = default)
    {
        var hoje = TimeZoneHelper.TodaySaoPaulo();
        if (req.DataPagamento > hoje)
            throw new ApiException(StatusCodes.Status422UnprocessableEntity, CodigosErro.DataPagamentoFutura, "A data de pagamento não pode ser futura.");

        if (req.ValorPago < 0)
            throw new ApiException(StatusCodes.Status400BadRequest, CodigosErro.DadosInvalidos, "Valor pago não pode ser negativo.");

        if (!req.PessoaPagadoraId.HasValue && string.IsNullOrWhiteSpace(req.NomePagadorInformado))
            throw new ApiException(StatusCodes.Status400BadRequest, CodigosErro.DadosInvalidos, "Informe a pessoa cadastrada ou o nome do pagador visitante.");

        var evento = await _db.Eventos.FirstOrDefaultAsync(e => e.Id == eventoId, ct)
            ?? throw new ApiException(StatusCodes.Status404NotFound, CodigosErro.RecursoNaoEncontrado, "Evento não encontrado.");

        _currentUser.EnsureFinancialAccessForUnit(user, evento.UnidadeOrganizadoraId);

        Pessoa? pessoa = null;
        if (req.PessoaPagadoraId.HasValue)
        {
            pessoa = await _db.Pessoas.FirstOrDefaultAsync(p => p.Id == req.PessoaPagadoraId.Value, ct)
                ?? throw new ApiException(StatusCodes.Status404NotFound, CodigosErro.RecursoNaoEncontrado, "Pessoa pagadora não encontrada.");
        }

        var pagamento = new PagamentoEvento
        {
            EventoId = evento.Id,
            PessoaPagadoraId = pessoa?.Id,
            NomePagadorInformado = req.NomePagadorInformado?.Trim(),
            ValorPago = Math.Round(req.ValorPago, 2),
            FormaPagamento = req.FormaPagamento,
            DataPagamento = req.DataPagamento,
            RegistradoPorUsuarioId = user.UsuarioId,
            RegistradoEmUtc = DateTime.UtcNow
        };

        _db.PagamentosEvento.Add(pagamento);
        _auditoria.Registrar(user.UsuarioId, evento.UnidadeOrganizadoraId, "REGISTRAR_PAGAMENTO_EVENTO", nameof(PagamentoEvento), evento.Id,
            $"Taxa de evento #{evento.Id} recebida: R$ {pagamento.ValorPago:N2} ({req.FormaPagamento}).");

        await _db.SaveChangesAsync(ct);

        return new PagamentoEventoDto(
            pagamento.Id,
            evento.Id,
            evento.Nome,
            pagamento.PessoaPagadoraId,
            pessoa?.NomeCompleto ?? pagamento.NomePagadorInformado ?? "Visitante",
            pagamento.ValorPago,
            pagamento.FormaPagamento,
            pagamento.DataPagamento,
            TimeZoneHelper.ToSaoPauloOffset(pagamento.RegistradoEmUtc));
    }

    public async Task<IReadOnlyList<PagamentoEventoDto>> ListarPagamentosEventoAsync(
        AuthenticatedUserInfo user,
        int eventoId,
        CancellationToken ct = default)
    {
        var evento = await _db.Eventos.AsNoTracking().FirstOrDefaultAsync(e => e.Id == eventoId, ct)
            ?? throw new ApiException(StatusCodes.Status404NotFound, CodigosErro.RecursoNaoEncontrado, "Evento não encontrado.");

        _currentUser.EnsureFinancialAccessForUnit(user, evento.UnidadeOrganizadoraId);

        var list = await _db.PagamentosEvento
            .AsNoTracking()
            .Include(p => p.PessoaPagadora)
            .Where(p => p.EventoId == eventoId)
            .OrderByDescending(p => p.DataPagamento)
            .ThenByDescending(p => p.Id)
            .ToListAsync(ct);

        return list.Select(p => new PagamentoEventoDto(
            p.Id,
            evento.Id,
            evento.Nome,
            p.PessoaPagadoraId,
            p.PessoaPagadora?.NomeCompleto ?? p.NomePagadorInformado ?? "Visitante",
            p.ValorPago,
            p.FormaPagamento,
            p.DataPagamento,
            TimeZoneHelper.ToSaoPauloOffset(p.RegistradoEmUtc))).ToList();
    }

    // ==================== ESPAÇOS E RESERVAS ====================

    public async Task<IReadOnlyList<EspacoDto>> ListarEspacosAsync(
        AuthenticatedUserInfo user,
        int? unidadeId = null,
        CancellationToken ct = default)
    {
        var query = _db.Espacos
            .AsNoTracking()
            .Include(e => e.Unidade)
            .AsQueryable();

        if (user.Role == RoleAdministrativa.SuperAdmin)
        {
            if (unidadeId.HasValue)
                query = query.Where(e => e.UnidadeId == unidadeId.Value);
        }
        else if (user.Role is RoleAdministrativa.Admin or RoleAdministrativa.Tesoureiro)
        {
            query = query.Where(e => e.UnidadeId == user.UnidadeAdministrativaId);
        }
        else if (user.UnidadeAssociadoId.HasValue)
        {
            query = query.Where(e => e.UnidadeId == user.UnidadeAssociadoId.Value && e.Ativo);
        }
        else
        {
            return Array.Empty<EspacoDto>();
        }

        var items = await query.OrderBy(e => e.Unidade.Nome).ThenBy(e => e.Nome).ToListAsync(ct);
        return items.Select(e => new EspacoDto(
            e.Id, e.UnidadeId, e.Unidade.Nome, e.Nome, e.Descricao, e.Ativo,
            e.HoraAbertura, e.HoraFechamento, e.DuracaoMaximaMinutos, e.AntecedenciaMaximaDias)).ToList();
    }

    public async Task<EspacoDto> CriarEspacoAsync(
        AuthenticatedUserInfo user,
        CriarOuAtualizarEspacoRequest req,
        CancellationToken ct = default)
    {
        _currentUser.EnsureAdminOrSuperAdminForUnit(user, req.UnidadeId);

        if (string.IsNullOrWhiteSpace(req.Nome) || req.DuracaoMaximaMinutos < 60 || req.AntecedenciaMaximaDias < 1 || req.HoraFechamento <= req.HoraAbertura)
            throw new ApiException(StatusCodes.Status400BadRequest, CodigosErro.DadosInvalidos, "Dados do espaço inválidos (duração mínima 60 min e fechamento após abertura).");

        var unidade = await _db.Unidades.FirstOrDefaultAsync(u => u.Id == req.UnidadeId, ct)
            ?? throw new ApiException(StatusCodes.Status404NotFound, CodigosErro.RecursoNaoEncontrado, "Unidade não encontrada.");

        var espaco = new Espaco
        {
            UnidadeId = req.UnidadeId,
            Nome = req.Nome.Trim(),
            Descricao = req.Descricao?.Trim(),
            Ativo = req.Ativo,
            HoraAbertura = req.HoraAbertura,
            HoraFechamento = req.HoraFechamento,
            DuracaoMaximaMinutos = req.DuracaoMaximaMinutos,
            AntecedenciaMaximaDias = req.AntecedenciaMaximaDias
        };

        _db.Espacos.Add(espaco);
        _auditoria.Registrar(user.UsuarioId, req.UnidadeId, "CRIAR_ESPACO", nameof(Espaco), null, $"Espaço '{espaco.Nome}' cadastrado.");
        await _db.SaveChangesAsync(ct);

        return new EspacoDto(
            espaco.Id, espaco.UnidadeId, unidade.Nome, espaco.Nome, espaco.Descricao, espaco.Ativo,
            espaco.HoraAbertura, espaco.HoraFechamento, espaco.DuracaoMaximaMinutos, espaco.AntecedenciaMaximaDias);
    }

    public async Task<OcupacaoEspacoDto> CriarBloqueioManutencaoAsync(
        AuthenticatedUserInfo user,
        CriarBloqueioManutencaoRequest req,
        CancellationToken ct = default)
    {
        var inicioUtc = req.Inicio.UtcDateTime;
        var fimUtc = req.Fim.UtcDateTime;
        if (fimUtc <= inicioUtc || string.IsNullOrWhiteSpace(req.DescricaoBloqueio))
            throw new ApiException(StatusCodes.Status400BadRequest, CodigosErro.DadosInvalidos, "Período e descrição do bloqueio são obrigatórios.");

        var sem = EspacoLocks.GetOrAdd(req.EspacoId, _ => new SemaphoreSlim(1, 1));
        await sem.WaitAsync(ct);
        try
        {
            await using var tx = await _db.Database.BeginTransactionAsync(ct);
            await LockEspacoRowIfMySqlAsync(req.EspacoId, ct);

            var espaco = await _db.Espacos.FirstOrDefaultAsync(e => e.Id == req.EspacoId, ct)
                ?? throw new ApiException(StatusCodes.Status404NotFound, CodigosErro.RecursoNaoEncontrado, "Espaço não encontrado.");

            _currentUser.EnsureAdminOrSuperAdminForUnit(user, espaco.UnidadeId);

            var conflito = await _db.OcupacoesEspaco
                .AnyAsync(o => o.EspacoId == espaco.Id && o.Ativa && o.InicioUtc < fimUtc && o.FimUtc > inicioUtc, ct);
            if (conflito)
                throw new ApiException(StatusCodes.Status409Conflict, CodigosErro.ReservaHorarioOcupado, "Já existe ocupação ativa neste espaço durante o período informado.");

            var ocupacao = new OcupacaoEspaco
            {
                EspacoId = espaco.Id,
                InicioUtc = inicioUtc,
                FimUtc = fimUtc,
                Origem = OrigemOcupacaoEspaco.Manutencao,
                DescricaoBloqueio = req.DescricaoBloqueio.Trim(),
                Ativa = true,
                CriadaPorUsuarioId = user.UsuarioId
            };

            _db.OcupacoesEspaco.Add(ocupacao);
            _auditoria.Registrar(user.UsuarioId, espaco.UnidadeId, "BLOQUEIO_MANUTENCAO_ESPACO", nameof(Espaco), espaco.Id,
                $"Bloqueio de manutenção no espaço '{espaco.Nome}'.");
            await _db.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);

            return new OcupacaoEspacoDto(
                ocupacao.Id, espaco.Id, espaco.Nome,
                TimeZoneHelper.ToSaoPauloOffset(ocupacao.InicioUtc),
                TimeZoneHelper.ToSaoPauloOffset(ocupacao.FimUtc),
                ocupacao.Origem, ocupacao.DescricaoBloqueio ?? "Manutenção", ocupacao.Ativa);
        }
        finally
        {
            sem.Release();
        }
    }

    public async Task<IReadOnlyList<OcupacaoEspacoDto>> ConsultarDisponibilidadeEspacoAsync(
        AuthenticatedUserInfo user,
        int espacoId,
        DateOnly? dataInicio = null,
        DateOnly? dataFim = null,
        CancellationToken ct = default)
    {
        var espaco = await _db.Espacos.AsNoTracking().FirstOrDefaultAsync(e => e.Id == espacoId, ct)
            ?? throw new ApiException(StatusCodes.Status404NotFound, CodigosErro.RecursoNaoEncontrado, "Espaço não encontrado.");

        if (user.Role != RoleAdministrativa.SuperAdmin
            && user.UnidadeAdministrativaId != espaco.UnidadeId
            && user.UnidadeAssociadoId != espaco.UnidadeId)
        {
            throw new ApiException(StatusCodes.Status403Forbidden, CodigosErro.UnidadeSemPermissao, "Você não possui acesso à agenda deste espaço.");
        }

        var query = _db.OcupacoesEspaco
            .AsNoTracking()
            .Include(o => o.Evento)
            .Where(o => o.EspacoId == espacoId && o.Ativa);

        if (dataInicio.HasValue)
        {
            var dtIni = dataInicio.Value.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc).AddHours(-3);
            query = query.Where(o => o.FimUtc >= dtIni);
        }
        if (dataFim.HasValue)
        {
            var dtFim = dataFim.Value.ToDateTime(TimeOnly.MaxValue, DateTimeKind.Utc).AddHours(3);
            query = query.Where(o => o.InicioUtc <= dtFim);
        }

        var list = await query.OrderBy(o => o.InicioUtc).ToListAsync(ct);
        return list.Select(o =>
        {
            var desc = o.Origem switch
            {
                OrigemOcupacaoEspaco.Reserva => "Reservado por associado",
                OrigemOcupacaoEspaco.Evento => o.Evento?.Nome ?? "Evento do clube",
                OrigemOcupacaoEspaco.Manutencao => o.DescricaoBloqueio ?? "Bloqueio de manutenção",
                _ => "Ocupado"
            };
            return new OcupacaoEspacoDto(
                o.Id, espaco.Id, espaco.Nome,
                TimeZoneHelper.ToSaoPauloOffset(o.InicioUtc),
                TimeZoneHelper.ToSaoPauloOffset(o.FimUtc),
                o.Origem, desc, o.Ativa);
        }).ToList();
    }

    public async Task<ReservaEspacoDto> CriarReservaAsync(
        AuthenticatedUserInfo user,
        CriarReservaRequest req,
        CancellationToken ct = default)
    {
        // Regra 9.1, D2 e 5.2: Apenas associado titular pagante (Associado, Fundador, Convidado) pode reservar espaços de sua própria unidade
        if (user.EhDependenteAtivo || !user.AssociadoId.HasValue || user.CategoriaCodigo == CodigosCategoriaAssociado.Simpatizante)
        {
            throw new ApiException(StatusCodes.Status403Forbidden, CodigosErro.RecursoSemPermissao,
                "Apenas associados titulares autorizados podem realizar reservas de espaços.");
        }

        var inicioUtc = req.Inicio.UtcDateTime;
        var fimUtc = req.Fim.UtcDateTime;
        var nowUtc = DateTime.UtcNow;

        if (fimUtc <= inicioUtc)
            throw new ApiException(StatusCodes.Status400BadRequest, CodigosErro.DadosInvalidos, "O horário final da reserva deve ser posterior ao início.");

        if (inicioUtc <= nowUtc)
            throw new ApiException(StatusCodes.Status422UnprocessableEntity, CodigosErro.ReservaForaRegrasEspaco, "Reservas só podem ser criadas para horários futuros.");

        var duracaoMinutos = (fimUtc - inicioUtc).TotalMinutes;
        if (duracaoMinutos < 60)
            throw new ApiException(StatusCodes.Status422UnprocessableEntity, CodigosErro.ReservaForaRegrasEspaco, "A duração mínima de uma reserva é de 60 minutos (1 hora).");

        // Serializa concorrência por EspacoId (em processo + SELECT FOR UPDATE no MySQL)
        var sem = EspacoLocks.GetOrAdd(req.EspacoId, _ => new SemaphoreSlim(1, 1));
        await sem.WaitAsync(ct);
        try
        {
            await using var tx = await _db.Database.BeginTransactionAsync(ct);
            await LockEspacoRowIfMySqlAsync(req.EspacoId, ct);

            var espaco = await _db.Espacos
                .Include(e => e.Unidade)
                .FirstOrDefaultAsync(e => e.Id == req.EspacoId && e.Ativo, ct)
                ?? throw new ApiException(StatusCodes.Status404NotFound, CodigosErro.RecursoNaoEncontrado, "Espaço não encontrado ou inativo.");

            if (espaco.UnidadeId != user.UnidadeAssociadoId)
                throw new ApiException(StatusCodes.Status403Forbidden, CodigosErro.UnidadeSemPermissao, "Você só pode reservar espaços da sua própria unidade.");

            if (duracaoMinutos > espaco.DuracaoMaximaMinutos)
            {
                throw new ApiException(StatusCodes.Status422UnprocessableEntity, CodigosErro.ReservaForaRegrasEspaco,
                    $"A duração máxima permitida para este espaço é de {espaco.DuracaoMaximaMinutos} minutos.");
            }

            var inicioLocal = TimeZoneHelper.ToSaoPauloOffset(inicioUtc);
            var fimLocal = TimeZoneHelper.ToSaoPauloOffset(fimUtc);
            var hojeLocal = TimeZoneHelper.TodaySaoPaulo(nowUtc);

            if (DateOnly.FromDateTime(inicioLocal.DateTime).DayNumber - hojeLocal.DayNumber > espaco.AntecedenciaMaximaDias)
            {
                throw new ApiException(StatusCodes.Status422UnprocessableEntity, CodigosErro.ReservaForaRegrasEspaco,
                    $"A antecedência máxima para reservar este espaço é de {espaco.AntecedenciaMaximaDias} dias.");
            }

            var horaIniLocal = TimeOnly.FromDateTime(inicioLocal.DateTime);
            var horaFimLocal = TimeOnly.FromDateTime(fimLocal.DateTime);
            if (DateOnly.FromDateTime(inicioLocal.DateTime) != DateOnly.FromDateTime(fimLocal.DateTime)
                || horaIniLocal < espaco.HoraAbertura
                || horaFimLocal > espaco.HoraFechamento)
            {
                throw new ApiException(StatusCodes.Status422UnprocessableEntity, CodigosErro.ReservaForaRegrasEspaco,
                    $"O horário da reserva deve estar dentro do funcionamento do espaço ({espaco.HoraAbertura:HH\\:mm} às {espaco.HoraFechamento:HH\\:mm}).");
            }

            // Verifica sobreposição (períodos adjacentes onde FimUtc == inicioUtc ou InicioUtc == fimUtc são permitidos!)
            var conflito = await _db.OcupacoesEspaco
                .AnyAsync(o => o.EspacoId == espaco.Id && o.Ativa && o.InicioUtc < fimUtc && o.FimUtc > inicioUtc, ct);

            if (conflito)
                throw new ApiException(StatusCodes.Status409Conflict, CodigosErro.ReservaHorarioOcupado, "Este espaço já está ocupado no período selecionado.");

            var reserva = new ReservaEspaco
            {
                EspacoId = espaco.Id,
                AssociadoId = user.AssociadoId.Value,
                InicioUtc = inicioUtc,
                FimUtc = fimUtc,
                Status = StatusReserva.Confirmada,
                CriadaEmUtc = nowUtc
            };
            _db.ReservasEspaco.Add(reserva);
            await _db.SaveChangesAsync(ct);

            var ocupacao = new OcupacaoEspaco
            {
                EspacoId = espaco.Id,
                InicioUtc = inicioUtc,
                FimUtc = fimUtc,
                Origem = OrigemOcupacaoEspaco.Reserva,
                ReservaEspacoId = reserva.Id,
                Ativa = true,
                CriadaPorUsuarioId = user.UsuarioId
            };
            _db.OcupacoesEspaco.Add(ocupacao);
            await _db.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);

            var associadoNome = await _db.Associados
                .Where(a => a.Id == reserva.AssociadoId)
                .Select(a => a.Pessoa.NomeCompleto)
                .FirstAsync(ct);

            return new ReservaEspacoDto(
                reserva.Id,
                espaco.Id,
                espaco.Nome,
                espaco.UnidadeId,
                espaco.Unidade.Nome,
                reserva.AssociadoId,
                associadoNome,
                TimeZoneHelper.ToSaoPauloOffset(reserva.InicioUtc),
                TimeZoneHelper.ToSaoPauloOffset(reserva.FimUtc),
                reserva.Status,
                TimeZoneHelper.ToSaoPauloOffset(reserva.CriadaEmUtc),
                null,
                null);
        }
        finally
        {
            sem.Release();
        }
    }

    public async Task<ReservaEspacoDto> CancelarReservaAsync(
        AuthenticatedUserInfo user,
        int reservaId,
        CancelarReservaRequest req,
        CancellationToken ct = default)
    {
        var reservaPrelim = await _db.ReservasEspaco
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.Id == reservaId, ct)
            ?? throw new ApiException(StatusCodes.Status404NotFound, CodigosErro.RecursoNaoEncontrado, "Reserva não encontrada.");

        var sem = EspacoLocks.GetOrAdd(reservaPrelim.EspacoId, _ => new SemaphoreSlim(1, 1));
        await sem.WaitAsync(ct);
        try
        {
            await using var tx = await _db.Database.BeginTransactionAsync(ct);
            await LockEspacoRowIfMySqlAsync(reservaPrelim.EspacoId, ct);

            var reserva = await _db.ReservasEspaco
                .Include(r => r.Espaco)
                    .ThenInclude(e => e.Unidade)
                .Include(r => r.Associado)
                    .ThenInclude(a => a.Pessoa)
                        .ThenInclude(p => p.Usuario)
                .FirstOrDefaultAsync(r => r.Id == reservaId, ct)
                ?? throw new ApiException(StatusCodes.Status404NotFound, CodigosErro.RecursoNaoEncontrado, "Reserva não encontrada.");

            if (reserva.Status == StatusReserva.Cancelada)
                throw new ApiException(StatusCodes.Status422UnprocessableEntity, CodigosErro.DadosInvalidos, "Esta reserva já se encontra cancelada.");

            var nowUtc = DateTime.UtcNow;
            var canceladoPeloProprioTitular = user.AssociadoId.HasValue && user.AssociadoId.Value == reserva.AssociadoId && user.Role == RoleAdministrativa.Nenhuma;

            if (canceladoPeloProprioTitular)
            {
                // Regra 9.2: O associado poderá cancelar até 24 horas antes do início
                if ((reserva.InicioUtc - nowUtc).TotalHours < 24)
                {
                    throw new ApiException(StatusCodes.Status422UnprocessableEntity, CodigosErro.ReservaCancelamentoForaPrazo,
                        "O cancelamento pelo associado só é permitido com no mínimo 24 horas de antecedência.");
                }
                reserva.CanceladaPorUsuarioId = null;
                reserva.JustificativaCancelamento = req.Justificativa?.Trim();
            }
            else
            {
                _currentUser.EnsureAdminOrSuperAdminForUnit(user, reserva.Espaco.UnidadeId);
                if (string.IsNullOrWhiteSpace(req.Justificativa))
                {
                    throw new ApiException(StatusCodes.Status400BadRequest, CodigosErro.DadosInvalidos,
                        "O cancelamento administrativo exige justificativa obrigatória.");
                }

                reserva.CanceladaPorUsuarioId = user.UsuarioId;
                reserva.JustificativaCancelamento = req.Justificativa.Trim();

                if (reserva.Associado.Pessoa.Usuario is not null)
                {
                    await _notificacao.CriarNotificacaoAsync(
                        reserva.Associado.Pessoa.Usuario.Id,
                        TipoNotificacao.ReservaCancelada,
                        "Reserva de espaço cancelada pela administração",
                        $"Sua reserva do espaço '{reserva.Espaco.Nome}' foi cancelada. Motivo: {reserva.JustificativaCancelamento}",
                        "ReservaEspaco",
                        reserva.Id,
                        $"RES_CANC_{reserva.Id}",
                        ct);
                }

                _auditoria.Registrar(user.UsuarioId, reserva.Espaco.UnidadeId, "CANCELAR_RESERVA_ADMIN", nameof(ReservaEspaco), reserva.Id,
                    $"Reserva #{reserva.Id} cancelada administrativamente.");
            }

            reserva.Status = StatusReserva.Cancelada;
            reserva.CanceladaEmUtc = nowUtc;

            var ocupacao = await _db.OcupacoesEspaco
                .FirstOrDefaultAsync(o => o.ReservaEspacoId == reserva.Id, ct);
            if (ocupacao is not null)
                ocupacao.Ativa = false;

            await _db.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);

            return new ReservaEspacoDto(
                reserva.Id,
                reserva.EspacoId,
                reserva.Espaco.Nome,
                reserva.Espaco.UnidadeId,
                reserva.Espaco.Unidade.Nome,
                reserva.AssociadoId,
                reserva.Associado.Pessoa.NomeCompleto,
                TimeZoneHelper.ToSaoPauloOffset(reserva.InicioUtc),
                TimeZoneHelper.ToSaoPauloOffset(reserva.FimUtc),
                reserva.Status,
                TimeZoneHelper.ToSaoPauloOffset(reserva.CriadaEmUtc),
                TimeZoneHelper.ToSaoPauloOffset(nowUtc),
                reserva.JustificativaCancelamento);
        }
        finally
        {
            sem.Release();
        }
    }

    public async Task<IReadOnlyList<ReservaEspacoDto>> ListarReservasAsync(
        AuthenticatedUserInfo user,
        int? unidadeId = null,
        CancellationToken ct = default)
    {
        var query = _db.ReservasEspaco
            .AsNoTracking()
            .Include(r => r.Espaco)
                .ThenInclude(e => e.Unidade)
            .Include(r => r.Associado)
                .ThenInclude(a => a.Pessoa)
            .AsQueryable();

        if (user.Role == RoleAdministrativa.SuperAdmin)
        {
            if (unidadeId.HasValue)
                query = query.Where(r => r.Espaco.UnidadeId == unidadeId.Value);
        }
        else if (user.Role == RoleAdministrativa.Admin)
        {
            query = query.Where(r => r.Espaco.UnidadeId == user.UnidadeAdministrativaId);
        }
        else if (user.AssociadoId.HasValue && !user.EhDependenteAtivo)
        {
            query = query.Where(r => r.AssociadoId == user.AssociadoId.Value);
        }
        else
        {
            throw new ApiException(StatusCodes.Status403Forbidden, CodigosErro.RecursoSemPermissao, "Você não possui acesso a reservas.");
        }

        var items = await query.OrderByDescending(r => r.InicioUtc).ToListAsync(ct);
        return items.Select(r => new ReservaEspacoDto(
            r.Id,
            r.EspacoId,
            r.Espaco.Nome,
            r.Espaco.UnidadeId,
            r.Espaco.Unidade.Nome,
            r.AssociadoId,
            r.Associado.Pessoa.NomeCompleto,
            TimeZoneHelper.ToSaoPauloOffset(r.InicioUtc),
            TimeZoneHelper.ToSaoPauloOffset(r.FimUtc),
            r.Status,
            TimeZoneHelper.ToSaoPauloOffset(r.CriadaEmUtc),
            r.CanceladaEmUtc.HasValue ? TimeZoneHelper.ToSaoPauloOffset(r.CanceladaEmUtc.Value) : null,
            r.JustificativaCancelamento)).ToList();
    }

    private async Task LockEspacoRowIfMySqlAsync(int espacoId, CancellationToken ct)
    {
        if (_db.Database.ProviderName?.Contains("MySql", StringComparison.OrdinalIgnoreCase) == true)
        {
            await _db.Database.ExecuteSqlInterpolatedAsync($"SELECT Id FROM Espaco WHERE Id = {espacoId} FOR UPDATE", ct);
        }
    }

    public static EventoDto ToEventoDto(Evento e) => new(
        Id: e.Id,
        UnidadeOrganizadoraId: e.UnidadeOrganizadoraId,
        UnidadeOrganizadoraNome: e.UnidadeOrganizadora.Nome,
        Nome: e.Nome,
        Descricao: e.Descricao,
        PublicoAlvo: e.PublicoAlvo,
        Inicio: TimeZoneHelper.ToSaoPauloOffset(e.DataHoraInicioUtc),
        Fim: TimeZoneHelper.ToSaoPauloOffset(e.DataHoraFimUtc),
        LimiteParticipantes: e.LimiteParticipantes,
        TaxaParticipacao: e.TaxaParticipacao,
        Status: e.Status,
        UnidadesParticipantesIds: e.UnidadesParticipantes.Select(u => u.UnidadeId).ToList(),
        UnidadesParticipantesNomes: e.UnidadesParticipantes.Select(u => u.Unidade.Nome).OrderBy(n => n).ToList(),
        EspacosOcupadosIds: e.OcupacoesEspaco.Where(o => o.Ativa).Select(o => o.EspacoId).ToList(),
        EspacosOcupadosNomes: e.OcupacoesEspaco.Where(o => o.Ativa).Select(o => o.Espaco.Nome).ToList());
}
