using Microsoft.EntityFrameworkCore;
using ProClubesApi.Authorization;
using ProClubesApi.Contracts;
using ProClubesApi.Data;
using ProClubesApi.Entities;

namespace ProClubesApi.Services;

public class DashboardAndSyncService
{
    private readonly ProClubesDbContext _db;
    private readonly AuthService _authService;
    private readonly FinanceiroService _financeiroService;
    private readonly EventoEspacoReservaService _eventoService;
    private readonly NotificacaoService _notificacaoService;

    public DashboardAndSyncService(
        ProClubesDbContext db,
        AuthService authService,
        FinanceiroService financeiroService,
        EventoEspacoReservaService eventoService,
        NotificacaoService notificacaoService)
    {
        _db = db;
        _authService = authService;
        _financeiroService = financeiroService;
        _eventoService = eventoService;
        _notificacaoService = notificacaoService;
    }

    public async Task<DashboardAdminDto> ObterDashboardAdminAsync(
        AuthenticatedUserInfo user,
        int? unidadeFiltroId = null,
        CancellationToken ct = default)
    {
        if (user.Role == RoleAdministrativa.Nenhuma)
            throw new ApiException(StatusCodes.Status403Forbidden, CodigosErro.RecursoSemPermissao, "Acesso restrito ao painel administrativo.");

        int? escopoUnidadeId = user.Role == RoleAdministrativa.SuperAdmin
            ? unidadeFiltroId
            : user.UnidadeAdministrativaId;

        var escopoNome = "Visão Global (Todas as Unidades)";
        if (escopoUnidadeId.HasValue)
        {
            var u = await _db.Unidades.AsNoTracking().FirstOrDefaultAsync(x => x.Id == escopoUnidadeId.Value, ct);
            escopoNome = u?.Nome ?? $"Unidade #{escopoUnidadeId}";
        }

        var hoje = TimeZoneHelper.TodaySaoPaulo();
        var compMesAtual = TimeZoneHelper.FirstDayOfMonth(hoje);
        var nowUtc = DateTime.UtcNow;

        var totalUnidades = await _db.Unidades.CountAsync(u => u.Ativa, ct);

        var qAssociados = _db.Associados.Where(a => a.Status == StatusAssociado.Ativo);
        var qSolicitacoes = _db.SolicitacoesCadastro.Where(s => s.Status == StatusSolicitacaoCadastro.Pendente);
        var qMensalidades = _db.Mensalidades.AsQueryable();
        var qPagamentosMes = _db.PagamentosMensalidade.Where(p => p.DataPagamento >= compMesAtual);
        var qEventos = _db.Eventos.Where(e => e.Status == StatusEvento.Programado && e.DataHoraInicioUtc >= nowUtc);
        var qReservas = _db.ReservasEspaco.Where(r => r.Status == StatusReserva.Confirmada && r.InicioUtc >= nowUtc);
        var qManifestacoes = _db.Manifestacoes.Where(m => m.Status != StatusManifestacao.Concluida);
        var qVotacoes = _db.Votacoes.Where(v => v.Estado == EstadoVotacao.Aberta);

        if (escopoUnidadeId.HasValue)
        {
            var uId = escopoUnidadeId.Value;
            qAssociados = qAssociados.Where(a => a.UnidadeId == uId);
            qSolicitacoes = qSolicitacoes.Where(s => s.UnidadePretendidaId == uId);
            qMensalidades = qMensalidades.Where(m => m.UnidadeEmissoraId == uId);
            qPagamentosMes = qPagamentosMes.Where(p => p.Mensalidade.UnidadeEmissoraId == uId);
            qEventos = qEventos.Where(e => e.UnidadeOrganizadoraId == uId || e.UnidadesParticipantes.Any(eu => eu.UnidadeId == uId));
            qReservas = qReservas.Where(r => r.Espaco.UnidadeId == uId);
            qManifestacoes = qManifestacoes.Where(m => m.UnidadeId == uId);
            qVotacoes = qVotacoes.Where(v => v.UnidadeId == uId || v.Escopo == EscopoVotacao.Global);
        }

        var mensalidadesAbertasMes = await qMensalidades.CountAsync(m => m.Status == StatusMensalidade.Aberta && m.Competencia == compMesAtual && m.DataVencimento >= hoje, ct);
        var mensalidadesVencidas = await qMensalidades.CountAsync(m => m.Status == StatusMensalidade.Aberta && m.DataVencimento < hoje, ct);
        var totalRecebido = await qPagamentosMes.SumAsync(p => (decimal?)p.ValorTotalPago, ct) ?? 0m;

        // Tesoureiro recebe apenas indicadores financeiros e da unidade
        if (user.Role == RoleAdministrativa.Tesoureiro)
        {
            return new DashboardAdminDto(
                escopoUnidadeId,
                escopoNome,
                1,
                0,
                0,
                mensalidadesAbertasMes,
                mensalidadesVencidas,
                totalRecebido,
                0,
                0,
                0,
                0);
        }

        return new DashboardAdminDto(
            UnidadeEscopoId: escopoUnidadeId,
            EscopoNome: escopoNome,
            TotalUnidadesAtivas: totalUnidades,
            TotalAssociadosAtivos: await qAssociados.CountAsync(ct),
            CadastrosPendentes: await qSolicitacoes.CountAsync(ct),
            MensalidadesEmAbertoMesAtual: mensalidadesAbertasMes,
            MensalidadesVencidas: mensalidadesVencidas,
            TotalRecebidoMesAtual: totalRecebido,
            EventosFuturos: await qEventos.CountAsync(ct),
            ReservasFuturasConfirmadas: await qReservas.CountAsync(ct),
            ManifestacoesPendentes: await qManifestacoes.CountAsync(ct),
            VotacoesAbertas: await qVotacoes.CountAsync(ct));
    }

    public async Task<ResumoSincronizacaoAppDto> ObterResumoSincronizacaoAppAsync(
        AuthenticatedUserInfo user,
        CancellationToken ct = default)
    {
        var perfil = await _authService.ObterPerfilAsync(user.UsuarioId, ct);

        var qtdVencidas = 0;
        MensalidadeDto? proximaMensalidade = null;

        if (user.AssociadoId.HasValue && !user.EhDependenteAtivo && user.CategoriaCodigo != CodigosCategoriaAssociado.Simpatizante)
        {
            var mensalidades = await _financeiroService.ListarMinhasMensalidadesAsync(user, ct);
            qtdVencidas = mensalidades.Count(m => m.SituacaoExibicao == "Vencida");
            proximaMensalidade = mensalidades
                .Where(m => m.Status == StatusMensalidade.Aberta)
                .OrderBy(m => m.DataVencimento)
                .FirstOrDefault();
        }

        var eventos = (await _eventoService.ListarEventosElegiveisAsync(user, null, ct))
            .Take(10)
            .ToList();

        IReadOnlyList<ReservaEspacoDto> reservas = Array.Empty<ReservaEspacoDto>();
        if (user.AssociadoId.HasValue && !user.EhDependenteAtivo && user.CategoriaCodigo != CodigosCategoriaAssociado.Simpatizante)
        {
            reservas = (await _eventoService.ListarReservasAsync(user, null, ct))
                .Where(r => r.Status == StatusReserva.Confirmada && r.Fim >= TimeZoneHelper.ToSaoPauloOffset(DateTime.UtcNow))
                .OrderBy(r => r.Inicio)
                .Take(10)
                .ToList();
        }

        var notifsPage = await _notificacaoService.ListarNotificacoesUsuarioAsync(user.UsuarioId, null, new PaginationQuery(1, 15), ct);
        var naoLidas = await _db.Notificacoes.CountAsync(n => n.DestinatarioUsuarioId == user.UsuarioId && n.LidaEmUtc == null, ct);

        return new ResumoSincronizacaoAppDto(
            SincronizadoEm: TimeZoneHelper.ToSaoPauloOffset(DateTime.UtcNow),
            Perfil: perfil,
            QuantidadeMensalidadesVencidas: qtdVencidas,
            ProximaMensalidade: proximaMensalidade,
            ProximosEventos: eventos,
            ReservasFuturas: reservas,
            NotificacoesRecentes: notifsPage.Items,
            NotificacoesNaoLidas: naoLidas);
    }
}

public static class DatabaseSeeder
{
    public static async Task EnsureSeededAsync(IServiceProvider services, IConfiguration configuration, CancellationToken ct = default)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ProClubesDbContext>();

        if (db.Database.ProviderName?.Contains("Sqlite", StringComparison.OrdinalIgnoreCase) == true)
        {
            await db.Database.EnsureCreatedAsync(ct);
        }
        else
        {
            await db.Database.MigrateAsync(ct);
        }

        var nowUtc = DateTime.UtcNow;
        var hoje = TimeZoneHelper.TodaySaoPaulo(nowUtc);
        var competenciaAtual = TimeZoneHelper.FirstDayOfMonth(hoje);

        // 1. Provisionar Super Admin inicial sem credencial fixa chumbada no código (lê de IConfiguration / variáveis de ambiente)
        var superAdminEmail = configuration["BootstrapSuperAdmin:Email"]
            ?? Environment.GetEnvironmentVariable("PROCLUBES_SUPERADMIN_EMAIL");
        var superAdminPassword = configuration["BootstrapSuperAdmin:Password"]
            ?? Environment.GetEnvironmentVariable("PROCLUBES_SUPERADMIN_PASSWORD");
        var superAdminCpf = CpfHelper.Normalize(
            configuration["BootstrapSuperAdmin:Cpf"]
            ?? Environment.GetEnvironmentVariable("PROCLUBES_SUPERADMIN_CPF"));

        var superAdminUser = await db.Usuarios
            .Include(u => u.Pessoa)
            .FirstOrDefaultAsync(u => u.RoleAdministrativa == RoleAdministrativa.SuperAdmin, ct);

        if (superAdminUser is null
            && !string.IsNullOrWhiteSpace(superAdminEmail)
            && !string.IsNullOrWhiteSpace(superAdminPassword)
            && !string.IsNullOrWhiteSpace(superAdminCpf))
        {
            var pessoaSa = new Pessoa
            {
                NomeCompleto = "Super Administrador ProClubes",
                Cpf = superAdminCpf,
                Sexo = "Masculino",
                EstadoCivil = "Casado(a)",
                DataNascimento = new DateOnly(1980, 1, 15),
                Logradouro = "Av. Brasil",
                Bairro = "Centro",
                Numero = "1000",
                Cidade = "Foz do Iguaçu",
                UF = "PR",
                Fone = "(45) 99999-0000",
                Email = superAdminEmail,
                CriadaEmUtc = nowUtc,
                AtualizadaEmUtc = nowUtc
            };
            db.Pessoas.Add(pessoaSa);
            await db.SaveChangesAsync(ct);

            superAdminUser = new Usuario
            {
                PessoaId = pessoaSa.Id,
                SenhaHash = PasswordHasherHelper.HashPassword(superAdminPassword),
                RoleAdministrativa = RoleAdministrativa.SuperAdmin,
                UnidadeAdministrativaId = null,
                EstadoConta = EstadoConta.Ativa,
                VersaoSeguranca = 1,
                CriadoEmUtc = nowUtc
            };
            db.Usuarios.Add(superAdminUser);
            await db.SaveChangesAsync(ct);
        }

        if (superAdminUser is null)
            return;

        // 2. ConfiguracaoFinanceira singleton
        if (!await db.ConfiguracoesFinanceiras.AnyAsync(c => c.Id == 1, ct))
        {
            db.ConfiguracoesFinanceiras.Add(new ConfiguracaoFinanceira
            {
                Id = 1,
                DiaGeracao = 1,
                DiaVencimento = 10,
                DiaAvisoVencimento = 7,
                PercentualMulta = 2.0000m,
                PercentualJuroMensal = 1.0000m,
                AtualizadaEmUtc = nowUtc,
                AlteradaPorUsuarioId = superAdminUser.Id
            });
            await db.SaveChangesAsync(ct);
        }

        // 3. Unidades iniciais e valores da tabela de exemplo (Seção 7.1)
        if (!await db.Unidades.AnyAsync(ct))
        {
            var foz = new Unidade
            {
                Nome = "ProClubes Foz",
                Sigla = "PC-FOZ",
                Logradouro = "Av. das Cataratas",
                Bairro = "Vila Yolanda",
                Numero = "1500",
                Cidade = "Foz do Iguaçu",
                UF = "PR",
                Fone = "(45) 3521-1000",
                Email = "foz@proclubes.com.br",
                Ativa = true,
                CriadaEmUtc = nowUtc
            };
            var resortFoz = new Unidade
            {
                Nome = "ProClubes Resort Foz",
                Sigla = "PC-RESORT",
                Logradouro = "Rodovia das Águas",
                Bairro = "Lago de Itaipu",
                Numero = "2200",
                Cidade = "Foz do Iguaçu",
                UF = "PR",
                Fone = "(45) 3521-2000",
                Email = "resortfoz@proclubes.com.br",
                Ativa = true,
                CriadaEmUtc = nowUtc
            };
            var maringa = new Unidade
            {
                Nome = "ProClubes Maringá",
                Sigla = "PC-MGA",
                Logradouro = "Av. Tiradentes",
                Bairro = "Zona 01",
                Numero = "850",
                Cidade = "Maringá",
                UF = "PR",
                Fone = "(44) 3221-3000",
                Email = "maringa@proclubes.com.br",
                Ativa = true,
                CriadaEmUtc = nowUtc
            };

            db.Unidades.AddRange(foz, resortFoz, maringa);
            await db.SaveChangesAsync(ct);

            var categorias = await db.CategoriasAssociado.ToDictionaryAsync(c => c.Codigo, c => c.Id, ct);

            void AddValores(int unidId, decimal vAssoc, decimal vFund, decimal vConv)
            {
                db.ValoresMensalidade.AddRange(
                    new ValorMensalidade { UnidadeId = unidId, CategoriaAssociadoId = categorias[CodigosCategoriaAssociado.Associado], Valor = vAssoc, CompetenciaInicio = new DateOnly(2026, 1, 1), CriadoPorUsuarioId = superAdminUser.Id, CriadoEmUtc = nowUtc },
                    new ValorMensalidade { UnidadeId = unidId, CategoriaAssociadoId = categorias[CodigosCategoriaAssociado.Fundador], Valor = vFund, CompetenciaInicio = new DateOnly(2026, 1, 1), CriadoPorUsuarioId = superAdminUser.Id, CriadoEmUtc = nowUtc },
                    new ValorMensalidade { UnidadeId = unidId, CategoriaAssociadoId = categorias[CodigosCategoriaAssociado.Convidado], Valor = vConv, CompetenciaInicio = new DateOnly(2026, 1, 1), CriadoPorUsuarioId = superAdminUser.Id, CriadoEmUtc = nowUtc },
                    new ValorMensalidade { UnidadeId = unidId, CategoriaAssociadoId = categorias[CodigosCategoriaAssociado.Simpatizante], Valor = 0m, CompetenciaInicio = new DateOnly(2026, 1, 1), CriadoPorUsuarioId = superAdminUser.Id, CriadoEmUtc = nowUtc }
                );
            }

            AddValores(foz.Id, 100.00m, 80.00m, 70.00m);
            AddValores(resortFoz.Id, 130.00m, 110.00m, 90.00m);
            AddValores(maringa.Id, 95.00m, 75.00m, 65.00m);

            db.Espacos.AddRange(
                new Espaco
                {
                    UnidadeId = foz.Id,
                    Nome = "Salão Social Cataratas",
                    Descricao = "Salão climatizado para eventos sociais e confraternizações",
                    Ativo = true,
                    HoraAbertura = new TimeOnly(8, 0),
                    HoraFechamento = new TimeOnly(22, 0),
                    DuracaoMaximaMinutos = 240,
                    AntecedenciaMaximaDias = 90
                },
                new Espaco
                {
                    UnidadeId = foz.Id,
                    Nome = "Quiosque Gourmet Itaipu",
                    Descricao = "Espaço com churrasqueira à beira da área verde",
                    Ativo = true,
                    HoraAbertura = new TimeOnly(9, 0),
                    HoraFechamento = new TimeOnly(21, 0),
                    DuracaoMaximaMinutos = 300,
                    AntecedenciaMaximaDias = 90
                },
                new Espaco
                {
                    UnidadeId = maringa.Id,
                    Nome = "Salão Nobre Ingá",
                    Descricao = "Espaço de eventos da unidade Maringá",
                    Ativo = true,
                    HoraAbertura = new TimeOnly(8, 0),
                    HoraFechamento = new TimeOnly(22, 0),
                    DuracaoMaximaMinutos = 240,
                    AntecedenciaMaximaDias = 90
                });

            await db.SaveChangesAsync(ct);
        }
    }
}
