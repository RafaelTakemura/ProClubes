using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ProClubesApi.Contracts;
using ProClubesApi.Data;
using ProClubesApi.Entities;
using ProClubesApi.Services;

namespace ProClubes.IntegrationTests;

public sealed class ProClubesTestFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly string _dbFilePath = Path.Combine(Path.GetTempPath(), $"proclubes_test_{Guid.NewGuid():N}.db");

    public static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters = { new JsonStringEnumConverter() }
    };

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Database:Provider"] = "Sqlite",
                ["ConnectionStrings:SqliteFallback"] = $"Data Source={_dbFilePath}",
                ["Database:AutoInitialize"] = "true",
                ["BackgroundJobs:Enabled"] = "false",
                ["RateLimiting:PermitLimit"] = "1000",
                ["Jwt:SigningKey"] = "TestOnly_Jwt_SigningKey_NotForProduction_Min32Bytes_2026!",
                ["BootstrapSuperAdmin:Email"] = "superadmin@proclubes.com.br",
                ["BootstrapSuperAdmin:Password"] = "SuperAdmin@2026!",
                ["BootstrapSuperAdmin:Cpf"] = "11122233344"
            });
        });
    }

    public Task InitializeAsync() => Task.CompletedTask;

    public new async Task DisposeAsync()
    {
        await base.DisposeAsync();
        try
        {
            if (File.Exists(_dbFilePath))
                File.Delete(_dbFilePath);
        }
        catch
        {
            // Ignora arquivo ainda liberando lock no Windows
        }
    }

    public async Task<HttpClient> CreateAuthenticatedClientAsync(string identificador, string senha)
    {
        var client = CreateClient();
        var resp = await client.PostAsJsonAsync("/api/v1/auth/login", new LoginRequest(identificador, senha), JsonOpts);
        resp.EnsureSuccessStatusCode();
        var login = await resp.Content.ReadFromJsonAsync<LoginResponse>(JsonOpts);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", login!.AccessToken);
        return client;
    }

    public Task<HttpClient> CreateSuperAdminClientAsync()
        => CreateAuthenticatedClientAsync("superadmin@proclubes.com.br", "SuperAdmin@2026!");
}

public class ProClubesApiIntegrationTests
{
    // Cenário 1: Login válido, tentativa inválida, rotação de refresh token e revogação no logout
    [Fact]
    public async Task Cenario01_Auth_LoginValido_TentativaInvalida_RefreshRotation_ELogout()
    {
        await using var factory = new ProClubesTestFactory();
        var client = factory.CreateClient();

        // Tentativa inválida -> 401 CREDENCIAIS_INVALIDAS
        var badResp = await client.PostAsJsonAsync("/api/v1/auth/login", new LoginRequest("superadmin@proclubes.com.br", "SenhaIncorreta!"), ProClubesTestFactory.JsonOpts);
        Assert.Equal(HttpStatusCode.Unauthorized, badResp.StatusCode);

        // Login válido
        var okResp = await client.PostAsJsonAsync("/api/v1/auth/login", new LoginRequest("superadmin@proclubes.com.br", "SuperAdmin@2026!"), ProClubesTestFactory.JsonOpts);
        Assert.Equal(HttpStatusCode.OK, okResp.StatusCode);
        var loginData = await okResp.Content.ReadFromJsonAsync<LoginResponse>(ProClubesTestFactory.JsonOpts);
        Assert.NotNull(loginData);
        Assert.False(string.IsNullOrWhiteSpace(loginData.AccessToken));

        // Refresh token rotation
        var refreshResp = await client.PostAsJsonAsync("/api/v1/auth/refresh", new RefreshRequest(loginData.RefreshToken), ProClubesTestFactory.JsonOpts);
        Assert.Equal(HttpStatusCode.OK, refreshResp.StatusCode);
        var rotated = await refreshResp.Content.ReadFromJsonAsync<LoginResponse>(ProClubesTestFactory.JsonOpts);
        Assert.NotNull(rotated);
        Assert.NotEqual(loginData.RefreshToken, rotated.RefreshToken);

        // Refresh antigo revogado -> falha ao reutilizar
        var reuseOld = await client.PostAsJsonAsync("/api/v1/auth/refresh", new RefreshRequest(loginData.RefreshToken), ProClubesTestFactory.JsonOpts);
        Assert.Equal(HttpStatusCode.Unauthorized, reuseOld.StatusCode);

        // Logout revoga refresh atual
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", rotated.AccessToken);
        var logoutResp = await client.PostAsJsonAsync("/api/v1/auth/logout", new LogoutRequest(rotated.RefreshToken), ProClubesTestFactory.JsonOpts);
        Assert.Equal(HttpStatusCode.NoContent, logoutResp.StatusCode);

        var afterLogout = await client.PostAsJsonAsync("/api/v1/auth/refresh", new RefreshRequest(rotated.RefreshToken), ProClubesTestFactory.JsonOpts);
        Assert.Equal(HttpStatusCode.Unauthorized, afterLogout.StatusCode);
    }

    // Cenário 2: Conta com cadastro pendente/rejeitado acessa apenas acompanhamento de status e reenvia sem duplicar CPF
    [Fact]
    public async Task Cenario02_CadastroRestrito_AcessaApenasStatus_EReenviaAposRejeicaoSemDuplicarCpf()
    {
        await using var factory = new ProClubesTestFactory();
        var anonClient = factory.CreateClient();
        var saClient = await factory.CreateSuperAdminClientAsync();

        var solReq = new SolicitarCadastroRequest(
            "Mariana Candidata",
            "22233344455",
            "Feminino",
            "Solteiro(a)",
            new DateOnly(1996, 4, 20),
            "Rua das Palmeiras",
            "Centro",
            "45",
            "Foz do Iguaçu",
            "PR",
            "(45) 98888-1111",
            "mariana@teste.com",
            1,
            "Mariana@2026!");

        var createResp = await anonClient.PostAsJsonAsync("/api/v1/cadastros/solicitacoes", solReq, ProClubesTestFactory.JsonOpts);
        Assert.Equal(HttpStatusCode.Created, createResp.StatusCode);
        var statusInicial = await createResp.Content.ReadFromJsonAsync<StatusCadastroResponse>(ProClubesTestFactory.JsonOpts);
        Assert.NotNull(statusInicial);
        Assert.Equal(StatusSolicitacaoCadastro.Pendente, statusInicial.Status);

        // Login da candidata (EstadoConta = RestritaCadastro)
        var candClient = await factory.CreateAuthenticatedClientAsync("mariana@teste.com", "Mariana@2026!");

        // Bloqueada nos módulos normais (/api/v1/me retorna 403 CADASTRO_PENDENTE)
        var meResp = await candClient.GetAsync("/api/v1/me");
        Assert.Equal(HttpStatusCode.Forbidden, meResp.StatusCode);

        // Permitida em /api/v1/me/cadastro/status
        var stResp = await candClient.GetAsync("/api/v1/me/cadastro/status");
        Assert.Equal(HttpStatusCode.OK, stResp.StatusCode);

        // Super Admin rejeita a solicitação com justificativa
        var rejResp = await saClient.PostAsJsonAsync(
            $"/api/v1/admin/cadastros/solicitacoes/{statusInicial.SolicitacaoId}/rejeitar",
            new RejeitarSolicitacaoRequest("Comprovante de endereço ilegível"),
            ProClubesTestFactory.JsonOpts);
        Assert.Equal(HttpStatusCode.OK, rejResp.StatusCode);

        // Candidata reenvia usando a mesma conta e mesmo CPF sem duplicação
        var reenvioReq = new ReenviarSolicitacaoCadastroRequest(
            "Mariana Candidata Silva",
            "Feminino",
            "Solteiro(a)",
            new DateOnly(1996, 4, 20),
            "Rua das Palmeiras Atualizada",
            "Centro",
            "450",
            "Foz do Iguaçu",
            "PR",
            "(45) 98888-1111",
            "mariana@teste.com",
            1);

        var reenvioResp = await candClient.PutAsJsonAsync("/api/v1/me/cadastro/reenvio", reenvioReq, ProClubesTestFactory.JsonOpts);
        Assert.Equal(HttpStatusCode.OK, reenvioResp.StatusCode);
        var statusReenviado = await reenvioResp.Content.ReadFromJsonAsync<StatusCadastroResponse>(ProClubesTestFactory.JsonOpts);
        Assert.Equal(StatusSolicitacaoCadastro.Pendente, statusReenviado!.Status);
        Assert.Equal(statusInicial.PessoaId, statusReenviado.PessoaId);
    }

    // Cenários 3 e 4: Escopo por unidade (Admin/Tesoureiro restritos à própria unidade) e no máximo 1 Admin ativo por unidade
    [Fact]
    public async Task Cenarios03_04_EscopoPorUnidade_EMaximoUmAdminAtivoPorUnidade()
    {
        await using var factory = new ProClubesTestFactory();
        var saClient = await factory.CreateSuperAdminClientAsync();

        // Criar 2 associados na Unidade 1 (Foz) para testar designação de Admin
        var a1 = await (await saClient.PostAsJsonAsync("/api/v1/admin/associados", new CriarAssociadoAdminRequest(
            "Carlos Admin Foz", "33344455566", "Masculino", "Casado(a)", new DateOnly(1985, 5, 10),
            "Rua A", "Centro", "10", "Foz do Iguaçu", "PR", "45999991111", "carlos.admin@teste.com", 1, "ASSOCIADO", "Senha@2026!"), ProClubesTestFactory.JsonOpts))
            .Content.ReadFromJsonAsync<AssociadoDto>(ProClubesTestFactory.JsonOpts);

        var a2 = await (await saClient.PostAsJsonAsync("/api/v1/admin/associados", new CriarAssociadoAdminRequest(
            "Roberto Segundo Admin", "44455566677", "Masculino", "Casado(a)", new DateOnly(1986, 6, 12),
            "Rua B", "Centro", "20", "Foz do Iguaçu", "PR", "45999992222", "roberto.admin@teste.com", 1, "ASSOCIADO", "Senha@2026!"), ProClubesTestFactory.JsonOpts))
            .Content.ReadFromJsonAsync<AssociadoDto>(ProClubesTestFactory.JsonOpts);

        // Designar Carlos como Admin da Unidade 1
        var desig1 = await saClient.PutAsJsonAsync("/api/v1/unidades/1/administrador",
            new DesignarRoleUnidadeRequest(a1!.UsuarioId, RoleAdministrativa.Admin), ProClubesTestFactory.JsonOpts);
        Assert.Equal(HttpStatusCode.OK, desig1.StatusCode);

        // Tentar designar Roberto como segundo Admin ativo da Unidade 1 -> 409 UNIDADE_JA_POSSUI_ADMIN_ATIVO (Cenário 4)
        var desig2 = await saClient.PutAsJsonAsync("/api/v1/unidades/1/administrador",
            new DesignarRoleUnidadeRequest(a2!.UsuarioId, RoleAdministrativa.Admin), ProClubesTestFactory.JsonOpts);
        Assert.Equal(HttpStatusCode.Conflict, desig2.StatusCode);

        // Admin da Unidade 1 não consegue acessar dados da Unidade 2 (Cenário 3)
        var adminUnid1Client = await factory.CreateAuthenticatedClientAsync("carlos.admin@teste.com", "Senha@2026!");
        var unid2Resp = await adminUnid1Client.GetAsync("/api/v1/unidades/2");
        Assert.Equal(HttpStatusCode.Forbidden, unid2Resp.StatusCode);

        // Mas acessa normalmente sua própria Unidade 1
        var unid1Resp = await adminUnid1Client.GetAsync("/api/v1/unidades/1");
        Assert.Equal(HttpStatusCode.OK, unid1Resp.StatusCode);
    }

    // Cenários 5, 7, 8 e 9: Transferência entre unidades preserva histórico financeiro; alteração de categoria no mês seguinte;
    // geração idempotente de mensalidades + dia 31 em fevereiro; e pagamentos separados de mensalidade vs evento
    [Fact]
    public async Task Cenarios05_07_08_09_TransferenciaUnidade_VigenciaMesSeguinte_MensalidadesIdempotentes_EPagamentosSeparados()
    {
        await using var factory = new ProClubesTestFactory();
        var saClient = await factory.CreateSuperAdminClientAsync();

        var assoc = await (await saClient.PostAsJsonAsync("/api/v1/admin/associados", new CriarAssociadoAdminRequest(
            "Fernando Associado", "55566677788", "Masculino", "Solteiro(a)", new DateOnly(1990, 3, 15),
            "Rua C", "Centro", "30", "Foz do Iguaçu", "PR", "45999993333", "fernando@teste.com", 1, "ASSOCIADO", "Senha@2026!"), ProClubesTestFactory.JsonOpts))
            .Content.ReadFromJsonAsync<AssociadoDto>(ProClubesTestFactory.JsonOpts);

        // Configurar vencimento no dia 31 e testar geração idempotente em fevereiro de 2026 (Cenário 8)
        await saClient.PutAsJsonAsync("/api/v1/admin/financeiro/configuracao",
            new AtualizarConfiguracaoFinanceiraRequest(1, 31, 25, 2.0m, 1.0m), ProClubesTestFactory.JsonOpts);

        using (var scope = factory.Services.CreateScope())
        {
            var finService = scope.ServiceProvider.GetRequiredService<FinanceiroService>();
            var db = scope.ServiceProvider.GetRequiredService<ProClubesDbContext>();

            // Ajustar DataInicioCobranca para 2026-02-01 para permitir emissão na competência 02/2026
            var dbAssoc = await db.Associados.FindAsync(assoc!.Id);
            dbAssoc!.DataInicioCobranca = new DateOnly(2026, 2, 1);
            await db.SaveChangesAsync();

            var geradas1 = await finService.GerarMensalidadesCompetenciaAsync(new DateOnly(2026, 2, 1), null);
            var geradas2 = await finService.GerarMensalidadesCompetenciaAsync(new DateOnly(2026, 2, 1), null);
            Assert.True(geradas1 >= 1);
            Assert.Equal(0, geradas2); // Idempotência comprovada!
        }

        var mensResp = await saClient.GetFromJsonAsync<PagedResponse<MensalidadeDto>>(
            $"/api/v1/admin/mensalidades?associadoId={assoc!.Id}", ProClubesTestFactory.JsonOpts);
        Assert.Single(mensResp!.Items);
        var mensFev = mensResp.Items[0];
        Assert.Equal(new DateOnly(2026, 2, 28), mensFev.DataVencimento); // Dia 31 em Fev/2026 -> 28/02/2026
        Assert.Equal(1, mensFev.UnidadeEmissoraId);

        // Cenário 7: Alteração de categoria agendada para o próximo mês sem pro-rata na competência já emitida
        var altCatResp = await saClient.PutAsJsonAsync($"/api/v1/admin/associados/{assoc.Id}/categoria",
            new AlterarCategoriaAssociadoRequest("FUNDADOR", "Promoção aprovada pela diretoria"), ProClubesTestFactory.JsonOpts);
        Assert.Equal(HttpStatusCode.OK, altCatResp.StatusCode);

        // Cenário 5: Super Admin transfere associado para a Unidade 2; mensalidade anterior permanece vinculada à Unidade 1
        var transfResp = await saClient.PostAsJsonAsync($"/api/v1/admin/associados/{assoc.Id}/transferencias",
            new TransferirAssociadoUnidadeRequest(2), ProClubesTestFactory.JsonOpts);
        Assert.Equal(HttpStatusCode.OK, transfResp.StatusCode);

        var mensAposTransf = await saClient.GetFromJsonAsync<PagedResponse<MensalidadeDto>>(
            $"/api/v1/admin/mensalidades?associadoId={assoc.Id}", ProClubesTestFactory.JsonOpts);
        Assert.Equal(1, mensAposTransf!.Items[0].UnidadeEmissoraId); // Permanece na unidade de origem (1)

        // Cenário 9: Pagamento de mensalidade e pagamento de evento registrados separadamente
        var pgtoMensResp = await saClient.PostAsJsonAsync($"/api/v1/admin/mensalidades/{mensFev.Id}/pagamentos",
            new RegistrarPagamentoMensalidadeRequest(new DateOnly(2026, 2, 20), FormaPagamento.Pix), ProClubesTestFactory.JsonOpts);
        Assert.Equal(HttpStatusCode.OK, pgtoMensResp.StatusCode);
        var mensPaga = await pgtoMensResp.Content.ReadFromJsonAsync<MensalidadeDto>(ProClubesTestFactory.JsonOpts);
        Assert.Equal(StatusMensalidade.Paga, mensPaga!.Status);

        var evCriado = await (await saClient.PostAsJsonAsync("/api/v1/admin/eventos", new CriarEventoRequest(
            1, "Jantar Dançante", "Evento social pago", PublicoAlvoEvento.Publico,
            DateTimeOffset.UtcNow.AddDays(10), DateTimeOffset.UtcNow.AddDays(10).AddHours(4), 100, 50.00m), ProClubesTestFactory.JsonOpts))
            .Content.ReadFromJsonAsync<EventoDto>(ProClubesTestFactory.JsonOpts);

        var pgtoEvResp = await saClient.PostAsJsonAsync($"/api/v1/admin/eventos/{evCriado!.Id}/pagamentos",
            new RegistrarPagamentoEventoRequest(assoc.PessoaId, null, 50.00m, FormaPagamento.CartaoCredito, new DateOnly(2026, 2, 20)), ProClubesTestFactory.JsonOpts);
        Assert.Equal(HttpStatusCode.Created, pgtoEvResp.StatusCode);
    }

    // Cenários 6 e 16: Dependente que completa 22 anos migra automaticamente para Simpatizante preservando Pessoa/Usuario;
    // Eventos públicos sem login, dependente vê restrito da unidade do titular e simpatizante vê apenas eventos públicos
    [Fact]
    public async Task Cenarios06_16_TransicaoDependente22AnosParaSimpatizante_EVisibilidadeDeEventos()
    {
        await using var factory = new ProClubesTestFactory();
        var saClient = await factory.CreateSuperAdminClientAsync();
        var anonClient = factory.CreateClient();

        // Criar evento público e evento restrito na Unidade 1
        await saClient.PostAsJsonAsync("/api/v1/admin/eventos", new CriarEventoRequest(
            1, "Feira Cultural Aberta", "Evento público", PublicoAlvoEvento.Publico,
            DateTimeOffset.UtcNow.AddDays(5), DateTimeOffset.UtcNow.AddDays(5).AddHours(3), null, 0m), ProClubesTestFactory.JsonOpts);

        await saClient.PostAsJsonAsync("/api/v1/admin/eventos", new CriarEventoRequest(
            1, "Torneio Interno de Tênis", "Exclusivo associados Unidade 1", PublicoAlvoEvento.Associados,
            DateTimeOffset.UtcNow.AddDays(6), DateTimeOffset.UtcNow.AddDays(6).AddHours(3), null, 0m), ProClubesTestFactory.JsonOpts);

        // Cenário 16 (parte 1): Consulta pública sem login retorna apenas o evento público
        var publicosAnon = await anonClient.GetFromJsonAsync<List<EventoDto>>("/api/v1/eventos/publicos", ProClubesTestFactory.JsonOpts);
        Assert.Single(publicosAnon!);
        Assert.Equal("Feira Cultural Aberta", publicosAnon![0].Nome);

        // Criar titular na Unidade 1 e cadastrar dependente de 21 anos
        var titular = await (await saClient.PostAsJsonAsync("/api/v1/admin/associados", new CriarAssociadoAdminRequest(
            "Paulo Titular", "66677788899", "Masculino", "Casado(a)", new DateOnly(1978, 1, 10),
            "Rua D", "Centro", "40", "Foz do Iguaçu", "PR", "45999994444", "paulo.titular@teste.com", 1, "ASSOCIADO", "Senha@2026!"), ProClubesTestFactory.JsonOpts))
            .Content.ReadFromJsonAsync<AssociadoDto>(ProClubesTestFactory.JsonOpts);

        var hojeSp = TimeZoneHelper.TodaySaoPaulo();
        var nasc21Anos = hojeSp.AddYears(-21);

        var depResp = await saClient.PostAsJsonAsync($"/api/v1/admin/associados/{titular!.Id}/dependentes", new CadastrarDependenteRequest(
            "Lucas Dependente", "77788899900", "Masculino", "Solteiro(a)", nasc21Anos,
            "Rua D", "Centro", "40", "Foz do Iguaçu", "PR", "45999995555", "lucas.dep@teste.com",
            TipoVinculoDependente.Filho, "Senha@2026!"), ProClubesTestFactory.JsonOpts);
        Assert.Equal(HttpStatusCode.Created, depResp.StatusCode);

        // Dependente ativo vê tanto o evento público quanto o restrito da unidade do titular
        var depClient = await factory.CreateAuthenticatedClientAsync("lucas.dep@teste.com", "Senha@2026!");
        var eventosDep = await depClient.GetFromJsonAsync<List<EventoDto>>("/api/v1/eventos", ProClubesTestFactory.JsonOpts);
        Assert.Equal(2, eventosDep!.Count);

        // Cenário 6: Simular chegada aos 22 anos e executar rotina de transição automática para Simpatizante
        using (var scope = factory.Services.CreateScope())
        {
            var service = scope.ServiceProvider.GetRequiredService<AssociadoAndCadastroService>();
            var migrados = await service.ProcessarTransicaoDependentes22AnosAsync(hojeSp.AddYears(1).AddDays(1));
            Assert.Equal(1, migrados);
        }

        // Após virar Simpatizante, preserva o mesmo login/CPF e passa a ver apenas eventos públicos (Cenário 16)
        var simpClient = await factory.CreateAuthenticatedClientAsync("lucas.dep@teste.com", "Senha@2026!");
        var perfilSimp = await simpClient.GetFromJsonAsync<PerfilUsuarioDto>("/api/v1/me", ProClubesTestFactory.JsonOpts);
        Assert.False(perfilSimp!.EhDependenteAtivo);
        Assert.Equal("SIMPATIZANTE", perfilSimp.CategoriaCodigo);

        var eventosSimp = await simpClient.GetFromJsonAsync<List<EventoDto>>("/api/v1/eventos", ProClubesTestFactory.JsonOpts);
        Assert.Single(eventosSimp!);
        Assert.Equal(PublicoAlvoEvento.Publico, eventosSimp![0].PublicoAlvo);
    }

    // Cenários 10 e 11: Conflito de reserva de espaço sem sobreposição (horário adjacente permitido) e cancelamento administrativo com notificação
    [Fact]
    public async Task Cenarios10_11_ReservasSemSobreposicao_HorarioAdjacentePermitido_ECancelamentoAdminGeraNotificacao()
    {
        await using var factory = new ProClubesTestFactory();
        var saClient = await factory.CreateSuperAdminClientAsync();

        var a1 = await (await saClient.PostAsJsonAsync("/api/v1/admin/associados", new CriarAssociadoAdminRequest(
            "Associado Reserva 1", "88899900011", "Feminino", "Casado(a)", new DateOnly(1988, 7, 7),
            "Rua E", "Centro", "50", "Foz do Iguaçu", "PR", "45999996666", "res1@teste.com", 1, "ASSOCIADO", "Senha@2026!"), ProClubesTestFactory.JsonOpts))
            .Content.ReadFromJsonAsync<AssociadoDto>(ProClubesTestFactory.JsonOpts);

        var a2 = await (await saClient.PostAsJsonAsync("/api/v1/admin/associados", new CriarAssociadoAdminRequest(
            "Associado Reserva 2", "99900011122", "Masculino", "Casado(a)", new DateOnly(1989, 8, 8),
            "Rua F", "Centro", "60", "Foz do Iguaçu", "PR", "45999997777", "res2@teste.com", 1, "ASSOCIADO", "Senha@2026!"), ProClubesTestFactory.JsonOpts))
            .Content.ReadFromJsonAsync<AssociadoDto>(ProClubesTestFactory.JsonOpts);

        var c1 = await factory.CreateAuthenticatedClientAsync("res1@teste.com", "Senha@2026!");
        var c2 = await factory.CreateAuthenticatedClientAsync("res2@teste.com", "Senha@2026!");

        var dataAlvo = TimeZoneHelper.TodaySaoPaulo().AddDays(5);
        var ini1 = new DateTimeOffset(dataAlvo.ToDateTime(new TimeOnly(10, 0)), TimeSpan.FromHours(-3));
        var fim1 = new DateTimeOffset(dataAlvo.ToDateTime(new TimeOnly(12, 0)), TimeSpan.FromHours(-3));

        // Primeira reserva das 10:00 às 12:00 confirma automaticamente
        var r1Resp = await c1.PostAsJsonAsync("/api/v1/reservas", new CriarReservaRequest(1, ini1, fim1), ProClubesTestFactory.JsonOpts);
        Assert.Equal(HttpStatusCode.Created, r1Resp.StatusCode);
        var r1 = await r1Resp.Content.ReadFromJsonAsync<ReservaEspacoDto>(ProClubesTestFactory.JsonOpts);

        // Tentativa sobreposta das 11:00 às 13:00 falha com 409 Conflict (RESERVA_HORARIO_OCUPADO)
        var iniOverlap = new DateTimeOffset(dataAlvo.ToDateTime(new TimeOnly(11, 0)), TimeSpan.FromHours(-3));
        var fimOverlap = new DateTimeOffset(dataAlvo.ToDateTime(new TimeOnly(13, 0)), TimeSpan.FromHours(-3));
        var overlapResp = await c2.PostAsJsonAsync("/api/v1/reservas", new CriarReservaRequest(1, iniOverlap, fimOverlap), ProClubesTestFactory.JsonOpts);
        Assert.Equal(HttpStatusCode.Conflict, overlapResp.StatusCode);

        // Horário adjacente das 12:00 às 14:00 é aceito normalmente (regra InicioA < FimB AND InicioB < FimA)
        var iniAdj = new DateTimeOffset(dataAlvo.ToDateTime(new TimeOnly(12, 0)), TimeSpan.FromHours(-3));
        var fimAdj = new DateTimeOffset(dataAlvo.ToDateTime(new TimeOnly(14, 0)), TimeSpan.FromHours(-3));
        var adjResp = await c2.PostAsJsonAsync("/api/v1/reservas", new CriarReservaRequest(1, iniAdj, fimAdj), ProClubesTestFactory.JsonOpts);
        Assert.Equal(HttpStatusCode.Created, adjResp.StatusCode);

        // Cenário 11: Cancelamento administrativo exige justificativa e gera notificação interna ao associado
        var cancelSemJust = await saClient.PostAsJsonAsync($"/api/v1/reservas/{r1!.Id}/cancelamento", new CancelarReservaRequest(null), ProClubesTestFactory.JsonOpts);
        Assert.Equal(HttpStatusCode.BadRequest, cancelSemJust.StatusCode);

        var cancelComJust = await saClient.PostAsJsonAsync($"/api/v1/reservas/{r1.Id}/cancelamento",
            new CancelarReservaRequest("Manutenção emergencial elétrica no salão"), ProClubesTestFactory.JsonOpts);
        Assert.Equal(HttpStatusCode.OK, cancelComJust.StatusCode);

        var notifsC1 = await c1.GetFromJsonAsync<PagedResponse<NotificacaoDto>>("/api/v1/notificacoes", ProClubesTestFactory.JsonOpts);
        Assert.Contains(notifsC1!.Items, n => n.Tipo == TipoNotificacao.ReservaCancelada && n.Mensagem.Contains("Manutenção emergencial"));
    }

    // Cenários 12, 13, 14 e 15: Categorias globais de manifestação desativadas preservam histórico;
    // restrições de acesso a terceiros e Tesoureiro; snapshot congelado de eleitores; voto único, encerramento e cancelamento de votação
    [Fact]
    public async Task Cenarios12_13_14_15_Manifestacoes_PermissoesTerceiros_VotacoesSnapshotCongelado_EVotoUnico()
    {
        await using var factory = new ProClubesTestFactory();
        var saClient = await factory.CreateSuperAdminClientAsync();

        var el1 = await (await saClient.PostAsJsonAsync("/api/v1/admin/associados", new CriarAssociadoAdminRequest(
            "Eleitor Contribuinte 1", "12312312387", "Masculino", "Casado(a)", new DateOnly(1982, 2, 2),
            "Rua G", "Centro", "70", "Foz do Iguaçu", "PR", "45999998881", "el1@teste.com", 1, "ASSOCIADO", "Senha@2026!"), ProClubesTestFactory.JsonOpts))
            .Content.ReadFromJsonAsync<AssociadoDto>(ProClubesTestFactory.JsonOpts);

        var el2 = await (await saClient.PostAsJsonAsync("/api/v1/admin/associados", new CriarAssociadoAdminRequest(
            "Eleitor Contribuinte 2", "23423423498", "Feminino", "Solteiro(a)", new DateOnly(1984, 4, 4),
            "Rua H", "Centro", "80", "Foz do Iguaçu", "PR", "45999998882", "el2@teste.com", 1, "ASSOCIADO", "Senha@2026!"), ProClubesTestFactory.JsonOpts))
            .Content.ReadFromJsonAsync<AssociadoDto>(ProClubesTestFactory.JsonOpts);

        var tesUser = await (await saClient.PostAsJsonAsync("/api/v1/admin/associados", new CriarAssociadoAdminRequest(
            "Tesoureiro Foz", "34534534509", "Masculino", "Casado(a)", new DateOnly(1981, 1, 1),
            "Rua I", "Centro", "90", "Foz do Iguaçu", "PR", "45999998883", "tes@teste.com", 1, "CONVIDADO", "Senha@2026!"), ProClubesTestFactory.JsonOpts))
            .Content.ReadFromJsonAsync<AssociadoDto>(ProClubesTestFactory.JsonOpts);

        await saClient.PutAsJsonAsync("/api/v1/unidades/1/administrador",
            new DesignarRoleUnidadeRequest(tesUser!.UsuarioId, RoleAdministrativa.Tesoureiro), ProClubesTestFactory.JsonOpts);

        var cEl1 = await factory.CreateAuthenticatedClientAsync("el1@teste.com", "Senha@2026!");
        var cEl2 = await factory.CreateAuthenticatedClientAsync("el2@teste.com", "Senha@2026!");
        var cTes = await factory.CreateAuthenticatedClientAsync("tes@teste.com", "Senha@2026!");

        // Cenário 12: Criar manifestação em categoria ativa, desativar categoria e verificar que histórico continua acessível mas novas são recusadas
        var cats = await cEl1.GetFromJsonAsync<List<CategoriaManifestacaoDto>>("/api/v1/manifestacoes/categorias", ProClubesTestFactory.JsonOpts);
        var catAlvo = cats!.First();

        var manifResp = await cEl1.PostAsJsonAsync("/api/v1/manifestacoes",
            new CriarManifestacaoRequest(catAlvo.Id, catAlvo.TipoManifestacao, "Iluminação da quadra", "Sugiro refletores LED"), ProClubesTestFactory.JsonOpts);
        Assert.Equal(HttpStatusCode.Created, manifResp.StatusCode);
        var manifCriada = await manifResp.Content.ReadFromJsonAsync<ManifestacaoDto>(ProClubesTestFactory.JsonOpts);

        // Desativar categoria pelo Super Admin
        await saClient.PutAsJsonAsync($"/api/v1/admin/manifestacoes/categorias/{catAlvo.Id}",
            new CriarOuAtualizarCategoriaManifestacaoRequest(catAlvo.Nome, catAlvo.TipoManifestacao, "Desativada", false), ProClubesTestFactory.JsonOpts);

        var novaManifCatInativa = await cEl1.PostAsJsonAsync("/api/v1/manifestacoes",
            new CriarManifestacaoRequest(catAlvo.Id, catAlvo.TipoManifestacao, "Nova tentativa", "Deve falhar"), ProClubesTestFactory.JsonOpts);
        Assert.Equal(HttpStatusCode.UnprocessableEntity, novaManifCatInativa.StatusCode);

        // Histórico permanece íntegro para o autor
        var histManif = await cEl1.GetAsync($"/api/v1/manifestacoes/{manifCriada!.Id}");
        Assert.Equal(HttpStatusCode.OK, histManif.StatusCode);

        // Cenário 13: Outro associado (cEl2) e Tesoureiro (cTes) não podem acessar manifestação de terceiro
        Assert.Equal(HttpStatusCode.Forbidden, (await cEl2.GetAsync($"/api/v1/manifestacoes/{manifCriada.Id}")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await cTes.GetAsync($"/api/v1/manifestacoes/{manifCriada.Id}")).StatusCode);

        // Cenário 14: Criar e abrir votação congela snapshot de eleitores (EleitorVotacao)
        var votResp = await saClient.PostAsJsonAsync("/api/v1/admin/votacoes", new CriarVotacaoRequest(
            "Aprovação do Orçamento 2027",
            "Deliberação da Unidade Foz",
            EscopoVotacao.Unidade,
            1,
            DateTimeOffset.UtcNow.AddMinutes(-5),
            DateTimeOffset.UtcNow.AddDays(2),
            new[] { "Aprovar", "Rejeitar", "Abstenção" },
            AbrirImediatamente: true), ProClubesTestFactory.JsonOpts);
        Assert.Equal(HttpStatusCode.Created, votResp.StatusCode);
        var votacao = await votResp.Content.ReadFromJsonAsync<VotacaoDto>(ProClubesTestFactory.JsonOpts);
        Assert.Equal(EstadoVotacao.Aberta, votacao!.Estado);
        Assert.Equal(2, votacao.TotalEleitoresAptos); // el1 e el2 (tesUser é CONVIDADO, sem direito a voto)

        // Novo associado contribuinte criado APÓS abertura da votação não entra no snapshot congelado
        await saClient.PostAsJsonAsync("/api/v1/admin/associados", new CriarAssociadoAdminRequest(
            "Associado Pós Snapshot", "45645645610", "Masculino", "Solteiro(a)", new DateOnly(1992, 9, 9),
            "Rua J", "Centro", "100", "Foz do Iguaçu", "PR", "45999998884", "pos@teste.com", 1, "ASSOCIADO", "Senha@2026!"), ProClubesTestFactory.JsonOpts);
        var cPos = await factory.CreateAuthenticatedClientAsync("pos@teste.com", "Senha@2026!");

        var votoNaoElegivel = await cPos.PostAsJsonAsync($"/api/v1/votacoes/{votacao.Id}/votos",
            new RegistrarVotoRequest(votacao.Opcoes[0].Id), ProClubesTestFactory.JsonOpts);
        Assert.Equal(HttpStatusCode.Forbidden, votoNaoElegivel.StatusCode);

        // Cenário 15: Eleitor 1 vota uma vez com sucesso; segunda tentativa é rejeitada com 409 VOTO_JA_REGISTRADO
        var v1 = await cEl1.PostAsJsonAsync($"/api/v1/votacoes/{votacao.Id}/votos",
            new RegistrarVotoRequest(votacao.Opcoes[0].Id), ProClubesTestFactory.JsonOpts);
        Assert.Equal(HttpStatusCode.OK, v1.StatusCode);

        var v1Dup = await cEl1.PostAsJsonAsync($"/api/v1/votacoes/{votacao.Id}/votos",
            new RegistrarVotoRequest(votacao.Opcoes[1].Id), ProClubesTestFactory.JsonOpts);
        Assert.Equal(HttpStatusCode.Conflict, v1Dup.StatusCode);

        // Resultados agregados bloqueados antes do encerramento
        Assert.Equal(HttpStatusCode.UnprocessableEntity, (await cEl1.GetAsync($"/api/v1/votacoes/{votacao.Id}/resultados")).StatusCode);

        // Cenário 13 (votos individuais): Tesoureiro bloqueado (403), Super Admin permitido e auditado
        Assert.Equal(HttpStatusCode.Forbidden, (await cTes.GetAsync($"/api/v1/admin/votacoes/{votacao.Id}/votos")).StatusCode);
        var auditVotos = await saClient.GetFromJsonAsync<List<VotoIndividualAuditoriaDto>>($"/api/v1/admin/votacoes/{votacao.Id}/votos", ProClubesTestFactory.JsonOpts);
        Assert.Single(auditVotos!);

        // Encerrar votação divulga resultados agregados
        var encResp = await saClient.PostAsync($"/api/v1/admin/votacoes/{votacao.Id}/encerramento", null);
        Assert.Equal(HttpStatusCode.OK, encResp.StatusCode);

        var resultados = await cEl1.GetFromJsonAsync<ResultadoVotacaoDto>($"/api/v1/votacoes/{votacao.Id}/resultados", ProClubesTestFactory.JsonOpts);
        Assert.Equal(1, resultados!.TotalVotosComputados);
        Assert.Equal(100.00m, resultados.Opcoes.First(o => o.OpcaoId == votacao.Opcoes[0].Id).Percentual);

        // Votação cancelada com justificativa não divulga resultados
        var votCancelavel = await (await saClient.PostAsJsonAsync("/api/v1/admin/votacoes", new CriarVotacaoRequest(
            "Votação Cancelável", "Teste cancelamento", EscopoVotacao.Unidade, 1,
            DateTimeOffset.UtcNow, DateTimeOffset.UtcNow.AddDays(1), new[] { "Sim", "Não" }, true), ProClubesTestFactory.JsonOpts))
            .Content.ReadFromJsonAsync<VotacaoDto>(ProClubesTestFactory.JsonOpts);

        var cancResp = await saClient.PostAsJsonAsync($"/api/v1/admin/votacoes/{votCancelavel!.Id}/cancelamento",
            new CancelarVotacaoRequest("Erro material no edital de convocação"), ProClubesTestFactory.JsonOpts);
        Assert.Equal(HttpStatusCode.OK, cancResp.StatusCode);
        Assert.Equal(HttpStatusCode.UnprocessableEntity, (await cEl1.GetAsync($"/api/v1/votacoes/{votCancelavel.Id}/resultados")).StatusCode);
    }
}
