using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using ProClubesApi.Entities;

namespace ProClubesApi.Data;

public sealed class DateOnlyConverter() : ValueConverter<DateOnly, DateTime>(
    d => d.ToDateTime(TimeOnly.MinValue),
    dt => DateOnly.FromDateTime(dt));

public sealed class NullableDateOnlyConverter() : ValueConverter<DateOnly?, DateTime?>(
    d => d.HasValue ? d.Value.ToDateTime(TimeOnly.MinValue) : null,
    dt => dt.HasValue ? DateOnly.FromDateTime(dt.Value) : null);

public sealed class TimeOnlyConverter() : ValueConverter<TimeOnly, TimeSpan>(
    t => t.ToTimeSpan(),
    ts => TimeOnly.FromTimeSpan(ts));

public sealed class NullableTimeOnlyConverter() : ValueConverter<TimeOnly?, TimeSpan?>(
    t => t.HasValue ? t.Value.ToTimeSpan() : null,
    ts => ts.HasValue ? TimeOnly.FromTimeSpan(ts.Value) : null);

public class ProClubesDbContext : DbContext
{
    public ProClubesDbContext(DbContextOptions<ProClubesDbContext> options) : base(options)
    {
    }

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        base.ConfigureConventions(configurationBuilder);

        configurationBuilder.Properties<DateOnly>()
            .HaveConversion<DateOnlyConverter>()
            .HaveColumnType("date");

        configurationBuilder.Properties<DateOnly?>()
            .HaveConversion<NullableDateOnlyConverter>()
            .HaveColumnType("date");

        configurationBuilder.Properties<TimeOnly>()
            .HaveConversion<TimeOnlyConverter>()
            .HaveColumnType("time");

        configurationBuilder.Properties<TimeOnly?>()
            .HaveConversion<NullableTimeOnlyConverter>()
            .HaveColumnType("time");
    }

    public DbSet<Unidade> Unidades => Set<Unidade>();
    public DbSet<Pessoa> Pessoas => Set<Pessoa>();
    public DbSet<Interesse> Interesses => Set<Interesse>();
    public DbSet<PessoaInteresse> PessoasInteresses => Set<PessoaInteresse>();
    public DbSet<Usuario> Usuarios => Set<Usuario>();
    public DbSet<SessaoRenovacao> SessoesRenovacao => Set<SessaoRenovacao>();
    public DbSet<CategoriaAssociado> CategoriasAssociado => Set<CategoriaAssociado>();
    public DbSet<Associado> Associados => Set<Associado>();
    public DbSet<HistoricoCategoriaAssociado> HistoricosCategoriaAssociado => Set<HistoricoCategoriaAssociado>();
    public DbSet<Dependencia> Dependencias => Set<Dependencia>();
    public DbSet<TransferenciaUnidade> TransferenciasUnidade => Set<TransferenciaUnidade>();
    public DbSet<SolicitacaoCadastro> SolicitacoesCadastro => Set<SolicitacaoCadastro>();
    public DbSet<ConfiguracaoFinanceira> ConfiguracoesFinanceiras => Set<ConfiguracaoFinanceira>();
    public DbSet<ValorMensalidade> ValoresMensalidade => Set<ValorMensalidade>();
    public DbSet<Mensalidade> Mensalidades => Set<Mensalidade>();
    public DbSet<PagamentoMensalidade> PagamentosMensalidade => Set<PagamentoMensalidade>();
    public DbSet<Evento> Eventos => Set<Evento>();
    public DbSet<EventoUnidade> EventosUnidades => Set<EventoUnidade>();
    public DbSet<PagamentoEvento> PagamentosEvento => Set<PagamentoEvento>();
    public DbSet<Espaco> Espacos => Set<Espaco>();
    public DbSet<ReservaEspaco> ReservasEspaco => Set<ReservaEspaco>();
    public DbSet<OcupacaoEspaco> OcupacoesEspaco => Set<OcupacaoEspaco>();
    public DbSet<CategoriaManifestacao> CategoriasManifestacao => Set<CategoriaManifestacao>();
    public DbSet<Manifestacao> Manifestacoes => Set<Manifestacao>();
    public DbSet<RespostaManifestacao> RespostasManifestacao => Set<RespostaManifestacao>();
    public DbSet<Notificacao> Notificacoes => Set<Notificacao>();
    public DbSet<Votacao> Votacoes => Set<Votacao>();
    public DbSet<OpcaoVotacao> OpcoesVotacao => Set<OpcaoVotacao>();
    public DbSet<EleitorVotacao> EleitoresVotacao => Set<EleitorVotacao>();
    public DbSet<Voto> Votos => Set<Voto>();
    public DbSet<RegistroAuditoria> RegistrosAuditoria => Set<RegistroAuditoria>();
    public DbSet<ExecucaoRotina> ExecucoesRotina => Set<ExecucaoRotina>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // 1. Unidade
        modelBuilder.Entity<Unidade>(e =>
        {
            e.ToTable("Unidade");
            e.HasKey(x => x.Id);
            e.Property(x => x.Nome).HasMaxLength(150).IsRequired();
            e.Property(x => x.Sigla).HasMaxLength(20).IsRequired();
            e.Property(x => x.Logradouro).HasMaxLength(150).IsRequired();
            e.Property(x => x.Bairro).HasMaxLength(100).IsRequired();
            e.Property(x => x.Numero).HasMaxLength(20).IsRequired();
            e.Property(x => x.Cidade).HasMaxLength(100).IsRequired();
            e.Property(x => x.UF).HasMaxLength(2).IsFixedLength().IsRequired();
            e.Property(x => x.Fone).HasMaxLength(20).IsRequired();
            e.Property(x => x.Email).HasMaxLength(254).IsRequired();
            e.Property(x => x.Ativa).HasDefaultValue(true).IsRequired();
            e.HasIndex(x => x.Sigla).IsUnique();
            e.HasIndex(x => new { x.Cidade, x.UF });
            e.HasIndex(x => x.Ativa);
        });

        // 2. Pessoa
        modelBuilder.Entity<Pessoa>(e =>
        {
            e.ToTable("Pessoa");
            e.HasKey(x => x.Id);
            e.Property(x => x.NomeCompleto).HasMaxLength(180).IsRequired();
            e.Property(x => x.Cpf).HasMaxLength(11).IsFixedLength().IsRequired();
            e.Property(x => x.Sexo).HasMaxLength(30).IsRequired();
            e.Property(x => x.EstadoCivil).HasMaxLength(40).IsRequired();
            e.Property(x => x.Logradouro).HasMaxLength(150).IsRequired();
            e.Property(x => x.Bairro).HasMaxLength(100).IsRequired();
            e.Property(x => x.Numero).HasMaxLength(20).IsRequired();
            e.Property(x => x.Cidade).HasMaxLength(100).IsRequired();
            e.Property(x => x.UF).HasMaxLength(2).IsFixedLength().IsRequired();
            e.Property(x => x.Fone).HasMaxLength(20).IsRequired();
            e.Property(x => x.Email).HasMaxLength(254).IsRequired();
            e.HasIndex(x => x.Cpf).IsUnique();
            e.HasIndex(x => x.NomeCompleto);
            e.HasIndex(x => x.Email);
        });

        // 3. Interesse
        modelBuilder.Entity<Interesse>(e =>
        {
            e.ToTable("Interesse");
            e.HasKey(x => x.Id);
            e.Property(x => x.Nome).HasMaxLength(80).IsRequired();
            e.Property(x => x.Ativo).HasDefaultValue(true).IsRequired();
            e.HasIndex(x => x.Nome).IsUnique();

            e.HasData(
                new Interesse { Id = 1, Nome = "Pesca", Ativo = true },
                new Interesse { Id = 2, Nome = "Caça", Ativo = true },
                new Interesse { Id = 3, Nome = "Comida", Ativo = true },
                new Interesse { Id = 4, Nome = "Bebida", Ativo = true },
                new Interesse { Id = 5, Nome = "Esportes Aquáticos", Ativo = true },
                new Interesse { Id = 6, Nome = "Eventos Sociais", Ativo = true }
            );
        });

        // 4. PessoaInteresse
        modelBuilder.Entity<PessoaInteresse>(e =>
        {
            e.ToTable("PessoaInteresse");
            e.HasKey(x => new { x.PessoaId, x.InteresseId });
            e.HasIndex(x => x.InteresseId);
            e.HasOne(x => x.Pessoa)
                .WithMany(p => p.Interesses)
                .HasForeignKey(x => x.PessoaId)
                .OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.Interesse)
                .WithMany()
                .HasForeignKey(x => x.InteresseId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // 5. Usuario
        modelBuilder.Entity<Usuario>(e =>
        {
            e.ToTable("Usuario", t =>
            {
                t.HasCheckConstraint(
                    "CK_Usuario_RoleUnidade",
                    "((RoleAdministrativa IN (1, 2) AND UnidadeAdministrativaId IS NOT NULL) OR (RoleAdministrativa IN (0, 3) AND UnidadeAdministrativaId IS NULL))");
            });
            e.HasKey(x => x.Id);
            e.Property(x => x.SenhaHash).HasMaxLength(512).IsRequired();
            e.Property(x => x.RoleAdministrativa).HasConversion<byte>().IsRequired();
            e.Property(x => x.EstadoConta).HasConversion<byte>().IsRequired();
            e.Property(x => x.VersaoSeguranca).HasDefaultValue(1).IsRequired();
            e.Property(x => x.AdminAtivoUnidadeId)
                .HasComputedColumnSql("CASE WHEN RoleAdministrativa = 2 AND EstadoConta = 1 THEN UnidadeAdministrativaId ELSE NULL END", stored: true);

            e.HasIndex(x => x.PessoaId).IsUnique();
            e.HasIndex(x => x.AdminAtivoUnidadeId).IsUnique();
            e.HasIndex(x => new { x.UnidadeAdministrativaId, x.RoleAdministrativa, x.EstadoConta });

            e.HasOne(x => x.Pessoa)
                .WithOne(p => p.Usuario)
                .HasForeignKey<Usuario>(x => x.PessoaId)
                .OnDelete(DeleteBehavior.Restrict);

            e.HasOne(x => x.UnidadeAdministrativa)
                .WithMany()
                .HasForeignKey(x => x.UnidadeAdministrativaId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // 6. SessaoRenovacao
        modelBuilder.Entity<SessaoRenovacao>(e =>
        {
            e.ToTable("SessaoRenovacao");
            e.HasKey(x => x.Id);
            e.Property(x => x.TokenHash).HasMaxLength(64).IsFixedLength().IsRequired();
            e.HasIndex(x => x.TokenHash).IsUnique();
            e.HasIndex(x => new { x.UsuarioId, x.ExpiraEmUtc });

            e.HasOne(x => x.Usuario)
                .WithMany()
                .HasForeignKey(x => x.UsuarioId)
                .OnDelete(DeleteBehavior.Restrict);

            e.HasOne(x => x.SubstituidoPorSessao)
                .WithMany()
                .HasForeignKey(x => x.SubstituidoPorSessaoId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // 7. CategoriaAssociado
        modelBuilder.Entity<CategoriaAssociado>(e =>
        {
            e.ToTable("CategoriaAssociado");
            e.HasKey(x => x.Id);
            e.Property(x => x.Codigo).HasMaxLength(32).IsRequired();
            e.Property(x => x.Nome).HasMaxLength(80).IsRequired();
            e.Property(x => x.Ativa).HasDefaultValue(true).IsRequired();
            e.HasIndex(x => x.Codigo).IsUnique();

            e.HasData(
                new CategoriaAssociado { Id = 1, Codigo = CodigosCategoriaAssociado.Associado, Nome = "Sócio Associado", TemDireitoVoto = true, IsentoMensalidade = false, Ativa = true },
                new CategoriaAssociado { Id = 2, Codigo = CodigosCategoriaAssociado.Fundador, Nome = "Sócio Fundador", TemDireitoVoto = true, IsentoMensalidade = false, Ativa = true },
                new CategoriaAssociado { Id = 3, Codigo = CodigosCategoriaAssociado.Convidado, Nome = "Sócio Convidado", TemDireitoVoto = false, IsentoMensalidade = false, Ativa = true },
                new CategoriaAssociado { Id = 4, Codigo = CodigosCategoriaAssociado.Simpatizante, Nome = "Simpatizante", TemDireitoVoto = false, IsentoMensalidade = true, Ativa = true }
            );
        });

        // 8. Associado
        modelBuilder.Entity<Associado>(e =>
        {
            e.ToTable("Associado");
            e.HasKey(x => x.Id);
            e.Property(x => x.Status).HasConversion<byte>().IsRequired();
            e.HasIndex(x => x.PessoaId).IsUnique();
            e.HasIndex(x => new { x.UnidadeId, x.Status, x.CategoriaAssociadoId });

            e.HasOne(x => x.Pessoa)
                .WithOne(p => p.Associado)
                .HasForeignKey<Associado>(x => x.PessoaId)
                .OnDelete(DeleteBehavior.Restrict);

            e.HasOne(x => x.Unidade)
                .WithMany()
                .HasForeignKey(x => x.UnidadeId)
                .OnDelete(DeleteBehavior.Restrict);

            e.HasOne(x => x.CategoriaAssociado)
                .WithMany()
                .HasForeignKey(x => x.CategoriaAssociadoId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // 9. HistoricoCategoriaAssociado
        modelBuilder.Entity<HistoricoCategoriaAssociado>(e =>
        {
            e.ToTable("HistoricoCategoriaAssociado");
            e.HasKey(x => x.Id);
            e.Property(x => x.Motivo).HasMaxLength(500);
            e.HasIndex(x => new { x.AssociadoId, x.VigenteApartir });

            e.HasOne(x => x.Associado)
                .WithMany()
                .HasForeignKey(x => x.AssociadoId)
                .OnDelete(DeleteBehavior.Restrict);

            e.HasOne(x => x.CategoriaAnterior)
                .WithMany()
                .HasForeignKey(x => x.CategoriaAnteriorId)
                .OnDelete(DeleteBehavior.Restrict);

            e.HasOne(x => x.CategoriaNova)
                .WithMany()
                .HasForeignKey(x => x.CategoriaNovaId)
                .OnDelete(DeleteBehavior.Restrict);

            e.HasOne(x => x.AlteradoPorUsuario)
                .WithMany()
                .HasForeignKey(x => x.AlteradoPorUsuarioId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // 10. Dependencia
        modelBuilder.Entity<Dependencia>(e =>
        {
            e.ToTable("Dependencia");
            e.HasKey(x => x.Id);
            e.Property(x => x.TipoVinculo).HasConversion<byte>().IsRequired();
            e.Property(x => x.MotivoFim).HasConversion<byte?>();
            e.Property(x => x.DependenteAtivoPessoaId)
                .HasComputedColumnSql("CASE WHEN DataFim IS NULL THEN PessoaDependenteId ELSE NULL END", stored: true);

            e.HasIndex(x => x.DependenteAtivoPessoaId).IsUnique();
            e.HasIndex(x => new { x.AssociadoTitularId, x.DataFim });
            e.HasIndex(x => x.PessoaDependenteId);

            e.HasOne(x => x.AssociadoTitular)
                .WithMany(a => a.Dependentes)
                .HasForeignKey(x => x.AssociadoTitularId)
                .OnDelete(DeleteBehavior.Restrict);

            e.HasOne(x => x.PessoaDependente)
                .WithMany()
                .HasForeignKey(x => x.PessoaDependenteId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // 11. TransferenciaUnidade
        modelBuilder.Entity<TransferenciaUnidade>(e =>
        {
            e.ToTable("TransferenciaUnidade", t =>
            {
                t.HasCheckConstraint("CK_TransferenciaUnidade_Distintas", "UnidadeOrigemId <> UnidadeDestinoId");
            });
            e.HasKey(x => x.Id);
            e.HasIndex(x => new { x.AssociadoId, x.DataEfetiva });

            e.HasOne(x => x.Associado)
                .WithMany()
                .HasForeignKey(x => x.AssociadoId)
                .OnDelete(DeleteBehavior.Restrict);

            e.HasOne(x => x.UnidadeOrigem)
                .WithMany()
                .HasForeignKey(x => x.UnidadeOrigemId)
                .OnDelete(DeleteBehavior.Restrict);

            e.HasOne(x => x.UnidadeDestino)
                .WithMany()
                .HasForeignKey(x => x.UnidadeDestinoId)
                .OnDelete(DeleteBehavior.Restrict);

            e.HasOne(x => x.RealizadaPorUsuario)
                .WithMany()
                .HasForeignKey(x => x.RealizadaPorUsuarioId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // 12. SolicitacaoCadastro
        modelBuilder.Entity<SolicitacaoCadastro>(e =>
        {
            e.ToTable("SolicitacaoCadastro");
            e.HasKey(x => x.Id);
            e.Property(x => x.Status).HasConversion<byte>().IsRequired();
            e.Property(x => x.JustificativaRejeicao).HasMaxLength(800);
            e.Property(x => x.PendentePessoaId)
                .HasComputedColumnSql("CASE WHEN Status = 0 THEN PessoaId ELSE NULL END", stored: true);

            e.HasIndex(x => x.PendentePessoaId).IsUnique();
            e.HasIndex(x => new { x.UnidadePretendidaId, x.Status, x.SolicitadaEmUtc });
            e.HasIndex(x => new { x.PessoaId, x.SolicitadaEmUtc });

            e.HasOne(x => x.Pessoa)
                .WithMany()
                .HasForeignKey(x => x.PessoaId)
                .OnDelete(DeleteBehavior.Restrict);

            e.HasOne(x => x.Usuario)
                .WithMany()
                .HasForeignKey(x => x.UsuarioId)
                .OnDelete(DeleteBehavior.Restrict);

            e.HasOne(x => x.UnidadePretendida)
                .WithMany()
                .HasForeignKey(x => x.UnidadePretendidaId)
                .OnDelete(DeleteBehavior.Restrict);

            e.HasOne(x => x.CategoriaAprovada)
                .WithMany()
                .HasForeignKey(x => x.CategoriaAprovadaId)
                .OnDelete(DeleteBehavior.Restrict);

            e.HasOne(x => x.AvaliadorUsuario)
                .WithMany()
                .HasForeignKey(x => x.AvaliadorUsuarioId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // 13. ConfiguracaoFinanceira
        modelBuilder.Entity<ConfiguracaoFinanceira>(e =>
        {
            e.ToTable("ConfiguracaoFinanceira", t =>
            {
                t.HasCheckConstraint("CK_ConfiguracaoFinanceira_Singleton", "Id = 1");
                t.HasCheckConstraint("CK_ConfiguracaoFinanceira_Dias", "DiaGeracao BETWEEN 1 AND 31 AND DiaVencimento BETWEEN 1 AND 31");
                t.HasCheckConstraint("CK_ConfiguracaoFinanceira_Percentuais", "PercentualMulta >= 0 AND PercentualJuroMensal >= 0");
            });
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).ValueGeneratedNever();
            e.Property(x => x.PercentualMulta).HasPrecision(7, 4).IsRequired();
            e.Property(x => x.PercentualJuroMensal).HasPrecision(7, 4).IsRequired();

            e.HasOne(x => x.AlteradaPorUsuario)
                .WithMany()
                .HasForeignKey(x => x.AlteradaPorUsuarioId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // 14. ValorMensalidade
        modelBuilder.Entity<ValorMensalidade>(e =>
        {
            e.ToTable("ValorMensalidade");
            e.HasKey(x => x.Id);
            e.Property(x => x.Valor).HasPrecision(12, 2).IsRequired();
            e.HasIndex(x => new { x.UnidadeId, x.CategoriaAssociadoId, x.CompetenciaInicio }).IsUnique();
            e.HasIndex(x => new { x.UnidadeId, x.CategoriaAssociadoId, x.CompetenciaFim });

            e.HasOne(x => x.Unidade)
                .WithMany()
                .HasForeignKey(x => x.UnidadeId)
                .OnDelete(DeleteBehavior.Restrict);

            e.HasOne(x => x.CategoriaAssociado)
                .WithMany()
                .HasForeignKey(x => x.CategoriaAssociadoId)
                .OnDelete(DeleteBehavior.Restrict);

            e.HasOne(x => x.CriadoPorUsuario)
                .WithMany()
                .HasForeignKey(x => x.CriadoPorUsuarioId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // 15. Mensalidade
        modelBuilder.Entity<Mensalidade>(e =>
        {
            e.ToTable("Mensalidade");
            e.HasKey(x => x.Id);
            e.Property(x => x.ValorPrincipal).HasPrecision(12, 2).IsRequired();
            e.Property(x => x.MultaPercentualEmissao).HasPrecision(7, 4).IsRequired();
            e.Property(x => x.JuroMensalPercentualEmissao).HasPrecision(7, 4).IsRequired();
            e.Property(x => x.Status).HasConversion<byte>().IsRequired();

            e.HasIndex(x => new { x.AssociadoId, x.Competencia }).IsUnique();
            e.HasIndex(x => new { x.UnidadeEmissoraId, x.Status, x.DataVencimento });
            e.HasIndex(x => new { x.AssociadoId, x.DataVencimento });

            e.HasOne(x => x.Associado)
                .WithMany()
                .HasForeignKey(x => x.AssociadoId)
                .OnDelete(DeleteBehavior.Restrict);

            e.HasOne(x => x.UnidadeEmissora)
                .WithMany()
                .HasForeignKey(x => x.UnidadeEmissoraId)
                .OnDelete(DeleteBehavior.Restrict);

            e.HasOne(x => x.CategoriaNaEmissao)
                .WithMany()
                .HasForeignKey(x => x.CategoriaNaEmissaoId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // 16. PagamentoMensalidade
        modelBuilder.Entity<PagamentoMensalidade>(e =>
        {
            e.ToTable("PagamentoMensalidade");
            e.HasKey(x => x.Id);
            e.Property(x => x.ValorPrincipalPago).HasPrecision(12, 2).IsRequired();
            e.Property(x => x.ValorMultaPago).HasPrecision(12, 2).IsRequired();
            e.Property(x => x.ValorJurosPago).HasPrecision(12, 2).IsRequired();
            e.Property(x => x.ValorTotalPago).HasPrecision(12, 2).IsRequired();
            e.Property(x => x.FormaPagamento).HasConversion<byte>().IsRequired();

            e.HasIndex(x => x.MensalidadeId).IsUnique();
            e.HasIndex(x => new { x.DataPagamento, x.FormaPagamento });

            e.HasOne(x => x.Mensalidade)
                .WithOne(m => m.Pagamento)
                .HasForeignKey<PagamentoMensalidade>(x => x.MensalidadeId)
                .OnDelete(DeleteBehavior.Restrict);

            e.HasOne(x => x.RegistradoPorUsuario)
                .WithMany()
                .HasForeignKey(x => x.RegistradoPorUsuarioId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // 17. Evento
        modelBuilder.Entity<Evento>(e =>
        {
            e.ToTable("Evento");
            e.HasKey(x => x.Id);
            e.Property(x => x.Nome).HasMaxLength(180).IsRequired();
            e.Property(x => x.PublicoAlvo).HasConversion<byte>().IsRequired();
            e.Property(x => x.TaxaParticipacao).HasPrecision(12, 2).HasDefaultValue(0m).IsRequired();
            e.Property(x => x.Status).HasConversion<byte>().IsRequired();

            e.HasIndex(x => new { x.UnidadeOrganizadoraId, x.DataHoraInicioUtc });
            e.HasIndex(x => new { x.PublicoAlvo, x.DataHoraInicioUtc });

            e.HasOne(x => x.UnidadeOrganizadora)
                .WithMany()
                .HasForeignKey(x => x.UnidadeOrganizadoraId)
                .OnDelete(DeleteBehavior.Restrict);

            e.HasOne(x => x.CriadoPorUsuario)
                .WithMany()
                .HasForeignKey(x => x.CriadoPorUsuarioId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // 18. EventoUnidade
        modelBuilder.Entity<EventoUnidade>(e =>
        {
            e.ToTable("EventoUnidade");
            e.HasKey(x => new { x.EventoId, x.UnidadeId });
            e.HasIndex(x => new { x.UnidadeId, x.EventoId });

            e.HasOne(x => x.Evento)
                .WithMany(ev => ev.UnidadesParticipantes)
                .HasForeignKey(x => x.EventoId)
                .OnDelete(DeleteBehavior.Restrict);

            e.HasOne(x => x.Unidade)
                .WithMany()
                .HasForeignKey(x => x.UnidadeId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // 19. PagamentoEvento
        modelBuilder.Entity<PagamentoEvento>(e =>
        {
            e.ToTable("PagamentoEvento");
            e.HasKey(x => x.Id);
            e.Property(x => x.NomePagadorInformado).HasMaxLength(180);
            e.Property(x => x.ValorPago).HasPrecision(12, 2).IsRequired();
            e.Property(x => x.FormaPagamento).HasConversion<byte>().IsRequired();

            e.HasIndex(x => new { x.EventoId, x.DataPagamento });
            e.HasIndex(x => x.PessoaPagadoraId);

            e.HasOne(x => x.Evento)
                .WithMany()
                .HasForeignKey(x => x.EventoId)
                .OnDelete(DeleteBehavior.Restrict);

            e.HasOne(x => x.PessoaPagadora)
                .WithMany()
                .HasForeignKey(x => x.PessoaPagadoraId)
                .OnDelete(DeleteBehavior.Restrict);

            e.HasOne(x => x.RegistradoPorUsuario)
                .WithMany()
                .HasForeignKey(x => x.RegistradoPorUsuarioId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // 20. Espaco
        modelBuilder.Entity<Espaco>(e =>
        {
            e.ToTable("Espaco");
            e.HasKey(x => x.Id);
            e.Property(x => x.Nome).HasMaxLength(150).IsRequired();
            e.Property(x => x.Ativo).HasDefaultValue(true).IsRequired();
            e.Property(x => x.AntecedenciaMaximaDias).HasDefaultValue(90).IsRequired();

            e.HasIndex(x => new { x.UnidadeId, x.Nome }).IsUnique();
            e.HasIndex(x => new { x.UnidadeId, x.Ativo });

            e.HasOne(x => x.Unidade)
                .WithMany()
                .HasForeignKey(x => x.UnidadeId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // 21. ReservaEspaco
        modelBuilder.Entity<ReservaEspaco>(e =>
        {
            e.ToTable("ReservaEspaco");
            e.HasKey(x => x.Id);
            e.Property(x => x.Status).HasConversion<byte>().IsRequired();
            e.Property(x => x.JustificativaCancelamento).HasMaxLength(600);

            e.HasIndex(x => new { x.EspacoId, x.Status, x.InicioUtc, x.FimUtc });
            e.HasIndex(x => new { x.AssociadoId, x.InicioUtc });

            e.HasOne(x => x.Espaco)
                .WithMany()
                .HasForeignKey(x => x.EspacoId)
                .OnDelete(DeleteBehavior.Restrict);

            e.HasOne(x => x.Associado)
                .WithMany()
                .HasForeignKey(x => x.AssociadoId)
                .OnDelete(DeleteBehavior.Restrict);

            e.HasOne(x => x.CanceladaPorUsuario)
                .WithMany()
                .HasForeignKey(x => x.CanceladaPorUsuarioId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // 22. OcupacaoEspaco
        modelBuilder.Entity<OcupacaoEspaco>(e =>
        {
            e.ToTable("OcupacaoEspaco");
            e.HasKey(x => x.Id);
            e.Property(x => x.Origem).HasConversion<byte>().IsRequired();
            e.Property(x => x.DescricaoBloqueio).HasMaxLength(300);
            e.Property(x => x.Ativa).HasDefaultValue(true).IsRequired();

            e.HasIndex(x => x.ReservaEspacoId).IsUnique();
            e.HasIndex(x => new { x.EspacoId, x.Ativa, x.InicioUtc, x.FimUtc });
            e.HasIndex(x => x.EventoId);

            e.HasOne(x => x.Espaco)
                .WithMany()
                .HasForeignKey(x => x.EspacoId)
                .OnDelete(DeleteBehavior.Restrict);

            e.HasOne(x => x.ReservaEspaco)
                .WithOne()
                .HasForeignKey<OcupacaoEspaco>(x => x.ReservaEspacoId)
                .OnDelete(DeleteBehavior.Restrict);

            e.HasOne(x => x.Evento)
                .WithMany(ev => ev.OcupacoesEspaco)
                .HasForeignKey(x => x.EventoId)
                .OnDelete(DeleteBehavior.Restrict);

            e.HasOne(x => x.CriadaPorUsuario)
                .WithMany()
                .HasForeignKey(x => x.CriadaPorUsuarioId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // 23. CategoriaManifestacao
        modelBuilder.Entity<CategoriaManifestacao>(e =>
        {
            e.ToTable("CategoriaManifestacao");
            e.HasKey(x => x.Id);
            e.Property(x => x.Nome).HasMaxLength(100).IsRequired();
            e.Property(x => x.TipoManifestacao).HasConversion<byte>().IsRequired();
            e.Property(x => x.Descricao).HasMaxLength(300);
            e.Property(x => x.Ativa).HasDefaultValue(true).IsRequired();

            e.HasIndex(x => new { x.TipoManifestacao, x.Nome }).IsUnique();
            e.HasIndex(x => new { x.Ativa, x.TipoManifestacao });

            e.HasData(
                new CategoriaManifestacao { Id = 1, Nome = "Manutenção", TipoManifestacao = TipoManifestacao.Solicitacao, Descricao = "Solicitações de manutenção de espaços e equipamentos", Ativa = true },
                new CategoriaManifestacao { Id = 2, Nome = "Infraestrutura", TipoManifestacao = TipoManifestacao.Reclamacao, Descricao = "Reclamações sobre infraestrutura das unidades", Ativa = true },
                new CategoriaManifestacao { Id = 3, Nome = "Atendimento", TipoManifestacao = TipoManifestacao.Reclamacao, Descricao = "Reclamações sobre atendimento", Ativa = true },
                new CategoriaManifestacao { Id = 4, Nome = "Financeiro", TipoManifestacao = TipoManifestacao.Solicitacao, Descricao = "Dúvidas ou solicitações financeiras", Ativa = true },
                new CategoriaManifestacao { Id = 5, Nome = "Eventos e Reservas", TipoManifestacao = TipoManifestacao.Sugestao, Descricao = "Sugestões para eventos e espaços", Ativa = true },
                new CategoriaManifestacao { Id = 6, Nome = "Melhorias e Novos Serviços", TipoManifestacao = TipoManifestacao.Sugestao, Descricao = "Sugestões gerais para o clube", Ativa = true }
            );
        });

        // 24. Manifestacao
        modelBuilder.Entity<Manifestacao>(e =>
        {
            e.ToTable("Manifestacao");
            e.HasKey(x => x.Id);
            e.Property(x => x.TipoManifestacao).HasConversion<byte>().IsRequired();
            e.Property(x => x.Titulo).HasMaxLength(150).IsRequired();
            e.Property(x => x.Descricao).IsRequired();
            e.Property(x => x.Status).HasConversion<byte>().IsRequired();

            e.HasIndex(x => new { x.UnidadeId, x.Status, x.CriadaEmUtc });
            e.HasIndex(x => new { x.PessoaAutorId, x.CriadaEmUtc });

            e.HasOne(x => x.Unidade)
                .WithMany()
                .HasForeignKey(x => x.UnidadeId)
                .OnDelete(DeleteBehavior.Restrict);

            e.HasOne(x => x.PessoaAutor)
                .WithMany()
                .HasForeignKey(x => x.PessoaAutorId)
                .OnDelete(DeleteBehavior.Restrict);

            e.HasOne(x => x.CategoriaManifestacao)
                .WithMany()
                .HasForeignKey(x => x.CategoriaManifestacaoId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // 25. RespostaManifestacao
        modelBuilder.Entity<RespostaManifestacao>(e =>
        {
            e.ToTable("RespostaManifestacao");
            e.HasKey(x => x.Id);
            e.Property(x => x.Mensagem).IsRequired();
            e.HasIndex(x => new { x.ManifestacaoId, x.CriadaEmUtc });

            e.HasOne(x => x.Manifestacao)
                .WithMany(m => m.Respostas)
                .HasForeignKey(x => x.ManifestacaoId)
                .OnDelete(DeleteBehavior.Restrict);

            e.HasOne(x => x.AutorUsuario)
                .WithMany()
                .HasForeignKey(x => x.AutorUsuarioId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // 26. Notificacao
        modelBuilder.Entity<Notificacao>(e =>
        {
            e.ToTable("Notificacao");
            e.HasKey(x => x.Id);
            e.Property(x => x.Tipo).HasConversion<byte>().IsRequired();
            e.Property(x => x.Titulo).HasMaxLength(150).IsRequired();
            e.Property(x => x.Mensagem).HasMaxLength(800).IsRequired();
            e.Property(x => x.ReferenciaTipo).HasMaxLength(40);
            e.Property(x => x.ChaveIdempotencia).HasMaxLength(100);

            e.HasIndex(x => new { x.DestinatarioUsuarioId, x.ChaveIdempotencia }).IsUnique();
            e.HasIndex(x => new { x.DestinatarioUsuarioId, x.LidaEmUtc, x.CriadaEmUtc });

            e.HasOne(x => x.DestinatarioUsuario)
                .WithMany()
                .HasForeignKey(x => x.DestinatarioUsuarioId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // 27. Votacao
        modelBuilder.Entity<Votacao>(e =>
        {
            e.ToTable("Votacao");
            e.HasKey(x => x.Id);
            e.Property(x => x.Titulo).HasMaxLength(180).IsRequired();
            e.Property(x => x.Escopo).HasConversion<byte>().IsRequired();
            e.Property(x => x.Estado).HasConversion<byte>().IsRequired();
            e.Property(x => x.JustificativaCancelamento).HasMaxLength(600);

            e.HasIndex(x => new { x.Estado, x.InicioUtc, x.FimPrevistoUtc });
            e.HasIndex(x => new { x.UnidadeId, x.Estado });

            e.HasOne(x => x.Unidade)
                .WithMany()
                .HasForeignKey(x => x.UnidadeId)
                .OnDelete(DeleteBehavior.Restrict);

            e.HasOne(x => x.CriadaPorUsuario)
                .WithMany()
                .HasForeignKey(x => x.CriadaPorUsuarioId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // 28. OpcaoVotacao
        modelBuilder.Entity<OpcaoVotacao>(e =>
        {
            e.ToTable("OpcaoVotacao");
            e.HasKey(x => x.Id);
            e.HasAlternateKey(x => new { x.Id, x.VotacaoId });
            e.Property(x => x.Texto).HasMaxLength(200).IsRequired();
            e.HasIndex(x => new { x.VotacaoId, x.Ordem }).IsUnique();

            e.HasOne(x => x.Votacao)
                .WithMany(v => v.Opcoes)
                .HasForeignKey(x => x.VotacaoId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // 29. EleitorVotacao
        modelBuilder.Entity<EleitorVotacao>(e =>
        {
            e.ToTable("EleitorVotacao");
            e.HasKey(x => x.Id);
            e.HasAlternateKey(x => new { x.Id, x.VotacaoId });
            e.HasIndex(x => new { x.VotacaoId, x.AssociadoId }).IsUnique();
            e.HasIndex(x => new { x.AssociadoId, x.VotacaoId });

            e.HasOne(x => x.Votacao)
                .WithMany(v => v.Eleitores)
                .HasForeignKey(x => x.VotacaoId)
                .OnDelete(DeleteBehavior.Restrict);

            e.HasOne(x => x.Associado)
                .WithMany()
                .HasForeignKey(x => x.AssociadoId)
                .OnDelete(DeleteBehavior.Restrict);

            e.HasOne(x => x.Pessoa)
                .WithMany()
                .HasForeignKey(x => x.PessoaId)
                .OnDelete(DeleteBehavior.Restrict);

            e.HasOne(x => x.UnidadeNaAbertura)
                .WithMany()
                .HasForeignKey(x => x.UnidadeIdNaAbertura)
                .OnDelete(DeleteBehavior.Restrict);

            e.HasOne(x => x.CategoriaNaAbertura)
                .WithMany()
                .HasForeignKey(x => x.CategoriaIdNaAbertura)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // 30. Voto
        modelBuilder.Entity<Voto>(e =>
        {
            e.ToTable("Voto");
            e.HasKey(x => x.Id);
            e.HasIndex(x => x.EleitorVotacaoId).IsUnique();
            e.HasIndex(x => new { x.VotacaoId, x.OpcaoVotacaoId });

            e.HasOne(x => x.Votacao)
                .WithMany(v => v.Votos)
                .HasForeignKey(x => x.VotacaoId)
                .OnDelete(DeleteBehavior.Restrict);

            e.HasOne(x => x.EleitorVotacao)
                .WithMany()
                .HasForeignKey(x => new { x.EleitorVotacaoId, x.VotacaoId })
                .HasPrincipalKey(ev => new { ev.Id, ev.VotacaoId })
                .OnDelete(DeleteBehavior.Restrict);

            e.HasOne(x => x.OpcaoVotacao)
                .WithMany()
                .HasForeignKey(x => new { x.OpcaoVotacaoId, x.VotacaoId })
                .HasPrincipalKey(ov => new { ov.Id, ov.VotacaoId })
                .OnDelete(DeleteBehavior.Restrict);
        });

        // 31. RegistroAuditoria
        modelBuilder.Entity<RegistroAuditoria>(e =>
        {
            e.ToTable("RegistroAuditoria");
            e.HasKey(x => x.Id);
            e.Property(x => x.TipoOperacao).HasMaxLength(80).IsRequired();
            e.Property(x => x.Entidade).HasMaxLength(70).IsRequired();
            e.Property(x => x.DescricaoSegura).HasMaxLength(600);

            e.HasIndex(x => new { x.UnidadeId, x.CriadaEmUtc });
            e.HasIndex(x => new { x.Entidade, x.EntidadeId, x.CriadaEmUtc });

            e.HasOne(x => x.UsuarioAutor)
                .WithMany()
                .HasForeignKey(x => x.UsuarioAutorId)
                .OnDelete(DeleteBehavior.Restrict);

            e.HasOne(x => x.Unidade)
                .WithMany()
                .HasForeignKey(x => x.UnidadeId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // 32. ExecucaoRotina
        modelBuilder.Entity<ExecucaoRotina>(e =>
        {
            e.ToTable("ExecucaoRotina");
            e.HasKey(x => x.Id);
            e.Property(x => x.NomeRotina).HasMaxLength(80).IsRequired();
            e.Property(x => x.ChaveCompetencia).HasMaxLength(100).IsRequired();
            e.Property(x => x.Estado).HasConversion<byte>().IsRequired();
            e.Property(x => x.ErroResumo).HasMaxLength(500);

            e.HasIndex(x => new { x.NomeRotina, x.ChaveCompetencia }).IsUnique();
        });
    }
}
