using Microsoft.EntityFrameworkCore;
using ProClubesApi.Authorization;
using ProClubesApi.Contracts;
using ProClubesApi.Data;
using ProClubesApi.Entities;

namespace ProClubesApi.Services;

public class AssociadoAndCadastroService
{
    private readonly ProClubesDbContext _db;
    private readonly CurrentUserService _currentUser;
    private readonly AuditoriaService _auditoria;
    private readonly NotificacaoService _notificacao;

    public AssociadoAndCadastroService(
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

    public async Task<StatusCadastroResponse> SolicitarCadastroAsync(SolicitarCadastroRequest req, CancellationToken ct = default)
    {
        var cpf = CpfHelper.Normalize(req.Cpf);
        if (!CpfHelper.IsValidFormat(cpf))
            throw new ApiException(StatusCodes.Status400BadRequest, CodigosErro.CpfInvalido, "CPF inválido; informe 11 dígitos numéricos.");

        if (string.IsNullOrWhiteSpace(req.NomeCompleto)
            || string.IsNullOrWhiteSpace(req.Sexo)
            || string.IsNullOrWhiteSpace(req.EstadoCivil)
            || (!string.IsNullOrWhiteSpace(req.UF) && req.UF.Trim().Length != 2)
            || string.IsNullOrWhiteSpace(req.Fone)
            || string.IsNullOrWhiteSpace(req.Email)
            || string.IsNullOrWhiteSpace(req.Senha)
            || req.Senha.Length < 6)
        {
            throw new ApiException(StatusCodes.Status400BadRequest, CodigosErro.DadosInvalidos, "Todos os dados cadastrais obrigatórios devem ser informados e a senha deve ter no mínimo 6 caracteres.");
        }

        var unidade = await _db.Unidades.FirstOrDefaultAsync(u => u.Id == req.UnidadePretendidaId && u.Ativa, ct);
        if (unidade is null)
            throw new ApiException(StatusCodes.Status404NotFound, CodigosErro.RecursoNaoEncontrado, "Unidade selecionada não encontrada ou inativa.");

        await using var tx = await _db.Database.BeginTransactionAsync(ct);
        var nowUtc = DateTime.UtcNow;

        var pessoaExistente = await _db.Pessoas
            .Include(p => p.Usuario)
            .Include(p => p.Associado)
            .FirstOrDefaultAsync(p => p.Cpf == cpf, ct);

        Pessoa pessoa;
        Usuario usuario;

        if (pessoaExistente is not null)
        {
            var temPendente = await _db.SolicitacoesCadastro
                .AnyAsync(s => s.PessoaId == pessoaExistente.Id && s.Status == StatusSolicitacaoCadastro.Pendente, ct);
            if (temPendente)
                throw new ApiException(StatusCodes.Status409Conflict, CodigosErro.SolicitacaoJaPendente, "Já existe uma solicitação de cadastro pendente para este CPF.");

            if (pessoaExistente.Associado is not null && pessoaExistente.Associado.Status == StatusAssociado.Ativo && pessoaExistente.Usuario?.EstadoConta == EstadoConta.Ativa)
                throw new ApiException(StatusCodes.Status409Conflict, CodigosErro.CpfJaCadastrado, "Este CPF já possui cadastro ativo no clube.");

            // Atualiza dados pessoais preservando identidade única (Regra 6.2 item 6 e D8)
            pessoa = pessoaExistente;
            pessoa.NomeCompleto = req.NomeCompleto.Trim();
            pessoa.Sexo = req.Sexo.Trim();
            pessoa.EstadoCivil = req.EstadoCivil.Trim();
            pessoa.DataNascimento = req.DataNascimento;
            pessoa.Logradouro = (req.Logradouro ?? string.Empty).Trim();
            pessoa.Bairro = (req.Bairro ?? string.Empty).Trim();
            pessoa.Numero = (req.Numero ?? string.Empty).Trim();
            pessoa.Cidade = (req.Cidade ?? string.Empty).Trim();
            pessoa.UF = (req.UF ?? string.Empty).Trim().ToUpperInvariant();
            pessoa.Fone = req.Fone.Trim();
            pessoa.Email = req.Email.Trim();
            pessoa.AtualizadaEmUtc = nowUtc;

            if (pessoa.Usuario is not null)
            {
                usuario = pessoa.Usuario;
                usuario.SenhaHash = PasswordHasherHelper.HashPassword(req.Senha);
                usuario.EstadoConta = EstadoConta.RestritaCadastro;
                usuario.VersaoSeguranca++;
            }
            else
            {
                usuario = new Usuario
                {
                    PessoaId = pessoa.Id,
                    SenhaHash = PasswordHasherHelper.HashPassword(req.Senha),
                    RoleAdministrativa = RoleAdministrativa.Nenhuma,
                    EstadoConta = EstadoConta.RestritaCadastro,
                    VersaoSeguranca = 1,
                    CriadoEmUtc = nowUtc
                };
                _db.Usuarios.Add(usuario);
                await _db.SaveChangesAsync(ct);
            }
        }
        else
        {
            pessoa = new Pessoa
            {
                NomeCompleto = req.NomeCompleto.Trim(),
                Cpf = cpf,
                Sexo = req.Sexo.Trim(),
                EstadoCivil = req.EstadoCivil.Trim(),
                DataNascimento = req.DataNascimento,
                Logradouro = (req.Logradouro ?? string.Empty).Trim(),
                Bairro = (req.Bairro ?? string.Empty).Trim(),
                Numero = (req.Numero ?? string.Empty).Trim(),
                Cidade = (req.Cidade ?? string.Empty).Trim(),
                UF = (req.UF ?? string.Empty).Trim().ToUpperInvariant(),
                Fone = req.Fone.Trim(),
                Email = req.Email.Trim(),
                CriadaEmUtc = nowUtc,
                AtualizadaEmUtc = nowUtc
            };
            _db.Pessoas.Add(pessoa);
            await _db.SaveChangesAsync(ct);

            usuario = new Usuario
            {
                PessoaId = pessoa.Id,
                SenhaHash = PasswordHasherHelper.HashPassword(req.Senha),
                RoleAdministrativa = RoleAdministrativa.Nenhuma,
                EstadoConta = EstadoConta.RestritaCadastro,
                VersaoSeguranca = 1,
                CriadoEmUtc = nowUtc
            };
            _db.Usuarios.Add(usuario);
            await _db.SaveChangesAsync(ct);
        }

        await AtualizarInteressesPessoaAsync(pessoa.Id, req.InteressesIds, ct);

        var categoriaPretendida = await ResolverCategoriaPretendidaAsync(req.CodigoCategoriaPretendida, ct);

        var solicitacao = new SolicitacaoCadastro
        {
            PessoaId = pessoa.Id,
            UsuarioId = usuario.Id,
            UnidadePretendidaId = unidade.Id,
            Status = StatusSolicitacaoCadastro.Pendente,
            CategoriaAprovadaId = categoriaPretendida?.Id,
            SolicitadaEmUtc = nowUtc
        };
        _db.SolicitacoesCadastro.Add(solicitacao);
        await _db.SaveChangesAsync(ct);

        await NotificarDiretoriaNovaSolicitacaoAsync(solicitacao, unidade, ct: ct);
        await _db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);

        return new StatusCadastroResponse(
            SolicitacaoId: solicitacao.Id,
            PessoaId: pessoa.Id,
            NomeCompleto: pessoa.NomeCompleto,
            Email: pessoa.Email,
            UnidadePretendidaId: unidade.Id,
            UnidadePretendidaNome: unidade.Nome,
            Status: solicitacao.Status,
            JustificativaRejeicao: null,
            CategoriaAprovadaNome: categoriaPretendida?.Nome,
            SolicitadaEm: TimeZoneHelper.ToSaoPauloOffset(solicitacao.SolicitadaEmUtc),
            DecididaEm: null);
    }

    public async Task<StatusCadastroResponse> ReenviarSolicitacaoRejeitadaAsync(
        AuthenticatedUserInfo user,
        ReenviarSolicitacaoCadastroRequest req,
        CancellationToken ct = default)
    {
        var temPendente = await _db.SolicitacoesCadastro
            .AnyAsync(s => s.PessoaId == user.PessoaId && s.Status == StatusSolicitacaoCadastro.Pendente, ct);
        if (temPendente)
            throw new ApiException(StatusCodes.Status409Conflict, CodigosErro.SolicitacaoJaPendente, "Você já possui uma solicitação pendente de análise.");

        var ultima = await _db.SolicitacoesCadastro
            .Include(s => s.CategoriaAprovada)
            .Where(s => s.PessoaId == user.PessoaId)
            .OrderByDescending(s => s.SolicitadaEmUtc)
            .FirstOrDefaultAsync(ct);

        if (ultima is null || ultima.Status != StatusSolicitacaoCadastro.Rejeitada)
            throw new ApiException(StatusCodes.Status422UnprocessableEntity, CodigosErro.DadosInvalidos, "Apenas solicitações rejeitadas podem ser corrigidas e reenviadas.");

        var unidade = await _db.Unidades.FirstOrDefaultAsync(u => u.Id == req.UnidadePretendidaId && u.Ativa, ct);
        if (unidade is null)
            throw new ApiException(StatusCodes.Status404NotFound, CodigosErro.RecursoNaoEncontrado, "Unidade selecionada não encontrada.");

        await using var tx = await _db.Database.BeginTransactionAsync(ct);
        var nowUtc = DateTime.UtcNow;

        var pessoa = await _db.Pessoas.FirstAsync(p => p.Id == user.PessoaId, ct);
        pessoa.NomeCompleto = req.NomeCompleto.Trim();
        pessoa.Sexo = req.Sexo.Trim();
        pessoa.EstadoCivil = req.EstadoCivil.Trim();
        pessoa.DataNascimento = req.DataNascimento;
        pessoa.Logradouro = (req.Logradouro ?? string.Empty).Trim();
        pessoa.Bairro = (req.Bairro ?? string.Empty).Trim();
        pessoa.Numero = (req.Numero ?? string.Empty).Trim();
        pessoa.Cidade = (req.Cidade ?? string.Empty).Trim();
        pessoa.UF = (req.UF ?? string.Empty).Trim().ToUpperInvariant();
        pessoa.Fone = req.Fone.Trim();
        pessoa.Email = req.Email.Trim();
        pessoa.AtualizadaEmUtc = nowUtc;

        await AtualizarInteressesPessoaAsync(pessoa.Id, req.InteressesIds, ct);

        var categoriaPretendida = !string.IsNullOrWhiteSpace(req.CodigoCategoriaPretendida)
            ? await ResolverCategoriaPretendidaAsync(req.CodigoCategoriaPretendida, ct)
            : (ultima.CategoriaAprovada ?? await ResolverCategoriaPretendidaAsync(null, ct));

        var novaSolicitacao = new SolicitacaoCadastro
        {
            PessoaId = pessoa.Id,
            UsuarioId = user.UsuarioId,
            UnidadePretendidaId = unidade.Id,
            Status = StatusSolicitacaoCadastro.Pendente,
            CategoriaAprovadaId = categoriaPretendida?.Id,
            SolicitadaEmUtc = nowUtc
        };
        _db.SolicitacoesCadastro.Add(novaSolicitacao);
        await _db.SaveChangesAsync(ct);

        await NotificarDiretoriaNovaSolicitacaoAsync(novaSolicitacao, unidade, ct: ct);
        await _db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);

        return new StatusCadastroResponse(
            SolicitacaoId: novaSolicitacao.Id,
            PessoaId: pessoa.Id,
            NomeCompleto: pessoa.NomeCompleto,
            Email: pessoa.Email,
            UnidadePretendidaId: unidade.Id,
            UnidadePretendidaNome: unidade.Nome,
            Status: novaSolicitacao.Status,
            JustificativaRejeicao: null,
            CategoriaAprovadaNome: categoriaPretendida?.Nome,
            SolicitadaEm: TimeZoneHelper.ToSaoPauloOffset(novaSolicitacao.SolicitadaEmUtc),
            DecididaEm: null);
    }

    public async Task<PagedResponse<SolicitacaoCadastroListItemDto>> ListarSolicitacoesAsync(
        AuthenticatedUserInfo user,
        int? unidadeId,
        StatusSolicitacaoCadastro? status,
        PaginationQuery pagination,
        CancellationToken ct = default)
    {
        if (user.Role != RoleAdministrativa.SuperAdmin && user.Role != RoleAdministrativa.Admin)
            throw new ApiException(StatusCodes.Status403Forbidden, CodigosErro.RecursoSemPermissao, "Apenas Admin da unidade ou Super Admin podem consultar solicitações de cadastro.");

        var query = _db.SolicitacoesCadastro
            .AsNoTracking()
            .Include(s => s.Pessoa)
            .Include(s => s.UnidadePretendida)
            .Include(s => s.CategoriaAprovada)
            .AsQueryable();

        if (user.Role == RoleAdministrativa.Admin)
        {
            if (unidadeId.HasValue && unidadeId.Value != user.UnidadeAdministrativaId)
                throw new ApiException(StatusCodes.Status403Forbidden, CodigosErro.UnidadeSemPermissao, "Acesso permitido apenas às solicitações da sua unidade.");

            query = query.Where(s => s.UnidadePretendidaId == user.UnidadeAdministrativaId);
        }
        else if (unidadeId.HasValue)
        {
            query = query.Where(s => s.UnidadePretendidaId == unidadeId.Value);
        }

        if (status.HasValue)
            query = query.Where(s => s.Status == status.Value);

        var total = await query.CountAsync(ct);
        var items = await query
            .OrderByDescending(s => s.SolicitadaEmUtc)
            .Skip(pagination.Skip)
            .Take(pagination.NormalizedPageSize)
            .ToListAsync(ct);

        var pessoaIds = items.Select(i => i.PessoaId).Distinct().ToList();
        var depsAtivas = pessoaIds.Count == 0
            ? new List<Dependencia>()
            : await _db.Dependencias
                .AsNoTracking()
                .Include(d => d.AssociadoTitular)
                    .ThenInclude(a => a.Pessoa)
                .Where(d => pessoaIds.Contains(d.PessoaDependenteId) && d.DataFim == null)
                .ToListAsync(ct);

        var dependenciasPorPessoa = depsAtivas
            .GroupBy(d => d.PessoaDependenteId)
            .ToDictionary(g => g.Key, g => g.OrderByDescending(d => d.Id).First());

        var dtos = items.Select(s =>
        {
            var categoriaOuVinculo = dependenciasPorPessoa.TryGetValue(s.PessoaId, out var dep)
                ? FormatarVinculoDependente(dep)
                : (s.CategoriaAprovada?.Nome ?? (s.Status == StatusSolicitacaoCadastro.Aprovada ? "Dependente" : null));

            return ToSolicitacaoListItemDto(s, categoriaOuVinculo);
        }).ToList();

        return new PagedResponse<SolicitacaoCadastroListItemDto>(dtos, total, pagination.NormalizedPage, pagination.NormalizedPageSize);
    }

    public async Task<SolicitacaoCadastroListItemDto> AprovarSolicitacaoAsync(
        AuthenticatedUserInfo user,
        int solicitacaoId,
        AprovarSolicitacaoRequest req,
        CancellationToken ct = default)
    {
        var solicitacao = await _db.SolicitacoesCadastro
            .Include(s => s.Pessoa)
            .Include(s => s.Usuario)
            .Include(s => s.UnidadePretendida)
            .Include(s => s.CategoriaAprovada)
            .FirstOrDefaultAsync(s => s.Id == solicitacaoId, ct);

        if (solicitacao is null)
            throw new ApiException(StatusCodes.Status404NotFound, CodigosErro.RecursoNaoEncontrado, "Solicitação não encontrada.");

        _currentUser.EnsureAdminOrSuperAdminForUnit(user, solicitacao.UnidadePretendidaId);

        if (solicitacao.Status != StatusSolicitacaoCadastro.Pendente)
            throw new ApiException(StatusCodes.Status422UnprocessableEntity, CodigosErro.DadosInvalidos, "Esta solicitação já foi decidida.");

        // Verifica se é conta individual de dependente previamente vinculado (D8) ou solicitação enviada pelo titular
        var depAtivo = await _db.Dependencias
            .Include(d => d.AssociadoTitular)
                .ThenInclude(a => a.Pessoa)
            .FirstOrDefaultAsync(d => d.PessoaDependenteId == solicitacao.PessoaId && d.DataFim == null, ct);

        await using var tx = await _db.Database.BeginTransactionAsync(ct);
        var nowUtc = DateTime.UtcNow;
        var hoje = TimeZoneHelper.TodaySaoPaulo(nowUtc);

        CategoriaAssociado? categoria = null;
        var aprovarComoDependente = depAtivo is not null &&
            (string.IsNullOrWhiteSpace(req.CodigoCategoria) ||
             string.Equals(req.CodigoCategoria.Trim(), "DEPENDENTE", StringComparison.OrdinalIgnoreCase));

        if (aprovarComoDependente)
        {
            if (depAtivo!.AssociadoTitular.UnidadeId != solicitacao.UnidadePretendidaId)
                throw new ApiException(StatusCodes.Status422UnprocessableEntity, CodigosErro.DadosInvalidos, "A unidade da solicitação do dependente deve corresponder à unidade do titular.");
        }
        else
        {
            var cod = string.IsNullOrWhiteSpace(req.CodigoCategoria)
                ? (solicitacao.CategoriaAprovada?.Codigo ?? CodigosCategoriaAssociado.Associado)
                : req.CodigoCategoria.Trim().ToUpperInvariant();
            categoria = await _db.CategoriasAssociado.FirstOrDefaultAsync(c => c.Codigo == cod && c.Ativa, ct);
            if (categoria is null)
                throw new ApiException(StatusCodes.Status400BadRequest, CodigosErro.DadosInvalidos, "Categoria associativa inválida.");

            var associado = await _db.Associados.FirstOrDefaultAsync(a => a.PessoaId == solicitacao.PessoaId, ct);
            var inicioCobranca = categoria.IsentoMensalidade ? (DateOnly?)null : TimeZoneHelper.FirstDayOfNextMonth(hoje);

            if (associado is null)
            {
                associado = new Associado
                {
                    PessoaId = solicitacao.PessoaId,
                    UnidadeId = solicitacao.UnidadePretendidaId,
                    CategoriaAssociadoId = categoria.Id,
                    Status = StatusAssociado.Ativo,
                    DataIngresso = hoje,
                    DataInicioCobranca = inicioCobranca
                };
                _db.Associados.Add(associado);
            }
            else
            {
                associado.UnidadeId = solicitacao.UnidadePretendidaId;
                associado.CategoriaAssociadoId = categoria.Id;
                associado.Status = StatusAssociado.Ativo;
                associado.DataIngresso = hoje;
                associado.DataInicioCobranca = inicioCobranca;
                associado.DesativadoEmUtc = null;
            }
        }

        solicitacao.Status = StatusSolicitacaoCadastro.Aprovada;
        solicitacao.CategoriaAprovadaId = categoria?.Id;
        solicitacao.AvaliadorUsuarioId = user.UsuarioId;
        solicitacao.DecididaEmUtc = nowUtc;

        solicitacao.Usuario.EstadoConta = EstadoConta.Ativa;
        solicitacao.Usuario.VersaoSeguranca++;

        await _notificacao.CriarNotificacaoAsync(
            solicitacao.UsuarioId,
            TipoNotificacao.SolicitacaoCadastroAnalisada,
            "Cadastro aprovado!",
            aprovarComoDependente
                ? $"Seu vínculo como dependente na unidade {solicitacao.UnidadePretendida.Nome} foi aprovado."
                : $"Sua solicitação de cadastro na unidade {solicitacao.UnidadePretendida.Nome} foi aprovada.",
            "SolicitacaoCadastro",
            solicitacao.Id,
            $"CAD_APROV_{solicitacao.Id}",
            ct);

        _auditoria.Registrar(user.UsuarioId, solicitacao.UnidadePretendidaId, "APROVAR_CADASTRO", nameof(SolicitacaoCadastro), solicitacao.Id, $"Solicitação #{solicitacao.Id} aprovada.");
        await _db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);

        return ToSolicitacaoListItemDto(
            solicitacao,
            categoria?.Nome ?? (depAtivo is not null ? FormatarVinculoDependente(depAtivo) : "Dependente"),
            nowUtc);
    }

    public async Task<SolicitacaoCadastroListItemDto> RejeitarSolicitacaoAsync(
        AuthenticatedUserInfo user,
        int solicitacaoId,
        RejeitarSolicitacaoRequest req,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(req.Justificativa))
            throw new ApiException(StatusCodes.Status400BadRequest, CodigosErro.DadosInvalidos, "A justificativa de rejeição é obrigatória.");

        var solicitacao = await _db.SolicitacoesCadastro
            .Include(s => s.Pessoa)
            .Include(s => s.UnidadePretendida)
            .FirstOrDefaultAsync(s => s.Id == solicitacaoId, ct);

        if (solicitacao is null)
            throw new ApiException(StatusCodes.Status404NotFound, CodigosErro.RecursoNaoEncontrado, "Solicitação não encontrada.");

        _currentUser.EnsureAdminOrSuperAdminForUnit(user, solicitacao.UnidadePretendidaId);

        if (solicitacao.Status != StatusSolicitacaoCadastro.Pendente)
            throw new ApiException(StatusCodes.Status422UnprocessableEntity, CodigosErro.DadosInvalidos, "Esta solicitação já foi decidida.");

        var nowUtc = DateTime.UtcNow;
        solicitacao.Status = StatusSolicitacaoCadastro.Rejeitada;
        solicitacao.JustificativaRejeicao = req.Justificativa.Trim();
        solicitacao.AvaliadorUsuarioId = user.UsuarioId;
        solicitacao.DecididaEmUtc = nowUtc;

        var depPendente = await _db.Dependencias
            .FirstOrDefaultAsync(d => d.PessoaDependenteId == solicitacao.PessoaId && d.DataFim == null, ct);
        if (depPendente is not null)
        {
            depPendente.DataFim = TimeZoneHelper.TodaySaoPaulo(nowUtc);
            depPendente.MotivoFim = MotivoFimDependencia.Administrativo;
        }

        await _notificacao.CriarNotificacaoAsync(
            solicitacao.UsuarioId,
            TipoNotificacao.SolicitacaoCadastroAnalisada,
            "Solicitação de cadastro precisa de ajustes",
            $"Sua solicitação foi analisada. Motivo: {solicitacao.JustificativaRejeicao}",
            "SolicitacaoCadastro",
            solicitacao.Id,
            $"CAD_REJ_{solicitacao.Id}",
            ct);

        _auditoria.Registrar(user.UsuarioId, solicitacao.UnidadePretendidaId, "REJEITAR_CADASTRO", nameof(SolicitacaoCadastro), solicitacao.Id, $"Solicitação #{solicitacao.Id} rejeitada.");
        await _db.SaveChangesAsync(ct);

        return ToSolicitacaoListItemDto(solicitacao, null, nowUtc);
    }

    public async Task<PagedResponse<AssociadoDto>> ListarAssociadosAsync(
        AuthenticatedUserInfo user,
        int? unidadeId,
        string? codigoCategoria,
        StatusAssociado? status,
        string? busca,
        PaginationQuery pagination,
        CancellationToken ct = default)
    {
        if (user.Role != RoleAdministrativa.SuperAdmin && user.Role != RoleAdministrativa.Admin)
            throw new ApiException(StatusCodes.Status403Forbidden, CodigosErro.RecursoSemPermissao, "Sem permissão para listar associados.");

        var query = _db.Associados
            .AsNoTracking()
            .Include(a => a.Pessoa)
                .ThenInclude(p => p.Usuario)
            .Include(a => a.Pessoa)
                .ThenInclude(p => p.Interesses)
                    .ThenInclude(pi => pi.Interesse)
            .Include(a => a.Unidade)
            .Include(a => a.CategoriaAssociado)
            .Include(a => a.Dependentes)
                .ThenInclude(d => d.PessoaDependente)
                    .ThenInclude(pd => pd.Usuario)
            .AsQueryable();

        if (user.Role == RoleAdministrativa.Admin)
        {
            if (unidadeId.HasValue && unidadeId.Value != user.UnidadeAdministrativaId)
                throw new ApiException(StatusCodes.Status403Forbidden, CodigosErro.UnidadeSemPermissao, "Acesso restrito aos associados da sua unidade.");

            query = query.Where(a => a.UnidadeId == user.UnidadeAdministrativaId);
        }
        else if (unidadeId.HasValue)
        {
            query = query.Where(a => a.UnidadeId == unidadeId.Value);
        }

        if (!string.IsNullOrWhiteSpace(codigoCategoria))
        {
            var cod = codigoCategoria.Trim().ToUpperInvariant();
            query = query.Where(a => a.CategoriaAssociado.Codigo == cod);
        }

        if (status.HasValue)
            query = query.Where(a => a.Status == status.Value);

        if (!string.IsNullOrWhiteSpace(busca))
        {
            var b = busca.Trim().ToLower();
            var cpfBusca = CpfHelper.Normalize(busca);
            query = query.Where(a =>
                a.Pessoa.NomeCompleto.ToLower().Contains(b)
                || a.Pessoa.Email.ToLower().Contains(b)
                || (cpfBusca.Length > 0 && a.Pessoa.Cpf.Contains(cpfBusca)));
        }

        var total = await query.CountAsync(ct);
        var items = await query
            .OrderBy(a => a.Pessoa.NomeCompleto)
            .Skip(pagination.Skip)
            .Take(pagination.NormalizedPageSize)
            .ToListAsync(ct);

        var dtos = items.Select(ToAssociadoDto).ToList();
        return new PagedResponse<AssociadoDto>(dtos, total, pagination.NormalizedPage, pagination.NormalizedPageSize);
    }

    public async Task<AssociadoDto> ObterAssociadoPorIdAsync(AuthenticatedUserInfo user, int associadoId, CancellationToken ct = default)
    {
        var associado = await _db.Associados
            .AsNoTracking()
            .Include(a => a.Pessoa)
                .ThenInclude(p => p.Usuario)
            .Include(a => a.Pessoa)
                .ThenInclude(p => p.Interesses)
                    .ThenInclude(pi => pi.Interesse)
            .Include(a => a.Unidade)
            .Include(a => a.CategoriaAssociado)
            .Include(a => a.Dependentes)
                .ThenInclude(d => d.PessoaDependente)
                    .ThenInclude(pd => pd.Usuario)
            .FirstOrDefaultAsync(a => a.Id == associadoId, ct);

        if (associado is null)
            throw new ApiException(StatusCodes.Status404NotFound, CodigosErro.RecursoNaoEncontrado, "Associado não encontrado.");

        if (user.AssociadoId != associado.Id)
            _currentUser.EnsureAdminOrSuperAdminForUnit(user, associado.UnidadeId);

        return ToAssociadoDto(associado);
    }

    public async Task<AssociadoDto> CriarAssociadoAdminAsync(
        AuthenticatedUserInfo user,
        CriarAssociadoAdminRequest req,
        CancellationToken ct = default)
    {
        _currentUser.EnsureAdminOrSuperAdminForUnit(user, req.UnidadeId);

        if (req.RoleInicial != RoleAdministrativa.Nenhuma)
        {
            _currentUser.EnsureSuperAdmin(user);

            if (req.RoleInicial == RoleAdministrativa.SuperAdmin)
                throw new ApiException(StatusCodes.Status400BadRequest, CodigosErro.DadosInvalidos, "Role SuperAdmin não pode ser atribuída a um associado de unidade.");
        }

        var cpf = CpfHelper.Normalize(req.Cpf);
        if (!CpfHelper.IsValidFormat(cpf))
            throw new ApiException(StatusCodes.Status400BadRequest, CodigosErro.CpfInvalido, "CPF inválido.");

        if (await _db.Pessoas.AnyAsync(p => p.Cpf == cpf, ct))
            throw new ApiException(StatusCodes.Status409Conflict, CodigosErro.CpfJaCadastrado, "CPF já cadastrado.");

        var unidade = await _db.Unidades.FirstOrDefaultAsync(u => u.Id == req.UnidadeId && u.Ativa, ct)
            ?? throw new ApiException(StatusCodes.Status404NotFound, CodigosErro.RecursoNaoEncontrado, "Unidade não encontrada.");

        if (req.RoleInicial == RoleAdministrativa.Admin)
        {
            var adminExistente = await _db.Usuarios
                .FirstOrDefaultAsync(u => u.UnidadeAdministrativaId == unidade.Id
                                          && u.RoleAdministrativa == RoleAdministrativa.Admin
                                          && u.EstadoConta == EstadoConta.Ativa, ct);
            if (adminExistente is not null)
                throw new ApiException(StatusCodes.Status409Conflict, CodigosErro.UnidadeJaPossuiAdminAtivo, "Esta unidade já possui um Admin ativo. Remova ou substitua o titular antes de designar outro.");
        }

        var codCat = req.CodigoCategoria.Trim().ToUpperInvariant();
        var categoria = await _db.CategoriasAssociado.FirstOrDefaultAsync(c => c.Codigo == codCat && c.Ativa, ct)
            ?? throw new ApiException(StatusCodes.Status400BadRequest, CodigosErro.DadosInvalidos, "Categoria associativa inválida.");

        await using var tx = await _db.Database.BeginTransactionAsync(ct);
        var nowUtc = DateTime.UtcNow;
        var hoje = TimeZoneHelper.TodaySaoPaulo(nowUtc);

        if (!string.IsNullOrWhiteSpace(req.UF) && req.UF.Trim().Length != 2)
            throw new ApiException(StatusCodes.Status400BadRequest, CodigosErro.DadosInvalidos, "Quando informada, a UF deve conter 2 caracteres.");

        var pessoa = new Pessoa
        {
            NomeCompleto = req.NomeCompleto.Trim(),
            Cpf = cpf,
            Sexo = req.Sexo.Trim(),
            EstadoCivil = req.EstadoCivil.Trim(),
            DataNascimento = req.DataNascimento,
            Logradouro = (req.Logradouro ?? string.Empty).Trim(),
            Bairro = (req.Bairro ?? string.Empty).Trim(),
            Numero = (req.Numero ?? string.Empty).Trim(),
            Cidade = (req.Cidade ?? string.Empty).Trim(),
            UF = (req.UF ?? string.Empty).Trim().ToUpperInvariant(),
            Fone = req.Fone.Trim(),
            Email = req.Email.Trim(),
            CriadaEmUtc = nowUtc,
            AtualizadaEmUtc = nowUtc
        };
        _db.Pessoas.Add(pessoa);
        await _db.SaveChangesAsync(ct);

        await AtualizarInteressesPessoaAsync(pessoa.Id, req.InteressesIds, ct);

        var usuario = new Usuario
        {
            PessoaId = pessoa.Id,
            SenhaHash = PasswordHasherHelper.HashPassword(string.IsNullOrWhiteSpace(req.SenhaInicial) ? "ProClubes@123" : req.SenhaInicial),
            RoleAdministrativa = req.RoleInicial,
            UnidadeAdministrativaId = req.RoleInicial != RoleAdministrativa.Nenhuma ? unidade.Id : null,
            AdminAtivoUnidadeId = req.RoleInicial == RoleAdministrativa.Admin ? unidade.Id : null,
            EstadoConta = EstadoConta.Ativa,
            VersaoSeguranca = 1,
            CriadoEmUtc = nowUtc
        };
        _db.Usuarios.Add(usuario);

        var associado = new Associado
        {
            PessoaId = pessoa.Id,
            UnidadeId = unidade.Id,
            CategoriaAssociadoId = categoria.Id,
            Status = StatusAssociado.Ativo,
            DataIngresso = hoje,
            DataInicioCobranca = categoria.IsentoMensalidade ? null : TimeZoneHelper.FirstDayOfNextMonth(hoje)
        };
        _db.Associados.Add(associado);
        await _db.SaveChangesAsync(ct);

        var sufixoRole = req.RoleInicial != RoleAdministrativa.Nenhuma ? $" e designado como {req.RoleInicial}" : "";
        _auditoria.Registrar(user.UsuarioId, unidade.Id, "CRIAR_ASSOCIADO", nameof(Associado), associado.Id, $"Associado #{associado.Id} cadastrado na categoria {categoria.Codigo}{sufixoRole}.");
        await _db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);

        return await ObterAssociadoPorIdAsync(user, associado.Id, ct);
    }

    public async Task<AssociadoDto> AlterarCategoriaAssociadoAsync(
        AuthenticatedUserInfo user,
        int associadoId,
        AlterarCategoriaAssociadoRequest req,
        CancellationToken ct = default)
    {
        var associado = await _db.Associados
            .Include(a => a.CategoriaAssociado)
            .FirstOrDefaultAsync(a => a.Id == associadoId, ct)
            ?? throw new ApiException(StatusCodes.Status404NotFound, CodigosErro.RecursoNaoEncontrado, "Associado não encontrado.");

        _currentUser.EnsureAdminOrSuperAdminForUnit(user, associado.UnidadeId);

        if (associado.Status != StatusAssociado.Ativo)
            throw new ApiException(StatusCodes.Status422UnprocessableEntity, CodigosErro.DadosInvalidos, "Não é permitido alterar a categoria de um associado inativo.");

        var novoCod = req.NovoCodigoCategoria.Trim().ToUpperInvariant();
        var novaCat = await _db.CategoriasAssociado.FirstOrDefaultAsync(c => c.Codigo == novoCod && c.Ativa, ct)
            ?? throw new ApiException(StatusCodes.Status400BadRequest, CodigosErro.DadosInvalidos, "Nova categoria associativa inválida.");

        if (associado.CategoriaAssociadoId == novaCat.Id)
            return await ObterAssociadoPorIdAsync(user, associadoId, ct);

        await using var tx = await _db.Database.BeginTransactionAsync(ct);
        var nowUtc = DateTime.UtcNow;
        var hoje = TimeZoneHelper.TodaySaoPaulo(nowUtc);
        var proximaCompetencia = TimeZoneHelper.FirstDayOfNextMonth(hoje);

        var catAnteriorId = associado.CategoriaAssociadoId;
        var eraIsento = associado.CategoriaAssociado.IsentoMensalidade;

        associado.CategoriaAssociadoId = novaCat.Id;
        if (novaCat.IsentoMensalidade)
        {
            associado.DataInicioCobranca = null;
        }
        else if (eraIsento || !associado.DataInicioCobranca.HasValue || associado.DataInicioCobranca.Value < proximaCompetencia)
        {
            if (eraIsento)
                associado.DataInicioCobranca = proximaCompetencia;
        }

        _db.HistoricosCategoriaAssociado.Add(new HistoricoCategoriaAssociado
        {
            AssociadoId = associado.Id,
            CategoriaAnteriorId = catAnteriorId,
            CategoriaNovaId = novaCat.Id,
            VigenteApartir = proximaCompetencia,
            AlteradoPorUsuarioId = user.UsuarioId,
            AlteradoEmUtc = nowUtc,
            Motivo = req.Motivo
        });

        _auditoria.Registrar(user.UsuarioId, associado.UnidadeId, "ALTERAR_CATEGORIA_ASSOCIADO", nameof(Associado), associado.Id, $"Categoria alterada para {novaCat.Codigo} vigente a partir de {proximaCompetencia:yyyy-MM-dd}.");
        await _db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);

        return await ObterAssociadoPorIdAsync(user, associadoId, ct);
    }

    public async Task<AssociadoDto> AlterarStatusAssociadoAsync(
        AuthenticatedUserInfo user,
        int associadoId,
        AlterarStatusAssociadoRequest req,
        CancellationToken ct = default)
    {
        var associado = await _db.Associados
            .Include(a => a.Pessoa)
                .ThenInclude(p => p.Usuario)
            .FirstOrDefaultAsync(a => a.Id == associadoId, ct)
            ?? throw new ApiException(StatusCodes.Status404NotFound, CodigosErro.RecursoNaoEncontrado, "Associado não encontrado.");

        _currentUser.EnsureAdminOrSuperAdminForUnit(user, associado.UnidadeId);

        var nowUtc = DateTime.UtcNow;
        if (req.Ativo)
        {
            associado.Status = StatusAssociado.Ativo;
            associado.DesativadoEmUtc = null;
            if (associado.Pessoa.Usuario is not null && associado.Pessoa.Usuario.EstadoConta == EstadoConta.Desativada)
            {
                associado.Pessoa.Usuario.EstadoConta = EstadoConta.Ativa;
                associado.Pessoa.Usuario.VersaoSeguranca++;
            }
            _auditoria.Registrar(user.UsuarioId, associado.UnidadeId, "REATIVAR_ASSOCIADO", nameof(Associado), associado.Id, $"Associado #{associado.Id} reativado.");
        }
        else
        {
            associado.Status = StatusAssociado.Inativo;
            associado.DesativadoEmUtc = nowUtc;
            if (associado.Pessoa.Usuario is not null && associado.Pessoa.Usuario.RoleAdministrativa != RoleAdministrativa.SuperAdmin)
            {
                if (associado.Pessoa.Usuario.RoleAdministrativa != RoleAdministrativa.Nenhuma)
                {
                    associado.Pessoa.Usuario.RoleAdministrativa = RoleAdministrativa.Nenhuma;
                    associado.Pessoa.Usuario.UnidadeAdministrativaId = null;
                    associado.Pessoa.Usuario.AdminAtivoUnidadeId = null;
                }
                associado.Pessoa.Usuario.EstadoConta = EstadoConta.Desativada;
                associado.Pessoa.Usuario.VersaoSeguranca++;
            }
            _auditoria.Registrar(user.UsuarioId, associado.UnidadeId, "DESATIVAR_ASSOCIADO", nameof(Associado), associado.Id, $"Associado #{associado.Id} desativado.");
        }

        await _db.SaveChangesAsync(ct);
        return await ObterAssociadoPorIdAsync(user, associadoId, ct);
    }

    public async Task<TransferenciaHistoricoDto> TransferirAssociadoUnidadeAsync(
        AuthenticatedUserInfo user,
        int associadoId,
        TransferirAssociadoUnidadeRequest req,
        CancellationToken ct = default)
    {
        // Regra 3.2, 4.1 e 7.3: Somente o Super Admin transfere associados entre unidades
        _currentUser.EnsureSuperAdmin(user);

        var associado = await _db.Associados
            .Include(a => a.Unidade)
            .FirstOrDefaultAsync(a => a.Id == associadoId, ct)
            ?? throw new ApiException(StatusCodes.Status404NotFound, CodigosErro.RecursoNaoEncontrado, "Associado não encontrado.");

        if (associado.Status != StatusAssociado.Ativo)
            throw new ApiException(StatusCodes.Status422UnprocessableEntity, CodigosErro.DadosInvalidos, "Não é permitido transferir um associado inativo.");

        if (associado.UnidadeId == req.UnidadeDestinoId)
            throw new ApiException(StatusCodes.Status422UnprocessableEntity, CodigosErro.TransferenciaMesmaUnidade, "A unidade de destino deve ser diferente da unidade atual.");

        var unidadeDestino = await _db.Unidades.FirstOrDefaultAsync(u => u.Id == req.UnidadeDestinoId && u.Ativa, ct)
            ?? throw new ApiException(StatusCodes.Status404NotFound, CodigosErro.RecursoNaoEncontrado, "Unidade de destino não encontrada ou inativa.");

        await using var tx = await _db.Database.BeginTransactionAsync(ct);
        var nowUtc = DateTime.UtcNow;
        var hoje = TimeZoneHelper.TodaySaoPaulo(nowUtc);
        var competenciaDestino = TimeZoneHelper.FirstDayOfNextMonth(hoje);

        var unidadeOrigemId = associado.UnidadeId;
        var unidadeOrigemNome = associado.Unidade.Nome;

        associado.UnidadeId = unidadeDestino.Id;

        var transferencia = new TransferenciaUnidade
        {
            AssociadoId = associado.Id,
            UnidadeOrigemId = unidadeOrigemId,
            UnidadeDestinoId = unidadeDestino.Id,
            DataEfetiva = hoje,
            CompetenciaCobrancaDestino = competenciaDestino,
            RealizadaPorUsuarioId = user.UsuarioId,
            RealizadaEmUtc = nowUtc
        };
        _db.TransferenciasUnidade.Add(transferencia);

        _auditoria.Registrar(
            user.UsuarioId,
            unidadeDestino.Id,
            "TRANSFERIR_ASSOCIADO_UNIDADE",
            nameof(Associado),
            associado.Id,
            $"Associado #{associado.Id} transferido da unidade #{unidadeOrigemId} para #{unidadeDestino.Id}. Cobrança no destino a partir de {competenciaDestino:yyyy-MM-dd}.");

        await _db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);

        return new TransferenciaHistoricoDto(
            Id: transferencia.Id,
            AssociadoId: associado.Id,
            UnidadeOrigemId: unidadeOrigemId,
            UnidadeOrigemNome: unidadeOrigemNome,
            UnidadeDestinoId: unidadeDestino.Id,
            UnidadeDestinoNome: unidadeDestino.Nome,
            DataEfetiva: transferencia.DataEfetiva,
            CompetenciaCobrancaDestino: transferencia.CompetenciaCobrancaDestino,
            RealizadaEm: TimeZoneHelper.ToSaoPauloOffset(transferencia.RealizadaEmUtc));
    }

    public async Task<DependenteDto> CadastrarDependenteAsync(
        AuthenticatedUserInfo user,
        int associadoTitularId,
        CadastrarDependenteRequest req,
        CancellationToken ct = default)
    {
        var titular = await _db.Associados
            .Include(a => a.Unidade)
            .Include(a => a.Pessoa)
            .FirstOrDefaultAsync(a => a.Id == associadoTitularId, ct)
            ?? throw new ApiException(StatusCodes.Status404NotFound, CodigosErro.RecursoNaoEncontrado, "Associado titular não encontrado.");

        var solicitadoPeloTitular = user.AssociadoId == titular.Id
            && user.Role != RoleAdministrativa.Admin
            && user.Role != RoleAdministrativa.SuperAdmin;
        if (!solicitadoPeloTitular)
            _currentUser.EnsureAdminOrSuperAdminForUnit(user, titular.UnidadeId);

        if (titular.Status != StatusAssociado.Ativo)
            throw new ApiException(StatusCodes.Status422UnprocessableEntity, CodigosErro.DadosInvalidos, "Não é permitido vincular dependentes a um associado titular inativo.");

        var cpf = CpfHelper.Normalize(req.Cpf);
        if (!CpfHelper.IsValidFormat(cpf))
            throw new ApiException(StatusCodes.Status400BadRequest, CodigosErro.CpfInvalido, "CPF do dependente inválido.");

        if (string.IsNullOrWhiteSpace(req.NomeCompleto) || string.IsNullOrWhiteSpace(req.Sexo))
            throw new ApiException(StatusCodes.Status400BadRequest, CodigosErro.DadosInvalidos, "Nome completo, CPF e sexo do dependente são obrigatórios.");

        if (!string.IsNullOrWhiteSpace(req.UF) && req.UF.Trim().Length != 2)
            throw new ApiException(StatusCodes.Status400BadRequest, CodigosErro.DadosInvalidos, "UF deve conter 2 caracteres.");

        var nowUtc = DateTime.UtcNow;
        var hoje = TimeZoneHelper.TodaySaoPaulo(nowUtc);

        // Regra 5.3: Somente cônjuge ou filho(a) de até 21 anos, inclusive
        if (req.TipoVinculo == TipoVinculoDependente.Filho)
        {
            var idade = TimeZoneHelper.CalculateAge(req.DataNascimento, hoje);
            if (idade < 0 || idade > 21)
                throw new ApiException(StatusCodes.Status422UnprocessableEntity, CodigosErro.DependenteIdadeInvalida, "Filho(a) dependente deve ter até 21 anos, inclusive.");
        }

        await using var tx = await _db.Database.BeginTransactionAsync(ct);

        var pessoaDep = await _db.Pessoas
            .Include(p => p.Usuario)
            .Include(p => p.Associado)
            .FirstOrDefaultAsync(p => p.Cpf == cpf, ct);

        var logradouro = string.IsNullOrWhiteSpace(req.Logradouro) ? (titular.Pessoa?.Logradouro ?? string.Empty) : req.Logradouro.Trim();
        var bairro = string.IsNullOrWhiteSpace(req.Bairro) ? (titular.Pessoa?.Bairro ?? string.Empty) : req.Bairro.Trim();
        var numero = string.IsNullOrWhiteSpace(req.Numero) ? (titular.Pessoa?.Numero ?? string.Empty) : req.Numero.Trim();
        var cidade = string.IsNullOrWhiteSpace(req.Cidade) ? (titular.Pessoa?.Cidade ?? string.Empty) : req.Cidade.Trim();
        var uf = string.IsNullOrWhiteSpace(req.UF) ? (titular.Pessoa?.UF ?? string.Empty) : req.UF.Trim().ToUpperInvariant();

        if (pessoaDep is not null)
        {
            var jaDependenteAtivo = await _db.Dependencias
                .AnyAsync(d => d.PessoaDependenteId == pessoaDep.Id && d.DataFim == null, ct);
            if (jaDependenteAtivo)
                throw new ApiException(StatusCodes.Status409Conflict, CodigosErro.DependenteJaVinculado, "Esta pessoa já possui vínculo ativo como dependente.");

            if (pessoaDep.Associado is not null && pessoaDep.Associado.Status == StatusAssociado.Ativo)
                throw new ApiException(StatusCodes.Status409Conflict, CodigosErro.CpfJaCadastrado, "Esta pessoa já possui vínculo ativo como associado titular/simpatizante.");

            var temSolicitacaoPendente = await _db.SolicitacoesCadastro
                .AnyAsync(s => s.PessoaId == pessoaDep.Id && s.Status == StatusSolicitacaoCadastro.Pendente, ct);
            if (temSolicitacaoPendente)
                throw new ApiException(StatusCodes.Status409Conflict, CodigosErro.SolicitacaoJaPendente, "Já existe uma solicitação de cadastro pendente para este CPF.");

            pessoaDep.NomeCompleto = req.NomeCompleto.Trim();
            pessoaDep.Sexo = req.Sexo.Trim();
            pessoaDep.EstadoCivil = string.IsNullOrWhiteSpace(req.EstadoCivil) ? pessoaDep.EstadoCivil : req.EstadoCivil.Trim();
            pessoaDep.DataNascimento = req.DataNascimento;
            pessoaDep.Logradouro = logradouro;
            pessoaDep.Bairro = bairro;
            pessoaDep.Numero = numero;
            pessoaDep.Cidade = cidade;
            pessoaDep.UF = uf;
            pessoaDep.Fone = string.IsNullOrWhiteSpace(req.Fone) ? pessoaDep.Fone : req.Fone.Trim();
            pessoaDep.Email = string.IsNullOrWhiteSpace(req.Email) ? pessoaDep.Email : req.Email.Trim();
            pessoaDep.AtualizadaEmUtc = nowUtc;
        }
        else
        {
            pessoaDep = new Pessoa
            {
                NomeCompleto = req.NomeCompleto.Trim(),
                Cpf = cpf,
                Sexo = req.Sexo.Trim(),
                EstadoCivil = (req.EstadoCivil ?? string.Empty).Trim(),
                DataNascimento = req.DataNascimento,
                Logradouro = logradouro,
                Bairro = bairro,
                Numero = numero,
                Cidade = cidade,
                UF = uf,
                Fone = (req.Fone ?? string.Empty).Trim(),
                Email = (req.Email ?? string.Empty).Trim(),
                CriadaEmUtc = nowUtc,
                AtualizadaEmUtc = nowUtc
            };
            _db.Pessoas.Add(pessoaDep);
            await _db.SaveChangesAsync(ct);
        }

        await AtualizarInteressesPessoaAsync(pessoaDep.Id, req.InteressesIds, ct);

        if (solicitadoPeloTitular || !string.IsNullOrWhiteSpace(req.SenhaContaPropria))
        {
            var senhaRaw = string.IsNullOrWhiteSpace(req.SenhaContaPropria) ? "ProClubes@123" : req.SenhaContaPropria;
            var estadoConta = solicitadoPeloTitular ? EstadoConta.RestritaCadastro : EstadoConta.Ativa;

            if (pessoaDep.Usuario is null)
            {
                var usuarioDep = new Usuario
                {
                    PessoaId = pessoaDep.Id,
                    SenhaHash = PasswordHasherHelper.HashPassword(senhaRaw),
                    RoleAdministrativa = RoleAdministrativa.Nenhuma,
                    EstadoConta = estadoConta,
                    VersaoSeguranca = 1,
                    CriadoEmUtc = nowUtc
                };
                _db.Usuarios.Add(usuarioDep);
                await _db.SaveChangesAsync(ct);
                pessoaDep.Usuario = usuarioDep;
            }
            else
            {
                if (!string.IsNullOrWhiteSpace(req.SenhaContaPropria))
                    pessoaDep.Usuario.SenhaHash = PasswordHasherHelper.HashPassword(req.SenhaContaPropria);
                pessoaDep.Usuario.EstadoConta = estadoConta;
                pessoaDep.Usuario.VersaoSeguranca++;
            }
        }

        var dep = new Dependencia
        {
            AssociadoTitularId = titular.Id,
            PessoaDependenteId = pessoaDep.Id,
            TipoVinculo = req.TipoVinculo,
            DataInicio = hoje
        };
        _db.Dependencias.Add(dep);
        await _db.SaveChangesAsync(ct);

        if (solicitadoPeloTitular)
        {
            var solicitacaoDep = new SolicitacaoCadastro
            {
                PessoaId = pessoaDep.Id,
                UsuarioId = pessoaDep.Usuario!.Id,
                UnidadePretendidaId = titular.UnidadeId,
                Status = StatusSolicitacaoCadastro.Pendente,
                CategoriaAprovadaId = null,
                SolicitadaEmUtc = nowUtc
            };
            _db.SolicitacoesCadastro.Add(solicitacaoDep);
            await _db.SaveChangesAsync(ct);
            await NotificarDiretoriaNovaSolicitacaoAsync(solicitacaoDep, titular.Unidade, ehDependente: true, ct: ct);
        }

        _auditoria.Registrar(
            user.UsuarioId,
            titular.UnidadeId,
            solicitadoPeloTitular ? "SOLICITAR_DEPENDENTE" : "VINCULAR_DEPENDENTE",
            nameof(Dependencia),
            dep.Id,
            solicitadoPeloTitular
                ? $"Solicitação de vínculo de dependente enviada pelo titular #{titular.Id}."
                : $"Dependente vinculado ao titular #{titular.Id}.");
        await _db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);

        return ToDependenteDto(dep, pessoaDep);
    }

    public async Task<DependenteDto> AlterarStatusDependenteAsync(
        AuthenticatedUserInfo user,
        int associadoTitularId,
        int dependenciaId,
        AlterarStatusDependenteRequest req,
        CancellationToken ct = default)
    {
        var titular = await _db.Associados
            .FirstOrDefaultAsync(a => a.Id == associadoTitularId, ct)
            ?? throw new ApiException(StatusCodes.Status404NotFound, CodigosErro.RecursoNaoEncontrado, "Associado titular não encontrado.");

        var solicitadoPeloTitular = user.AssociadoId == titular.Id
            && user.Role != RoleAdministrativa.Admin
            && user.Role != RoleAdministrativa.SuperAdmin;
        if (!solicitadoPeloTitular)
            _currentUser.EnsureAdminOrSuperAdminForUnit(user, titular.UnidadeId);

        var dep = await _db.Dependencias
            .Include(d => d.PessoaDependente)
                .ThenInclude(p => p.Usuario)
            .Include(d => d.PessoaDependente)
                .ThenInclude(p => p.Associado)
            .FirstOrDefaultAsync(d => d.Id == dependenciaId && d.AssociadoTitularId == titular.Id, ct)
            ?? throw new ApiException(StatusCodes.Status404NotFound, CodigosErro.RecursoNaoEncontrado, "Vínculo de dependente não encontrado para este associado.");

        if (dep.PessoaDependente.Usuario?.EstadoConta == EstadoConta.RestritaCadastro)
        {
            if (dep.DataFim is null)
                throw new ApiException(StatusCodes.Status422UnprocessableEntity, CodigosErro.DadosInvalidos, "Este dependente ainda aguarda aprovação da diretoria.");

            if (req.Ativo)
                throw new ApiException(StatusCodes.Status422UnprocessableEntity, CodigosErro.DadosInvalidos, "Este cadastro de dependente não foi aprovado pela diretoria. Envie uma nova solicitação para vinculá-lo.");
        }

        var nowUtc = DateTime.UtcNow;
        var hoje = TimeZoneHelper.TodaySaoPaulo(nowUtc);

        if (req.Ativo)
        {
            if (titular.Status != StatusAssociado.Ativo)
                throw new ApiException(StatusCodes.Status422UnprocessableEntity, CodigosErro.DadosInvalidos, "Não é permitido reativar dependentes de um associado titular inativo.");

            if (dep.TipoVinculo == TipoVinculoDependente.Filho)
            {
                var idade = TimeZoneHelper.CalculateAge(dep.PessoaDependente.DataNascimento, hoje);
                if (idade < 0 || idade > 21)
                    throw new ApiException(StatusCodes.Status422UnprocessableEntity, CodigosErro.DependenteIdadeInvalida, "Filho(a) dependente deve ter até 21 anos, inclusive, para reativar o vínculo.");
            }

            var outroVinculoAtivo = await _db.Dependencias
                .AnyAsync(d => d.PessoaDependenteId == dep.PessoaDependenteId && d.Id != dep.Id && d.DataFim == null, ct);
            if (outroVinculoAtivo)
                throw new ApiException(StatusCodes.Status409Conflict, CodigosErro.DependenteJaVinculado, "Esta pessoa já possui outro vínculo ativo como dependente.");

            if (dep.PessoaDependente.Associado is not null && dep.PessoaDependente.Associado.Status == StatusAssociado.Ativo)
                throw new ApiException(StatusCodes.Status409Conflict, CodigosErro.CpfJaCadastrado, "Esta pessoa já possui vínculo ativo como associado titular/simpatizante.");

            dep.DataFim = null;
            dep.MotivoFim = null;

            if (dep.PessoaDependente.Usuario is not null)
            {
                dep.PessoaDependente.Usuario.EstadoConta = EstadoConta.Ativa;
                dep.PessoaDependente.Usuario.VersaoSeguranca++;
            }
        }
        else
        {
            dep.DataFim ??= hoje;
            dep.MotivoFim = solicitadoPeloTitular
                ? MotivoFimDependencia.SolicitacaoTitular
                : MotivoFimDependencia.Administrativo;

            if (dep.PessoaDependente.Usuario is not null)
                dep.PessoaDependente.Usuario.VersaoSeguranca++;
        }

        _auditoria.Registrar(
            user.UsuarioId,
            titular.UnidadeId,
            req.Ativo ? "REATIVAR_DEPENDENTE" : "DESATIVAR_DEPENDENTE",
            nameof(Dependencia),
            dep.Id,
            $"Status do dependente #{dep.Id} alterado para {(req.Ativo ? "Ativo" : "Encerrado")}.");
        await _db.SaveChangesAsync(ct);

        return ToDependenteDto(dep);
    }

    public async Task<int> ProcessarTransicaoDependentes22AnosAsync(DateOnly? dataReferencia = null, CancellationToken ct = default)
    {
        var hoje = dataReferencia ?? TimeZoneHelper.TodaySaoPaulo();
        var dataLimiteNascimento = hoje.AddYears(-22);

        var filhosParaTransicao = await _db.Dependencias
            .Include(d => d.PessoaDependente)
                .ThenInclude(p => p.Usuario)
            .Include(d => d.AssociadoTitular)
            .Where(d => d.DataFim == null
                        && d.TipoVinculo == TipoVinculoDependente.Filho
                        && d.PessoaDependente.DataNascimento <= dataLimiteNascimento)
            .ToListAsync(ct);

        if (filhosParaTransicao.Count == 0)
            return 0;

        var catSimpatizante = await _db.CategoriasAssociado
            .FirstAsync(c => c.Codigo == CodigosCategoriaAssociado.Simpatizante, ct);

        var convertidos = 0;
        foreach (var dep in filhosParaTransicao)
        {
            await using var tx = await _db.Database.BeginTransactionAsync(ct);
            dep.DataFim = hoje;
            dep.MotivoFim = MotivoFimDependencia.Idade22Anos;

            var associadoExistente = await _db.Associados
                .FirstOrDefaultAsync(a => a.PessoaId == dep.PessoaDependenteId, ct);

            if (associadoExistente is null)
            {
                associadoExistente = new Associado
                {
                    PessoaId = dep.PessoaDependenteId,
                    UnidadeId = dep.AssociadoTitular.UnidadeId,
                    CategoriaAssociadoId = catSimpatizante.Id,
                    Status = StatusAssociado.Ativo,
                    DataIngresso = hoje,
                    DataInicioCobranca = null
                };
                _db.Associados.Add(associadoExistente);
            }
            else
            {
                associadoExistente.UnidadeId = dep.AssociadoTitular.UnidadeId;
                associadoExistente.CategoriaAssociadoId = catSimpatizante.Id;
                associadoExistente.Status = StatusAssociado.Ativo;
                associadoExistente.DataInicioCobranca = null;
                associadoExistente.DesativadoEmUtc = null;
            }

            await _db.SaveChangesAsync(ct);

            if (dep.PessoaDependente.Usuario is not null)
            {
                await _notificacao.CriarNotificacaoAsync(
                    dep.PessoaDependente.Usuario.Id,
                    TipoNotificacao.TransicaoDependente,
                    "Atualização de vínculo associativo",
                    "Ao completar 22 anos, seu vínculo de dependência foi encerrado e seu cadastro passou automaticamente para a categoria Simpatizante.",
                    "Associado",
                    associadoExistente.Id,
                    $"TRANSICAO_22_{dep.Id}",
                    ct);
            }

            _auditoria.Registrar(
                null,
                dep.AssociadoTitular.UnidadeId,
                "TRANSICAO_DEPENDENTE_22_ANOS",
                nameof(Dependencia),
                dep.Id,
                $"Dependente #{dep.Id} convertido automaticamente para Simpatizante (Associado #{associadoExistente.Id}).");

            await _db.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);
            convertidos++;
        }

        return convertidos;
    }

    private async Task AtualizarInteressesPessoaAsync(int pessoaId, IReadOnlyList<int>? interessesIds, CancellationToken ct)
    {
        if (interessesIds is null)
            return;

        var atuais = await _db.PessoasInteresses.Where(pi => pi.PessoaId == pessoaId).ToListAsync(ct);
        _db.PessoasInteresses.RemoveRange(atuais);

        var idsValidos = await _db.Interesses
            .Where(i => i.Ativo && interessesIds.Distinct().Contains(i.Id))
            .Select(i => i.Id)
            .ToListAsync(ct);

        foreach (var id in idsValidos)
        {
            _db.PessoasInteresses.Add(new PessoaInteresse
            {
                PessoaId = pessoaId,
                InteresseId = id
            });
        }
    }

    private async Task<CategoriaAssociado?> ResolverCategoriaPretendidaAsync(string? codigoCategoria, CancellationToken ct)
    {
        var codigoCat = string.IsNullOrWhiteSpace(codigoCategoria)
            ? CodigosCategoriaAssociado.Associado
            : codigoCategoria.Trim().ToUpperInvariant();

        return await _db.CategoriasAssociado
            .FirstOrDefaultAsync(c => c.Codigo == codigoCat && c.Ativa, ct)
            ?? await _db.CategoriasAssociado
                .FirstOrDefaultAsync(c => c.Codigo == CodigosCategoriaAssociado.Associado, ct);
    }

    private static string FormatarVinculoDependente(Dependencia dep)
    {
        var vinculoLabel = dep.TipoVinculo == TipoVinculoDependente.Conjuge ? "Cônjuge" : "Filho(a)";
        return $"Dependente — {vinculoLabel} (Titular: {dep.AssociadoTitular.Pessoa.NomeCompleto})";
    }

    private async Task NotificarDiretoriaNovaSolicitacaoAsync(
        SolicitacaoCadastro solicitacao,
        Unidade unidade,
        bool ehDependente = false,
        CancellationToken ct = default)
    {
        var destinatarios = await _db.Usuarios
            .AsNoTracking()
            .Where(u => u.EstadoConta == EstadoConta.Ativa
                        && (u.RoleAdministrativa == RoleAdministrativa.SuperAdmin
                            || (u.RoleAdministrativa == RoleAdministrativa.Admin && u.UnidadeAdministrativaId == unidade.Id)))
            .Select(u => u.Id)
            .ToListAsync(ct);

        var titulo = ehDependente ? "Nova solicitação de dependente" : "Nova solicitação de cadastro";
        var mensagem = ehDependente
            ? $"Há uma nova solicitação de vínculo de dependente pendente para a unidade {unidade.Nome}."
            : $"Há uma nova solicitação de cadastro pendente para a unidade {unidade.Nome}.";

        foreach (var destId in destinatarios)
        {
            await _notificacao.CriarNotificacaoAsync(
                destId,
                TipoNotificacao.SolicitacaoCadastroRecebida,
                titulo,
                mensagem,
                "SolicitacaoCadastro",
                solicitacao.Id,
                $"NOVA_SOL_{solicitacao.Id}_USR_{destId}",
                ct);
        }
    }

    private static SolicitacaoCadastroListItemDto ToSolicitacaoListItemDto(
        SolicitacaoCadastro s,
        string? categoriaAprovadaNome,
        DateTime? decididaEmUtcOverride = null)
    {
        var decididaUtc = decididaEmUtcOverride ?? s.DecididaEmUtc;
        return new SolicitacaoCadastroListItemDto(
            Id: s.Id,
            PessoaId: s.PessoaId,
            NomeCompleto: s.Pessoa.NomeCompleto,
            CpfMascarado: CpfHelper.Mask(s.Pessoa.Cpf),
            Email: s.Pessoa.Email,
            Fone: s.Pessoa.Fone,
            UnidadePretendidaId: s.UnidadePretendidaId,
            UnidadePretendidaNome: s.UnidadePretendida.Nome,
            Status: s.Status,
            JustificativaRejeicao: s.Status == StatusSolicitacaoCadastro.Aprovada ? null : s.JustificativaRejeicao,
            CategoriaAprovadaNome: categoriaAprovadaNome,
            SolicitadaEm: TimeZoneHelper.ToSaoPauloOffset(s.SolicitadaEmUtc),
            DecididaEm: decididaUtc.HasValue ? TimeZoneHelper.ToSaoPauloOffset(decididaUtc.Value) : null,
            Sexo: s.Pessoa.Sexo,
            EstadoCivil: s.Pessoa.EstadoCivil,
            DataNascimento: s.Pessoa.DataNascimento,
            Logradouro: s.Pessoa.Logradouro,
            Bairro: s.Pessoa.Bairro,
            Numero: s.Pessoa.Numero,
            Cidade: s.Pessoa.Cidade,
            UF: s.Pessoa.UF);
    }

    private static DependenteDto ToDependenteDto(Dependencia d, Pessoa? pessoaOverride = null)
    {
        var pessoa = pessoaOverride ?? d.PessoaDependente;
        var pendenteAprovacao = d.DataFim == null && pessoa.Usuario?.EstadoConta == EstadoConta.RestritaCadastro;
        return new DependenteDto(
            DependenciaId: d.Id,
            AssociadoTitularId: d.AssociadoTitularId,
            PessoaDependenteId: d.PessoaDependenteId,
            UsuarioDependenteId: pessoa.Usuario?.Id,
            NomeCompleto: pessoa.NomeCompleto,
            CpfMascarado: CpfHelper.Mask(pessoa.Cpf),
            Sexo: pessoa.Sexo,
            DataNascimento: pessoa.DataNascimento,
            Email: pessoa.Email,
            Fone: pessoa.Fone,
            TipoVinculo: d.TipoVinculo,
            DataInicio: d.DataInicio,
            DataFim: d.DataFim,
            MotivoFim: d.MotivoFim,
            Ativo: d.DataFim == null && !pendenteAprovacao,
            PendenteAprovacao: pendenteAprovacao);
    }

    public static AssociadoDto ToAssociadoDto(Associado a) => new(
        Id: a.Id,
        PessoaId: a.PessoaId,
        UsuarioId: a.Pessoa.Usuario?.Id,
        NomeCompleto: a.Pessoa.NomeCompleto,
        CpfMascarado: CpfHelper.Mask(a.Pessoa.Cpf),
        Sexo: a.Pessoa.Sexo,
        EstadoCivil: a.Pessoa.EstadoCivil,
        DataNascimento: a.Pessoa.DataNascimento,
        Email: a.Pessoa.Email,
        Fone: a.Pessoa.Fone,
        Logradouro: a.Pessoa.Logradouro,
        Bairro: a.Pessoa.Bairro,
        Numero: a.Pessoa.Numero,
        Cidade: a.Pessoa.Cidade,
        UF: a.Pessoa.UF,
        UnidadeId: a.UnidadeId,
        UnidadeNome: a.Unidade.Nome,
        CategoriaAssociadoId: a.CategoriaAssociadoId,
        CategoriaCodigo: a.CategoriaAssociado.Codigo,
        CategoriaNome: a.CategoriaAssociado.Nome,
        Status: a.Status,
        DataIngresso: a.DataIngresso,
        DataInicioCobranca: a.DataInicioCobranca,
        RoleAdministrativa: a.Pessoa.Usuario?.RoleAdministrativa ?? RoleAdministrativa.Nenhuma,
        Interesses: a.Pessoa.Interesses.Select(i => i.Interesse.Nome).OrderBy(n => n).ToList(),
        Dependentes: a.Dependentes
            .OrderByDescending(d => d.DataFim == null)
            .ThenBy(d => d.PessoaDependente.NomeCompleto)
            .Select(d => ToDependenteDto(d))
            .ToList());
}
