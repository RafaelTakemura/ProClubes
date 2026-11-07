using Microsoft.EntityFrameworkCore;
using ProClubesApi.Authorization;
using ProClubesApi.Contracts;
using ProClubesApi.Data;
using ProClubesApi.Entities;

namespace ProClubesApi.Services;

public class FinanceiroService
{
    private readonly ProClubesDbContext _db;
    private readonly CurrentUserService _currentUser;
    private readonly AuditoriaService _auditoria;
    private readonly NotificacaoService _notificacao;

    public FinanceiroService(
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

    public async Task<ConfiguracaoFinanceiraDto> ObterConfiguracaoAsync(AuthenticatedUserInfo user, CancellationToken ct = default)
    {
        if (user.Role == RoleAdministrativa.Nenhuma)
            throw new ApiException(StatusCodes.Status403Forbidden, CodigosErro.RecursoSemPermissao, "Acesso restrito à equipe administrativa/financeira.");

        var config = await ObterOuCriarConfiguracaoInternaAsync(user.UsuarioId, ct);
        return new ConfiguracaoFinanceiraDto(
            config.DiaGeracao,
            config.DiaVencimento,
            config.DiaAvisoVencimento,
            config.PercentualMulta,
            config.PercentualJuroMensal,
            TimeZoneHelper.ToSaoPauloOffset(config.AtualizadaEmUtc));
    }

    public async Task<ConfiguracaoFinanceiraDto> AtualizarConfiguracaoAsync(
        AuthenticatedUserInfo user,
        AtualizarConfiguracaoFinanceiraRequest req,
        CancellationToken ct = default)
    {
        // D1: Calendário de cobrança, multas e juros globais, gerenciados pelo Super Admin
        _currentUser.EnsureSuperAdmin(user);

        if (req.DiaGeracao is < 1 or > 31
            || req.DiaVencimento is < 1 or > 31
            || (req.DiaAvisoVencimento.HasValue && req.DiaAvisoVencimento.Value is < 1 or > 31)
            || req.PercentualMulta < 0
            || req.PercentualJuroMensal < 0)
        {
            throw new ApiException(StatusCodes.Status400BadRequest, CodigosErro.DadosInvalidos, "Parâmetros financeiros inválidos (dias entre 1 e 31, percentuais não negativos).");
        }

        var config = await ObterOuCriarConfiguracaoInternaAsync(user.UsuarioId, ct);
        config.DiaGeracao = req.DiaGeracao;
        config.DiaVencimento = req.DiaVencimento;
        config.DiaAvisoVencimento = req.DiaAvisoVencimento;
        config.PercentualMulta = Math.Round(req.PercentualMulta, 4);
        config.PercentualJuroMensal = Math.Round(req.PercentualJuroMensal, 4);
        config.AtualizadaEmUtc = DateTime.UtcNow;
        config.AlteradaPorUsuarioId = user.UsuarioId;

        _auditoria.Registrar(user.UsuarioId, null, "ATUALIZAR_CONFIG_FINANCEIRA", nameof(ConfiguracaoFinanceira), config.Id,
            $"Vencimento dia {config.DiaVencimento}, multa {config.PercentualMulta}%, juros {config.PercentualJuroMensal}%.");

        await _db.SaveChangesAsync(ct);

        return new ConfiguracaoFinanceiraDto(
            config.DiaGeracao,
            config.DiaVencimento,
            config.DiaAvisoVencimento,
            config.PercentualMulta,
            config.PercentualJuroMensal,
            TimeZoneHelper.ToSaoPauloOffset(config.AtualizadaEmUtc));
    }

    public async Task<IReadOnlyList<ValorMensalidadeDto>> ListarValoresMensalidadeAsync(
        AuthenticatedUserInfo user,
        int? unidadeId,
        CancellationToken ct = default)
    {
        if (user.Role == RoleAdministrativa.Nenhuma)
            throw new ApiException(StatusCodes.Status403Forbidden, CodigosErro.RecursoSemPermissao, "Acesso restrito.");

        var query = _db.ValoresMensalidade
            .AsNoTracking()
            .Include(v => v.Unidade)
            .Include(v => v.CategoriaAssociado)
            .AsQueryable();

        if (user.Role != RoleAdministrativa.SuperAdmin)
        {
            if (unidadeId.HasValue && unidadeId.Value != user.UnidadeAdministrativaId)
                throw new ApiException(StatusCodes.Status403Forbidden, CodigosErro.UnidadeSemPermissao, "Escopo limitado à sua unidade.");
            query = query.Where(v => v.UnidadeId == user.UnidadeAdministrativaId);
        }
        else if (unidadeId.HasValue)
        {
            query = query.Where(v => v.UnidadeId == unidadeId.Value);
        }

        var items = await query
            .OrderBy(v => v.Unidade.Nome)
            .ThenBy(v => v.CategoriaAssociado.Id)
            .ThenByDescending(v => v.CompetenciaInicio)
            .ToListAsync(ct);

        return items.Select(v => new ValorMensalidadeDto(
            v.Id,
            v.UnidadeId,
            v.Unidade.Nome,
            v.CategoriaAssociadoId,
            v.CategoriaAssociado.Codigo,
            v.CategoriaAssociado.Nome,
            v.Valor,
            v.CompetenciaInicio,
            v.CompetenciaFim)).ToList();
    }

    public async Task<ValorMensalidadeDto> DefinirValorMensalidadeAsync(
        AuthenticatedUserInfo user,
        DefinirValorMensalidadeRequest req,
        CancellationToken ct = default)
    {
        _currentUser.EnsureFinancialAccessForUnit(user, req.UnidadeId);

        if (req.Valor < 0)
            throw new ApiException(StatusCodes.Status400BadRequest, CodigosErro.DadosInvalidos, "O valor da mensalidade não pode ser negativo.");

        var unidade = await _db.Unidades.FirstOrDefaultAsync(u => u.Id == req.UnidadeId, ct)
            ?? throw new ApiException(StatusCodes.Status404NotFound, CodigosErro.RecursoNaoEncontrado, "Unidade não encontrada.");

        var cod = req.CodigoCategoria.Trim().ToUpperInvariant();
        var categoria = await _db.CategoriasAssociado.FirstOrDefaultAsync(c => c.Codigo == cod, ct)
            ?? throw new ApiException(StatusCodes.Status400BadRequest, CodigosErro.DadosInvalidos, "Categoria associativa inválida.");

        var valorEfetivo = categoria.IsentoMensalidade ? 0m : Math.Round(req.Valor, 2);

        await using var tx = await _db.Database.BeginTransactionAsync(ct);
        var hoje = TimeZoneHelper.TodaySaoPaulo();
        var proximaCompetencia = TimeZoneHelper.FirstDayOfNextMonth(hoje);

        var existentes = await _db.ValoresMensalidade
            .Where(v => v.UnidadeId == unidade.Id && v.CategoriaAssociadoId == categoria.Id)
            .OrderBy(v => v.CompetenciaInicio)
            .ToListAsync(ct);

        DateOnly competenciaInicio;
        if (req.CompetenciaInicio.HasValue)
        {
            competenciaInicio = TimeZoneHelper.FirstDayOfMonth(req.CompetenciaInicio.Value);
            if (existentes.Count > 0 && competenciaInicio < proximaCompetencia)
            {
                throw new ApiException(StatusCodes.Status422UnprocessableEntity, CodigosErro.VigenciaValorSobreposta,
                    "Alterações de valor de mensalidade incidem apenas a partir da competência do mês seguinte.");
            }
        }
        else
        {
            competenciaInicio = existentes.Count == 0
                ? TimeZoneHelper.FirstDayOfMonth(hoje)
                : proximaCompetencia;
        }

        // Fecha vigência anterior em aberto ou impede sobreposição
        foreach (var v in existentes)
        {
            if (v.CompetenciaInicio == competenciaInicio)
            {
                throw new ApiException(StatusCodes.Status409Conflict, CodigosErro.VigenciaValorSobreposta,
                    "Já existe um valor cadastrado com a mesma competência de início para esta unidade e categoria.");
            }

            var fimEfetivo = v.CompetenciaFim ?? DateOnly.MaxValue;
            if (competenciaInicio <= fimEfetivo && v.CompetenciaInicio < competenciaInicio)
            {
                if (v.CompetenciaFim is null)
                {
                    v.CompetenciaFim = competenciaInicio.AddMonths(-1);
                }
                else
                {
                    throw new ApiException(StatusCodes.Status409Conflict, CodigosErro.VigenciaValorSobreposta,
                        "O intervalo de vigência sobrepõe um período histórico já fechado.");
                }
            }
            else if (v.CompetenciaInicio > competenciaInicio)
            {
                throw new ApiException(StatusCodes.Status409Conflict, CodigosErro.VigenciaValorSobreposta,
                    "Já existe vigência futura posterior à competência informada.");
            }
        }

        var novoValor = new ValorMensalidade
        {
            UnidadeId = unidade.Id,
            CategoriaAssociadoId = categoria.Id,
            Valor = valorEfetivo,
            CompetenciaInicio = competenciaInicio,
            CompetenciaFim = null,
            CriadoPorUsuarioId = user.UsuarioId,
            CriadoEmUtc = DateTime.UtcNow
        };

        _db.ValoresMensalidade.Add(novoValor);
        _auditoria.Registrar(user.UsuarioId, unidade.Id, "DEFINIR_VALOR_MENSALIDADE", nameof(ValorMensalidade), null,
            $"Valor {valorEfetivo:C2} definido para {categoria.Codigo} a partir de {competenciaInicio:yyyy-MM-dd}.");

        await _db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);

        return new ValorMensalidadeDto(
            novoValor.Id,
            unidade.Id,
            unidade.Nome,
            categoria.Id,
            categoria.Codigo,
            categoria.Nome,
            novoValor.Valor,
            novoValor.CompetenciaInicio,
            novoValor.CompetenciaFim);
    }

    public async Task<int> GerarMensalidadesCompetenciaAsync(
        DateOnly? competenciaAlvo = null,
        int? usuarioAcionadorId = null,
        CancellationToken ct = default)
    {
        var hoje = TimeZoneHelper.TodaySaoPaulo();
        var competencia = TimeZoneHelper.FirstDayOfMonth(competenciaAlvo ?? hoje);
        var config = await ObterOuCriarConfiguracaoInternaAsync(usuarioAcionadorId, ct);

        var dataGeracao = TimeZoneHelper.ResolveDayInMonth(competencia.Year, competencia.Month, config.DiaGeracao);
        var dataVencimento = TimeZoneHelper.ResolveDayInMonth(competencia.Year, competencia.Month, config.DiaVencimento);

        var associadosAtivos = await _db.Associados
            .Include(a => a.Pessoa)
                .ThenInclude(p => p.Usuario)
            .Include(a => a.CategoriaAssociado)
            .Where(a => a.Status == StatusAssociado.Ativo
                        && a.DataInicioCobranca != null
                        && a.DataInicioCobranca <= competencia)
            .ToListAsync(ct);

        var valoresVigentes = await _db.ValoresMensalidade
            .Where(v => v.CompetenciaInicio <= competencia
                        && (v.CompetenciaFim == null || v.CompetenciaFim >= competencia))
            .ToListAsync(ct);

        var transferenciasPosteriores = await _db.TransferenciasUnidade
            .Where(t => t.CompetenciaCobrancaDestino > competencia)
            .OrderBy(t => t.RealizadaEmUtc)
            .ToListAsync(ct);

        var mudancasCatPosteriores = await _db.HistoricosCategoriaAssociado
            .Include(h => h.CategoriaAnterior)
            .Where(h => h.VigenteApartir > competencia)
            .OrderBy(h => h.AlteradoEmUtc)
            .ToListAsync(ct);

        var jaGeradasIds = (await _db.Mensalidades
            .Where(m => m.Competencia == competencia)
            .Select(m => m.AssociadoId)
            .ToListAsync(ct)).ToHashSet();

        var geradasCount = 0;

        foreach (var assoc in associadosAtivos)
        {
            if (jaGeradasIds.Contains(assoc.Id))
                continue;

            // Determina unidade emissora na competência (respeitando transferência cujo efeito é somente no mês seguinte)
            var transfPosterior = transferenciasPosteriores.FirstOrDefault(t => t.AssociadoId == assoc.Id);
            var unidadeEmissoraId = transfPosterior?.UnidadeOrigemId ?? assoc.UnidadeId;

            // Determina categoria vigente na competência (respeitando troca de categoria com efeito no mês seguinte)
            var mudancaPosterior = mudancasCatPosteriores.FirstOrDefault(h => h.AssociadoId == assoc.Id);
            var categoriaEmissaoId = mudancaPosterior?.CategoriaAnteriorId ?? assoc.CategoriaAssociadoId;
            var isentoNaCompetencia = mudancaPosterior?.CategoriaAnterior.IsentoMensalidade ?? assoc.CategoriaAssociado.IsentoMensalidade;

            if (isentoNaCompetencia)
                continue;

            var tabelaValor = valoresVigentes
                .OrderByDescending(v => v.CompetenciaInicio)
                .FirstOrDefault(v => v.UnidadeId == unidadeEmissoraId && v.CategoriaAssociadoId == categoriaEmissaoId);

            if (tabelaValor is null || tabelaValor.Valor <= 0)
                continue;

            var mensalidade = new Mensalidade
            {
                AssociadoId = assoc.Id,
                UnidadeEmissoraId = unidadeEmissoraId,
                CategoriaNaEmissaoId = categoriaEmissaoId,
                Competencia = competencia,
                DataGeracao = dataGeracao,
                DataVencimento = dataVencimento,
                ValorPrincipal = tabelaValor.Valor,
                MultaPercentualEmissao = config.PercentualMulta,
                JuroMensalPercentualEmissao = config.PercentualJuroMensal,
                Status = StatusMensalidade.Aberta,
                GeradaEmUtc = DateTime.UtcNow
            };

            _db.Mensalidades.Add(mensalidade);
            await _db.SaveChangesAsync(ct);
            jaGeradasIds.Add(assoc.Id);
            geradasCount++;

            if (assoc.Pessoa.Usuario is not null)
            {
                await _notificacao.CriarNotificacaoAsync(
                    assoc.Pessoa.Usuario.Id,
                    TipoNotificacao.MensalidadeGerada,
                    $"Mensalidade {competencia:MM/yyyy} disponível",
                    $"Sua mensalidade da competência {competencia:MM/yyyy} no valor de R$ {mensalidade.ValorPrincipal:N2} vence em {mensalidade.DataVencimento:dd/MM/yyyy}.",
                    "Mensalidade",
                    mensalidade.Id,
                    $"MENS_GER_{mensalidade.Id}",
                    ct);
                await _db.SaveChangesAsync(ct);
            }
        }

        return geradasCount;
    }

    public async Task<MensalidadeDto> RegistrarPagamentoMensalidadeAsync(
        AuthenticatedUserInfo user,
        int mensalidadeId,
        RegistrarPagamentoMensalidadeRequest req,
        CancellationToken ct = default)
    {
        var hoje = TimeZoneHelper.TodaySaoPaulo();
        if (req.DataPagamento > hoje)
            throw new ApiException(StatusCodes.Status422UnprocessableEntity, CodigosErro.DataPagamentoFutura, "A data de pagamento não pode ser futura.");

        await using var tx = await _db.Database.BeginTransactionAsync(ct);

        var mensalidade = await _db.Mensalidades
            .Include(m => m.Associado)
                .ThenInclude(a => a.Pessoa)
            .Include(m => m.UnidadeEmissora)
            .Include(m => m.CategoriaNaEmissao)
            .Include(m => m.Pagamento)
            .FirstOrDefaultAsync(m => m.Id == mensalidadeId, ct)
            ?? throw new ApiException(StatusCodes.Status404NotFound, CodigosErro.RecursoNaoEncontrado, "Mensalidade não encontrada.");

        // Regra D3: Débitos anteriores à transferência ficam sob administração financeira da unidade de origem (UnidadeEmissoraId)
        _currentUser.EnsureFinancialAccessForUnit(user, mensalidade.UnidadeEmissoraId);

        if (mensalidade.Status == StatusMensalidade.Paga || mensalidade.Pagamento is not null)
            throw new ApiException(StatusCodes.Status409Conflict, CodigosErro.MensalidadeJaPaga, "Esta mensalidade já foi paga integralmente.");

        var (multa, juros, total, _) = CalcularEncargos(mensalidade, req.DataPagamento);

        var pagamento = new PagamentoMensalidade
        {
            MensalidadeId = mensalidade.Id,
            ValorPrincipalPago = mensalidade.ValorPrincipal,
            ValorMultaPago = multa,
            ValorJurosPago = juros,
            ValorTotalPago = total,
            FormaPagamento = req.FormaPagamento,
            DataPagamento = req.DataPagamento,
            RegistradoPorUsuarioId = user.UsuarioId,
            RegistradoEmUtc = DateTime.UtcNow
        };

        mensalidade.Status = StatusMensalidade.Paga;
        _db.PagamentosMensalidade.Add(pagamento);

        _auditoria.Registrar(
            user.UsuarioId,
            mensalidade.UnidadeEmissoraId,
            "REGISTRAR_PAGAMENTO_MENSALIDADE",
            nameof(Mensalidade),
            mensalidade.Id,
            $"Pagamento integral de R$ {total:N2} ({req.FormaPagamento}) na competência {mensalidade.Competencia:MM/yyyy}.");

        await _db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);

        mensalidade.Pagamento = pagamento;
        return ToMensalidadeDto(mensalidade, hoje);
    }

    public async Task<IReadOnlyList<MensalidadeDto>> ListarMinhasMensalidadesAsync(
        AuthenticatedUserInfo user,
        CancellationToken ct = default)
    {
        // Regra D2 e 7.4: Dependente não acessa nem gerencia mensalidades do titular
        if (user.EhDependenteAtivo && !user.AssociadoId.HasValue)
            throw new ApiException(StatusCodes.Status403Forbidden, CodigosErro.RecursoSemPermissao, "Dependentes não possuem acesso ao módulo financeiro.");

        if (!user.AssociadoId.HasValue)
            return Array.Empty<MensalidadeDto>();

        var hoje = TimeZoneHelper.TodaySaoPaulo();
        var items = await _db.Mensalidades
            .AsNoTracking()
            .Include(m => m.Associado)
                .ThenInclude(a => a.Pessoa)
            .Include(m => m.UnidadeEmissora)
            .Include(m => m.CategoriaNaEmissao)
            .Include(m => m.Pagamento)
            .Where(m => m.AssociadoId == user.AssociadoId.Value)
            .OrderByDescending(m => m.Competencia)
            .ToListAsync(ct);

        return items.Select(m => ToMensalidadeDto(m, hoje)).ToList();
    }

    public async Task<PagedResponse<MensalidadeDto>> ListarMensalidadesAdminAsync(
        AuthenticatedUserInfo user,
        int? unidadeId,
        int? associadoId,
        DateOnly? competencia,
        string? situacao,
        PaginationQuery pagination,
        CancellationToken ct = default)
    {
        if (user.Role == RoleAdministrativa.Nenhuma)
            throw new ApiException(StatusCodes.Status403Forbidden, CodigosErro.RecursoSemPermissao, "Acesso restrito ao módulo financeiro administrativo.");

        var hoje = TimeZoneHelper.TodaySaoPaulo();
        var query = _db.Mensalidades
            .AsNoTracking()
            .Include(m => m.Associado)
                .ThenInclude(a => a.Pessoa)
            .Include(m => m.UnidadeEmissora)
            .Include(m => m.CategoriaNaEmissao)
            .Include(m => m.Pagamento)
            .AsQueryable();

        if (user.Role != RoleAdministrativa.SuperAdmin)
        {
            if (unidadeId.HasValue && unidadeId.Value != user.UnidadeAdministrativaId)
                throw new ApiException(StatusCodes.Status403Forbidden, CodigosErro.UnidadeSemPermissao, "Você só pode consultar mensalidades emitidas pela sua unidade.");

            query = query.Where(m => m.UnidadeEmissoraId == user.UnidadeAdministrativaId);
        }
        else if (unidadeId.HasValue)
        {
            query = query.Where(m => m.UnidadeEmissoraId == unidadeId.Value);
        }

        if (associadoId.HasValue)
            query = query.Where(m => m.AssociadoId == associadoId.Value);

        if (competencia.HasValue)
        {
            var compNorm = TimeZoneHelper.FirstDayOfMonth(competencia.Value);
            query = query.Where(m => m.Competencia == compNorm);
        }

        if (!string.IsNullOrWhiteSpace(situacao))
        {
            var s = situacao.Trim().ToUpperInvariant();
            if (s == "PAGA")
                query = query.Where(m => m.Status == StatusMensalidade.Paga);
            else if (s == "VENCIDA" || s == "ATRASADA")
                query = query.Where(m => m.Status == StatusMensalidade.Aberta && m.DataVencimento < hoje);
            else if (s == "ABERTA" || s == "EMABERTO")
                query = query.Where(m => m.Status == StatusMensalidade.Aberta && m.DataVencimento >= hoje);
        }

        var total = await query.CountAsync(ct);
        var items = await query
            .OrderByDescending(m => m.Competencia)
            .ThenBy(m => m.Associado.Pessoa.NomeCompleto)
            .Skip(pagination.Skip)
            .Take(pagination.NormalizedPageSize)
            .ToListAsync(ct);

        var dtos = items.Select(m => ToMensalidadeDto(m, hoje)).ToList();
        return new PagedResponse<MensalidadeDto>(dtos, total, pagination.NormalizedPage, pagination.NormalizedPageSize);
    }

    public static (decimal Multa, decimal Juros, decimal Total, int DiasAtraso) CalcularEncargos(Mensalidade m, DateOnly dataReferencia)
    {
        if (m.Status == StatusMensalidade.Paga && m.Pagamento is not null)
        {
            var diasPago = m.Pagamento.DataPagamento > m.DataVencimento
                ? m.Pagamento.DataPagamento.DayNumber - m.DataVencimento.DayNumber
                : 0;
            return (m.Pagamento.ValorMultaPago, m.Pagamento.ValorJurosPago, m.Pagamento.ValorTotalPago, diasPago);
        }

        if (dataReferencia <= m.DataVencimento)
            return (0m, 0m, m.ValorPrincipal, 0);

        var diasAtraso = dataReferencia.DayNumber - m.DataVencimento.DayNumber;
        var multa = Math.Round(m.ValorPrincipal * (m.MultaPercentualEmissao / 100m), 2, MidpointRounding.AwayFromZero);
        var juros = Math.Round(m.ValorPrincipal * (m.JuroMensalPercentualEmissao / 100m) * (diasAtraso / 30m), 2, MidpointRounding.AwayFromZero);
        var total = m.ValorPrincipal + multa + juros;
        return (multa, juros, total, diasAtraso);
    }

    public static MensalidadeDto ToMensalidadeDto(Mensalidade m, DateOnly hoje)
    {
        var (multa, juros, total, diasAtraso) = CalcularEncargos(m, hoje);
        var compMesAtual = TimeZoneHelper.FirstDayOfMonth(hoje);

        string situacao;
        if (m.Status == StatusMensalidade.Paga)
            situacao = "Paga";
        else if (m.DataVencimento < hoje)
            situacao = "Vencida";
        else if (m.Competencia > compMesAtual)
            situacao = "Futura";
        else
            situacao = "Em aberto";

        return new MensalidadeDto(
            Id: m.Id,
            AssociadoId: m.AssociadoId,
            AssociadoNome: m.Associado.Pessoa.NomeCompleto,
            CpfMascarado: CpfHelper.Mask(m.Associado.Pessoa.Cpf),
            UnidadeEmissoraId: m.UnidadeEmissoraId,
            UnidadeEmissoraNome: m.UnidadeEmissora.Nome,
            CategoriaNaEmissaoNome: m.CategoriaNaEmissao.Nome,
            Competencia: m.Competencia,
            DataGeracao: m.DataGeracao,
            DataVencimento: m.DataVencimento,
            ValorPrincipal: m.ValorPrincipal,
            ValorMultaCalculado: multa,
            ValorJurosCalculado: juros,
            ValorTotalAtualizado: total,
            Status: m.Status,
            SituacaoExibicao: situacao,
            DiasAtraso: diasAtraso,
            Pagamento: m.Pagamento is null ? null : new PagamentoMensalidadeDto(
                m.Pagamento.Id,
                m.Pagamento.ValorPrincipalPago,
                m.Pagamento.ValorMultaPago,
                m.Pagamento.ValorJurosPago,
                m.Pagamento.ValorTotalPago,
                m.Pagamento.FormaPagamento,
                m.Pagamento.DataPagamento,
                TimeZoneHelper.ToSaoPauloOffset(m.Pagamento.RegistradoEmUtc)));
    }

    private async Task<ConfiguracaoFinanceira> ObterOuCriarConfiguracaoInternaAsync(int? usuarioId, CancellationToken ct)
    {
        var config = await _db.ConfiguracoesFinanceiras.FirstOrDefaultAsync(c => c.Id == 1, ct);
        if (config is not null)
            return config;

        var autorId = usuarioId ?? await _db.Usuarios.OrderBy(u => u.Id).Select(u => u.Id).FirstAsync(ct);
        config = new ConfiguracaoFinanceira
        {
            Id = 1,
            DiaGeracao = 1,
            DiaVencimento = 10,
            DiaAvisoVencimento = 7,
            PercentualMulta = 2.0000m,
            PercentualJuroMensal = 1.0000m,
            AtualizadaEmUtc = DateTime.UtcNow,
            AlteradaPorUsuarioId = autorId
        };
        _db.ConfiguracoesFinanceiras.Add(config);
        await _db.SaveChangesAsync(ct);
        return config;
    }
}
