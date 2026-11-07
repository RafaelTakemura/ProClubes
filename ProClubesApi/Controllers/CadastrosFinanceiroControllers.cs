using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using ProClubesApi.Authorization;
using ProClubesApi.Contracts;
using ProClubesApi.Data;
using ProClubesApi.Entities;
using ProClubesApi.Services;

namespace ProClubesApi.Controllers;

[ApiController]
[Route("api/v1")]
public class CadastrosEAssociadosController : ControllerBase
{
    private readonly AssociadoAndCadastroService _service;
    private readonly CurrentUserService _currentUser;
    private readonly ProClubesDbContext _db;

    public CadastrosEAssociadosController(
        AssociadoAndCadastroService service,
        CurrentUserService currentUser,
        ProClubesDbContext db)
    {
        _service = service;
        _currentUser = currentUser;
        _db = db;
    }

    [AllowAnonymous]
    [HttpGet("interesses")]
    public async Task<ActionResult<IReadOnlyList<Interesse>>> ListarInteresses(CancellationToken ct)
    {
        var list = await _db.Interesses.AsNoTracking().Where(i => i.Ativo).OrderBy(i => i.Nome).ToListAsync(ct);
        return Ok(list);
    }

    [AllowAnonymous]
    [EnableRateLimiting("SensitiveEndpoints")]
    [HttpPost("cadastros/solicitacoes")]
    public async Task<ActionResult<StatusCadastroResponse>> SolicitarCadastro(
        [FromBody] SolicitarCadastroRequest request,
        CancellationToken ct)
    {
        var status = await _service.SolicitarCadastroAsync(request, ct);
        return StatusCode(StatusCodes.Status201Created, status);
    }

    [Authorize]
    [HttpGet("admin/cadastros/solicitacoes")]
    public async Task<ActionResult<PagedResponse<SolicitacaoCadastroListItemDto>>> ListarSolicitacoes(
        [FromQuery] int? unidadeId,
        [FromQuery] StatusSolicitacaoCadastro? status,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        var user = await _currentUser.GetRequiredUserAsync(ct: ct);
        var result = await _service.ListarSolicitacoesAsync(user, unidadeId, status, new PaginationQuery(page, pageSize), ct);
        return Ok(result);
    }

    [Authorize]
    [HttpPost("admin/cadastros/solicitacoes/{id:int}/aprovar")]
    public async Task<ActionResult<SolicitacaoCadastroListItemDto>> AprovarSolicitacao(
        int id,
        [FromBody] AprovarSolicitacaoRequest request,
        CancellationToken ct)
    {
        var user = await _currentUser.GetRequiredUserAsync(ct: ct);
        var result = await _service.AprovarSolicitacaoAsync(user, id, request, ct);
        return Ok(result);
    }

    [Authorize]
    [HttpPost("admin/cadastros/solicitacoes/{id:int}/rejeitar")]
    public async Task<ActionResult<SolicitacaoCadastroListItemDto>> RejeitarSolicitacao(
        int id,
        [FromBody] RejeitarSolicitacaoRequest request,
        CancellationToken ct)
    {
        var user = await _currentUser.GetRequiredUserAsync(ct: ct);
        var result = await _service.RejeitarSolicitacaoAsync(user, id, request, ct);
        return Ok(result);
    }

    [Authorize]
    [HttpGet("admin/associados")]
    public async Task<ActionResult<PagedResponse<AssociadoDto>>> ListarAssociados(
        [FromQuery] int? unidadeId,
        [FromQuery] string? codigoCategoria,
        [FromQuery] StatusAssociado? status,
        [FromQuery] string? busca,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        var user = await _currentUser.GetRequiredUserAsync(ct: ct);
        var result = await _service.ListarAssociadosAsync(user, unidadeId, codigoCategoria, status, busca, new PaginationQuery(page, pageSize), ct);
        return Ok(result);
    }

    [Authorize]
    [HttpGet("admin/associados/{id:int}")]
    public async Task<ActionResult<AssociadoDto>> ObterAssociado(int id, CancellationToken ct)
    {
        var user = await _currentUser.GetRequiredUserAsync(ct: ct);
        return Ok(await _service.ObterAssociadoPorIdAsync(user, id, ct));
    }

    [Authorize]
    [HttpPost("admin/associados")]
    public async Task<ActionResult<AssociadoDto>> CriarAssociado([FromBody] CriarAssociadoAdminRequest request, CancellationToken ct)
    {
        var user = await _currentUser.GetRequiredUserAsync(ct: ct);
        var criado = await _service.CriarAssociadoAdminAsync(user, request, ct);
        return CreatedAtAction(nameof(ObterAssociado), new { id = criado.Id }, criado);
    }

    [Authorize]
    [HttpPut("admin/associados/{id:int}/categoria")]
    public async Task<ActionResult<AssociadoDto>> AlterarCategoria(
        int id,
        [FromBody] AlterarCategoriaAssociadoRequest request,
        CancellationToken ct)
    {
        var user = await _currentUser.GetRequiredUserAsync(ct: ct);
        return Ok(await _service.AlterarCategoriaAssociadoAsync(user, id, request, ct));
    }

    [Authorize]
    [HttpPatch("admin/associados/{id:int}/status")]
    public async Task<ActionResult<AssociadoDto>> AlterarStatus(
        int id,
        [FromBody] AlterarStatusAssociadoRequest request,
        CancellationToken ct)
    {
        var user = await _currentUser.GetRequiredUserAsync(ct: ct);
        return Ok(await _service.AlterarStatusAssociadoAsync(user, id, request, ct));
    }

    [Authorize]
    [HttpPost("admin/associados/{id:int}/transferencias")]
    public async Task<ActionResult<TransferenciaHistoricoDto>> TransferirUnidade(
        int id,
        [FromBody] TransferirAssociadoUnidadeRequest request,
        CancellationToken ct)
    {
        var user = await _currentUser.GetRequiredUserAsync(ct: ct);
        return Ok(await _service.TransferirAssociadoUnidadeAsync(user, id, request, ct));
    }

    [Authorize]
    [HttpPost("admin/associados/{id:int}/dependentes")]
    public async Task<ActionResult<DependenteDto>> CadastrarDependente(
        int id,
        [FromBody] CadastrarDependenteRequest request,
        CancellationToken ct)
    {
        var user = await _currentUser.GetRequiredUserAsync(ct: ct);
        var dep = await _service.CadastrarDependenteAsync(user, id, request, ct);
        return StatusCode(StatusCodes.Status201Created, dep);
    }
}

[ApiController]
[Route("api/v1")]
[Authorize]
public class FinanceiroController : ControllerBase
{
    private readonly FinanceiroService _financeiro;
    private readonly CurrentUserService _currentUser;

    public FinanceiroController(FinanceiroService financeiro, CurrentUserService currentUser)
    {
        _financeiro = financeiro;
        _currentUser = currentUser;
    }

    [HttpGet("admin/financeiro/configuracao")]
    public async Task<ActionResult<ConfiguracaoFinanceiraDto>> ObterConfiguracao(CancellationToken ct)
    {
        var user = await _currentUser.GetRequiredUserAsync(ct: ct);
        return Ok(await _financeiro.ObterConfiguracaoAsync(user, ct));
    }

    [HttpPut("admin/financeiro/configuracao")]
    public async Task<ActionResult<ConfiguracaoFinanceiraDto>> AtualizarConfiguracao(
        [FromBody] AtualizarConfiguracaoFinanceiraRequest request,
        CancellationToken ct)
    {
        var user = await _currentUser.GetRequiredUserAsync(ct: ct);
        return Ok(await _financeiro.AtualizarConfiguracaoAsync(user, request, ct));
    }

    [HttpGet("admin/financeiro/valores")]
    public async Task<ActionResult<IReadOnlyList<ValorMensalidadeDto>>> ListarValores([FromQuery] int? unidadeId, CancellationToken ct)
    {
        var user = await _currentUser.GetRequiredUserAsync(ct: ct);
        return Ok(await _financeiro.ListarValoresMensalidadeAsync(user, unidadeId, ct));
    }

    [HttpPost("admin/financeiro/valores")]
    public async Task<ActionResult<ValorMensalidadeDto>> DefinirValor([FromBody] DefinirValorMensalidadeRequest request, CancellationToken ct)
    {
        var user = await _currentUser.GetRequiredUserAsync(ct: ct);
        var valor = await _financeiro.DefinirValorMensalidadeAsync(user, request, ct);
        return StatusCode(StatusCodes.Status201Created, valor);
    }

    [HttpGet("admin/mensalidades")]
    public async Task<ActionResult<PagedResponse<MensalidadeDto>>> ListarMensalidades(
        [FromQuery] int? unidadeId,
        [FromQuery] int? associadoId,
        [FromQuery] DateOnly? competencia,
        [FromQuery] string? situacao,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        var user = await _currentUser.GetRequiredUserAsync(ct: ct);
        var result = await _financeiro.ListarMensalidadesAdminAsync(
            user, unidadeId, associadoId, competencia, situacao, new PaginationQuery(page, pageSize), ct);
        return Ok(result);
    }

    [HttpPost("admin/mensalidades/{id:int}/pagamentos")]
    public async Task<ActionResult<MensalidadeDto>> RegistrarPagamento(
        int id,
        [FromBody] RegistrarPagamentoMensalidadeRequest request,
        CancellationToken ct)
    {
        var user = await _currentUser.GetRequiredUserAsync(ct: ct);
        var paga = await _financeiro.RegistrarPagamentoMensalidadeAsync(user, id, request, ct);
        return Ok(paga);
    }
}
