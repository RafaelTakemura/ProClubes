using System.Collections.Concurrent;
using Microsoft.EntityFrameworkCore;
using ProClubesApi.Authorization;
using ProClubesApi.Contracts;
using ProClubesApi.Data;
using ProClubesApi.Entities;

namespace ProClubesApi.Services;

public class EventoEspacoReservaService
{
    private const long TamanhoMaximoFotoBytes = 5 * 1024 * 1024;
    private static readonly TimeSpan OffsetSaoPaulo = TimeSpan.FromHours(-3);
    private static readonly ConcurrentDictionary<int, SemaphoreSlim> EspacoLocks = new();
    private static readonly Dictionary<string, string> ExtensoesPermitidas = new(StringComparer.OrdinalIgnoreCase)
    {
        ["image/jpeg"] = ".jpg",
        ["image/jpg"] = ".jpg",
        ["image/png"] = ".png",
        ["image/webp"] = ".webp"
    };

    private readonly ProClubesDbContext _db;
    private readonly CurrentUserService _currentUser;
    private readonly AuditoriaService _auditoria;
    private readonly NotificacaoService _notificacao;
    private readonly IWebHostEnvironment _env;

    public EventoEspacoReservaService(
        ProClubesDbContext db,
        CurrentUserService currentUser,
        AuditoriaService auditoria,
        NotificacaoService notificacao,
        IWebHostEnvironment env)
    {
        _db = db;
        _currentUser = currentUser;
        _auditoria = auditoria;
        _notificacao = notificacao;
        _env = env;
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
            .Include(e => e.Fotos)
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
            .Include(e => e.Fotos)
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
            query = query.Where(e => e.PublicoAlvo == PublicoAlvoEvento.Publico && e.Status == StatusEvento.Programado);
        }
        else if (user.EhDependenteAtivo && user.UnidadeTitularId.HasValue)
        {
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

        if (req.Fim <= req.Inicio)
            throw new ApiException(StatusCodes.Status400BadRequest, CodigosErro.DadosInvalidos, "A data/hora final do evento deve ser posterior ao início.");

        if (req.TaxaParticipacao < 0 || (req.LimiteParticipantes.HasValue && req.LimiteParticipantes.Value <= 0))
            throw new ApiException(StatusCodes.Status400BadRequest, CodigosErro.DadosInvalidos, "Taxa não pode ser negativa e o limite de participantes, quando informado, deve ser positivo.");

        var espacosIds = req.EspacosIds?.Distinct().OrderBy(x => x).ToList() ?? new List<int>();
        if (espacosIds.Count == 0)
            throw new ApiException(StatusCodes.Status400BadRequest, CodigosErro.DadosInvalidos, "Selecione ao menos um espaço disponível da unidade para realizar o evento.");

        var unidadesIds = new HashSet<int> { req.UnidadeOrganizadoraId };
        if (req.UnidadesCompartilhadasIds is { Count: > 0 })
        {
            foreach (var uId in req.UnidadesCompartilhadasIds)
                unidadesIds.Add(uId);
        }

        if (unidadesIds.Any(id => id != req.UnidadeOrganizadoraId) && user.Role != RoleAdministrativa.SuperAdmin)
        {
            throw new ApiException(StatusCodes.Status403Forbidden, CodigosErro.UnidadeSemPermissao,
                "Somente o Super Admin pode compartilhar eventos entre diferentes unidades.");
        }

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
            var nowUtc = DateTime.UtcNow;
            var ocupacoesPlanejadas = new List<(int EspacoId, DateTime InicioUtc, DateTime FimUtc)>();
            var valorReservaTotal = 0m;

            foreach (var espId in espacosIds)
            {
                await LockEspacoRowIfMySqlAsync(espId, ct);
                var espaco = await _db.Espacos.FirstOrDefaultAsync(e => e.Id == espId && e.Ativo, ct)
                    ?? throw new ApiException(StatusCodes.Status404NotFound, CodigosErro.RecursoNaoEncontrado, $"Espaço #{espId} não encontrado ou inativo.");

                if (espaco.UnidadeId != req.UnidadeOrganizadoraId)
                    throw new ApiException(StatusCodes.Status422UnprocessableEntity, CodigosErro.DadosInvalidos, $"O espaço '{espaco.Nome}' não pertence à unidade organizadora do evento.");

                var periodo = NormalizarEValidarPeriodo(espaco, req.Inicio, req.Fim, aplicarRegrasReservaParticular: false, nowUtc);

                var conflito = await _db.OcupacoesEspaco
                    .AnyAsync(o => o.EspacoId == espId && o.Ativa && o.InicioUtc < periodo.FimUtc && o.FimUtc > periodo.InicioUtc, ct);
                if (conflito)
                    throw new ApiException(StatusCodes.Status409Conflict, CodigosErro.ReservaHorarioOcupado, $"O espaço '{espaco.Nome}' já possui ocupação na agenda para o período selecionado.");

                ocupacoesPlanejadas.Add((espId, periodo.InicioUtc, periodo.FimUtc));
                valorReservaTotal += periodo.ValorTotal;
            }

            var eventoInicioUtc = ocupacoesPlanejadas.Min(o => o.InicioUtc);
            var eventoFimUtc = ocupacoesPlanejadas.Max(o => o.FimUtc);

            var evento = new Evento
            {
                UnidadeOrganizadoraId = req.UnidadeOrganizadoraId,
                Nome = req.Nome.Trim(),
                Descricao = req.Descricao?.Trim(),
                PublicoAlvo = req.PublicoAlvo,
                DataHoraInicioUtc = eventoInicioUtc,
                DataHoraFimUtc = eventoFimUtc,
                LimiteParticipantes = req.LimiteParticipantes,
                TaxaParticipacao = Math.Round(req.TaxaParticipacao, 2),
                ValorReservaEspaco = Math.Round(valorReservaTotal, 2),
                Status = StatusEvento.Programado,
                CriadoPorUsuarioId = user.UsuarioId,
                CriadoEmUtc = nowUtc
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

            foreach (var oc in ocupacoesPlanejadas)
            {
                _db.OcupacoesEspaco.Add(new OcupacaoEspaco
                {
                    EspacoId = oc.EspacoId,
                    InicioUtc = oc.InicioUtc,
                    FimUtc = oc.FimUtc,
                    Origem = OrigemOcupacaoEspaco.Evento,
                    EventoId = evento.Id,
                    Ativa = true,
                    CriadaPorUsuarioId = user.UsuarioId
                });
            }

            _auditoria.Registrar(user.UsuarioId, req.UnidadeOrganizadoraId, "CRIAR_EVENTO", nameof(Evento), evento.Id,
                $"Evento '{evento.Nome}' criado ({evento.PublicoAlvo}) vinculando {espacosIds.Count} espaço(s).");

            await _db.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);

            var completo = await _db.Eventos
                .AsNoTracking()
                .Include(e => e.UnidadeOrganizadora)
                .Include(e => e.UnidadesParticipantes)
                    .ThenInclude(eu => eu.Unidade)
                .Include(e => e.OcupacoesEspaco)
                    .ThenInclude(o => o.Espaco)
                .Include(e => e.Fotos)
                .FirstAsync(e => e.Id == evento.Id, ct);

            return ToEventoDto(completo);
        }
        finally
        {
            foreach (var sem in acquiredLocks)
                sem.Release();
        }
    }

    public async Task<FotoItemDto> AdicionarFotoEventoAsync(
        AuthenticatedUserInfo user,
        int eventoId,
        UploadFotoRequest req,
        CancellationToken ct = default)
    {
        var evento = await _db.Eventos
            .Include(e => e.Fotos)
            .FirstOrDefaultAsync(e => e.Id == eventoId, ct)
            ?? throw new ApiException(StatusCodes.Status404NotFound, CodigosErro.RecursoNaoEncontrado, "Evento não encontrado.");

        _currentUser.EnsureAdminOrSuperAdminForUnit(user, evento.UnidadeOrganizadoraId);

        var (nomeArquivo, contentType, caminhoRelativo) = await SalvarFotoBase64Async("eventos", req, ct);
        var ordem = evento.Fotos.Count == 0 ? 1 : evento.Fotos.Max(f => f.Ordem) + 1;

        var foto = new FotoEvento
        {
            EventoId = evento.Id,
            NomeArquivo = nomeArquivo,
            ContentType = contentType,
            CaminhoRelativo = caminhoRelativo,
            Ordem = ordem,
            CriadaEmUtc = DateTime.UtcNow,
            CriadaPorUsuarioId = user.UsuarioId
        };
        _db.FotosEvento.Add(foto);
        await _db.SaveChangesAsync(ct);

        _auditoria.Registrar(user.UsuarioId, evento.UnidadeOrganizadoraId, "ADICIONAR_FOTO_EVENTO", nameof(Evento), evento.Id,
            $"Foto '{nomeArquivo}' adicionada ao evento #{evento.Id}.");
        await _db.SaveChangesAsync(ct);

        return ToFotoEventoDto(foto);
    }

    public async Task RemoverFotoEventoAsync(
        AuthenticatedUserInfo user,
        int eventoId,
        int fotoId,
        CancellationToken ct = default)
    {
        var foto = await _db.FotosEvento
            .Include(f => f.Evento)
            .FirstOrDefaultAsync(f => f.Id == fotoId && f.EventoId == eventoId, ct)
            ?? throw new ApiException(StatusCodes.Status404NotFound, CodigosErro.RecursoNaoEncontrado, "Foto do evento não encontrada.");

        _currentUser.EnsureAdminOrSuperAdminForUnit(user, foto.Evento.UnidadeOrganizadoraId);

        var caminho = foto.CaminhoRelativo;
        _db.FotosEvento.Remove(foto);
        _auditoria.Registrar(user.UsuarioId, foto.Evento.UnidadeOrganizadoraId, "REMOVER_FOTO_EVENTO", nameof(Evento), eventoId,
            $"Foto #{fotoId} removida do evento #{eventoId}.");
        await _db.SaveChangesAsync(ct);
        ExcluirArquivoSeguro(caminho);
    }

    public async Task<(string CaminhoAbsoluto, string ContentType)> ObterArquivoFotoEventoAsync(int fotoId, CancellationToken ct = default)
    {
        var foto = await _db.FotosEvento.AsNoTracking().FirstOrDefaultAsync(f => f.Id == fotoId, ct)
            ?? throw new ApiException(StatusCodes.Status404NotFound, CodigosErro.RecursoNaoEncontrado, "Foto não encontrada.");
        return ResolverCaminhoFotoExistente(foto.CaminhoRelativo, foto.ContentType);
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
            .Include(e => e.Fotos)
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
        return items.Select(ToEspacoDto).ToList();
    }

    public async Task<IReadOnlyList<EspacoDto>> ListarEspacosDisponiveisAsync(
        AuthenticatedUserInfo user,
        int unidadeId,
        DateTimeOffset inicio,
        DateTimeOffset fim,
        CancellationToken ct = default)
    {
        if (fim <= inicio)
            throw new ApiException(StatusCodes.Status400BadRequest, CodigosErro.DadosInvalidos, "A data/hora final deve ser posterior ao início.");

        EnsureAcessoUnidadeEspaco(user, unidadeId);

        var espacosAtivos = await _db.Espacos
            .AsNoTracking()
            .Include(e => e.Unidade)
            .Include(e => e.Fotos)
            .Where(e => e.UnidadeId == unidadeId && e.Ativo)
            .OrderBy(e => e.Nome)
            .ToListAsync(ct);

        if (espacosAtivos.Count == 0)
            return Array.Empty<EspacoDto>();

        var espacosIds = espacosAtivos.Select(e => e.Id).ToList();
        var janelaInicioUtc = inicio.UtcDateTime.AddDays(-2);
        var janelaFimUtc = fim.UtcDateTime.AddDays(2);

        var ocupacoesJanela = await _db.OcupacoesEspaco
            .AsNoTracking()
            .Where(o => espacosIds.Contains(o.EspacoId) && o.Ativa && o.InicioUtc < janelaFimUtc && o.FimUtc > janelaInicioUtc)
            .ToListAsync(ct);

        var nowUtc = DateTime.UtcNow;
        var disponiveis = new List<EspacoDto>();

        foreach (var espaco in espacosAtivos)
        {
            try
            {
                var periodo = NormalizarEValidarPeriodo(espaco, inicio, fim, aplicarRegrasReservaParticular: false, nowUtc);
                var ocupado = ocupacoesJanela.Any(o => o.EspacoId == espaco.Id && o.InicioUtc < periodo.FimUtc && o.FimUtc > periodo.InicioUtc);
                if (!ocupado)
                    disponiveis.Add(ToEspacoDto(espaco));
            }
            catch (ApiException)
            {
                // Espaço incompatível com o período informado (ex.: período multi-dia para espaço PorHora) não aparece como disponível
            }
        }

        return disponiveis;
    }

    public async Task<EspacoDto> CriarEspacoAsync(
        AuthenticatedUserInfo user,
        CriarOuAtualizarEspacoRequest req,
        CancellationToken ct = default)
    {
        _currentUser.EnsureAdminOrSuperAdminForUnit(user, req.UnidadeId);
        ValidarRequestEspaco(req);

        var unidade = await _db.Unidades.FirstOrDefaultAsync(u => u.Id == req.UnidadeId, ct)
            ?? throw new ApiException(StatusCodes.Status404NotFound, CodigosErro.RecursoNaoEncontrado, "Unidade não encontrada.");

        var duracaoMax = req.TipoLocacao == TipoLocacaoEspaco.PorDia && req.DuracaoMaximaMinutos < 60
            ? 1440
            : req.DuracaoMaximaMinutos;

        var espaco = new Espaco
        {
            UnidadeId = req.UnidadeId,
            Nome = req.Nome.Trim(),
            Descricao = req.Descricao?.Trim(),
            Ativo = req.Ativo,
            HoraAbertura = req.HoraAbertura,
            HoraFechamento = req.HoraFechamento,
            DuracaoMaximaMinutos = duracaoMax,
            AntecedenciaMaximaDias = req.AntecedenciaMaximaDias,
            TipoLocacao = req.TipoLocacao,
            ValorReserva = Math.Round(req.ValorReserva, 2)
        };

        _db.Espacos.Add(espaco);
        await _db.SaveChangesAsync(ct);

        _auditoria.Registrar(user.UsuarioId, req.UnidadeId, "CRIAR_ESPACO", nameof(Espaco), espaco.Id,
            $"Espaço '{espaco.Nome}' cadastrado ({espaco.TipoLocacao}, R$ {espaco.ValorReserva:N2}).");
        await _db.SaveChangesAsync(ct);

        espaco.Unidade = unidade;
        return ToEspacoDto(espaco);
    }

    public async Task<EspacoDto> AtualizarEspacoAsync(
        AuthenticatedUserInfo user,
        int espacoId,
        CriarOuAtualizarEspacoRequest req,
        CancellationToken ct = default)
    {
        var espaco = await _db.Espacos
            .Include(e => e.Unidade)
            .Include(e => e.Fotos)
            .FirstOrDefaultAsync(e => e.Id == espacoId, ct)
            ?? throw new ApiException(StatusCodes.Status404NotFound, CodigosErro.RecursoNaoEncontrado, "Espaço não encontrado.");

        _currentUser.EnsureAdminOrSuperAdminForUnit(user, espaco.UnidadeId);
        if (req.UnidadeId != espaco.UnidadeId)
            _currentUser.EnsureAdminOrSuperAdminForUnit(user, req.UnidadeId);

        ValidarRequestEspaco(req);

        var unidade = req.UnidadeId == espaco.UnidadeId
            ? espaco.Unidade
            : await _db.Unidades.FirstOrDefaultAsync(u => u.Id == req.UnidadeId, ct)
                ?? throw new ApiException(StatusCodes.Status404NotFound, CodigosErro.RecursoNaoEncontrado, "Unidade não encontrada.");

        espaco.UnidadeId = unidade.Id;
        espaco.Unidade = unidade;
        espaco.Nome = req.Nome.Trim();
        espaco.Descricao = req.Descricao?.Trim();
        espaco.Ativo = req.Ativo;
        espaco.HoraAbertura = req.HoraAbertura;
        espaco.HoraFechamento = req.HoraFechamento;
        espaco.DuracaoMaximaMinutos = req.TipoLocacao == TipoLocacaoEspaco.PorDia && req.DuracaoMaximaMinutos < 60
            ? 1440
            : req.DuracaoMaximaMinutos;
        espaco.AntecedenciaMaximaDias = req.AntecedenciaMaximaDias;
        espaco.TipoLocacao = req.TipoLocacao;
        espaco.ValorReserva = Math.Round(req.ValorReserva, 2);

        _auditoria.Registrar(user.UsuarioId, espaco.UnidadeId, "ATUALIZAR_ESPACO", nameof(Espaco), espaco.Id,
            $"Espaço '{espaco.Nome}' atualizado ({espaco.TipoLocacao}, R$ {espaco.ValorReserva:N2}).");
        await _db.SaveChangesAsync(ct);

        return ToEspacoDto(espaco);
    }

    public async Task<FotoItemDto> AdicionarFotoEspacoAsync(
        AuthenticatedUserInfo user,
        int espacoId,
        UploadFotoRequest req,
        CancellationToken ct = default)
    {
        var espaco = await _db.Espacos
            .Include(e => e.Fotos)
            .FirstOrDefaultAsync(e => e.Id == espacoId, ct)
            ?? throw new ApiException(StatusCodes.Status404NotFound, CodigosErro.RecursoNaoEncontrado, "Espaço não encontrado.");

        _currentUser.EnsureAdminOrSuperAdminForUnit(user, espaco.UnidadeId);

        var (nomeArquivo, contentType, caminhoRelativo) = await SalvarFotoBase64Async("espacos", req, ct);
        var ordem = espaco.Fotos.Count == 0 ? 1 : espaco.Fotos.Max(f => f.Ordem) + 1;

        var foto = new FotoEspaco
        {
            EspacoId = espaco.Id,
            NomeArquivo = nomeArquivo,
            ContentType = contentType,
            CaminhoRelativo = caminhoRelativo,
            Ordem = ordem,
            CriadaEmUtc = DateTime.UtcNow,
            CriadaPorUsuarioId = user.UsuarioId
        };
        _db.FotosEspaco.Add(foto);
        await _db.SaveChangesAsync(ct);

        _auditoria.Registrar(user.UsuarioId, espaco.UnidadeId, "ADICIONAR_FOTO_ESPACO", nameof(Espaco), espaco.Id,
            $"Foto '{nomeArquivo}' adicionada ao espaço #{espaco.Id}.");
        await _db.SaveChangesAsync(ct);

        return ToFotoEspacoDto(foto);
    }

    public async Task RemoverFotoEspacoAsync(
        AuthenticatedUserInfo user,
        int espacoId,
        int fotoId,
        CancellationToken ct = default)
    {
        var foto = await _db.FotosEspaco
            .Include(f => f.Espaco)
            .FirstOrDefaultAsync(f => f.Id == fotoId && f.EspacoId == espacoId, ct)
            ?? throw new ApiException(StatusCodes.Status404NotFound, CodigosErro.RecursoNaoEncontrado, "Foto do espaço não encontrada.");

        _currentUser.EnsureAdminOrSuperAdminForUnit(user, foto.Espaco.UnidadeId);

        var caminho = foto.CaminhoRelativo;
        _db.FotosEspaco.Remove(foto);
        _auditoria.Registrar(user.UsuarioId, foto.Espaco.UnidadeId, "REMOVER_FOTO_ESPACO", nameof(Espaco), espacoId,
            $"Foto #{fotoId} removida do espaço #{espacoId}.");
        await _db.SaveChangesAsync(ct);
        ExcluirArquivoSeguro(caminho);
    }

    public async Task<(string CaminhoAbsoluto, string ContentType)> ObterArquivoFotoEspacoAsync(int fotoId, CancellationToken ct = default)
    {
        var foto = await _db.FotosEspaco.AsNoTracking().FirstOrDefaultAsync(f => f.Id == fotoId, ct)
            ?? throw new ApiException(StatusCodes.Status404NotFound, CodigosErro.RecursoNaoEncontrado, "Foto não encontrada.");
        return ResolverCaminhoFotoExistente(foto.CaminhoRelativo, foto.ContentType);
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

            if (espaco.TipoLocacao == TipoLocacaoEspaco.PorDia)
            {
                var periodoDia = NormalizarEValidarPeriodo(espaco, req.Inicio, req.Fim, aplicarRegrasReservaParticular: false, DateTime.UtcNow);
                inicioUtc = periodoDia.InicioUtc;
                fimUtc = periodoDia.FimUtc;
            }

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

        EnsureAcessoUnidadeEspaco(user, espaco.UnidadeId);

        return await ConsultarOcupacoesInternoAsync(espaco, dataInicio, dataFim, ct);
    }

    public async Task<AgendaEspacoDto> ConsultarAgendaEspacoAsync(
        AuthenticatedUserInfo user,
        int espacoId,
        DateOnly? dataInicio = null,
        DateOnly? dataFim = null,
        CancellationToken ct = default)
    {
        var espaco = await _db.Espacos.AsNoTracking().FirstOrDefaultAsync(e => e.Id == espacoId, ct)
            ?? throw new ApiException(StatusCodes.Status404NotFound, CodigosErro.RecursoNaoEncontrado, "Espaço não encontrado.");

        EnsureAcessoUnidadeEspaco(user, espaco.UnidadeId);

        var hoje = TimeZoneHelper.TodaySaoPaulo();
        var ini = dataInicio ?? hoje;
        var fim = espaco.TipoLocacao == TipoLocacaoEspaco.PorDia
            ? (dataFim ?? ini.AddDays(29))
            : ini;

        if (fim < ini)
            fim = ini;

        if (espaco.TipoLocacao == TipoLocacaoEspaco.PorDia && fim.DayNumber - ini.DayNumber > 90)
            fim = ini.AddDays(90);

        var ocupacoesDto = await ConsultarOcupacoesInternoAsync(espaco, ini, fim, ct);
        var slots = new List<SlotAgendaDto>();

        if (espaco.TipoLocacao == TipoLocacaoEspaco.PorDia)
        {
            for (var d = ini; d <= fim; d = d.AddDays(1))
            {
                var slotIni = new DateTimeOffset(d.ToDateTime(TimeOnly.MinValue), OffsetSaoPaulo);
                var slotFim = new DateTimeOffset(d.AddDays(1).ToDateTime(TimeOnly.MinValue), OffsetSaoPaulo);
                var conflito = ocupacoesDto.FirstOrDefault(o => o.Inicio < slotFim && o.Fim > slotIni);

                slots.Add(new SlotAgendaDto(
                    slotIni,
                    slotFim,
                    d.ToString("dd/MM/yyyy"),
                    conflito is null,
                    conflito?.Origem,
                    conflito?.DescricaoPublica));
            }
        }
        else
        {
            var horaCursor = espaco.HoraAbertura;
            while (horaCursor.AddHours(1) <= espaco.HoraFechamento && horaCursor.AddHours(1) > horaCursor)
            {
                var horaProx = horaCursor.AddHours(1);
                var slotIni = new DateTimeOffset(ini.ToDateTime(horaCursor), OffsetSaoPaulo);
                var slotFim = new DateTimeOffset(ini.ToDateTime(horaProx), OffsetSaoPaulo);
                var conflito = ocupacoesDto.FirstOrDefault(o => o.Inicio < slotFim && o.Fim > slotIni);

                slots.Add(new SlotAgendaDto(
                    slotIni,
                    slotFim,
                    $"{horaCursor:HH\\:mm} - {horaProx:HH\\:mm}",
                    conflito is null,
                    conflito?.Origem,
                    conflito?.DescricaoPublica));

                horaCursor = horaProx;
            }
        }

        return new AgendaEspacoDto(
            espaco.Id,
            espaco.Nome,
            espaco.TipoLocacao,
            espaco.ValorReserva,
            espaco.HoraAbertura,
            espaco.HoraFechamento,
            ini,
            fim,
            slots,
            ocupacoesDto);
    }

    public async Task<ReservaEspacoDto> CriarReservaAsync(
        AuthenticatedUserInfo user,
        CriarReservaRequest req,
        CancellationToken ct = default)
    {
        if (user.EhDependenteAtivo || !user.AssociadoId.HasValue || user.CategoriaCodigo == CodigosCategoriaAssociado.Simpatizante)
        {
            throw new ApiException(StatusCodes.Status403Forbidden, CodigosErro.RecursoSemPermissao,
                "Apenas associados titulares autorizados podem realizar reservas de espaços.");
        }

        var nowUtc = DateTime.UtcNow;
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

            var periodo = NormalizarEValidarPeriodo(espaco, req.Inicio, req.Fim, aplicarRegrasReservaParticular: true, nowUtc);

            var conflito = await _db.OcupacoesEspaco
                .AnyAsync(o => o.EspacoId == espaco.Id && o.Ativa && o.InicioUtc < periodo.FimUtc && o.FimUtc > periodo.InicioUtc, ct);

            if (conflito)
                throw new ApiException(StatusCodes.Status409Conflict, CodigosErro.ReservaHorarioOcupado, "Este espaço já está ocupado na agenda para o período selecionado.");

            var reserva = new ReservaEspaco
            {
                EspacoId = espaco.Id,
                AssociadoId = user.AssociadoId.Value,
                InicioUtc = periodo.InicioUtc,
                FimUtc = periodo.FimUtc,
                ValorTotal = periodo.ValorTotal,
                Status = StatusReserva.Confirmada,
                CriadaEmUtc = nowUtc
            };
            _db.ReservasEspaco.Add(reserva);
            await _db.SaveChangesAsync(ct);

            var ocupacao = new OcupacaoEspaco
            {
                EspacoId = espaco.Id,
                InicioUtc = periodo.InicioUtc,
                FimUtc = periodo.FimUtc,
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
                null,
                espaco.TipoLocacao,
                reserva.ValorTotal);
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

            return ToReservaDto(reserva);
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
        return items.Select(ToReservaDto).ToList();
    }

    // ==================== HELPERS ====================

    private async Task<IReadOnlyList<OcupacaoEspacoDto>> ConsultarOcupacoesInternoAsync(
        Espaco espaco,
        DateOnly? dataInicio,
        DateOnly? dataFim,
        CancellationToken ct)
    {
        var query = _db.OcupacoesEspaco
            .AsNoTracking()
            .Include(o => o.Evento)
            .Where(o => o.EspacoId == espaco.Id && o.Ativa);

        if (dataInicio.HasValue)
        {
            var dtIni = new DateTimeOffset(dataInicio.Value.ToDateTime(TimeOnly.MinValue), OffsetSaoPaulo).UtcDateTime;
            query = query.Where(o => o.FimUtc > dtIni);
        }
        if (dataFim.HasValue)
        {
            var dtFim = new DateTimeOffset(dataFim.Value.AddDays(1).ToDateTime(TimeOnly.MinValue), OffsetSaoPaulo).UtcDateTime;
            query = query.Where(o => o.InicioUtc < dtFim);
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

    private static void EnsureAcessoUnidadeEspaco(AuthenticatedUserInfo user, int unidadeId)
    {
        if (user.Role == RoleAdministrativa.SuperAdmin)
            return;
        if (user.Role is RoleAdministrativa.Admin or RoleAdministrativa.Tesoureiro && user.UnidadeAdministrativaId == unidadeId)
            return;
        if (user.UnidadeAssociadoId == unidadeId)
            return;

        throw new ApiException(StatusCodes.Status403Forbidden, CodigosErro.UnidadeSemPermissao, "Você não possui acesso aos espaços desta unidade.");
    }

    private static void ValidarRequestEspaco(CriarOuAtualizarEspacoRequest req)
    {
        if (string.IsNullOrWhiteSpace(req.Nome))
            throw new ApiException(StatusCodes.Status400BadRequest, CodigosErro.DadosInvalidos, "O nome do espaço é obrigatório.");

        if (!Enum.IsDefined(req.TipoLocacao))
            throw new ApiException(StatusCodes.Status400BadRequest, CodigosErro.DadosInvalidos, "Tipo de locação do espaço inválido.");

        if (req.ValorReserva < 0)
            throw new ApiException(StatusCodes.Status400BadRequest, CodigosErro.DadosInvalidos, "O valor da reserva não pode ser negativo.");

        if (req.HoraFechamento <= req.HoraAbertura)
            throw new ApiException(StatusCodes.Status400BadRequest, CodigosErro.DadosInvalidos, "O horário de fechamento deve ser posterior à abertura.");

        if (req.AntecedenciaMaximaDias < 1)
            throw new ApiException(StatusCodes.Status400BadRequest, CodigosErro.DadosInvalidos, "A antecedência máxima deve ser de pelo menos 1 dia.");

        if (req.TipoLocacao == TipoLocacaoEspaco.PorHora && req.DuracaoMaximaMinutos < 60)
            throw new ApiException(StatusCodes.Status400BadRequest, CodigosErro.DadosInvalidos, "A duração máxima mínima para locação por hora é de 60 minutos.");
    }

    private static (DateTime InicioUtc, DateTime FimUtc, decimal ValorTotal) NormalizarEValidarPeriodo(
        Espaco espaco,
        DateTimeOffset inicioReq,
        DateTimeOffset fimReq,
        bool aplicarRegrasReservaParticular,
        DateTime nowUtc)
    {
        var inicioUtc = inicioReq.UtcDateTime;
        var fimUtc = fimReq.UtcDateTime;

        if (fimUtc <= inicioUtc)
            throw new ApiException(StatusCodes.Status400BadRequest, CodigosErro.DadosInvalidos, "O período final deve ser posterior ao início.");

        var inicioLocal = TimeZoneHelper.ToSaoPauloOffset(inicioUtc);
        var fimLocal = TimeZoneHelper.ToSaoPauloOffset(fimUtc);
        var hojeLocal = TimeZoneHelper.TodaySaoPaulo(nowUtc);
        var dataIni = DateOnly.FromDateTime(inicioLocal.DateTime);
        var dataFim = DateOnly.FromDateTime(fimLocal.DateTime);

        if (espaco.TipoLocacao == TipoLocacaoEspaco.PorDia)
        {
            if (fimLocal.TimeOfDay == TimeSpan.Zero && dataFim > dataIni)
                dataFim = dataFim.AddDays(-1);

            if (dataFim < dataIni)
                throw new ApiException(StatusCodes.Status400BadRequest, CodigosErro.DadosInvalidos, "A data final deve ser igual ou posterior à data inicial.");

            var inicioDiaUtc = new DateTimeOffset(dataIni.ToDateTime(TimeOnly.MinValue), inicioLocal.Offset).UtcDateTime;
            var fimDiaUtc = new DateTimeOffset(dataFim.AddDays(1).ToDateTime(TimeOnly.MinValue), inicioLocal.Offset).UtcDateTime;

            if (aplicarRegrasReservaParticular)
            {
                if (dataIni < hojeLocal || fimDiaUtc <= nowUtc)
                    throw new ApiException(StatusCodes.Status422UnprocessableEntity, CodigosErro.ReservaForaRegrasEspaco, "Reservas por dia só podem ser criadas para datas vigentes ou futuras.");

                if (dataIni.DayNumber - hojeLocal.DayNumber > espaco.AntecedenciaMaximaDias)
                {
                    throw new ApiException(StatusCodes.Status422UnprocessableEntity, CodigosErro.ReservaForaRegrasEspaco,
                        $"A antecedência máxima para reservar este espaço é de {espaco.AntecedenciaMaximaDias} dias.");
                }
            }

            var dias = dataFim.DayNumber - dataIni.DayNumber + 1;
            var valorDia = Math.Round(espaco.ValorReserva * dias, 2);
            return (inicioDiaUtc, fimDiaUtc, valorDia);
        }

        // TipoLocacaoEspaco.PorHora
        var duracaoMinutos = (fimUtc - inicioUtc).TotalMinutes;

        if (aplicarRegrasReservaParticular)
        {
            if (inicioUtc <= nowUtc)
                throw new ApiException(StatusCodes.Status422UnprocessableEntity, CodigosErro.ReservaForaRegrasEspaco, "Reservas só podem ser criadas para horários futuros.");

            if (duracaoMinutos < 60)
                throw new ApiException(StatusCodes.Status422UnprocessableEntity, CodigosErro.ReservaForaRegrasEspaco, "A duração mínima de uma reserva por hora é de 60 minutos.");

            if (duracaoMinutos > espaco.DuracaoMaximaMinutos)
            {
                throw new ApiException(StatusCodes.Status422UnprocessableEntity, CodigosErro.ReservaForaRegrasEspaco,
                    $"A duração máxima permitida para este espaço é de {espaco.DuracaoMaximaMinutos} minutos.");
            }

            if (dataIni.DayNumber - hojeLocal.DayNumber > espaco.AntecedenciaMaximaDias)
            {
                throw new ApiException(StatusCodes.Status422UnprocessableEntity, CodigosErro.ReservaForaRegrasEspaco,
                    $"A antecedência máxima para reservar este espaço é de {espaco.AntecedenciaMaximaDias} dias.");
            }
        }

        var horaIniLocal = TimeOnly.FromDateTime(inicioLocal.DateTime);
        var horaFimLocal = TimeOnly.FromDateTime(fimLocal.DateTime);
        if (dataIni != dataFim || horaIniLocal < espaco.HoraAbertura || horaFimLocal > espaco.HoraFechamento)
        {
            throw new ApiException(StatusCodes.Status422UnprocessableEntity, CodigosErro.ReservaForaRegrasEspaco,
                $"Este espaço é locado por horário: informe início e término no mesmo dia dentro de {espaco.HoraAbertura:HH\\:mm} às {espaco.HoraFechamento:HH\\:mm}.");
        }

        var horas = (decimal)(fimUtc - inicioUtc).TotalHours;
        var valorHora = Math.Round(espaco.ValorReserva * horas, 2);
        return (inicioUtc, fimUtc, valorHora);
    }

    private async Task<(string NomeArquivo, string ContentType, string CaminhoRelativo)> SalvarFotoBase64Async(
        string pasta,
        UploadFotoRequest req,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(req.NomeArquivo) || string.IsNullOrWhiteSpace(req.ConteudoBase64))
            throw new ApiException(StatusCodes.Status400BadRequest, CodigosErro.DadosInvalidos, "Nome do arquivo e conteúdo da foto são obrigatórios.");

        var contentType = (req.ContentType ?? string.Empty).Trim().ToLowerInvariant();
        if (contentType == "image/jpg")
            contentType = "image/jpeg";

        if (!ExtensoesPermitidas.TryGetValue(contentType, out var ext))
            throw new ApiException(StatusCodes.Status400BadRequest, CodigosErro.DadosInvalidos, "Formato de imagem não suportado. Utilize JPG, PNG ou WEBP.");

        var base64 = req.ConteudoBase64.Trim();
        var commaIdx = base64.IndexOf(',');
        if (base64.StartsWith("data:", StringComparison.OrdinalIgnoreCase) && commaIdx >= 0)
            base64 = base64[(commaIdx + 1)..];

        byte[] bytes;
        try
        {
            bytes = Convert.FromBase64String(base64);
        }
        catch (FormatException)
        {
            throw new ApiException(StatusCodes.Status400BadRequest, CodigosErro.DadosInvalidos, "Conteúdo da imagem em Base64 inválido.");
        }

        if (bytes.Length == 0 || bytes.Length > TamanhoMaximoFotoBytes)
            throw new ApiException(StatusCodes.Status400BadRequest, CodigosErro.DadosInvalidos, "A foto deve possuir entre 1 byte e 5 MB.");

        var nomeSeguro = Path.GetFileName(req.NomeArquivo.Trim());
        if (nomeSeguro.Length > 200)
            nomeSeguro = nomeSeguro[^200..];

        var arquivoDisco = $"{Guid.NewGuid():N}{ext}";
        var dirRelativo = Path.Combine("uploads", pasta);
        var dirAbsoluto = Path.Combine(_env.ContentRootPath, dirRelativo);
        Directory.CreateDirectory(dirAbsoluto);

        var caminhoRelativo = Path.Combine(dirRelativo, arquivoDisco);
        var caminhoAbsoluto = Path.Combine(_env.ContentRootPath, caminhoRelativo);
        await File.WriteAllBytesAsync(caminhoAbsoluto, bytes, ct);

        return (nomeSeguro, contentType, caminhoRelativo.Replace('\\', '/'));
    }

    private (string CaminhoAbsoluto, string ContentType) ResolverCaminhoFotoExistente(string caminhoRelativo, string contentType)
    {
        var uploadsRoot = Path.GetFullPath(Path.Combine(_env.ContentRootPath, "uploads"));
        var fullPath = Path.GetFullPath(Path.Combine(_env.ContentRootPath, caminhoRelativo));
        if (!fullPath.StartsWith(uploadsRoot, StringComparison.OrdinalIgnoreCase) || !File.Exists(fullPath))
            throw new ApiException(StatusCodes.Status404NotFound, CodigosErro.RecursoNaoEncontrado, "Arquivo de foto não encontrado.");

        return (fullPath, contentType);
    }

    private void ExcluirArquivoSeguro(string caminhoRelativo)
    {
        try
        {
            var uploadsRoot = Path.GetFullPath(Path.Combine(_env.ContentRootPath, "uploads"));
            var fullPath = Path.GetFullPath(Path.Combine(_env.ContentRootPath, caminhoRelativo));
            if (fullPath.StartsWith(uploadsRoot, StringComparison.OrdinalIgnoreCase) && File.Exists(fullPath))
                File.Delete(fullPath);
        }
        catch
        {
            // Ignora falha ao remover arquivo órfão
        }
    }

    private async Task LockEspacoRowIfMySqlAsync(int espacoId, CancellationToken ct)
    {
        if (_db.Database.ProviderName?.Contains("MySql", StringComparison.OrdinalIgnoreCase) == true)
        {
            await _db.Database.ExecuteSqlInterpolatedAsync($"SELECT Id FROM Espaco WHERE Id = {espacoId} FOR UPDATE", ct);
        }
    }

    private static FotoItemDto ToFotoEspacoDto(FotoEspaco f) => new(
        f.Id, f.NomeArquivo, f.ContentType, $"api/v1/midia/espacos/{f.Id}", f.Ordem, TimeZoneHelper.ToSaoPauloOffset(f.CriadaEmUtc));

    private static FotoItemDto ToFotoEventoDto(FotoEvento f) => new(
        f.Id, f.NomeArquivo, f.ContentType, $"api/v1/midia/eventos/{f.Id}", f.Ordem, TimeZoneHelper.ToSaoPauloOffset(f.CriadaEmUtc));

    private static EspacoDto ToEspacoDto(Espaco e) => new(
        e.Id,
        e.UnidadeId,
        e.Unidade.Nome,
        e.Nome,
        e.Descricao,
        e.Ativo,
        e.HoraAbertura,
        e.HoraFechamento,
        e.DuracaoMaximaMinutos,
        e.AntecedenciaMaximaDias,
        e.TipoLocacao,
        e.ValorReserva,
        e.Fotos.OrderBy(f => f.Ordem).ThenBy(f => f.Id).Select(ToFotoEspacoDto).ToList());

    private static ReservaEspacoDto ToReservaDto(ReservaEspaco r) => new(
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
        r.JustificativaCancelamento,
        r.Espaco.TipoLocacao,
        r.ValorTotal);

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
        EspacosOcupadosNomes: e.OcupacoesEspaco.Where(o => o.Ativa).Select(o => o.Espaco.Nome).ToList(),
        ValorReservaEspaco: e.ValorReservaEspaco,
        Fotos: e.Fotos.OrderBy(f => f.Ordem).ThenBy(f => f.Id).Select(ToFotoEventoDto).ToList());
}
