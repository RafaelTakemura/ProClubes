using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using ProClubesApi.Authorization;
using ProClubesApi.Contracts;
using ProClubesApi.Services;

namespace ProClubesApi.Controllers;

[ApiController]
[Route("api/v1")]
public class AuthAndMeController : ControllerBase
{
    private readonly AuthService _authService;
    private readonly CurrentUserService _currentUser;
    private readonly AssociadoAndCadastroService _cadastroService;
    private readonly FinanceiroService _financeiroService;
    private readonly DashboardAndSyncService _syncService;

    public AuthAndMeController(
        AuthService authService,
        CurrentUserService currentUser,
        AssociadoAndCadastroService cadastroService,
        FinanceiroService financeiroService,
        DashboardAndSyncService syncService)
    {
        _authService = authService;
        _currentUser = currentUser;
        _cadastroService = cadastroService;
        _financeiroService = financeiroService;
        _syncService = syncService;
    }

    [AllowAnonymous]
    [EnableRateLimiting("SensitiveEndpoints")]
    [HttpPost("auth/login")]
    public async Task<ActionResult<LoginResponse>> Login([FromBody] LoginRequest request, CancellationToken ct)
    {
        var response = await _authService.LoginAsync(request, ct);
        return Ok(response);
    }

    [AllowAnonymous]
    [EnableRateLimiting("SensitiveEndpoints")]
    [HttpPost("auth/refresh")]
    public async Task<ActionResult<LoginResponse>> Refresh([FromBody] RefreshRequest request, CancellationToken ct)
    {
        var response = await _authService.RefreshAsync(request, ct);
        return Ok(response);
    }

    [Authorize]
    [HttpPost("auth/logout")]
    public async Task<IActionResult> Logout([FromBody] LogoutRequest? request, CancellationToken ct)
    {
        var user = await _currentUser.GetRequiredUserAsync(allowRestritaCadastro: true, ct);
        await _authService.LogoutAsync(user.UsuarioId, request?.RefreshToken, ct);
        return NoContent();
    }

    [Authorize]
    [HttpGet("me")]
    public async Task<ActionResult<PerfilUsuarioDto>> GetMe(CancellationToken ct)
    {
        var user = await _currentUser.GetRequiredUserAsync(allowRestritaCadastro: false, ct);
        var perfil = await _authService.ObterPerfilAsync(user.UsuarioId, ct);
        return Ok(perfil);
    }

    [Authorize]
    [HttpGet("me/cadastro/status")]
    public async Task<ActionResult<StatusCadastroResponse>> GetStatusCadastro(CancellationToken ct)
    {
        var user = await _currentUser.GetRequiredUserAsync(allowRestritaCadastro: true, ct);
        var status = await _authService.ObterStatusCadastroAsync(user.UsuarioId, ct);
        return Ok(status);
    }

    [Authorize]
    [HttpPut("me/cadastro/reenvio")]
    public async Task<ActionResult<StatusCadastroResponse>> ReenviarCadastro(
        [FromBody] ReenviarSolicitacaoCadastroRequest request,
        CancellationToken ct)
    {
        var user = await _currentUser.GetRequiredUserAsync(allowRestritaCadastro: true, ct);
        var status = await _cadastroService.ReenviarSolicitacaoRejeitadaAsync(user, request, ct);
        return Ok(status);
    }

    [Authorize]
    [HttpGet("me/mensalidades")]
    public async Task<ActionResult<IReadOnlyList<MensalidadeDto>>> GetMinhasMensalidades(CancellationToken ct)
    {
        var user = await _currentUser.GetRequiredUserAsync(allowRestritaCadastro: false, ct);
        var list = await _financeiroService.ListarMinhasMensalidadesAsync(user, ct);
        return Ok(list);
    }

    [Authorize]
    [HttpGet("me/sincronizacao")]
    public async Task<ActionResult<ResumoSincronizacaoAppDto>> GetResumoSincronizacao(CancellationToken ct)
    {
        var user = await _currentUser.GetRequiredUserAsync(allowRestritaCadastro: false, ct);
        var resumo = await _syncService.ObterResumoSincronizacaoAppAsync(user, ct);
        return Ok(resumo);
    }
}

[ApiController]
[Route("api/v1")]
public class UnidadesController : ControllerBase
{
    private readonly UnidadeService _unidadeService;
    private readonly DashboardAndSyncService _dashboardService;
    private readonly CurrentUserService _currentUser;

    public UnidadesController(
        UnidadeService unidadeService,
        DashboardAndSyncService dashboardService,
        CurrentUserService currentUser)
    {
        _unidadeService = unidadeService;
        _dashboardService = dashboardService;
        _currentUser = currentUser;
    }

    [AllowAnonymous]
    [HttpGet("unidades/publicas")]
    public async Task<ActionResult<IReadOnlyList<UnidadeDto>>> ListarUnidadesPublicas(CancellationToken ct)
    {
        return Ok(await _unidadeService.ListarUnidadesPublicasAtivasAsync(ct));
    }

    [Authorize]
    [HttpGet("unidades")]
    public async Task<ActionResult<IReadOnlyList<UnidadeDto>>> ListarUnidades(CancellationToken ct)
    {
        var user = await _currentUser.GetRequiredUserAsync(ct: ct);
        return Ok(await _unidadeService.ListarUnidadesAsync(user, ct));
    }

    [Authorize]
    [HttpGet("unidades/{id:int}")]
    public async Task<ActionResult<UnidadeDto>> ObterUnidade(int id, CancellationToken ct)
    {
        var user = await _currentUser.GetRequiredUserAsync(ct: ct);
        return Ok(await _unidadeService.ObterUnidadePorIdAsync(user, id, ct));
    }

    [Authorize]
    [HttpPost("unidades")]
    public async Task<ActionResult<UnidadeDto>> CriarUnidade([FromBody] CriarOuAtualizarUnidadeRequest request, CancellationToken ct)
    {
        var user = await _currentUser.GetRequiredUserAsync(ct: ct);
        var criada = await _unidadeService.CriarUnidadeAsync(user, request, ct);
        return CreatedAtAction(nameof(ObterUnidade), new { id = criada.Id }, criada);
    }

    [Authorize]
    [HttpPut("unidades/{id:int}")]
    public async Task<ActionResult<UnidadeDto>> AtualizarUnidade(int id, [FromBody] CriarOuAtualizarUnidadeRequest request, CancellationToken ct)
    {
        var user = await _currentUser.GetRequiredUserAsync(ct: ct);
        var atualizada = await _unidadeService.AtualizarUnidadeAsync(user, id, request, ct);
        return Ok(atualizada);
    }

    [Authorize]
    [HttpPut("unidades/{id:int}/administrador")]
    public async Task<ActionResult<UnidadeDto>> DesignarAdministradorOuTesoureiro(
        int id,
        [FromBody] DesignarRoleUnidadeRequest request,
        CancellationToken ct)
    {
        var user = await _currentUser.GetRequiredUserAsync(ct: ct);
        var atualizada = await _unidadeService.DesignarRoleUnidadeAsync(user, id, request, ct);
        return Ok(atualizada);
    }

    [Authorize]
    [HttpGet("admin/dashboard")]
    public async Task<ActionResult<DashboardAdminDto>> ObterDashboard([FromQuery] int? unidadeId, CancellationToken ct)
    {
        var user = await _currentUser.GetRequiredUserAsync(ct: ct);
        return Ok(await _dashboardService.ObterDashboardAdminAsync(user, unidadeId, ct));
    }
}
