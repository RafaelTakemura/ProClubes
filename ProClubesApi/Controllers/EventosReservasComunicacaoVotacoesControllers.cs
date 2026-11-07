using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProClubesApi.Authorization;
using ProClubesApi.Contracts;
using ProClubesApi.Entities;
using ProClubesApi.Services;

namespace ProClubesApi.Controllers;

[ApiController]
[Route("api/v1")]
public class EventosEspacosReservasController : ControllerBase
{
    private readonly EventoEspacoReservaService _service;
    private readonly CurrentUserService _currentUser;

    public EventosEspacosReservasController(
        EventoEspacoReservaService service,
        CurrentUserService currentUser)
    {
        _service = service;
        _currentUser = currentUser;
    }

    [AllowAnonymous]
    [HttpGet("eventos/publicos")]
    public async Task<ActionResult<IReadOnlyList<EventoDto>>> ListarEventosPublicos([FromQuery] int? unidadeId, CancellationToken ct)
    {
        return Ok(await _service.ListarEventosPublicosAsync(unidadeId, ct));
    }

    [Authorize]
    [HttpGet("eventos")]
    public async Task<ActionResult<IReadOnlyList<EventoDto>>> ListarEventos([FromQuery] int? unidadeId, CancellationToken ct)
    {
        var user = await _currentUser.GetRequiredUserAsync(ct: ct);
        return Ok(await _service.ListarEventosElegiveisAsync(user, unidadeId, ct));
    }

    [Authorize]
    [HttpPost("admin/eventos")]
    public async Task<ActionResult<EventoDto>> CriarEvento([FromBody] CriarEventoRequest request, CancellationToken ct)
    {
        var user = await _currentUser.GetRequiredUserAsync(ct: ct);
        var criado = await _service.CriarEventoAsync(user, request, ct);
        return StatusCode(StatusCodes.Status201Created, criado);
    }

    [Authorize]
    [HttpPost("admin/eventos/{id:int}/pagamentos")]
    public async Task<ActionResult<PagamentoEventoDto>> RegistrarPagamentoEvento(
        int id,
        [FromBody] RegistrarPagamentoEventoRequest request,
        CancellationToken ct)
    {
        var user = await _currentUser.GetRequiredUserAsync(ct: ct);
        var pgto = await _service.RegistrarPagamentoEventoAsync(user, id, request, ct);
        return StatusCode(StatusCodes.Status201Created, pgto);
    }

    [Authorize]
    [HttpGet("admin/eventos/{id:int}/pagamentos")]
    public async Task<ActionResult<IReadOnlyList<PagamentoEventoDto>>> ListarPagamentosEvento(int id, CancellationToken ct)
    {
        var user = await _currentUser.GetRequiredUserAsync(ct: ct);
        return Ok(await _service.ListarPagamentosEventoAsync(user, id, ct));
    }

    [Authorize]
    [HttpGet("espacos")]
    public async Task<ActionResult<IReadOnlyList<EspacoDto>>> ListarEspacos([FromQuery] int? unidadeId, CancellationToken ct)
    {
        var user = await _currentUser.GetRequiredUserAsync(ct: ct);
        return Ok(await _service.ListarEspacosAsync(user, unidadeId, ct));
    }

    [Authorize]
    [HttpPost("admin/espacos")]
    public async Task<ActionResult<EspacoDto>> CriarEspaco([FromBody] CriarOuAtualizarEspacoRequest request, CancellationToken ct)
    {
        var user = await _currentUser.GetRequiredUserAsync(ct: ct);
        var espaco = await _service.CriarEspacoAsync(user, request, ct);
        return StatusCode(StatusCodes.Status201Created, espaco);
    }

    [Authorize]
    [HttpPost("admin/espacos/bloqueios")]
    public async Task<ActionResult<OcupacaoEspacoDto>> CriarBloqueioManutencao(
        [FromBody] CriarBloqueioManutencaoRequest request,
        CancellationToken ct)
    {
        var user = await _currentUser.GetRequiredUserAsync(ct: ct);
        var ocup = await _service.CriarBloqueioManutencaoAsync(user, request, ct);
        return StatusCode(StatusCodes.Status201Created, ocup);
    }

    [Authorize]
    [HttpGet("espacos/disponibilidade")]
    public async Task<ActionResult<IReadOnlyList<OcupacaoEspacoDto>>> ConsultarDisponibilidade(
        [FromQuery] int espacoId,
        [FromQuery] DateOnly? dataInicio,
        [FromQuery] DateOnly? dataFim,
        CancellationToken ct)
    {
        var user = await _currentUser.GetRequiredUserAsync(ct: ct);
        return Ok(await _service.ConsultarDisponibilidadeEspacoAsync(user, espacoId, dataInicio, dataFim, ct));
    }

    [Authorize]
    [HttpGet("reservas")]
    public async Task<ActionResult<IReadOnlyList<ReservaEspacoDto>>> ListarReservas([FromQuery] int? unidadeId, CancellationToken ct)
    {
        var user = await _currentUser.GetRequiredUserAsync(ct: ct);
        return Ok(await _service.ListarReservasAsync(user, unidadeId, ct));
    }

    [Authorize]
    [HttpPost("reservas")]
    public async Task<ActionResult<ReservaEspacoDto>> CriarReserva([FromBody] CriarReservaRequest request, CancellationToken ct)
    {
        var user = await _currentUser.GetRequiredUserAsync(ct: ct);
        var reserva = await _service.CriarReservaAsync(user, request, ct);
        return StatusCode(StatusCodes.Status201Created, reserva);
    }

    [Authorize]
    [HttpPost("reservas/{id:int}/cancelamento")]
    public async Task<ActionResult<ReservaEspacoDto>> CancelarReserva(
        int id,
        [FromBody] CancelarReservaRequest? request,
        CancellationToken ct)
    {
        var user = await _currentUser.GetRequiredUserAsync(ct: ct);
        var reserva = await _service.CancelarReservaAsync(user, id, request ?? new CancelarReservaRequest(), ct);
        return Ok(reserva);
    }
}

[ApiController]
[Route("api/v1")]
[Authorize]
public class ManifestacoesNotificacoesVotacoesController : ControllerBase
{
    private readonly ManifestacaoService _manifestacaoService;
    private readonly NotificacaoService _notificacaoService;
    private readonly VotacaoService _votacaoService;
    private readonly CurrentUserService _currentUser;

    public ManifestacoesNotificacoesVotacoesController(
        ManifestacaoService manifestacaoService,
        NotificacaoService notificacaoService,
        VotacaoService votacaoService,
        CurrentUserService currentUser)
    {
        _manifestacaoService = manifestacaoService;
        _notificacaoService = notificacaoService;
        _votacaoService = votacaoService;
        _currentUser = currentUser;
    }

    // ==================== MANIFESTAÇÕES ====================

    [HttpGet("manifestacoes/categorias")]
    public async Task<ActionResult<IReadOnlyList<CategoriaManifestacaoDto>>> ListarCategoriasManifestacao(
        [FromQuery] bool incluirInativas = false,
        CancellationToken ct = default)
    {
        var user = await _currentUser.GetRequiredUserAsync(ct: ct);
        var apenasAtivas = !(incluirInativas && user.Role == RoleAdministrativa.SuperAdmin);
        return Ok(await _manifestacaoService.ListarCategoriasAsync(apenasAtivas, ct));
    }

    [HttpPost("admin/manifestacoes/categorias")]
    public async Task<ActionResult<CategoriaManifestacaoDto>> CriarCategoriaManifestacao(
        [FromBody] CriarOuAtualizarCategoriaManifestacaoRequest request,
        CancellationToken ct)
    {
        var user = await _currentUser.GetRequiredUserAsync(ct: ct);
        var criada = await _manifestacaoService.CriarCategoriaAsync(user, request, ct);
        return StatusCode(StatusCodes.Status201Created, criada);
    }

    [HttpPut("admin/manifestacoes/categorias/{id:int}")]
    public async Task<ActionResult<CategoriaManifestacaoDto>> AtualizarCategoriaManifestacao(
        int id,
        [FromBody] CriarOuAtualizarCategoriaManifestacaoRequest request,
        CancellationToken ct)
    {
        var user = await _currentUser.GetRequiredUserAsync(ct: ct);
        return Ok(await _manifestacaoService.AtualizarCategoriaAsync(user, id, request, ct));
    }

    [HttpGet("manifestacoes")]
    public async Task<ActionResult<PagedResponse<ManifestacaoDto>>> ListarManifestacoes(
        [FromQuery] int? unidadeId,
        [FromQuery] StatusManifestacao? status,
        [FromQuery] TipoManifestacao? tipo,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        var user = await _currentUser.GetRequiredUserAsync(ct: ct);
        return Ok(await _manifestacaoService.ListarManifestacoesAsync(user, unidadeId, status, tipo, new PaginationQuery(page, pageSize), ct));
    }

    [HttpGet("manifestacoes/{id:int}")]
    public async Task<ActionResult<ManifestacaoDto>> ObterManifestacao(int id, CancellationToken ct)
    {
        var user = await _currentUser.GetRequiredUserAsync(ct: ct);
        return Ok(await _manifestacaoService.ObterManifestacaoPorIdAsync(user, id, ct));
    }

    [HttpPost("manifestacoes")]
    public async Task<ActionResult<ManifestacaoDto>> CriarManifestacao(
        [FromBody] CriarManifestacaoRequest request,
        CancellationToken ct)
    {
        var user = await _currentUser.GetRequiredUserAsync(ct: ct);
        var criada = await _manifestacaoService.CriarManifestacaoAsync(user, request, ct);
        return CreatedAtAction(nameof(ObterManifestacao), new { id = criada.Id }, criada);
    }

    [HttpPost("manifestacoes/{id:int}/respostas")]
    public async Task<ActionResult<ManifestacaoDto>> ResponderManifestacao(
        int id,
        [FromBody] ResponderManifestacaoRequest request,
        CancellationToken ct)
    {
        var user = await _currentUser.GetRequiredUserAsync(ct: ct);
        return Ok(await _manifestacaoService.ResponderManifestacaoAsync(user, id, request, ct));
    }

    // ==================== NOTIFICAÇÕES ====================

    [HttpGet("notificacoes")]
    public async Task<ActionResult<PagedResponse<NotificacaoDto>>> ListarNotificacoes(
        [FromQuery] bool? apenasNaoLidas,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        var user = await _currentUser.GetRequiredUserAsync(allowRestritaCadastro: true, ct);
        return Ok(await _notificacaoService.ListarNotificacoesUsuarioAsync(user.UsuarioId, apenasNaoLidas, new PaginationQuery(page, pageSize), ct));
    }

    [HttpPatch("notificacoes/{id:int}/leitura")]
    public async Task<ActionResult<NotificacaoDto>> MarcarNotificacaoLida(int id, CancellationToken ct)
    {
        var user = await _currentUser.GetRequiredUserAsync(allowRestritaCadastro: true, ct);
        return Ok(await _notificacaoService.MarcarComoLidaAsync(user.UsuarioId, id, ct));
    }

    // ==================== VOTAÇÕES ====================

    [HttpGet("votacoes")]
    public async Task<ActionResult<IReadOnlyList<VotacaoDto>>> ListarVotacoes([FromQuery] int? unidadeId, CancellationToken ct)
    {
        var user = await _currentUser.GetRequiredUserAsync(ct: ct);
        return Ok(await _votacaoService.ListarVotacoesAsync(user, unidadeId, ct));
    }

    [HttpGet("votacoes/{id:int}")]
    public async Task<ActionResult<VotacaoDto>> ObterVotacao(int id, CancellationToken ct)
    {
        var user = await _currentUser.GetRequiredUserAsync(ct: ct);
        return Ok(await _votacaoService.ObterVotacaoPorIdAsync(user, id, ct));
    }

    [HttpPost("admin/votacoes")]
    public async Task<ActionResult<VotacaoDto>> CriarVotacao([FromBody] CriarVotacaoRequest request, CancellationToken ct)
    {
        var user = await _currentUser.GetRequiredUserAsync(ct: ct);
        var criada = await _votacaoService.CriarVotacaoAsync(user, request, ct);
        return CreatedAtAction(nameof(ObterVotacao), new { id = criada.Id }, criada);
    }

    [HttpPost("admin/votacoes/{id:int}/abertura")]
    public async Task<ActionResult<VotacaoDto>> AbrirVotacao(int id, CancellationToken ct)
    {
        var user = await _currentUser.GetRequiredUserAsync(ct: ct);
        return Ok(await _votacaoService.AbrirVotacaoAsync(user, id, ct));
    }

    [HttpPost("votacoes/{id:int}/votos")]
    public async Task<ActionResult<VotacaoDto>> RegistrarVoto(int id, [FromBody] RegistrarVotoRequest request, CancellationToken ct)
    {
        var user = await _currentUser.GetRequiredUserAsync(ct: ct);
        return Ok(await _votacaoService.RegistrarVotoAsync(user, id, request, ct));
    }

    [HttpGet("votacoes/{id:int}/resultados")]
    public async Task<ActionResult<ResultadoVotacaoDto>> ObterResultadosAgregados(int id, CancellationToken ct)
    {
        var user = await _currentUser.GetRequiredUserAsync(ct: ct);
        return Ok(await _votacaoService.ObterResultadosAgregadosAsync(user, id, ct));
    }

    [HttpPost("admin/votacoes/{id:int}/encerramento")]
    public async Task<ActionResult<VotacaoDto>> EncerrarVotacao(int id, CancellationToken ct)
    {
        var user = await _currentUser.GetRequiredUserAsync(ct: ct);
        return Ok(await _votacaoService.EncerrarVotacaoAsync(user, id, ct));
    }

    [HttpPost("admin/votacoes/{id:int}/cancelamento")]
    public async Task<ActionResult<VotacaoDto>> CancelarVotacao(int id, [FromBody] CancelarVotacaoRequest request, CancellationToken ct)
    {
        var user = await _currentUser.GetRequiredUserAsync(ct: ct);
        return Ok(await _votacaoService.CancelarVotacaoAsync(user, id, request, ct));
    }

    [HttpGet("admin/votacoes/{id:int}/votos")]
    public async Task<ActionResult<IReadOnlyList<VotoIndividualAuditoriaDto>>> ConsultarVotosIndividuais(int id, CancellationToken ct)
    {
        var user = await _currentUser.GetRequiredUserAsync(ct: ct);
        return Ok(await _votacaoService.ConsultarVotosIndividuaisAsync(user, id, ct));
    }
}
