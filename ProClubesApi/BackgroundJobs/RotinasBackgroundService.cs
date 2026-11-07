using Microsoft.EntityFrameworkCore;
using ProClubesApi.Data;
using ProClubesApi.Entities;
using ProClubesApi.Services;

namespace ProClubesApi.BackgroundJobs;

public class RotinasAutomaticasRunner
{
    private readonly ProClubesDbContext _db;
    private readonly FinanceiroService _financeiro;
    private readonly AssociadoAndCadastroService _associadoService;
    private readonly VotacaoService _votacaoService;
    private readonly ILogger<RotinasAutomaticasRunner> _logger;

    public RotinasAutomaticasRunner(
        ProClubesDbContext db,
        FinanceiroService financeiro,
        AssociadoAndCadastroService associadoService,
        VotacaoService votacaoService,
        ILogger<RotinasAutomaticasRunner> logger)
    {
        _db = db;
        _financeiro = financeiro;
        _associadoService = associadoService;
        _votacaoService = votacaoService;
        _logger = logger;
    }

    public async Task ExecutarCicloCompletoIdempotenteAsync(CancellationToken ct = default)
    {
        var hoje = TimeZoneHelper.TodaySaoPaulo();
        var competencia = TimeZoneHelper.FirstDayOfMonth(hoje);

        await ExecutarRotinaRastreadaAsync(
            "GERACAO_MENSALIDADES",
            $"{competencia:yyyy-MM}",
            async token => await _financeiro.GerarMensalidadesCompetenciaAsync(competencia, null, token),
            ct);

        await ExecutarRotinaRastreadaAsync(
            "TRANSICAO_DEPENDENTES_22_ANOS",
            $"{hoje:yyyy-MM-dd}",
            async token => await _associadoService.ProcessarTransicaoDependentes22AnosAsync(hoje, token),
            ct);

        await _votacaoService.ProcessarCicloAutomaticoVotacoesAsync(ct);
    }

    public async Task ExecutarRotinaRastreadaAsync(
        string nomeRotina,
        string chaveCompetencia,
        Func<CancellationToken, Task> acao,
        CancellationToken ct = default)
    {
        var exec = await _db.ExecucoesRotina
            .FirstOrDefaultAsync(e => e.NomeRotina == nomeRotina && e.ChaveCompetencia == chaveCompetencia, ct);

        if (exec is null)
        {
            exec = new ExecucaoRotina
            {
                NomeRotina = nomeRotina,
                ChaveCompetencia = chaveCompetencia,
                Estado = EstadoExecucaoRotina.EmExecucao,
                IniciadaEmUtc = DateTime.UtcNow
            };
            _db.ExecucoesRotina.Add(exec);
            await _db.SaveChangesAsync(ct);
        }
        else
        {
            exec.Estado = EstadoExecucaoRotina.EmExecucao;
            exec.IniciadaEmUtc = DateTime.UtcNow;
            exec.ErroResumo = null;
            await _db.SaveChangesAsync(ct);
        }

        try
        {
            await acao(ct);
            exec.Estado = EstadoExecucaoRotina.Concluida;
            exec.ConcluidaEmUtc = DateTime.UtcNow;
            await _db.SaveChangesAsync(ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Falha na rotina automática {NomeRotina} ({Chave})", nomeRotina, chaveCompetencia);
            exec.Estado = EstadoExecucaoRotina.Falhou;
            exec.ConcluidaEmUtc = DateTime.UtcNow;
            exec.ErroResumo = ex.Message.Length > 500 ? ex.Message[..500] : ex.Message;
            await _db.SaveChangesAsync(ct);
        }
    }
}

public class RotinasBackgroundHostedService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<RotinasBackgroundHostedService> _logger;

    public RotinasBackgroundHostedService(IServiceProvider serviceProvider, ILogger<RotinasBackgroundHostedService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var runner = scope.ServiceProvider.GetRequiredService<RotinasAutomaticasRunner>();
                await runner.ExecutarCicloCompletoIdempotenteAsync(stoppingToken);
            }
            catch (Exception ex) when (!stoppingToken.IsCancellationRequested)
            {
                _logger.LogWarning(ex, "Erro durante ciclo periódico de rotinas automáticas.");
            }

            await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
        }
    }
}
