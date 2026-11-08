using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using ProClubesApi.Authorization;
using ProClubesApi.Contracts;
using ProClubesApi.Data;
using ProClubesApi.Entities;

namespace ProClubesApi.Services;

public class AuthService
{
    private readonly ProClubesDbContext _db;
    private readonly JwtOptions _jwtOptions;
    private readonly AuditoriaService _auditoria;

    public AuthService(
        ProClubesDbContext db,
        IOptions<JwtOptions> jwtOptions,
        AuditoriaService auditoria)
    {
        _db = db;
        _jwtOptions = jwtOptions.Value;
        _auditoria = auditoria;
    }

    public async Task<LoginResponse> LoginAsync(LoginRequest request, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(request.Identificador) || string.IsNullOrWhiteSpace(request.Senha))
            throw new ApiException(StatusCodes.Status400BadRequest, CodigosErro.DadosInvalidos, "Informe identificador e senha.");

        var ident = request.Identificador.Trim();
        var cpfNorm = CpfHelper.Normalize(ident);

        var usuario = await _db.Usuarios
            .Include(u => u.Pessoa)
            .FirstOrDefaultAsync(u =>
                u.Pessoa.Email.ToLower() == ident.ToLower()
                || (cpfNorm.Length == 11 && u.Pessoa.Cpf == cpfNorm), ct);

        if (usuario is null || !PasswordHasherHelper.VerifyPassword(usuario.SenhaHash, request.Senha))
            throw new ApiException(StatusCodes.Status401Unauthorized, CodigosErro.CredenciaisInvalidas, "Credenciais inválidas.");

        if (usuario.EstadoConta is EstadoConta.Bloqueada or EstadoConta.Desativada)
            throw new ApiException(StatusCodes.Status403Forbidden, CodigosErro.ContaBloqueadaOuDesativada, "Sua conta encontra-se desativada ou bloqueada.");

        var associado = await _db.Associados
            .AsNoTracking()
            .FirstOrDefaultAsync(a => a.PessoaId == usuario.PessoaId, ct);

        var depAtivo = await _db.Dependencias
            .AsNoTracking()
            .Include(d => d.AssociadoTitular)
            .FirstOrDefaultAsync(d => d.PessoaDependenteId == usuario.PessoaId && d.DataFim == null, ct);

        if (usuario.EstadoConta == EstadoConta.Ativa
            && usuario.RoleAdministrativa == RoleAdministrativa.Nenhuma
            && associado is not null
            && associado.Status == StatusAssociado.Inativo
            && depAtivo is null)
        {
            throw new ApiException(StatusCodes.Status403Forbidden, CodigosErro.ContaBloqueadaOuDesativada, "Vínculo associativo inativo.");
        }

        var nowUtc = DateTime.UtcNow;
        usuario.UltimoLoginUtc = nowUtc;

        var rawRefresh = PasswordHasherHelper.GenerateSecureRandomToken();
        var refreshHash = PasswordHasherHelper.ComputeSha256Hex(rawRefresh);

        var sessao = new SessaoRenovacao
        {
            UsuarioId = usuario.Id,
            TokenHash = refreshHash,
            CriadoEmUtc = nowUtc,
            ExpiraEmUtc = nowUtc.AddDays(_jwtOptions.RefreshTokenDays)
        };
        _db.SessoesRenovacao.Add(sessao);
        await _db.SaveChangesAsync(ct);

        var (accessToken, expiraEmUtc) = GenerateAccessToken(usuario);
        var perfil = await ObterPerfilAsync(usuario.Id, ct);

        return new LoginResponse(
            AccessToken: accessToken,
            RefreshToken: rawRefresh,
            ExpiraEm: TimeZoneHelper.ToSaoPauloOffset(expiraEmUtc),
            Usuario: perfil);
    }

    public async Task<LoginResponse> RefreshAsync(RefreshRequest request, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(request.RefreshToken))
            throw new ApiException(StatusCodes.Status400BadRequest, CodigosErro.DadosInvalidos, "Refresh token obrigatório.");

        var hash = PasswordHasherHelper.ComputeSha256Hex(request.RefreshToken.Trim());
        var nowUtc = DateTime.UtcNow;

        var sessaoAtual = await _db.SessoesRenovacao
            .Include(s => s.Usuario)
            .FirstOrDefaultAsync(s => s.TokenHash == hash, ct);

        if (sessaoAtual is null || sessaoAtual.RevogadoEmUtc.HasValue || sessaoAtual.ExpiraEmUtc <= nowUtc)
            throw new ApiException(StatusCodes.Status401Unauthorized, CodigosErro.SessaoInvalidaOuRevogada, "Sessão de renovação inválida ou expirada.");

        var usuario = sessaoAtual.Usuario;
        if (usuario.EstadoConta is EstadoConta.Bloqueada or EstadoConta.Desativada)
            throw new ApiException(StatusCodes.Status403Forbidden, CodigosErro.ContaBloqueadaOuDesativada, "Conta desativada ou bloqueada.");

        var novoRawRefresh = PasswordHasherHelper.GenerateSecureRandomToken();
        var novoHash = PasswordHasherHelper.ComputeSha256Hex(novoRawRefresh);

        var novaSessao = new SessaoRenovacao
        {
            UsuarioId = usuario.Id,
            TokenHash = novoHash,
            CriadoEmUtc = nowUtc,
            ExpiraEmUtc = nowUtc.AddDays(_jwtOptions.RefreshTokenDays)
        };

        _db.SessoesRenovacao.Add(novaSessao);
        await _db.SaveChangesAsync(ct);

        sessaoAtual.RevogadoEmUtc = nowUtc;
        sessaoAtual.SubstituidoPorSessaoId = novaSessao.Id;
        await _db.SaveChangesAsync(ct);

        var (accessToken, expiraEmUtc) = GenerateAccessToken(usuario);
        var perfil = await ObterPerfilAsync(usuario.Id, ct);

        return new LoginResponse(
            AccessToken: accessToken,
            RefreshToken: novoRawRefresh,
            ExpiraEm: TimeZoneHelper.ToSaoPauloOffset(expiraEmUtc),
            Usuario: perfil);
    }

    public async Task LogoutAsync(int usuarioId, string? refreshToken, CancellationToken ct = default)
    {
        var nowUtc = DateTime.UtcNow;
        if (!string.IsNullOrWhiteSpace(refreshToken))
        {
            var hash = PasswordHasherHelper.ComputeSha256Hex(refreshToken.Trim());
            var sessao = await _db.SessoesRenovacao
                .FirstOrDefaultAsync(s => s.UsuarioId == usuarioId && s.TokenHash == hash && s.RevogadoEmUtc == null, ct);
            if (sessao is not null)
                sessao.RevogadoEmUtc = nowUtc;
        }
        else
        {
            var sessoesAtivas = await _db.SessoesRenovacao
                .Where(s => s.UsuarioId == usuarioId && s.RevogadoEmUtc == null)
                .ToListAsync(ct);
            foreach (var s in sessoesAtivas)
                s.RevogadoEmUtc = nowUtc;
        }

        var usuario = await _db.Usuarios.FirstOrDefaultAsync(u => u.Id == usuarioId, ct);
        if (usuario is not null)
            usuario.VersaoSeguranca++;

        await _db.SaveChangesAsync(ct);
    }

    public async Task<PerfilUsuarioDto> ObterPerfilAsync(int usuarioId, CancellationToken ct = default)
    {
        var usuario = await _db.Usuarios
            .AsNoTracking()
            .Include(u => u.Pessoa)
                .ThenInclude(p => p.Interesses)
                    .ThenInclude(pi => pi.Interesse)
            .Include(u => u.UnidadeAdministrativa)
            .FirstOrDefaultAsync(u => u.Id == usuarioId, ct);

        if (usuario is null)
            throw new ApiException(StatusCodes.Status404NotFound, CodigosErro.RecursoNaoEncontrado, "Usuário não encontrado.");

        var associado = await _db.Associados
            .AsNoTracking()
            .Include(a => a.Unidade)
            .Include(a => a.CategoriaAssociado)
            .FirstOrDefaultAsync(a => a.PessoaId == usuario.PessoaId && a.Status == StatusAssociado.Ativo, ct);

        var dep = await _db.Dependencias
            .AsNoTracking()
            .Include(d => d.AssociadoTitular)
                .ThenInclude(at => at.Pessoa)
            .Include(d => d.AssociadoTitular)
                .ThenInclude(at => at.Unidade)
            .FirstOrDefaultAsync(d => d.PessoaDependenteId == usuario.PessoaId && d.DataFim == null, ct);

        return new PerfilUsuarioDto(
            UsuarioId: usuario.Id,
            PessoaId: usuario.PessoaId,
            NomeCompleto: usuario.Pessoa.NomeCompleto,
            CpfMascarado: CpfHelper.Mask(usuario.Pessoa.Cpf),
            Email: usuario.Pessoa.Email,
            Fone: usuario.Pessoa.Fone,
            RoleAdministrativa: usuario.RoleAdministrativa,
            UnidadeAdministrativaId: usuario.UnidadeAdministrativaId,
            UnidadeAdministrativaNome: usuario.UnidadeAdministrativa?.Nome,
            EstadoConta: usuario.EstadoConta,
            AssociadoId: associado?.Id,
            UnidadeAssociadoId: associado?.UnidadeId,
            UnidadeAssociadoNome: associado?.Unidade.Nome,
            CategoriaCodigo: associado?.CategoriaAssociado.Codigo,
            CategoriaNome: associado?.CategoriaAssociado.Nome,
            EhDependenteAtivo: dep is not null,
            TitularAssociadoId: dep?.AssociadoTitularId,
            TitularNome: dep?.AssociadoTitular.Pessoa.NomeCompleto,
            UnidadeTitularId: dep?.AssociadoTitular.UnidadeId,
            UnidadeTitularNome: dep?.AssociadoTitular.Unidade.Nome,
            Interesses: usuario.Pessoa.Interesses.Select(i => i.Interesse.Nome).OrderBy(n => n).ToList());
    }

    public async Task<StatusCadastroResponse> ObterStatusCadastroAsync(int usuarioId, CancellationToken ct = default)
    {
        var solicitacao = await _db.SolicitacoesCadastro
            .AsNoTracking()
            .Include(s => s.Pessoa)
            .Include(s => s.UnidadePretendida)
            .Include(s => s.CategoriaAprovada)
            .Where(s => s.UsuarioId == usuarioId)
            .OrderByDescending(s => s.SolicitadaEmUtc)
            .FirstOrDefaultAsync(ct);

        if (solicitacao is null)
            throw new ApiException(StatusCodes.Status404NotFound, CodigosErro.RecursoNaoEncontrado, "Nenhuma solicitação de cadastro encontrada para este usuário.");

        return new StatusCadastroResponse(
            SolicitacaoId: solicitacao.Id,
            PessoaId: solicitacao.PessoaId,
            NomeCompleto: solicitacao.Pessoa.NomeCompleto,
            Email: solicitacao.Pessoa.Email,
            UnidadePretendidaId: solicitacao.UnidadePretendidaId,
            UnidadePretendidaNome: solicitacao.UnidadePretendida.Nome,
            Status: solicitacao.Status,
            JustificativaRejeicao: solicitacao.JustificativaRejeicao,
            CategoriaAprovadaNome: solicitacao.CategoriaAprovada?.Nome,
            SolicitadaEm: TimeZoneHelper.ToSaoPauloOffset(solicitacao.SolicitadaEmUtc),
            DecididaEm: solicitacao.DecididaEmUtc.HasValue ? TimeZoneHelper.ToSaoPauloOffset(solicitacao.DecididaEmUtc.Value) : null);
    }

    private (string Token, DateTime ExpiresAtUtc) GenerateAccessToken(Usuario usuario)
    {
        var nowUtc = DateTime.UtcNow;
        var expiresUtc = nowUtc.AddMinutes(_jwtOptions.AccessTokenMinutes);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, usuario.Id.ToString()),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new(ProClubesClaimTypes.UsuarioId, usuario.Id.ToString()),
            new(ProClubesClaimTypes.PessoaId, usuario.PessoaId.ToString()),
            new(ProClubesClaimTypes.RoleAdministrativa, ((byte)usuario.RoleAdministrativa).ToString()),
            new(ProClubesClaimTypes.EstadoConta, ((byte)usuario.EstadoConta).ToString()),
            new(ProClubesClaimTypes.VersaoSeguranca, usuario.VersaoSeguranca.ToString()),
            new(ClaimTypes.Role, usuario.RoleAdministrativa.ToString())
        };

        if (usuario.UnidadeAdministrativaId.HasValue)
            claims.Add(new Claim(ProClubesClaimTypes.UnidadeAdministrativaId, usuario.UnidadeAdministrativaId.Value.ToString()));

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtOptions.SigningKey));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var jwt = new JwtSecurityToken(
            issuer: _jwtOptions.Issuer,
            audience: _jwtOptions.Audience,
            claims: claims,
            notBefore: nowUtc,
            expires: expiresUtc,
            signingCredentials: creds);

        return (new JwtSecurityTokenHandler().WriteToken(jwt), expiresUtc);
    }
}

public class UnidadeService
{
    private readonly ProClubesDbContext _db;
    private readonly CurrentUserService _currentUser;
    private readonly AuditoriaService _auditoria;

    public UnidadeService(ProClubesDbContext db, CurrentUserService currentUser, AuditoriaService auditoria)
    {
        _db = db;
        _currentUser = currentUser;
        _auditoria = auditoria;
    }

    public async Task<IReadOnlyList<UnidadeDto>> ListarUnidadesPublicasAtivasAsync(CancellationToken ct = default)
    {
        var unidades = await _db.Unidades
            .AsNoTracking()
            .Where(u => u.Ativa)
            .OrderBy(u => u.Nome)
            .ToListAsync(ct);

        return unidades.Select(u => new UnidadeDto(
            u.Id, u.Nome, u.Sigla, u.Logradouro, u.Bairro, u.Numero, u.Cidade, u.UF, u.Fone, u.Email, u.Ativa,
            null, null, Array.Empty<MembroEquipeDto>())).ToList();
    }

    public async Task<IReadOnlyList<UnidadeDto>> ListarUnidadesAsync(AuthenticatedUserInfo user, CancellationToken ct = default)
    {
        var query = _db.Unidades.AsNoTracking().AsQueryable();

        if (user.Role != RoleAdministrativa.SuperAdmin)
        {
            var permitidas = new HashSet<int>();
            if (user.UnidadeAdministrativaId.HasValue) permitidas.Add(user.UnidadeAdministrativaId.Value);
            if (user.UnidadeAssociadoId.HasValue) permitidas.Add(user.UnidadeAssociadoId.Value);
            if (user.UnidadeTitularId.HasValue) permitidas.Add(user.UnidadeTitularId.Value);

            query = query.Where(u => permitidas.Contains(u.Id));
        }

        var unidades = await query.OrderBy(u => u.Nome).ToListAsync(ct);
        var ids = unidades.Select(u => u.Id).ToList();

        var equipe = await _db.Usuarios
            .AsNoTracking()
            .Include(u => u.Pessoa)
            .Where(u => u.UnidadeAdministrativaId.HasValue
                        && ids.Contains(u.UnidadeAdministrativaId.Value)
                        && u.EstadoConta == EstadoConta.Ativa
                        && (u.RoleAdministrativa == RoleAdministrativa.Admin || u.RoleAdministrativa == RoleAdministrativa.Tesoureiro))
            .ToListAsync(ct);

        return unidades.Select(u =>
        {
            var admin = equipe.FirstOrDefault(e => e.UnidadeAdministrativaId == u.Id && e.RoleAdministrativa == RoleAdministrativa.Admin);
            var tesoureiros = equipe
                .Where(e => e.UnidadeAdministrativaId == u.Id && e.RoleAdministrativa == RoleAdministrativa.Tesoureiro)
                .Select(t => new MembroEquipeDto(t.Id, t.PessoaId, t.Pessoa.NomeCompleto, t.Pessoa.Email, t.RoleAdministrativa))
                .ToList();

            return new UnidadeDto(
                u.Id, u.Nome, u.Sigla, u.Logradouro, u.Bairro, u.Numero, u.Cidade, u.UF, u.Fone, u.Email, u.Ativa,
                admin?.Id, admin?.Pessoa.NomeCompleto, tesoureiros);
        }).ToList();
    }

    public async Task<UnidadeDto> ObterUnidadePorIdAsync(AuthenticatedUserInfo user, int unidadeId, CancellationToken ct = default)
    {
        if (user.Role != RoleAdministrativa.SuperAdmin
            && user.UnidadeAdministrativaId != unidadeId
            && user.UnidadeAssociadoId != unidadeId
            && user.UnidadeTitularId != unidadeId)
        {
            throw new ApiException(StatusCodes.Status403Forbidden, CodigosErro.UnidadeSemPermissao, "Você não possui acesso a esta unidade.");
        }

        var lista = await ListarUnidadesAsync(user, ct);
        var unidade = lista.FirstOrDefault(u => u.Id == unidadeId);
        if (unidade is null)
            throw new ApiException(StatusCodes.Status404NotFound, CodigosErro.RecursoNaoEncontrado, "Unidade não encontrada.");

        return unidade;
    }

    public async Task<UnidadeDto> CriarUnidadeAsync(AuthenticatedUserInfo user, CriarOuAtualizarUnidadeRequest req, CancellationToken ct = default)
    {
        _currentUser.EnsureSuperAdmin(user);
        ValidarRequestUnidade(req);

        var siglaNorm = req.Sigla.Trim().ToUpperInvariant();
        if (await _db.Unidades.AnyAsync(u => u.Sigla.ToUpper() == siglaNorm, ct))
            throw new ApiException(StatusCodes.Status409Conflict, CodigosErro.SiglaUnidadeDuplicada, "Já existe uma unidade com esta sigla.");

        var unidade = new Unidade
        {
            Nome = req.Nome.Trim(),
            Sigla = siglaNorm,
            Logradouro = req.Logradouro.Trim(),
            Bairro = req.Bairro.Trim(),
            Numero = req.Numero.Trim(),
            Cidade = req.Cidade.Trim(),
            UF = req.UF.Trim().ToUpperInvariant(),
            Fone = req.Fone.Trim(),
            Email = req.Email.Trim(),
            Ativa = req.Ativa,
            CriadaEmUtc = DateTime.UtcNow
        };

        _db.Unidades.Add(unidade);
        await _db.SaveChangesAsync(ct);

        _auditoria.Registrar(user.UsuarioId, unidade.Id, "CRIAR_UNIDADE", nameof(Unidade), unidade.Id, $"Unidade {unidade.Sigla} criada.");
        await _db.SaveChangesAsync(ct);

        return await ObterUnidadePorIdAsync(user, unidade.Id, ct);
    }

    public async Task<UnidadeDto> AtualizarUnidadeAsync(AuthenticatedUserInfo user, int unidadeId, CriarOuAtualizarUnidadeRequest req, CancellationToken ct = default)
    {
        _currentUser.EnsureSuperAdmin(user);
        ValidarRequestUnidade(req);

        var unidade = await _db.Unidades.FirstOrDefaultAsync(u => u.Id == unidadeId, ct);
        if (unidade is null)
            throw new ApiException(StatusCodes.Status404NotFound, CodigosErro.RecursoNaoEncontrado, "Unidade não encontrada.");

        var siglaNorm = req.Sigla.Trim().ToUpperInvariant();
        if (await _db.Unidades.AnyAsync(u => u.Id != unidadeId && u.Sigla.ToUpper() == siglaNorm, ct))
            throw new ApiException(StatusCodes.Status409Conflict, CodigosErro.SiglaUnidadeDuplicada, "Já existe outra unidade com esta sigla.");

        unidade.Nome = req.Nome.Trim();
        unidade.Sigla = siglaNorm;
        unidade.Logradouro = req.Logradouro.Trim();
        unidade.Bairro = req.Bairro.Trim();
        unidade.Numero = req.Numero.Trim();
        unidade.Cidade = req.Cidade.Trim();
        unidade.UF = req.UF.Trim().ToUpperInvariant();
        unidade.Fone = req.Fone.Trim();
        unidade.Email = req.Email.Trim();
        unidade.Ativa = req.Ativa;

        _auditoria.Registrar(user.UsuarioId, unidade.Id, "ATUALIZAR_UNIDADE", nameof(Unidade), unidade.Id, $"Unidade {unidade.Sigla} atualizada.");
        await _db.SaveChangesAsync(ct);

        return await ObterUnidadePorIdAsync(user, unidade.Id, ct);
    }

    public async Task<UnidadeDto> DesignarRoleUnidadeAsync(
        AuthenticatedUserInfo user,
        int unidadeId,
        DesignarRoleUnidadeRequest req,
        CancellationToken ct = default)
    {
        _currentUser.EnsureSuperAdmin(user);

        var unidade = await _db.Unidades.FirstOrDefaultAsync(u => u.Id == unidadeId, ct);
        if (unidade is null)
            throw new ApiException(StatusCodes.Status404NotFound, CodigosErro.RecursoNaoEncontrado, "Unidade não encontrada.");

        if (req.Role == RoleAdministrativa.SuperAdmin)
            throw new ApiException(StatusCodes.Status400BadRequest, CodigosErro.DadosInvalidos, "Role SuperAdmin não é vinculada a uma unidade.");

        await using var tx = await _db.Database.BeginTransactionAsync(ct);

        // Se UsuarioId for nulo e Role=Admin, remove o Admin atual da unidade (deixando a unidade sem Admin, sob Super Admin)
        if (!req.UsuarioId.HasValue)
        {
            if (req.Role == RoleAdministrativa.Admin)
            {
                var adminAtual = await _db.Usuarios
                    .FirstOrDefaultAsync(u => u.UnidadeAdministrativaId == unidadeId && u.RoleAdministrativa == RoleAdministrativa.Admin && u.EstadoConta == EstadoConta.Ativa, ct);
                if (adminAtual is not null)
                {
                    adminAtual.RoleAdministrativa = RoleAdministrativa.Nenhuma;
                    adminAtual.UnidadeAdministrativaId = null;
                    adminAtual.AdminAtivoUnidadeId = null;
                    adminAtual.VersaoSeguranca++;
                    _auditoria.Registrar(user.UsuarioId, unidadeId, "REMOVER_ADMIN_UNIDADE", nameof(Usuario), adminAtual.Id, $"Admin removido da unidade {unidade.Sigla}.");
                    await _db.SaveChangesAsync(ct);
                }
            }
            await tx.CommitAsync(ct);
            return await ObterUnidadePorIdAsync(user, unidadeId, ct);
        }

        var alvo = await _db.Usuarios
            .Include(u => u.Pessoa)
            .FirstOrDefaultAsync(u => u.Id == req.UsuarioId.Value, ct);

        if (alvo is null)
            throw new ApiException(StatusCodes.Status404NotFound, CodigosErro.RecursoNaoEncontrado, "Usuário alvo não encontrado.");

        if (alvo.RoleAdministrativa == RoleAdministrativa.SuperAdmin)
            throw new ApiException(StatusCodes.Status422UnprocessableEntity, CodigosErro.DadosInvalidos, "Não é permitido alterar a role do Super Admin por este endpoint.");

        if (req.Role == RoleAdministrativa.Nenhuma)
        {
            alvo.RoleAdministrativa = RoleAdministrativa.Nenhuma;
            alvo.UnidadeAdministrativaId = null;
            alvo.AdminAtivoUnidadeId = null;
            alvo.VersaoSeguranca++;
            _auditoria.Registrar(user.UsuarioId, unidadeId, "REVOGAR_ROLE_UNIDADE", nameof(Usuario), alvo.Id, $"Role administrativa removida na unidade {unidade.Sigla}.");
        }
        else if (req.Role == RoleAdministrativa.Admin)
        {
            // Regra 4.1 e 28.3 (4.2): No máximo um Admin ativo por unidade
            var adminExistente = await _db.Usuarios
                .FirstOrDefaultAsync(u => u.UnidadeAdministrativaId == unidadeId
                                          && u.RoleAdministrativa == RoleAdministrativa.Admin
                                          && u.EstadoConta == EstadoConta.Ativa
                                          && u.Id != alvo.Id, ct);
            if (adminExistente is not null)
                throw new ApiException(StatusCodes.Status409Conflict, CodigosErro.UnidadeJaPossuiAdminAtivo, "Esta unidade já possui um Admin ativo. Remova ou substitua o titular antes de designar outro.");

            alvo.RoleAdministrativa = RoleAdministrativa.Admin;
            alvo.UnidadeAdministrativaId = unidadeId;
            alvo.AdminAtivoUnidadeId = unidadeId;
            alvo.EstadoConta = EstadoConta.Ativa;
            alvo.VersaoSeguranca++;
            _auditoria.Registrar(user.UsuarioId, unidadeId, "DESIGNAR_ADMIN_UNIDADE", nameof(Usuario), alvo.Id, $"Designado Admin para a unidade {unidade.Sigla}.");
        }
        else if (req.Role == RoleAdministrativa.Tesoureiro)
        {
            alvo.RoleAdministrativa = RoleAdministrativa.Tesoureiro;
            alvo.UnidadeAdministrativaId = unidadeId;
            alvo.AdminAtivoUnidadeId = null;
            alvo.EstadoConta = EstadoConta.Ativa;
            alvo.VersaoSeguranca++;
            _auditoria.Registrar(user.UsuarioId, unidadeId, "DESIGNAR_TESOUREIRO_UNIDADE", nameof(Usuario), alvo.Id, $"Designado Tesoureiro para a unidade {unidade.Sigla}.");
        }

        await _db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);

        return await ObterUnidadePorIdAsync(user, unidadeId, ct);
    }

    private static void ValidarRequestUnidade(CriarOuAtualizarUnidadeRequest req)
    {
        if (string.IsNullOrWhiteSpace(req.Nome)
            || string.IsNullOrWhiteSpace(req.Sigla)
            || string.IsNullOrWhiteSpace(req.Logradouro)
            || string.IsNullOrWhiteSpace(req.Bairro)
            || string.IsNullOrWhiteSpace(req.Numero)
            || string.IsNullOrWhiteSpace(req.Cidade)
            || string.IsNullOrWhiteSpace(req.UF)
            || req.UF.Trim().Length != 2
            || string.IsNullOrWhiteSpace(req.Fone)
            || string.IsNullOrWhiteSpace(req.Email))
        {
            throw new ApiException(StatusCodes.Status400BadRequest, CodigosErro.DadosInvalidos, "Todos os campos da unidade são obrigatórios e a UF deve conter 2 caracteres.");
        }
    }
}
