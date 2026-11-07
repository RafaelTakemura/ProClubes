using System.Collections.Concurrent;
using Microsoft.EntityFrameworkCore;
using ProClubesApi.Authorization;
using ProClubesApi.Contracts;
using ProClubesApi.Data;
using ProClubesApi.Entities;

namespace ProClubesApi.Services;

public class ManifestacaoService
{
    private readonly ProClubesDbContext _db;
    private readonly CurrentUserService _currentUser;
    private readonly AuditoriaService _auditoria;
    private readonly NotificacaoService _notificacao;

    public ManifestacaoService(
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

    public async Task<IReadOnlyList<CategoriaManifestacaoDto>> ListarCategoriasAsync(
        bool apenasAtivas = true,
        CancellationToken ct = default)
    {
        var query = _db.CategoriasManifestacao.AsNoTracking().AsQueryable();
        if (apenasAtivas)
            query = query.Where(c => c.Ativa);

        var items = await query.OrderBy(c => c.TipoManifestacao).ThenBy(c => c.Nome).ToListAsync(ct);
        return items.Select(c => new CategoriaManifestacaoDto(c.Id, c.Nome, c.TipoManifestacao, c.Descricao, c.Ativa)).ToList();
    }

    public async Task<CategoriaManifestacaoDto> CriarCategoriaAsync(
        AuthenticatedUserInfo user,
        CriarOuAtualizarCategoriaManifestacaoRequest req,
        CancellationToken ct = default)
    {
        // Regra 10.2: Categorias globais criadas/editadas/desativadas exclusivamente pelo Super Admin
        _currentUser.EnsureSuperAdmin(user);

        if (string.IsNullOrWhiteSpace(req.Nome))
            throw new ApiException(StatusCodes.Status400BadRequest, CodigosErro.DadosInvalidos, "O nome da categoria é obrigatório.");

        var cat = new CategoriaManifestacao
        {
            Nome = req.Nome.Trim(),
            TipoManifestacao = req.TipoManifestacao,
            Descricao = req.Descricao?.Trim(),
            Ativa = req.Ativa
        };

        _db.CategoriasManifestacao.Add(cat);
        _auditoria.Registrar(user.UsuarioId, null, "CRIAR_CATEGORIA_MANIFESTACAO", nameof(CategoriaManifestacao), null,
            $"Categoria global '{cat.Nome}' ({cat.TipoManifestacao}) criada.");
        await _db.SaveChangesAsync(ct);

        return new CategoriaManifestacaoDto(cat.Id, cat.Nome, cat.TipoManifestacao, cat.Descricao, cat.Ativa);
    }

    public async Task<CategoriaManifestacaoDto> AtualizarCategoriaAsync(
        AuthenticatedUserInfo user,
        int categoriaId,
        CriarOuAtualizarCategoriaManifestacaoRequest req,
        CancellationToken ct = default)
    {
        _currentUser.EnsureSuperAdmin(user);

        var cat = await _db.CategoriasManifestacao.FirstOrDefaultAsync(c => c.Id == categoriaId, ct)
            ?? throw new ApiException(StatusCodes.Status404NotFound, CodigosErro.RecursoNaoEncontrado, "Categoria de manifestação não encontrada.");

        if (string.IsNullOrWhiteSpace(req.Nome))
            throw new ApiException(StatusCodes.Status400BadRequest, CodigosErro.DadosInvalidos, "O nome da categoria é obrigatório.");

        cat.Nome = req.Nome.Trim();
        cat.TipoManifestacao = req.TipoManifestacao;
        cat.Descricao = req.Descricao?.Trim();
        cat.Ativa = req.Ativa;

        _auditoria.Registrar(user.UsuarioId, null, "ATUALIZAR_CATEGORIA_MANIFESTACAO", nameof(CategoriaManifestacao), cat.Id,
            $"Categoria global '{cat.Nome}' atualizada (Ativa={cat.Ativa}).");
        await _db.SaveChangesAsync(ct);

        return new CategoriaManifestacaoDto(cat.Id, cat.Nome, cat.TipoManifestacao, cat.Descricao, cat.Ativa);
    }

    public async Task<ManifestacaoDto> CriarManifestacaoAsync(
        AuthenticatedUserInfo user,
        CriarManifestacaoRequest req,
        CancellationToken ct = default)
    {
        // Regra D2 e 10.1: Apenas associado titular autorizado abre manifestação
        if (user.EhDependenteAtivo || !user.AssociadoId.HasValue || !user.UnidadeAssociadoId.HasValue || user.CategoriaCodigo == CodigosCategoriaAssociado.Simpatizante)
        {
            throw new ApiException(StatusCodes.Status403Forbidden, CodigosErro.RecursoSemPermissao,
                "Apenas associados titulares autorizados podem registrar manifestações.");
        }

        if (string.IsNullOrWhiteSpace(req.Titulo) || string.IsNullOrWhiteSpace(req.Descricao))
            throw new ApiException(StatusCodes.Status400BadRequest, CodigosErro.DadosInvalidos, "Título e descrição são obrigatórios.");

        var categoria = await _db.CategoriasManifestacao
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == req.CategoriaManifestacaoId, ct)
            ?? throw new ApiException(StatusCodes.Status404NotFound, CodigosErro.RecursoNaoEncontrado, "Categoria não encontrada.");

        // Regra 10.2 e 28.1 (24): Categoria inativa não aparece/não é aceita em novas manifestações; tipo deve corresponder
        if (!categoria.Ativa || categoria.TipoManifestacao != req.TipoManifestacao)
        {
            throw new ApiException(StatusCodes.Status422UnprocessableEntity, CodigosErro.CategoriaManifestacaoInativaOuIncompativel,
                "A categoria selecionada está inativa ou não corresponde ao tipo de manifestação informado.");
        }

        var manifestacao = new Manifestacao
        {
            UnidadeId = user.UnidadeAssociadoId.Value,
            PessoaAutorId = user.PessoaId,
            CategoriaManifestacaoId = categoria.Id,
            TipoManifestacao = req.TipoManifestacao,
            Titulo = req.Titulo.Trim(),
            Descricao = req.Descricao.Trim(),
            Status = StatusManifestacao.Aberta,
            CriadaEmUtc = DateTime.UtcNow
        };

        _db.Manifestacoes.Add(manifestacao);
        await _db.SaveChangesAsync(ct);

        return await ObterManifestacaoPorIdAsync(user, manifestacao.Id, ct);
    }

    public async Task<ManifestacaoDto> ResponderManifestacaoAsync(
        AuthenticatedUserInfo user,
        int manifestacaoId,
        ResponderManifestacaoRequest req,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(req.Mensagem))
            throw new ApiException(StatusCodes.Status400BadRequest, CodigosErro.DadosInvalidos, "A mensagem de resposta é obrigatória.");

        var manifestacao = await _db.Manifestacoes
            .Include(m => m.PessoaAutor)
                .ThenInclude(p => p.Usuario)
            .FirstOrDefaultAsync(m => m.Id == manifestacaoId, ct)
            ?? throw new ApiException(StatusCodes.Status404NotFound, CodigosErro.RecursoNaoEncontrado, "Manifestação não encontrada.");

        var ehAutor = manifestacao.PessoaAutorId == user.PessoaId;
        var ehDiretoriaAutorizada = user.Role == RoleAdministrativa.SuperAdmin
                                    || (user.Role == RoleAdministrativa.Admin && user.UnidadeAdministrativaId == manifestacao.UnidadeId);

        if (!ehAutor && !ehDiretoriaAutorizada)
            throw new ApiException(StatusCodes.Status403Forbidden, CodigosErro.UnidadeSemPermissao, "Você não possui permissão para responder a esta manifestação.");

        var nowUtc = DateTime.UtcNow;
        var resposta = new RespostaManifestacao
        {
            ManifestacaoId = manifestacao.Id,
            AutorUsuarioId = user.UsuarioId,
            Mensagem = req.Mensagem.Trim(),
            CriadaEmUtc = nowUtc
        };
        _db.RespostasManifestacao.Add(resposta);

        if (ehDiretoriaAutorizada)
        {
            if (req.NovoStatus.HasValue)
            {
                manifestacao.Status = req.NovoStatus.Value;
                if (req.NovoStatus.Value == StatusManifestacao.Concluida)
                    manifestacao.ConcluidaEmUtc = nowUtc;
            }
            else if (manifestacao.Status == StatusManifestacao.Aberta)
            {
                manifestacao.Status = StatusManifestacao.EmAtendimento;
            }

            if (manifestacao.PessoaAutor.Usuario is not null)
            {
                await _notificacao.CriarNotificacaoAsync(
                    manifestacao.PessoaAutor.Usuario.Id,
                    TipoNotificacao.ManifestacaoAtualizada,
                    $"Atualização na manifestação #{manifestacao.Id}",
                    $"A diretoria respondeu à sua manifestação '{manifestacao.Titulo}' (Status: {manifestacao.Status}).",
                    "Manifestacao",
                    manifestacao.Id,
                    null,
                    ct);
            }
        }

        await _db.SaveChangesAsync(ct);
        return await ObterManifestacaoPorIdAsync(user, manifestacao.Id, ct);
    }

    public async Task<ManifestacaoDto> ObterManifestacaoPorIdAsync(
        AuthenticatedUserInfo user,
        int manifestacaoId,
        CancellationToken ct = default)
    {
        var m = await _db.Manifestacoes
            .AsNoTracking()
            .Include(x => x.Unidade)
            .Include(x => x.PessoaAutor)
            .Include(x => x.CategoriaManifestacao)
            .Include(x => x.Respostas)
                .ThenInclude(r => r.AutorUsuario)
                    .ThenInclude(u => u.Pessoa)
            .FirstOrDefaultAsync(x => x.Id == manifestacaoId, ct)
            ?? throw new ApiException(StatusCodes.Status404NotFound, CodigosErro.RecursoNaoEncontrado, "Manifestação não encontrada.");

        var ehAutor = m.PessoaAutorId == user.PessoaId;
        var ehDiretoriaAutorizada = user.Role == RoleAdministrativa.SuperAdmin
                                    || (user.Role == RoleAdministrativa.Admin && user.UnidadeAdministrativaId == m.UnidadeId);

        if (!ehAutor && !ehDiretoriaAutorizada)
            throw new ApiException(StatusCodes.Status403Forbidden, CodigosErro.RecursoSemPermissao, "Você não possui permissão para visualizar esta manifestação.");

        return ToDto(m);
    }

    public async Task<PagedResponse<ManifestacaoDto>> ListarManifestacoesAsync(
        AuthenticatedUserInfo user,
        int? unidadeId,
        StatusManifestacao? status,
        TipoManifestacao? tipo,
        PaginationQuery pagination,
        CancellationToken ct = default)
    {
        var query = _db.Manifestacoes
            .AsNoTracking()
            .Include(x => x.Unidade)
            .Include(x => x.PessoaAutor)
            .Include(x => x.CategoriaManifestacao)
            .Include(x => x.Respostas)
                .ThenInclude(r => r.AutorUsuario)
                    .ThenInclude(u => u.Pessoa)
            .AsQueryable();

        if (user.Role == RoleAdministrativa.SuperAdmin)
        {
            if (unidadeId.HasValue)
                query = query.Where(m => m.UnidadeId == unidadeId.Value);
        }
        else if (user.Role == RoleAdministrativa.Admin)
        {
            if (unidadeId.HasValue && unidadeId.Value != user.UnidadeAdministrativaId)
                throw new ApiException(StatusCodes.Status403Forbidden, CodigosErro.UnidadeSemPermissao, "Escopo restrito à sua unidade.");
            query = query.Where(m => m.UnidadeId == user.UnidadeAdministrativaId);
        }
        else if (user.Role == RoleAdministrativa.Tesoureiro || user.EhDependenteAtivo)
        {
            throw new ApiException(StatusCodes.Status403Forbidden, CodigosErro.RecursoSemPermissao, "Seu perfil não possui acesso ao módulo de manifestações.");
        }
        else
        {
            // Regra D5: Autor consulta seu histórico mesmo se tiver sido transferido de unidade posteriormente
            query = query.Where(m => m.PessoaAutorId == user.PessoaId);
        }

        if (status.HasValue)
            query = query.Where(m => m.Status == status.Value);

        if (tipo.HasValue)
            query = query.Where(m => m.TipoManifestacao == tipo.Value);

        var total = await query.CountAsync(ct);
        var items = await query
            .OrderByDescending(m => m.CriadaEmUtc)
            .Skip(pagination.Skip)
            .Take(pagination.NormalizedPageSize)
            .ToListAsync(ct);

        return new PagedResponse<ManifestacaoDto>(items.Select(ToDto).ToList(), total, pagination.NormalizedPage, pagination.NormalizedPageSize);
    }

    private static ManifestacaoDto ToDto(Manifestacao m) => new(
        Id: m.Id,
        UnidadeId: m.UnidadeId,
        UnidadeNome: m.Unidade.Nome,
        PessoaAutorId: m.PessoaAutorId,
        AutorNome: m.PessoaAutor.NomeCompleto,
        CategoriaManifestacaoId: m.CategoriaManifestacaoId,
        CategoriaNome: m.CategoriaManifestacao.Nome,
        TipoManifestacao: m.TipoManifestacao,
        Titulo: m.Titulo,
        Descricao: m.Descricao,
        Status: m.Status,
        CriadaEm: TimeZoneHelper.ToSaoPauloOffset(m.CriadaEmUtc),
        ConcluidaEm: m.ConcluidaEmUtc.HasValue ? TimeZoneHelper.ToSaoPauloOffset(m.ConcluidaEmUtc.Value) : null,
        Respostas: m.Respostas.OrderBy(r => r.CriadaEmUtc).Select(r => new RespostaManifestacaoDto(
            r.Id,
            r.AutorUsuarioId,
            r.AutorUsuario.Pessoa.NomeCompleto,
            r.Mensagem,
            TimeZoneHelper.ToSaoPauloOffset(r.CriadaEmUtc))).ToList());
}

public class VotacaoService
{
    private static readonly ConcurrentDictionary<int, SemaphoreSlim> VotacaoLocks = new();

    private readonly ProClubesDbContext _db;
    private readonly CurrentUserService _currentUser;
    private readonly AuditoriaService _auditoria;
    private readonly NotificacaoService _notificacao;

    public VotacaoService(
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

    public async Task<VotacaoDto> CriarVotacaoAsync(
        AuthenticatedUserInfo user,
        CriarVotacaoRequest req,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(req.Titulo) || req.Opcoes is null || req.Opcoes.Count(o => !string.IsNullOrWhiteSpace(o)) < 2)
            throw new ApiException(StatusCodes.Status400BadRequest, CodigosErro.DadosInvalidos, "A votação exige título e no mínimo 2 opções válidas.");

        var inicioUtc = req.Inicio.UtcDateTime;
        var fimUtc = req.FimPrevisto.UtcDateTime;
        if (fimUtc <= inicioUtc)
            throw new ApiException(StatusCodes.Status400BadRequest, CodigosErro.DadosInvalidos, "A data/hora final prevista deve ser posterior ao início.");

        if (req.Escopo == EscopoVotacao.Global)
        {
            _currentUser.EnsureSuperAdmin(user);
            if (req.UnidadeId.HasValue)
                throw new ApiException(StatusCodes.Status400BadRequest, CodigosErro.DadosInvalidos, "Votação global não deve ter UnidadeId preenchido.");
        }
        else
        {
            if (!req.UnidadeId.HasValue)
                throw new ApiException(StatusCodes.Status400BadRequest, CodigosErro.DadosInvalidos, "Votação de unidade exige UnidadeId.");
            _currentUser.EnsureAdminOrSuperAdminForUnit(user, req.UnidadeId.Value);
        }

        await using var tx = await _db.Database.BeginTransactionAsync(ct);
        var nowUtc = DateTime.UtcNow;

        var votacao = new Votacao
        {
            Titulo = req.Titulo.Trim(),
            Descricao = req.Descricao?.Trim(),
            Escopo = req.Escopo,
            UnidadeId = req.Escopo == EscopoVotacao.Unidade ? req.UnidadeId : null,
            InicioUtc = inicioUtc,
            FimPrevistoUtc = fimUtc,
            Estado = EstadoVotacao.Programada,
            CriadaPorUsuarioId = user.UsuarioId,
            CriadaEmUtc = nowUtc
        };
        _db.Votacoes.Add(votacao);
        await _db.SaveChangesAsync(ct);

        var opcoesLimpas = req.Opcoes.Where(o => !string.IsNullOrWhiteSpace(o)).Select(o => o.Trim()).ToList();
        for (var i = 0; i < opcoesLimpas.Count; i++)
        {
            _db.OpcoesVotacao.Add(new OpcaoVotacao
            {
                VotacaoId = votacao.Id,
                Texto = opcoesLimpas[i],
                Ordem = i + 1
            });
        }
        await _db.SaveChangesAsync(ct);

        if (req.AbrirImediatamente || inicioUtc <= nowUtc)
        {
            await CongelarSnapshotEAbrirInternoAsync(votacao, nowUtc, ct);
        }

        _auditoria.Registrar(user.UsuarioId, votacao.UnidadeId, "CRIAR_VOTACAO", nameof(Votacao), votacao.Id,
            $"Votação '{votacao.Titulo}' ({votacao.Escopo}) criada com estado {votacao.Estado}.");
        await _db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);

        return await ObterVotacaoPorIdAsync(user, votacao.Id, ct);
    }

    public async Task<VotacaoDto> AbrirVotacaoAsync(
        AuthenticatedUserInfo user,
        int votacaoId,
        CancellationToken ct = default)
    {
        var sem = VotacaoLocks.GetOrAdd(votacaoId, _ => new SemaphoreSlim(1, 1));
        await sem.WaitAsync(ct);
        try
        {
            await using var tx = await _db.Database.BeginTransactionAsync(ct);
            var votacao = await _db.Votacoes.FirstOrDefaultAsync(v => v.Id == votacaoId, ct)
                ?? throw new ApiException(StatusCodes.Status404NotFound, CodigosErro.RecursoNaoEncontrado, "Votação não encontrada.");

            ValidarEscopoAdministrativoVotacao(user, votacao);

            if (votacao.Estado == EstadoVotacao.Aberta)
            {
                await tx.CommitAsync(ct);
                return await ObterVotacaoPorIdAsync(user, votacao.Id, ct);
            }

            if (votacao.Estado != EstadoVotacao.Programada)
                throw new ApiException(StatusCodes.Status422UnprocessableEntity, CodigosErro.DadosInvalidos, "Apenas votações programadas podem ser abertas.");

            await CongelarSnapshotEAbrirInternoAsync(votacao, DateTime.UtcNow, ct);
            _auditoria.Registrar(user.UsuarioId, votacao.UnidadeId, "ABRIR_VOTACAO", nameof(Votacao), votacao.Id,
                $"Votação #{votacao.Id} aberta e snapshot de eleitores congelado.");
            await _db.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);

            return await ObterVotacaoPorIdAsync(user, votacao.Id, ct);
        }
        finally
        {
            sem.Release();
        }
    }

    public async Task<VotacaoDto> RegistrarVotoAsync(
        AuthenticatedUserInfo user,
        int votacaoId,
        RegistrarVotoRequest req,
        CancellationToken ct = default)
    {
        // Regra D2 e 12.1: Dependente, Simpatizante e Convidado não votam
        if (user.EhDependenteAtivo || !user.AssociadoId.HasValue)
            throw new ApiException(StatusCodes.Status403Forbidden, CodigosErro.VotacaoNaoElegivel, "Seu perfil não possui direito a voto.");

        var sem = VotacaoLocks.GetOrAdd(votacaoId, _ => new SemaphoreSlim(1, 1));
        await sem.WaitAsync(ct);
        try
        {
            await using var tx = await _db.Database.BeginTransactionAsync(ct);
            var nowUtc = DateTime.UtcNow;

            var votacao = await _db.Votacoes
                .Include(v => v.Opcoes)
                .FirstOrDefaultAsync(v => v.Id == votacaoId, ct)
                ?? throw new ApiException(StatusCodes.Status404NotFound, CodigosErro.RecursoNaoEncontrado, "Votação não encontrada.");

            if (votacao.Estado != EstadoVotacao.Aberta || nowUtc > votacao.FimPrevistoUtc)
                throw new ApiException(StatusCodes.Status422UnprocessableEntity, CodigosErro.VotacaoForaPeriodo, "Esta votação não está aberta para recebimento de votos.");

            var eleitor = await _db.EleitoresVotacao
                .FirstOrDefaultAsync(ev => ev.VotacaoId == votacao.Id && ev.AssociadoId == user.AssociadoId.Value, ct);

            if (eleitor is null)
            {
                throw new ApiException(StatusCodes.Status403Forbidden, CodigosErro.VotacaoNaoElegivel,
                    "Você não consta na lista de eleitores aptos congelada na abertura desta votação.");
            }

            var opcao = votacao.Opcoes.FirstOrDefault(o => o.Id == req.OpcaoVotacaoId);
            if (opcao is null)
                throw new ApiException(StatusCodes.Status400BadRequest, CodigosErro.DadosInvalidos, "A opção escolhida não pertence a esta votação.");

            var jaVotou = await _db.Votos.AnyAsync(v => v.EleitorVotacaoId == eleitor.Id, ct);
            if (jaVotou)
                throw new ApiException(StatusCodes.Status409Conflict, CodigosErro.VotoJaRegistrado, "Você já registrou seu voto nesta votação.");

            var voto = new Voto
            {
                VotacaoId = votacao.Id,
                EleitorVotacaoId = eleitor.Id,
                OpcaoVotacaoId = opcao.Id,
                RegistradoEmUtc = nowUtc
            };

            _db.Votos.Add(voto);
            await _db.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);

            return await ObterVotacaoPorIdAsync(user, votacao.Id, ct);
        }
        finally
        {
            sem.Release();
        }
    }

    public async Task<VotacaoDto> EncerrarVotacaoAsync(
        AuthenticatedUserInfo user,
        int votacaoId,
        CancellationToken ct = default)
    {
        var sem = VotacaoLocks.GetOrAdd(votacaoId, _ => new SemaphoreSlim(1, 1));
        await sem.WaitAsync(ct);
        try
        {
            await using var tx = await _db.Database.BeginTransactionAsync(ct);
            var votacao = await _db.Votacoes.FirstOrDefaultAsync(v => v.Id == votacaoId, ct)
                ?? throw new ApiException(StatusCodes.Status404NotFound, CodigosErro.RecursoNaoEncontrado, "Votação não encontrada.");

            ValidarEscopoAdministrativoVotacao(user, votacao);

            if (votacao.Estado == EstadoVotacao.Encerrada)
            {
                await tx.CommitAsync(ct);
                return await ObterVotacaoPorIdAsync(user, votacao.Id, ct);
            }

            if (votacao.Estado == EstadoVotacao.Cancelada)
                throw new ApiException(StatusCodes.Status422UnprocessableEntity, CodigosErro.DadosInvalidos, "Votação cancelada não pode ser encerrada com publicação de resultados.");

            var nowUtc = DateTime.UtcNow;
            votacao.Estado = EstadoVotacao.Encerrada;
            votacao.EncerradaEmUtc = nowUtc;
            votacao.ResultadosDivulgadosEmUtc = nowUtc;

            _auditoria.Registrar(user.UsuarioId, votacao.UnidadeId, "ENCERRAR_VOTACAO", nameof(Votacao), votacao.Id,
                $"Votação #{votacao.Id} encerrada e resultados divulgados.");

            await _db.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);

            return await ObterVotacaoPorIdAsync(user, votacao.Id, ct);
        }
        finally
        {
            sem.Release();
        }
    }

    public async Task<VotacaoDto> CancelarVotacaoAsync(
        AuthenticatedUserInfo user,
        int votacaoId,
        CancelarVotacaoRequest req,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(req.Justificativa))
            throw new ApiException(StatusCodes.Status400BadRequest, CodigosErro.DadosInvalidos, "O cancelamento de uma votação exige justificativa.");

        var sem = VotacaoLocks.GetOrAdd(votacaoId, _ => new SemaphoreSlim(1, 1));
        await sem.WaitAsync(ct);
        try
        {
            await using var tx = await _db.Database.BeginTransactionAsync(ct);
            var votacao = await _db.Votacoes.FirstOrDefaultAsync(v => v.Id == votacaoId, ct)
                ?? throw new ApiException(StatusCodes.Status404NotFound, CodigosErro.RecursoNaoEncontrado, "Votação não encontrada.");

            ValidarEscopoAdministrativoVotacao(user, votacao);

            if (votacao.Estado == EstadoVotacao.Cancelada)
            {
                await tx.CommitAsync(ct);
                return await ObterVotacaoPorIdAsync(user, votacao.Id, ct);
            }

            votacao.Estado = EstadoVotacao.Cancelada;
            votacao.JustificativaCancelamento = req.Justificativa.Trim();
            votacao.ResultadosDivulgadosEmUtc = null; // Regra 12.2: Cancelamento não divulga resultado oficial

            _auditoria.Registrar(user.UsuarioId, votacao.UnidadeId, "CANCELAR_VOTACAO", nameof(Votacao), votacao.Id,
                $"Votação #{votacao.Id} cancelada. Justificativa registrada.");

            await _db.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);

            return await ObterVotacaoPorIdAsync(user, votacao.Id, ct);
        }
        finally
        {
            sem.Release();
        }
    }

    public async Task<ResultadoVotacaoDto> ObterResultadosAgregadosAsync(
        AuthenticatedUserInfo user,
        int votacaoId,
        CancellationToken ct = default)
    {
        var votacao = await _db.Votacoes
            .AsNoTracking()
            .Include(v => v.Opcoes)
            .FirstOrDefaultAsync(v => v.Id == votacaoId, ct)
            ?? throw new ApiException(StatusCodes.Status404NotFound, CodigosErro.RecursoNaoEncontrado, "Votação não encontrada.");

        // Regra 12.2 e 12.3: Somente votações encerradas com resultados divulgados expõem resultados oficiais
        if (votacao.Estado != EstadoVotacao.Encerrada || !votacao.ResultadosDivulgadosEmUtc.HasValue)
        {
            throw new ApiException(StatusCodes.Status422UnprocessableEntity, CodigosErro.DadosInvalidos,
                "Os resultados consolidados só ficam disponíveis após o encerramento e divulgação oficial da votação.");
        }

        var ehAdminAutorizado = user.Role == RoleAdministrativa.SuperAdmin
                                || (user.Role == RoleAdministrativa.Admin && votacao.Escopo == EscopoVotacao.Unidade && votacao.UnidadeId == user.UnidadeAdministrativaId);

        if (!ehAdminAutorizado)
        {
            if (!user.AssociadoId.HasValue)
                throw new ApiException(StatusCodes.Status403Forbidden, CodigosErro.VotacaoNaoElegivel, "Acesso aos resultados permitido apenas aos eleitores elegíveis desta votação.");

            var integraSnapshot = await _db.EleitoresVotacao
                .AnyAsync(ev => ev.VotacaoId == votacao.Id && ev.AssociadoId == user.AssociadoId.Value, ct);

            if (!integraSnapshot)
                throw new ApiException(StatusCodes.Status403Forbidden, CodigosErro.VotacaoNaoElegivel, "Acesso aos resultados permitido apenas aos eleitores elegíveis desta votação.");
        }

        var totalEleitores = await _db.EleitoresVotacao.CountAsync(ev => ev.VotacaoId == votacao.Id, ct);
        var votosPorOpcao = await _db.Votos
            .Where(v => v.VotacaoId == votacao.Id)
            .GroupBy(v => v.OpcaoVotacaoId)
            .Select(g => new { OpcaoId = g.Key, Qtde = g.Count() })
            .ToDictionaryAsync(x => x.OpcaoId, x => x.Qtde, ct);

        var totalVotos = votosPorOpcao.Values.Sum();

        var opcoesDto = votacao.Opcoes
            .OrderBy(o => o.Ordem)
            .Select(o =>
            {
                var qtde = votosPorOpcao.GetValueOrDefault(o.Id, 0);
                var perc = totalVotos == 0 ? 0m : Math.Round((decimal)qtde * 100m / totalVotos, 2);
                return new ResultadoOpcaoDto(o.Id, o.Texto, o.Ordem, qtde, perc);
            })
            .ToList();

        return new ResultadoVotacaoDto(
            votacao.Id,
            votacao.Titulo,
            votacao.Estado,
            TimeZoneHelper.ToSaoPauloOffset(votacao.ResultadosDivulgadosEmUtc.Value),
            totalEleitores,
            totalVotos,
            opcoesDto);
    }

    public async Task<IReadOnlyList<VotoIndividualAuditoriaDto>> ConsultarVotosIndividuaisAsync(
        AuthenticatedUserInfo user,
        int votacaoId,
        CancellationToken ct = default)
    {
        // Regra 4.1, 12.3 e D7: Tesoureiro NUNCA consulta votos individuais; Admin apenas votação local da sua unidade; Super Admin todas.
        if (user.Role == RoleAdministrativa.Tesoureiro || user.Role == RoleAdministrativa.Nenhuma)
        {
            throw new ApiException(StatusCodes.Status403Forbidden, CodigosErro.VotosIndividuaisAcessoNegado,
                "Acesso negado: a identificação de votos individuais é restrita ao Super Admin e ao Admin da unidade responsável.");
        }

        var votacao = await _db.Votacoes.AsNoTracking().FirstOrDefaultAsync(v => v.Id == votacaoId, ct)
            ?? throw new ApiException(StatusCodes.Status404NotFound, CodigosErro.RecursoNaoEncontrado, "Votação não encontrada.");

        if (user.Role == RoleAdministrativa.Admin)
        {
            if (votacao.Escopo != EscopoVotacao.Unidade || votacao.UnidadeId != user.UnidadeAdministrativaId)
            {
                throw new ApiException(StatusCodes.Status403Forbidden, CodigosErro.VotosIndividuaisAcessoNegado,
                    "O Admin da unidade só pode consultar votos individualizados de votações locais da sua própria unidade.");
            }
        }

        var votos = await _db.Votos
            .AsNoTracking()
            .Include(v => v.EleitorVotacao)
                .ThenInclude(ev => ev.Pessoa)
            .Include(v => v.EleitorVotacao)
                .ThenInclude(ev => ev.UnidadeNaAbertura)
            .Include(v => v.EleitorVotacao)
                .ThenInclude(ev => ev.CategoriaNaAbertura)
            .Include(v => v.OpcaoVotacao)
            .Where(v => v.VotacaoId == votacaoId)
            .OrderBy(v => v.EleitorVotacao.Pessoa.NomeCompleto)
            .ToListAsync(ct);

        _auditoria.Registrar(user.UsuarioId, votacao.UnidadeId, "CONSULTAR_VOTOS_INDIVIDUAIS", nameof(Votacao), votacao.Id,
            $"Consulta de auditoria aos votos individuais da votação #{votacao.Id}.");
        await _db.SaveChangesAsync(ct);

        return votos.Select(v => new VotoIndividualAuditoriaDto(
            VotoId: v.Id,
            AssociadoId: v.EleitorVotacao.AssociadoId,
            PessoaId: v.EleitorVotacao.PessoaId,
            EleitorNome: v.EleitorVotacao.Pessoa.NomeCompleto,
            CpfMascarado: CpfHelper.Mask(v.EleitorVotacao.Pessoa.Cpf),
            UnidadeNaAberturaNome: v.EleitorVotacao.UnidadeNaAbertura.Nome,
            CategoriaNaAberturaNome: v.EleitorVotacao.CategoriaNaAbertura.Nome,
            OpcaoVotacaoId: v.OpcaoVotacaoId,
            OpcaoEscolhidaTexto: v.OpcaoVotacao.Texto,
            RegistradoEm: TimeZoneHelper.ToSaoPauloOffset(v.RegistradoEmUtc))).ToList();
    }

    public async Task<IReadOnlyList<VotacaoDto>> ListarVotacoesAsync(
        AuthenticatedUserInfo user,
        int? unidadeId = null,
        CancellationToken ct = default)
    {
        if (user.Role == RoleAdministrativa.Tesoureiro && !user.AssociadoId.HasValue)
            return Array.Empty<VotacaoDto>();

        var query = _db.Votacoes
            .AsNoTracking()
            .Include(v => v.Unidade)
            .Include(v => v.Opcoes)
            .AsQueryable();

        if (user.Role == RoleAdministrativa.SuperAdmin)
        {
            if (unidadeId.HasValue)
                query = query.Where(v => v.UnidadeId == unidadeId.Value || v.Escopo == EscopoVotacao.Global);
        }
        else if (user.Role == RoleAdministrativa.Admin)
        {
            query = query.Where(v => v.UnidadeId == user.UnidadeAdministrativaId || v.Escopo == EscopoVotacao.Global);
        }
        else if (user.AssociadoId.HasValue && !user.EhDependenteAtivo)
        {
            var assocId = user.AssociadoId.Value;
            query = query.Where(v => _db.EleitoresVotacao.Any(ev => ev.VotacaoId == v.Id && ev.AssociadoId == assocId));
        }
        else
        {
            return Array.Empty<VotacaoDto>();
        }

        var votacoes = await query.OrderByDescending(v => v.InicioUtc).ToListAsync(ct);
        var resultado = new List<VotacaoDto>(votacoes.Count);
        foreach (var v in votacoes)
        {
            resultado.Add(await MapVotacaoDtoAsync(user, v, ct));
        }
        return resultado;
    }

    public async Task<VotacaoDto> ObterVotacaoPorIdAsync(
        AuthenticatedUserInfo user,
        int votacaoId,
        CancellationToken ct = default)
    {
        var votacao = await _db.Votacoes
            .AsNoTracking()
            .Include(v => v.Unidade)
            .Include(v => v.Opcoes)
            .FirstOrDefaultAsync(v => v.Id == votacaoId, ct)
            ?? throw new ApiException(StatusCodes.Status404NotFound, CodigosErro.RecursoNaoEncontrado, "Votação não encontrada.");

        return await MapVotacaoDtoAsync(user, votacao, ct);
    }

    public async Task<(int Abertas, int Encerradas)> ProcessarCicloAutomaticoVotacoesAsync(CancellationToken ct = default)
    {
        var nowUtc = DateTime.UtcNow;
        var abertasCount = 0;
        var encerradasCount = 0;

        var paraAbrir = await _db.Votacoes
            .Where(v => v.Estado == EstadoVotacao.Programada && v.InicioUtc <= nowUtc && v.FimPrevistoUtc > nowUtc)
            .Select(v => v.Id)
            .ToListAsync(ct);

        foreach (var vId in paraAbrir)
        {
            var sem = VotacaoLocks.GetOrAdd(vId, _ => new SemaphoreSlim(1, 1));
            await sem.WaitAsync(ct);
            try
            {
                await using var tx = await _db.Database.BeginTransactionAsync(ct);
                var v = await _db.Votacoes.FirstOrDefaultAsync(x => x.Id == vId, ct);
                if (v is not null && v.Estado == EstadoVotacao.Programada)
                {
                    await CongelarSnapshotEAbrirInternoAsync(v, nowUtc, ct);
                    await _db.SaveChangesAsync(ct);
                    await tx.CommitAsync(ct);
                    abertasCount++;
                }
            }
            finally
            {
                sem.Release();
            }
        }

        var paraEncerrar = await _db.Votacoes
            .Where(v => v.Estado == EstadoVotacao.Aberta && v.FimPrevistoUtc <= nowUtc)
            .Select(v => v.Id)
            .ToListAsync(ct);

        foreach (var vId in paraEncerrar)
        {
            var sem = VotacaoLocks.GetOrAdd(vId, _ => new SemaphoreSlim(1, 1));
            await sem.WaitAsync(ct);
            try
            {
                await using var tx = await _db.Database.BeginTransactionAsync(ct);
                var v = await _db.Votacoes.FirstOrDefaultAsync(x => x.Id == vId, ct);
                if (v is not null && v.Estado == EstadoVotacao.Aberta)
                {
                    v.Estado = EstadoVotacao.Encerrada;
                    v.EncerradaEmUtc = nowUtc;
                    v.ResultadosDivulgadosEmUtc = nowUtc;
                    _auditoria.Registrar(null, v.UnidadeId, "ENCERRAR_VOTACAO_AUTOMATICO", nameof(Votacao), v.Id,
                        $"Votação #{v.Id} encerrada automaticamente no prazo.");
                    await _db.SaveChangesAsync(ct);
                    await tx.CommitAsync(ct);
                    encerradasCount++;
                }
            }
            finally
            {
                sem.Release();
            }
        }

        return (abertasCount, encerradasCount);
    }

    private async Task CongelarSnapshotEAbrirInternoAsync(Votacao votacao, DateTime nowUtc, CancellationToken ct)
    {
        var queryAssociadosAptos = _db.Associados
            .Include(a => a.CategoriaAssociado)
            .Include(a => a.Pessoa)
                .ThenInclude(p => p.Usuario)
            .Where(a => a.Status == StatusAssociado.Ativo && a.CategoriaAssociado.TemDireitoVoto);

        if (votacao.Escopo == EscopoVotacao.Unidade && votacao.UnidadeId.HasValue)
            queryAssociadosAptos = queryAssociadosAptos.Where(a => a.UnidadeId == votacao.UnidadeId.Value);

        var aptos = await queryAssociadosAptos.ToListAsync(ct);
        var jaNoSnapshot = (await _db.EleitoresVotacao
            .Where(ev => ev.VotacaoId == votacao.Id)
            .Select(ev => ev.AssociadoId)
            .ToListAsync(ct)).ToHashSet();

        foreach (var assoc in aptos)
        {
            if (jaNoSnapshot.Contains(assoc.Id))
                continue;

            _db.EleitoresVotacao.Add(new EleitorVotacao
            {
                VotacaoId = votacao.Id,
                AssociadoId = assoc.Id,
                PessoaId = assoc.PessoaId,
                UnidadeIdNaAbertura = assoc.UnidadeId,
                CategoriaIdNaAbertura = assoc.CategoriaAssociadoId,
                CriadoEmUtc = nowUtc
            });

            if (assoc.Pessoa.Usuario is not null)
            {
                await _notificacao.CriarNotificacaoAsync(
                    assoc.Pessoa.Usuario.Id,
                    TipoNotificacao.VotacaoAberta,
                    $"Votação aberta: {votacao.Titulo}",
                    $"Você está apto a votar em '{votacao.Titulo}'. Prazo até {TimeZoneHelper.ToSaoPauloOffset(votacao.FimPrevistoUtc):dd/MM/yyyy HH:mm}.",
                    "Votacao",
                    votacao.Id,
                    $"VOT_ABERTA_{votacao.Id}_USR_{assoc.Pessoa.Usuario.Id}",
                    ct);
            }
        }

        await _db.SaveChangesAsync(ct);
        votacao.SnapshotConcluidoEmUtc = nowUtc;
        votacao.Estado = EstadoVotacao.Aberta;
    }

    private void ValidarEscopoAdministrativoVotacao(AuthenticatedUserInfo user, Votacao votacao)
    {
        if (votacao.Escopo == EscopoVotacao.Global)
        {
            _currentUser.EnsureSuperAdmin(user);
        }
        else if (votacao.UnidadeId.HasValue)
        {
            _currentUser.EnsureAdminOrSuperAdminForUnit(user, votacao.UnidadeId.Value);
        }
    }

    private async Task<VotacaoDto> MapVotacaoDtoAsync(AuthenticatedUserInfo user, Votacao v, CancellationToken ct)
    {
        var totalEleitores = await _db.EleitoresVotacao.CountAsync(ev => ev.VotacaoId == v.Id, ct);
        var totalVotos = await _db.Votos.CountAsync(vt => vt.VotacaoId == v.Id, ct);

        var elegivel = false;
        var jaVotou = false;
        if (user.AssociadoId.HasValue)
        {
            var eleitor = await _db.EleitoresVotacao
                .AsNoTracking()
                .FirstOrDefaultAsync(ev => ev.VotacaoId == v.Id && ev.AssociadoId == user.AssociadoId.Value, ct);
            if (eleitor is not null)
            {
                elegivel = true;
                jaVotou = await _db.Votos.AnyAsync(vt => vt.EleitorVotacaoId == eleitor.Id, ct);
            }
        }

        return new VotacaoDto(
            Id: v.Id,
            Titulo: v.Titulo,
            Descricao: v.Descricao,
            Escopo: v.Escopo,
            UnidadeId: v.UnidadeId,
            UnidadeNome: v.Unidade?.Nome,
            Inicio: TimeZoneHelper.ToSaoPauloOffset(v.InicioUtc),
            FimPrevisto: TimeZoneHelper.ToSaoPauloOffset(v.FimPrevistoUtc),
            Estado: v.Estado,
            SnapshotConcluidoEm: v.SnapshotConcluidoEmUtc.HasValue ? TimeZoneHelper.ToSaoPauloOffset(v.SnapshotConcluidoEmUtc.Value) : null,
            EncerradaEm: v.EncerradaEmUtc.HasValue ? TimeZoneHelper.ToSaoPauloOffset(v.EncerradaEmUtc.Value) : null,
            ResultadosDivulgadosEm: v.ResultadosDivulgadosEmUtc.HasValue ? TimeZoneHelper.ToSaoPauloOffset(v.ResultadosDivulgadosEmUtc.Value) : null,
            JustificativaCancelamento: v.JustificativaCancelamento,
            TotalEleitoresAptos: totalEleitores,
            TotalVotosRegistrados: totalVotos,
            UsuarioAtualElegivel: elegivel,
            UsuarioAtualJaVotou: jaVotou,
            Opcoes: v.Opcoes.OrderBy(o => o.Ordem).Select(o => new OpcaoVotacaoDto(o.Id, o.Texto, o.Ordem)).ToList());
    }
}
