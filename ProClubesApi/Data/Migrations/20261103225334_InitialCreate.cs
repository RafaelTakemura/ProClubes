using System;
using Microsoft.EntityFrameworkCore.Migrations;
using MySql.EntityFrameworkCore.Metadata;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace ProClubesApi.Data.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterDatabase()
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "CategoriaAssociado",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    Codigo = table.Column<string>(type: "varchar(32)", maxLength: 32, nullable: false),
                    Nome = table.Column<string>(type: "varchar(80)", maxLength: 80, nullable: false),
                    TemDireitoVoto = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    IsentoMensalidade = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    Ativa = table.Column<bool>(type: "tinyint(1)", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CategoriaAssociado", x => x.Id);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "CategoriaManifestacao",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    Nome = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false),
                    TipoManifestacao = table.Column<byte>(type: "tinyint unsigned", nullable: false),
                    Descricao = table.Column<string>(type: "varchar(300)", maxLength: 300, nullable: true),
                    Ativa = table.Column<bool>(type: "tinyint(1)", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CategoriaManifestacao", x => x.Id);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "ExecucaoRotina",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    NomeRotina = table.Column<string>(type: "varchar(80)", maxLength: 80, nullable: false),
                    ChaveCompetencia = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false),
                    Estado = table.Column<byte>(type: "tinyint unsigned", nullable: false),
                    IniciadaEmUtc = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    ConcluidaEmUtc = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    ErroResumo = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExecucaoRotina", x => x.Id);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "Interesse",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    Nome = table.Column<string>(type: "varchar(80)", maxLength: 80, nullable: false),
                    Ativo = table.Column<bool>(type: "tinyint(1)", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Interesse", x => x.Id);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "Pessoa",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    NomeCompleto = table.Column<string>(type: "varchar(180)", maxLength: 180, nullable: false),
                    Cpf = table.Column<string>(type: "char(11)", fixedLength: true, maxLength: 11, nullable: false),
                    Sexo = table.Column<string>(type: "varchar(30)", maxLength: 30, nullable: false),
                    EstadoCivil = table.Column<string>(type: "varchar(40)", maxLength: 40, nullable: false),
                    DataNascimento = table.Column<DateOnly>(type: "date", nullable: false),
                    Logradouro = table.Column<string>(type: "varchar(150)", maxLength: 150, nullable: false),
                    Bairro = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false),
                    Numero = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false),
                    Cidade = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false),
                    UF = table.Column<string>(type: "char(2)", fixedLength: true, maxLength: 2, nullable: false),
                    Fone = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false),
                    Email = table.Column<string>(type: "varchar(254)", maxLength: 254, nullable: false),
                    CriadaEmUtc = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    AtualizadaEmUtc = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Pessoa", x => x.Id);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "Unidade",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    Nome = table.Column<string>(type: "varchar(150)", maxLength: 150, nullable: false),
                    Sigla = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false),
                    Logradouro = table.Column<string>(type: "varchar(150)", maxLength: 150, nullable: false),
                    Bairro = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false),
                    Numero = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false),
                    Cidade = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false),
                    UF = table.Column<string>(type: "char(2)", fixedLength: true, maxLength: 2, nullable: false),
                    Fone = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false),
                    Email = table.Column<string>(type: "varchar(254)", maxLength: 254, nullable: false),
                    Ativa = table.Column<bool>(type: "tinyint(1)", nullable: false, defaultValue: true),
                    CriadaEmUtc = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Unidade", x => x.Id);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "PessoaInteresse",
                columns: table => new
                {
                    PessoaId = table.Column<int>(type: "int", nullable: false),
                    InteresseId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PessoaInteresse", x => new { x.PessoaId, x.InteresseId });
                    table.ForeignKey(
                        name: "FK_PessoaInteresse_Interesse_InteresseId",
                        column: x => x.InteresseId,
                        principalTable: "Interesse",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PessoaInteresse_Pessoa_PessoaId",
                        column: x => x.PessoaId,
                        principalTable: "Pessoa",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "Associado",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    PessoaId = table.Column<int>(type: "int", nullable: false),
                    UnidadeId = table.Column<int>(type: "int", nullable: false),
                    CategoriaAssociadoId = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<byte>(type: "tinyint unsigned", nullable: false),
                    DataIngresso = table.Column<DateOnly>(type: "date", nullable: false),
                    DataInicioCobranca = table.Column<DateOnly>(type: "date", nullable: true),
                    DesativadoEmUtc = table.Column<DateTime>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Associado", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Associado_CategoriaAssociado_CategoriaAssociadoId",
                        column: x => x.CategoriaAssociadoId,
                        principalTable: "CategoriaAssociado",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Associado_Pessoa_PessoaId",
                        column: x => x.PessoaId,
                        principalTable: "Pessoa",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Associado_Unidade_UnidadeId",
                        column: x => x.UnidadeId,
                        principalTable: "Unidade",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "Espaco",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    UnidadeId = table.Column<int>(type: "int", nullable: false),
                    Nome = table.Column<string>(type: "varchar(150)", maxLength: 150, nullable: false),
                    Descricao = table.Column<string>(type: "longtext", nullable: true),
                    Ativo = table.Column<bool>(type: "tinyint(1)", nullable: false, defaultValue: true),
                    HoraAbertura = table.Column<TimeOnly>(type: "time", nullable: false),
                    HoraFechamento = table.Column<TimeOnly>(type: "time", nullable: false),
                    DuracaoMaximaMinutos = table.Column<int>(type: "int", nullable: false),
                    AntecedenciaMaximaDias = table.Column<int>(type: "int", nullable: false, defaultValue: 90)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Espaco", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Espaco_Unidade_UnidadeId",
                        column: x => x.UnidadeId,
                        principalTable: "Unidade",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "Manifestacao",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    UnidadeId = table.Column<int>(type: "int", nullable: false),
                    PessoaAutorId = table.Column<int>(type: "int", nullable: false),
                    CategoriaManifestacaoId = table.Column<int>(type: "int", nullable: false),
                    TipoManifestacao = table.Column<byte>(type: "tinyint unsigned", nullable: false),
                    Titulo = table.Column<string>(type: "varchar(150)", maxLength: 150, nullable: false),
                    Descricao = table.Column<string>(type: "longtext", nullable: false),
                    Status = table.Column<byte>(type: "tinyint unsigned", nullable: false),
                    CriadaEmUtc = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    ConcluidaEmUtc = table.Column<DateTime>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Manifestacao", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Manifestacao_CategoriaManifestacao_CategoriaManifestacaoId",
                        column: x => x.CategoriaManifestacaoId,
                        principalTable: "CategoriaManifestacao",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Manifestacao_Pessoa_PessoaAutorId",
                        column: x => x.PessoaAutorId,
                        principalTable: "Pessoa",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Manifestacao_Unidade_UnidadeId",
                        column: x => x.UnidadeId,
                        principalTable: "Unidade",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "Usuario",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    PessoaId = table.Column<int>(type: "int", nullable: false),
                    SenhaHash = table.Column<string>(type: "varchar(512)", maxLength: 512, nullable: false),
                    RoleAdministrativa = table.Column<byte>(type: "tinyint unsigned", nullable: false),
                    UnidadeAdministrativaId = table.Column<int>(type: "int", nullable: true),
                    EstadoConta = table.Column<byte>(type: "tinyint unsigned", nullable: false),
                    VersaoSeguranca = table.Column<int>(type: "int", nullable: false, defaultValue: 1),
                    AdminAtivoUnidadeId = table.Column<int>(type: "int", nullable: true, computedColumnSql: "CASE WHEN RoleAdministrativa = 2 AND EstadoConta = 1 THEN UnidadeAdministrativaId ELSE NULL END", stored: true),
                    CriadoEmUtc = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    UltimoLoginUtc = table.Column<DateTime>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Usuario", x => x.Id);
                    table.CheckConstraint("CK_Usuario_RoleUnidade", "((RoleAdministrativa IN (1, 2) AND UnidadeAdministrativaId IS NOT NULL) OR (RoleAdministrativa IN (0, 3) AND UnidadeAdministrativaId IS NULL))");
                    table.ForeignKey(
                        name: "FK_Usuario_Pessoa_PessoaId",
                        column: x => x.PessoaId,
                        principalTable: "Pessoa",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Usuario_Unidade_UnidadeAdministrativaId",
                        column: x => x.UnidadeAdministrativaId,
                        principalTable: "Unidade",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "Dependencia",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    AssociadoTitularId = table.Column<int>(type: "int", nullable: false),
                    PessoaDependenteId = table.Column<int>(type: "int", nullable: false),
                    TipoVinculo = table.Column<byte>(type: "tinyint unsigned", nullable: false),
                    DataInicio = table.Column<DateOnly>(type: "date", nullable: false),
                    DataFim = table.Column<DateOnly>(type: "date", nullable: true),
                    MotivoFim = table.Column<byte>(type: "tinyint unsigned", nullable: true),
                    DependenteAtivoPessoaId = table.Column<int>(type: "int", nullable: true, computedColumnSql: "CASE WHEN DataFim IS NULL THEN PessoaDependenteId ELSE NULL END", stored: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Dependencia", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Dependencia_Associado_AssociadoTitularId",
                        column: x => x.AssociadoTitularId,
                        principalTable: "Associado",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Dependencia_Pessoa_PessoaDependenteId",
                        column: x => x.PessoaDependenteId,
                        principalTable: "Pessoa",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "Mensalidade",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    AssociadoId = table.Column<int>(type: "int", nullable: false),
                    UnidadeEmissoraId = table.Column<int>(type: "int", nullable: false),
                    CategoriaNaEmissaoId = table.Column<int>(type: "int", nullable: false),
                    Competencia = table.Column<DateOnly>(type: "date", nullable: false),
                    DataGeracao = table.Column<DateOnly>(type: "date", nullable: false),
                    DataVencimento = table.Column<DateOnly>(type: "date", nullable: false),
                    ValorPrincipal = table.Column<decimal>(type: "decimal(12,2)", precision: 12, scale: 2, nullable: false),
                    MultaPercentualEmissao = table.Column<decimal>(type: "decimal(7,4)", precision: 7, scale: 4, nullable: false),
                    JuroMensalPercentualEmissao = table.Column<decimal>(type: "decimal(7,4)", precision: 7, scale: 4, nullable: false),
                    Status = table.Column<byte>(type: "tinyint unsigned", nullable: false),
                    GeradaEmUtc = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Mensalidade", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Mensalidade_Associado_AssociadoId",
                        column: x => x.AssociadoId,
                        principalTable: "Associado",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Mensalidade_CategoriaAssociado_CategoriaNaEmissaoId",
                        column: x => x.CategoriaNaEmissaoId,
                        principalTable: "CategoriaAssociado",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Mensalidade_Unidade_UnidadeEmissoraId",
                        column: x => x.UnidadeEmissoraId,
                        principalTable: "Unidade",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "ConfiguracaoFinanceira",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false),
                    DiaGeracao = table.Column<byte>(type: "tinyint unsigned", nullable: false),
                    DiaVencimento = table.Column<byte>(type: "tinyint unsigned", nullable: false),
                    DiaAvisoVencimento = table.Column<byte>(type: "tinyint unsigned", nullable: true),
                    PercentualMulta = table.Column<decimal>(type: "decimal(7,4)", precision: 7, scale: 4, nullable: false),
                    PercentualJuroMensal = table.Column<decimal>(type: "decimal(7,4)", precision: 7, scale: 4, nullable: false),
                    AtualizadaEmUtc = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    AlteradaPorUsuarioId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ConfiguracaoFinanceira", x => x.Id);
                    table.CheckConstraint("CK_ConfiguracaoFinanceira_Dias", "DiaGeracao BETWEEN 1 AND 31 AND DiaVencimento BETWEEN 1 AND 31");
                    table.CheckConstraint("CK_ConfiguracaoFinanceira_Percentuais", "PercentualMulta >= 0 AND PercentualJuroMensal >= 0");
                    table.CheckConstraint("CK_ConfiguracaoFinanceira_Singleton", "Id = 1");
                    table.ForeignKey(
                        name: "FK_ConfiguracaoFinanceira_Usuario_AlteradaPorUsuarioId",
                        column: x => x.AlteradaPorUsuarioId,
                        principalTable: "Usuario",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "Evento",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    UnidadeOrganizadoraId = table.Column<int>(type: "int", nullable: false),
                    Nome = table.Column<string>(type: "varchar(180)", maxLength: 180, nullable: false),
                    Descricao = table.Column<string>(type: "longtext", nullable: true),
                    PublicoAlvo = table.Column<byte>(type: "tinyint unsigned", nullable: false),
                    DataHoraInicioUtc = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    DataHoraFimUtc = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    LimiteParticipantes = table.Column<int>(type: "int", nullable: true),
                    TaxaParticipacao = table.Column<decimal>(type: "decimal(12,2)", precision: 12, scale: 2, nullable: false, defaultValue: 0m),
                    Status = table.Column<byte>(type: "tinyint unsigned", nullable: false),
                    CriadoPorUsuarioId = table.Column<int>(type: "int", nullable: false),
                    CriadoEmUtc = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Evento", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Evento_Unidade_UnidadeOrganizadoraId",
                        column: x => x.UnidadeOrganizadoraId,
                        principalTable: "Unidade",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Evento_Usuario_CriadoPorUsuarioId",
                        column: x => x.CriadoPorUsuarioId,
                        principalTable: "Usuario",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "HistoricoCategoriaAssociado",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    AssociadoId = table.Column<int>(type: "int", nullable: false),
                    CategoriaAnteriorId = table.Column<int>(type: "int", nullable: false),
                    CategoriaNovaId = table.Column<int>(type: "int", nullable: false),
                    VigenteApartir = table.Column<DateOnly>(type: "date", nullable: false),
                    AlteradoPorUsuarioId = table.Column<int>(type: "int", nullable: true),
                    AlteradoEmUtc = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    Motivo = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HistoricoCategoriaAssociado", x => x.Id);
                    table.ForeignKey(
                        name: "FK_HistoricoCategoriaAssociado_Associado_AssociadoId",
                        column: x => x.AssociadoId,
                        principalTable: "Associado",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_HistoricoCategoriaAssociado_CategoriaAssociado_CategoriaAnte~",
                        column: x => x.CategoriaAnteriorId,
                        principalTable: "CategoriaAssociado",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_HistoricoCategoriaAssociado_CategoriaAssociado_CategoriaNova~",
                        column: x => x.CategoriaNovaId,
                        principalTable: "CategoriaAssociado",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_HistoricoCategoriaAssociado_Usuario_AlteradoPorUsuarioId",
                        column: x => x.AlteradoPorUsuarioId,
                        principalTable: "Usuario",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "Notificacao",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    DestinatarioUsuarioId = table.Column<int>(type: "int", nullable: false),
                    Tipo = table.Column<byte>(type: "tinyint unsigned", nullable: false),
                    Titulo = table.Column<string>(type: "varchar(150)", maxLength: 150, nullable: false),
                    Mensagem = table.Column<string>(type: "varchar(800)", maxLength: 800, nullable: false),
                    ReferenciaTipo = table.Column<string>(type: "varchar(40)", maxLength: 40, nullable: true),
                    ReferenciaId = table.Column<int>(type: "int", nullable: true),
                    CriadaEmUtc = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    LidaEmUtc = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    ChaveIdempotencia = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Notificacao", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Notificacao_Usuario_DestinatarioUsuarioId",
                        column: x => x.DestinatarioUsuarioId,
                        principalTable: "Usuario",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "RegistroAuditoria",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    UsuarioAutorId = table.Column<int>(type: "int", nullable: true),
                    UnidadeId = table.Column<int>(type: "int", nullable: true),
                    TipoOperacao = table.Column<string>(type: "varchar(80)", maxLength: 80, nullable: false),
                    Entidade = table.Column<string>(type: "varchar(70)", maxLength: 70, nullable: false),
                    EntidadeId = table.Column<int>(type: "int", nullable: true),
                    DescricaoSegura = table.Column<string>(type: "varchar(600)", maxLength: 600, nullable: true),
                    CriadaEmUtc = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RegistroAuditoria", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RegistroAuditoria_Unidade_UnidadeId",
                        column: x => x.UnidadeId,
                        principalTable: "Unidade",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RegistroAuditoria_Usuario_UsuarioAutorId",
                        column: x => x.UsuarioAutorId,
                        principalTable: "Usuario",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "ReservaEspaco",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    EspacoId = table.Column<int>(type: "int", nullable: false),
                    AssociadoId = table.Column<int>(type: "int", nullable: false),
                    InicioUtc = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    FimUtc = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    Status = table.Column<byte>(type: "tinyint unsigned", nullable: false),
                    CriadaEmUtc = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    CanceladaEmUtc = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    CanceladaPorUsuarioId = table.Column<int>(type: "int", nullable: true),
                    JustificativaCancelamento = table.Column<string>(type: "varchar(600)", maxLength: 600, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReservaEspaco", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ReservaEspaco_Associado_AssociadoId",
                        column: x => x.AssociadoId,
                        principalTable: "Associado",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ReservaEspaco_Espaco_EspacoId",
                        column: x => x.EspacoId,
                        principalTable: "Espaco",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ReservaEspaco_Usuario_CanceladaPorUsuarioId",
                        column: x => x.CanceladaPorUsuarioId,
                        principalTable: "Usuario",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "RespostaManifestacao",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    ManifestacaoId = table.Column<int>(type: "int", nullable: false),
                    AutorUsuarioId = table.Column<int>(type: "int", nullable: false),
                    Mensagem = table.Column<string>(type: "longtext", nullable: false),
                    CriadaEmUtc = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RespostaManifestacao", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RespostaManifestacao_Manifestacao_ManifestacaoId",
                        column: x => x.ManifestacaoId,
                        principalTable: "Manifestacao",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RespostaManifestacao_Usuario_AutorUsuarioId",
                        column: x => x.AutorUsuarioId,
                        principalTable: "Usuario",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "SessaoRenovacao",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    UsuarioId = table.Column<int>(type: "int", nullable: false),
                    TokenHash = table.Column<string>(type: "char(64)", fixedLength: true, maxLength: 64, nullable: false),
                    CriadoEmUtc = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    ExpiraEmUtc = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    RevogadoEmUtc = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    SubstituidoPorSessaoId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SessaoRenovacao", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SessaoRenovacao_SessaoRenovacao_SubstituidoPorSessaoId",
                        column: x => x.SubstituidoPorSessaoId,
                        principalTable: "SessaoRenovacao",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SessaoRenovacao_Usuario_UsuarioId",
                        column: x => x.UsuarioId,
                        principalTable: "Usuario",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "SolicitacaoCadastro",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    PessoaId = table.Column<int>(type: "int", nullable: false),
                    UsuarioId = table.Column<int>(type: "int", nullable: false),
                    UnidadePretendidaId = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<byte>(type: "tinyint unsigned", nullable: false),
                    CategoriaAprovadaId = table.Column<int>(type: "int", nullable: true),
                    JustificativaRejeicao = table.Column<string>(type: "varchar(800)", maxLength: 800, nullable: true),
                    AvaliadorUsuarioId = table.Column<int>(type: "int", nullable: true),
                    SolicitadaEmUtc = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    DecididaEmUtc = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    PendentePessoaId = table.Column<int>(type: "int", nullable: true, computedColumnSql: "CASE WHEN Status = 0 THEN PessoaId ELSE NULL END", stored: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SolicitacaoCadastro", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SolicitacaoCadastro_CategoriaAssociado_CategoriaAprovadaId",
                        column: x => x.CategoriaAprovadaId,
                        principalTable: "CategoriaAssociado",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SolicitacaoCadastro_Pessoa_PessoaId",
                        column: x => x.PessoaId,
                        principalTable: "Pessoa",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SolicitacaoCadastro_Unidade_UnidadePretendidaId",
                        column: x => x.UnidadePretendidaId,
                        principalTable: "Unidade",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SolicitacaoCadastro_Usuario_AvaliadorUsuarioId",
                        column: x => x.AvaliadorUsuarioId,
                        principalTable: "Usuario",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SolicitacaoCadastro_Usuario_UsuarioId",
                        column: x => x.UsuarioId,
                        principalTable: "Usuario",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "TransferenciaUnidade",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    AssociadoId = table.Column<int>(type: "int", nullable: false),
                    UnidadeOrigemId = table.Column<int>(type: "int", nullable: false),
                    UnidadeDestinoId = table.Column<int>(type: "int", nullable: false),
                    DataEfetiva = table.Column<DateOnly>(type: "date", nullable: false),
                    CompetenciaCobrancaDestino = table.Column<DateOnly>(type: "date", nullable: false),
                    RealizadaPorUsuarioId = table.Column<int>(type: "int", nullable: false),
                    RealizadaEmUtc = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TransferenciaUnidade", x => x.Id);
                    table.CheckConstraint("CK_TransferenciaUnidade_Distintas", "UnidadeOrigemId <> UnidadeDestinoId");
                    table.ForeignKey(
                        name: "FK_TransferenciaUnidade_Associado_AssociadoId",
                        column: x => x.AssociadoId,
                        principalTable: "Associado",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TransferenciaUnidade_Unidade_UnidadeDestinoId",
                        column: x => x.UnidadeDestinoId,
                        principalTable: "Unidade",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TransferenciaUnidade_Unidade_UnidadeOrigemId",
                        column: x => x.UnidadeOrigemId,
                        principalTable: "Unidade",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TransferenciaUnidade_Usuario_RealizadaPorUsuarioId",
                        column: x => x.RealizadaPorUsuarioId,
                        principalTable: "Usuario",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "ValorMensalidade",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    UnidadeId = table.Column<int>(type: "int", nullable: false),
                    CategoriaAssociadoId = table.Column<int>(type: "int", nullable: false),
                    Valor = table.Column<decimal>(type: "decimal(12,2)", precision: 12, scale: 2, nullable: false),
                    CompetenciaInicio = table.Column<DateOnly>(type: "date", nullable: false),
                    CompetenciaFim = table.Column<DateOnly>(type: "date", nullable: true),
                    CriadoPorUsuarioId = table.Column<int>(type: "int", nullable: false),
                    CriadoEmUtc = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ValorMensalidade", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ValorMensalidade_CategoriaAssociado_CategoriaAssociadoId",
                        column: x => x.CategoriaAssociadoId,
                        principalTable: "CategoriaAssociado",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ValorMensalidade_Unidade_UnidadeId",
                        column: x => x.UnidadeId,
                        principalTable: "Unidade",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ValorMensalidade_Usuario_CriadoPorUsuarioId",
                        column: x => x.CriadoPorUsuarioId,
                        principalTable: "Usuario",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "Votacao",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    Titulo = table.Column<string>(type: "varchar(180)", maxLength: 180, nullable: false),
                    Descricao = table.Column<string>(type: "longtext", nullable: true),
                    Escopo = table.Column<byte>(type: "tinyint unsigned", nullable: false),
                    UnidadeId = table.Column<int>(type: "int", nullable: true),
                    InicioUtc = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    FimPrevistoUtc = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    Estado = table.Column<byte>(type: "tinyint unsigned", nullable: false),
                    SnapshotConcluidoEmUtc = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    EncerradaEmUtc = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    ResultadosDivulgadosEmUtc = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    JustificativaCancelamento = table.Column<string>(type: "varchar(600)", maxLength: 600, nullable: true),
                    CriadaPorUsuarioId = table.Column<int>(type: "int", nullable: false),
                    CriadaEmUtc = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Votacao", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Votacao_Unidade_UnidadeId",
                        column: x => x.UnidadeId,
                        principalTable: "Unidade",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Votacao_Usuario_CriadaPorUsuarioId",
                        column: x => x.CriadaPorUsuarioId,
                        principalTable: "Usuario",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "PagamentoMensalidade",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    MensalidadeId = table.Column<int>(type: "int", nullable: false),
                    ValorPrincipalPago = table.Column<decimal>(type: "decimal(12,2)", precision: 12, scale: 2, nullable: false),
                    ValorMultaPago = table.Column<decimal>(type: "decimal(12,2)", precision: 12, scale: 2, nullable: false),
                    ValorJurosPago = table.Column<decimal>(type: "decimal(12,2)", precision: 12, scale: 2, nullable: false),
                    ValorTotalPago = table.Column<decimal>(type: "decimal(12,2)", precision: 12, scale: 2, nullable: false),
                    FormaPagamento = table.Column<byte>(type: "tinyint unsigned", nullable: false),
                    DataPagamento = table.Column<DateOnly>(type: "date", nullable: false),
                    RegistradoPorUsuarioId = table.Column<int>(type: "int", nullable: false),
                    RegistradoEmUtc = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PagamentoMensalidade", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PagamentoMensalidade_Mensalidade_MensalidadeId",
                        column: x => x.MensalidadeId,
                        principalTable: "Mensalidade",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PagamentoMensalidade_Usuario_RegistradoPorUsuarioId",
                        column: x => x.RegistradoPorUsuarioId,
                        principalTable: "Usuario",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "EventoUnidade",
                columns: table => new
                {
                    EventoId = table.Column<int>(type: "int", nullable: false),
                    UnidadeId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EventoUnidade", x => new { x.EventoId, x.UnidadeId });
                    table.ForeignKey(
                        name: "FK_EventoUnidade_Evento_EventoId",
                        column: x => x.EventoId,
                        principalTable: "Evento",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_EventoUnidade_Unidade_UnidadeId",
                        column: x => x.UnidadeId,
                        principalTable: "Unidade",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "PagamentoEvento",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    EventoId = table.Column<int>(type: "int", nullable: false),
                    PessoaPagadoraId = table.Column<int>(type: "int", nullable: true),
                    NomePagadorInformado = table.Column<string>(type: "varchar(180)", maxLength: 180, nullable: true),
                    ValorPago = table.Column<decimal>(type: "decimal(12,2)", precision: 12, scale: 2, nullable: false),
                    FormaPagamento = table.Column<byte>(type: "tinyint unsigned", nullable: false),
                    DataPagamento = table.Column<DateOnly>(type: "date", nullable: false),
                    RegistradoPorUsuarioId = table.Column<int>(type: "int", nullable: false),
                    RegistradoEmUtc = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PagamentoEvento", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PagamentoEvento_Evento_EventoId",
                        column: x => x.EventoId,
                        principalTable: "Evento",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PagamentoEvento_Pessoa_PessoaPagadoraId",
                        column: x => x.PessoaPagadoraId,
                        principalTable: "Pessoa",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PagamentoEvento_Usuario_RegistradoPorUsuarioId",
                        column: x => x.RegistradoPorUsuarioId,
                        principalTable: "Usuario",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "OcupacaoEspaco",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    EspacoId = table.Column<int>(type: "int", nullable: false),
                    InicioUtc = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    FimUtc = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    Origem = table.Column<byte>(type: "tinyint unsigned", nullable: false),
                    ReservaEspacoId = table.Column<int>(type: "int", nullable: true),
                    EventoId = table.Column<int>(type: "int", nullable: true),
                    DescricaoBloqueio = table.Column<string>(type: "varchar(300)", maxLength: 300, nullable: true),
                    Ativa = table.Column<bool>(type: "tinyint(1)", nullable: false, defaultValue: true),
                    CriadaPorUsuarioId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OcupacaoEspaco", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OcupacaoEspaco_Espaco_EspacoId",
                        column: x => x.EspacoId,
                        principalTable: "Espaco",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OcupacaoEspaco_Evento_EventoId",
                        column: x => x.EventoId,
                        principalTable: "Evento",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OcupacaoEspaco_ReservaEspaco_ReservaEspacoId",
                        column: x => x.ReservaEspacoId,
                        principalTable: "ReservaEspaco",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OcupacaoEspaco_Usuario_CriadaPorUsuarioId",
                        column: x => x.CriadaPorUsuarioId,
                        principalTable: "Usuario",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "EleitorVotacao",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    VotacaoId = table.Column<int>(type: "int", nullable: false),
                    AssociadoId = table.Column<int>(type: "int", nullable: false),
                    PessoaId = table.Column<int>(type: "int", nullable: false),
                    UnidadeIdNaAbertura = table.Column<int>(type: "int", nullable: false),
                    CategoriaIdNaAbertura = table.Column<int>(type: "int", nullable: false),
                    CriadoEmUtc = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EleitorVotacao", x => x.Id);
                    table.UniqueConstraint("AK_EleitorVotacao_Id_VotacaoId", x => new { x.Id, x.VotacaoId });
                    table.ForeignKey(
                        name: "FK_EleitorVotacao_Associado_AssociadoId",
                        column: x => x.AssociadoId,
                        principalTable: "Associado",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_EleitorVotacao_CategoriaAssociado_CategoriaIdNaAbertura",
                        column: x => x.CategoriaIdNaAbertura,
                        principalTable: "CategoriaAssociado",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_EleitorVotacao_Pessoa_PessoaId",
                        column: x => x.PessoaId,
                        principalTable: "Pessoa",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_EleitorVotacao_Unidade_UnidadeIdNaAbertura",
                        column: x => x.UnidadeIdNaAbertura,
                        principalTable: "Unidade",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_EleitorVotacao_Votacao_VotacaoId",
                        column: x => x.VotacaoId,
                        principalTable: "Votacao",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "OpcaoVotacao",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    VotacaoId = table.Column<int>(type: "int", nullable: false),
                    Texto = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: false),
                    Ordem = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OpcaoVotacao", x => x.Id);
                    table.UniqueConstraint("AK_OpcaoVotacao_Id_VotacaoId", x => new { x.Id, x.VotacaoId });
                    table.ForeignKey(
                        name: "FK_OpcaoVotacao_Votacao_VotacaoId",
                        column: x => x.VotacaoId,
                        principalTable: "Votacao",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "Voto",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    VotacaoId = table.Column<int>(type: "int", nullable: false),
                    EleitorVotacaoId = table.Column<int>(type: "int", nullable: false),
                    OpcaoVotacaoId = table.Column<int>(type: "int", nullable: false),
                    RegistradoEmUtc = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Voto", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Voto_EleitorVotacao_EleitorVotacaoId_VotacaoId",
                        columns: x => new { x.EleitorVotacaoId, x.VotacaoId },
                        principalTable: "EleitorVotacao",
                        principalColumns: new[] { "Id", "VotacaoId" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Voto_OpcaoVotacao_OpcaoVotacaoId_VotacaoId",
                        columns: x => new { x.OpcaoVotacaoId, x.VotacaoId },
                        principalTable: "OpcaoVotacao",
                        principalColumns: new[] { "Id", "VotacaoId" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Voto_Votacao_VotacaoId",
                        column: x => x.VotacaoId,
                        principalTable: "Votacao",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.InsertData(
                table: "CategoriaAssociado",
                columns: new[] { "Id", "Ativa", "Codigo", "IsentoMensalidade", "Nome", "TemDireitoVoto" },
                values: new object[,]
                {
                    { 1, true, "ASSOCIADO", false, "Sócio Associado", true },
                    { 2, true, "FUNDADOR", false, "Sócio Fundador", true },
                    { 3, true, "CONVIDADO", false, "Sócio Convidado", false },
                    { 4, true, "SIMPATIZANTE", true, "Simpatizante", false }
                });

            migrationBuilder.InsertData(
                table: "CategoriaManifestacao",
                columns: new[] { "Id", "Ativa", "Descricao", "Nome", "TipoManifestacao" },
                values: new object[,]
                {
                    { 1, true, "Solicitações de manutenção de espaços e equipamentos", "Manutenção", (byte)1 },
                    { 2, true, "Reclamações sobre infraestrutura das unidades", "Infraestrutura", (byte)2 },
                    { 3, true, "Reclamações sobre atendimento", "Atendimento", (byte)2 },
                    { 4, true, "Dúvidas ou solicitações financeiras", "Financeiro", (byte)1 },
                    { 5, true, "Sugestões para eventos e espaços", "Eventos e Reservas", (byte)3 },
                    { 6, true, "Sugestões gerais para o clube", "Melhorias e Novos Serviços", (byte)3 }
                });

            migrationBuilder.InsertData(
                table: "Interesse",
                columns: new[] { "Id", "Ativo", "Nome" },
                values: new object[,]
                {
                    { 1, true, "Pesca" },
                    { 2, true, "Caça" },
                    { 3, true, "Comida" },
                    { 4, true, "Bebida" },
                    { 5, true, "Esportes Aquáticos" },
                    { 6, true, "Eventos Sociais" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_Associado_CategoriaAssociadoId",
                table: "Associado",
                column: "CategoriaAssociadoId");

            migrationBuilder.CreateIndex(
                name: "IX_Associado_PessoaId",
                table: "Associado",
                column: "PessoaId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Associado_UnidadeId_Status_CategoriaAssociadoId",
                table: "Associado",
                columns: new[] { "UnidadeId", "Status", "CategoriaAssociadoId" });

            migrationBuilder.CreateIndex(
                name: "IX_CategoriaAssociado_Codigo",
                table: "CategoriaAssociado",
                column: "Codigo",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CategoriaManifestacao_Ativa_TipoManifestacao",
                table: "CategoriaManifestacao",
                columns: new[] { "Ativa", "TipoManifestacao" });

            migrationBuilder.CreateIndex(
                name: "IX_CategoriaManifestacao_TipoManifestacao_Nome",
                table: "CategoriaManifestacao",
                columns: new[] { "TipoManifestacao", "Nome" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ConfiguracaoFinanceira_AlteradaPorUsuarioId",
                table: "ConfiguracaoFinanceira",
                column: "AlteradaPorUsuarioId");

            migrationBuilder.CreateIndex(
                name: "IX_Dependencia_AssociadoTitularId_DataFim",
                table: "Dependencia",
                columns: new[] { "AssociadoTitularId", "DataFim" });

            migrationBuilder.CreateIndex(
                name: "IX_Dependencia_DependenteAtivoPessoaId",
                table: "Dependencia",
                column: "DependenteAtivoPessoaId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Dependencia_PessoaDependenteId",
                table: "Dependencia",
                column: "PessoaDependenteId");

            migrationBuilder.CreateIndex(
                name: "IX_EleitorVotacao_AssociadoId_VotacaoId",
                table: "EleitorVotacao",
                columns: new[] { "AssociadoId", "VotacaoId" });

            migrationBuilder.CreateIndex(
                name: "IX_EleitorVotacao_CategoriaIdNaAbertura",
                table: "EleitorVotacao",
                column: "CategoriaIdNaAbertura");

            migrationBuilder.CreateIndex(
                name: "IX_EleitorVotacao_PessoaId",
                table: "EleitorVotacao",
                column: "PessoaId");

            migrationBuilder.CreateIndex(
                name: "IX_EleitorVotacao_UnidadeIdNaAbertura",
                table: "EleitorVotacao",
                column: "UnidadeIdNaAbertura");

            migrationBuilder.CreateIndex(
                name: "IX_EleitorVotacao_VotacaoId_AssociadoId",
                table: "EleitorVotacao",
                columns: new[] { "VotacaoId", "AssociadoId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Espaco_UnidadeId_Ativo",
                table: "Espaco",
                columns: new[] { "UnidadeId", "Ativo" });

            migrationBuilder.CreateIndex(
                name: "IX_Espaco_UnidadeId_Nome",
                table: "Espaco",
                columns: new[] { "UnidadeId", "Nome" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Evento_CriadoPorUsuarioId",
                table: "Evento",
                column: "CriadoPorUsuarioId");

            migrationBuilder.CreateIndex(
                name: "IX_Evento_PublicoAlvo_DataHoraInicioUtc",
                table: "Evento",
                columns: new[] { "PublicoAlvo", "DataHoraInicioUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_Evento_UnidadeOrganizadoraId_DataHoraInicioUtc",
                table: "Evento",
                columns: new[] { "UnidadeOrganizadoraId", "DataHoraInicioUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_EventoUnidade_UnidadeId_EventoId",
                table: "EventoUnidade",
                columns: new[] { "UnidadeId", "EventoId" });

            migrationBuilder.CreateIndex(
                name: "IX_ExecucaoRotina_NomeRotina_ChaveCompetencia",
                table: "ExecucaoRotina",
                columns: new[] { "NomeRotina", "ChaveCompetencia" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_HistoricoCategoriaAssociado_AlteradoPorUsuarioId",
                table: "HistoricoCategoriaAssociado",
                column: "AlteradoPorUsuarioId");

            migrationBuilder.CreateIndex(
                name: "IX_HistoricoCategoriaAssociado_AssociadoId_VigenteApartir",
                table: "HistoricoCategoriaAssociado",
                columns: new[] { "AssociadoId", "VigenteApartir" });

            migrationBuilder.CreateIndex(
                name: "IX_HistoricoCategoriaAssociado_CategoriaAnteriorId",
                table: "HistoricoCategoriaAssociado",
                column: "CategoriaAnteriorId");

            migrationBuilder.CreateIndex(
                name: "IX_HistoricoCategoriaAssociado_CategoriaNovaId",
                table: "HistoricoCategoriaAssociado",
                column: "CategoriaNovaId");

            migrationBuilder.CreateIndex(
                name: "IX_Interesse_Nome",
                table: "Interesse",
                column: "Nome",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Manifestacao_CategoriaManifestacaoId",
                table: "Manifestacao",
                column: "CategoriaManifestacaoId");

            migrationBuilder.CreateIndex(
                name: "IX_Manifestacao_PessoaAutorId_CriadaEmUtc",
                table: "Manifestacao",
                columns: new[] { "PessoaAutorId", "CriadaEmUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_Manifestacao_UnidadeId_Status_CriadaEmUtc",
                table: "Manifestacao",
                columns: new[] { "UnidadeId", "Status", "CriadaEmUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_Mensalidade_AssociadoId_Competencia",
                table: "Mensalidade",
                columns: new[] { "AssociadoId", "Competencia" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Mensalidade_AssociadoId_DataVencimento",
                table: "Mensalidade",
                columns: new[] { "AssociadoId", "DataVencimento" });

            migrationBuilder.CreateIndex(
                name: "IX_Mensalidade_CategoriaNaEmissaoId",
                table: "Mensalidade",
                column: "CategoriaNaEmissaoId");

            migrationBuilder.CreateIndex(
                name: "IX_Mensalidade_UnidadeEmissoraId_Status_DataVencimento",
                table: "Mensalidade",
                columns: new[] { "UnidadeEmissoraId", "Status", "DataVencimento" });

            migrationBuilder.CreateIndex(
                name: "IX_Notificacao_DestinatarioUsuarioId_ChaveIdempotencia",
                table: "Notificacao",
                columns: new[] { "DestinatarioUsuarioId", "ChaveIdempotencia" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Notificacao_DestinatarioUsuarioId_LidaEmUtc_CriadaEmUtc",
                table: "Notificacao",
                columns: new[] { "DestinatarioUsuarioId", "LidaEmUtc", "CriadaEmUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_OcupacaoEspaco_CriadaPorUsuarioId",
                table: "OcupacaoEspaco",
                column: "CriadaPorUsuarioId");

            migrationBuilder.CreateIndex(
                name: "IX_OcupacaoEspaco_EspacoId_Ativa_InicioUtc_FimUtc",
                table: "OcupacaoEspaco",
                columns: new[] { "EspacoId", "Ativa", "InicioUtc", "FimUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_OcupacaoEspaco_EventoId",
                table: "OcupacaoEspaco",
                column: "EventoId");

            migrationBuilder.CreateIndex(
                name: "IX_OcupacaoEspaco_ReservaEspacoId",
                table: "OcupacaoEspaco",
                column: "ReservaEspacoId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_OpcaoVotacao_VotacaoId_Ordem",
                table: "OpcaoVotacao",
                columns: new[] { "VotacaoId", "Ordem" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PagamentoEvento_EventoId_DataPagamento",
                table: "PagamentoEvento",
                columns: new[] { "EventoId", "DataPagamento" });

            migrationBuilder.CreateIndex(
                name: "IX_PagamentoEvento_PessoaPagadoraId",
                table: "PagamentoEvento",
                column: "PessoaPagadoraId");

            migrationBuilder.CreateIndex(
                name: "IX_PagamentoEvento_RegistradoPorUsuarioId",
                table: "PagamentoEvento",
                column: "RegistradoPorUsuarioId");

            migrationBuilder.CreateIndex(
                name: "IX_PagamentoMensalidade_DataPagamento_FormaPagamento",
                table: "PagamentoMensalidade",
                columns: new[] { "DataPagamento", "FormaPagamento" });

            migrationBuilder.CreateIndex(
                name: "IX_PagamentoMensalidade_MensalidadeId",
                table: "PagamentoMensalidade",
                column: "MensalidadeId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PagamentoMensalidade_RegistradoPorUsuarioId",
                table: "PagamentoMensalidade",
                column: "RegistradoPorUsuarioId");

            migrationBuilder.CreateIndex(
                name: "IX_Pessoa_Cpf",
                table: "Pessoa",
                column: "Cpf",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Pessoa_Email",
                table: "Pessoa",
                column: "Email");

            migrationBuilder.CreateIndex(
                name: "IX_Pessoa_NomeCompleto",
                table: "Pessoa",
                column: "NomeCompleto");

            migrationBuilder.CreateIndex(
                name: "IX_PessoaInteresse_InteresseId",
                table: "PessoaInteresse",
                column: "InteresseId");

            migrationBuilder.CreateIndex(
                name: "IX_RegistroAuditoria_Entidade_EntidadeId_CriadaEmUtc",
                table: "RegistroAuditoria",
                columns: new[] { "Entidade", "EntidadeId", "CriadaEmUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_RegistroAuditoria_UnidadeId_CriadaEmUtc",
                table: "RegistroAuditoria",
                columns: new[] { "UnidadeId", "CriadaEmUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_RegistroAuditoria_UsuarioAutorId",
                table: "RegistroAuditoria",
                column: "UsuarioAutorId");

            migrationBuilder.CreateIndex(
                name: "IX_ReservaEspaco_AssociadoId_InicioUtc",
                table: "ReservaEspaco",
                columns: new[] { "AssociadoId", "InicioUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_ReservaEspaco_CanceladaPorUsuarioId",
                table: "ReservaEspaco",
                column: "CanceladaPorUsuarioId");

            migrationBuilder.CreateIndex(
                name: "IX_ReservaEspaco_EspacoId_Status_InicioUtc_FimUtc",
                table: "ReservaEspaco",
                columns: new[] { "EspacoId", "Status", "InicioUtc", "FimUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_RespostaManifestacao_AutorUsuarioId",
                table: "RespostaManifestacao",
                column: "AutorUsuarioId");

            migrationBuilder.CreateIndex(
                name: "IX_RespostaManifestacao_ManifestacaoId_CriadaEmUtc",
                table: "RespostaManifestacao",
                columns: new[] { "ManifestacaoId", "CriadaEmUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_SessaoRenovacao_SubstituidoPorSessaoId",
                table: "SessaoRenovacao",
                column: "SubstituidoPorSessaoId");

            migrationBuilder.CreateIndex(
                name: "IX_SessaoRenovacao_TokenHash",
                table: "SessaoRenovacao",
                column: "TokenHash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SessaoRenovacao_UsuarioId_ExpiraEmUtc",
                table: "SessaoRenovacao",
                columns: new[] { "UsuarioId", "ExpiraEmUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_SolicitacaoCadastro_AvaliadorUsuarioId",
                table: "SolicitacaoCadastro",
                column: "AvaliadorUsuarioId");

            migrationBuilder.CreateIndex(
                name: "IX_SolicitacaoCadastro_CategoriaAprovadaId",
                table: "SolicitacaoCadastro",
                column: "CategoriaAprovadaId");

            migrationBuilder.CreateIndex(
                name: "IX_SolicitacaoCadastro_PendentePessoaId",
                table: "SolicitacaoCadastro",
                column: "PendentePessoaId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SolicitacaoCadastro_PessoaId_SolicitadaEmUtc",
                table: "SolicitacaoCadastro",
                columns: new[] { "PessoaId", "SolicitadaEmUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_SolicitacaoCadastro_UnidadePretendidaId_Status_SolicitadaEmU~",
                table: "SolicitacaoCadastro",
                columns: new[] { "UnidadePretendidaId", "Status", "SolicitadaEmUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_SolicitacaoCadastro_UsuarioId",
                table: "SolicitacaoCadastro",
                column: "UsuarioId");

            migrationBuilder.CreateIndex(
                name: "IX_TransferenciaUnidade_AssociadoId_DataEfetiva",
                table: "TransferenciaUnidade",
                columns: new[] { "AssociadoId", "DataEfetiva" });

            migrationBuilder.CreateIndex(
                name: "IX_TransferenciaUnidade_RealizadaPorUsuarioId",
                table: "TransferenciaUnidade",
                column: "RealizadaPorUsuarioId");

            migrationBuilder.CreateIndex(
                name: "IX_TransferenciaUnidade_UnidadeDestinoId",
                table: "TransferenciaUnidade",
                column: "UnidadeDestinoId");

            migrationBuilder.CreateIndex(
                name: "IX_TransferenciaUnidade_UnidadeOrigemId",
                table: "TransferenciaUnidade",
                column: "UnidadeOrigemId");

            migrationBuilder.CreateIndex(
                name: "IX_Unidade_Ativa",
                table: "Unidade",
                column: "Ativa");

            migrationBuilder.CreateIndex(
                name: "IX_Unidade_Cidade_UF",
                table: "Unidade",
                columns: new[] { "Cidade", "UF" });

            migrationBuilder.CreateIndex(
                name: "IX_Unidade_Sigla",
                table: "Unidade",
                column: "Sigla",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Usuario_AdminAtivoUnidadeId",
                table: "Usuario",
                column: "AdminAtivoUnidadeId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Usuario_PessoaId",
                table: "Usuario",
                column: "PessoaId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Usuario_UnidadeAdministrativaId_RoleAdministrativa_EstadoCon~",
                table: "Usuario",
                columns: new[] { "UnidadeAdministrativaId", "RoleAdministrativa", "EstadoConta" });

            migrationBuilder.CreateIndex(
                name: "IX_ValorMensalidade_CategoriaAssociadoId",
                table: "ValorMensalidade",
                column: "CategoriaAssociadoId");

            migrationBuilder.CreateIndex(
                name: "IX_ValorMensalidade_CriadoPorUsuarioId",
                table: "ValorMensalidade",
                column: "CriadoPorUsuarioId");

            migrationBuilder.CreateIndex(
                name: "IX_ValorMensalidade_UnidadeId_CategoriaAssociadoId_CompetenciaF~",
                table: "ValorMensalidade",
                columns: new[] { "UnidadeId", "CategoriaAssociadoId", "CompetenciaFim" });

            migrationBuilder.CreateIndex(
                name: "IX_ValorMensalidade_UnidadeId_CategoriaAssociadoId_CompetenciaI~",
                table: "ValorMensalidade",
                columns: new[] { "UnidadeId", "CategoriaAssociadoId", "CompetenciaInicio" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Votacao_CriadaPorUsuarioId",
                table: "Votacao",
                column: "CriadaPorUsuarioId");

            migrationBuilder.CreateIndex(
                name: "IX_Votacao_Estado_InicioUtc_FimPrevistoUtc",
                table: "Votacao",
                columns: new[] { "Estado", "InicioUtc", "FimPrevistoUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_Votacao_UnidadeId_Estado",
                table: "Votacao",
                columns: new[] { "UnidadeId", "Estado" });

            migrationBuilder.CreateIndex(
                name: "IX_Voto_EleitorVotacaoId",
                table: "Voto",
                column: "EleitorVotacaoId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Voto_EleitorVotacaoId_VotacaoId",
                table: "Voto",
                columns: new[] { "EleitorVotacaoId", "VotacaoId" });

            migrationBuilder.CreateIndex(
                name: "IX_Voto_OpcaoVotacaoId_VotacaoId",
                table: "Voto",
                columns: new[] { "OpcaoVotacaoId", "VotacaoId" });

            migrationBuilder.CreateIndex(
                name: "IX_Voto_VotacaoId_OpcaoVotacaoId",
                table: "Voto",
                columns: new[] { "VotacaoId", "OpcaoVotacaoId" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ConfiguracaoFinanceira");

            migrationBuilder.DropTable(
                name: "Dependencia");

            migrationBuilder.DropTable(
                name: "EventoUnidade");

            migrationBuilder.DropTable(
                name: "ExecucaoRotina");

            migrationBuilder.DropTable(
                name: "HistoricoCategoriaAssociado");

            migrationBuilder.DropTable(
                name: "Notificacao");

            migrationBuilder.DropTable(
                name: "OcupacaoEspaco");

            migrationBuilder.DropTable(
                name: "PagamentoEvento");

            migrationBuilder.DropTable(
                name: "PagamentoMensalidade");

            migrationBuilder.DropTable(
                name: "PessoaInteresse");

            migrationBuilder.DropTable(
                name: "RegistroAuditoria");

            migrationBuilder.DropTable(
                name: "RespostaManifestacao");

            migrationBuilder.DropTable(
                name: "SessaoRenovacao");

            migrationBuilder.DropTable(
                name: "SolicitacaoCadastro");

            migrationBuilder.DropTable(
                name: "TransferenciaUnidade");

            migrationBuilder.DropTable(
                name: "ValorMensalidade");

            migrationBuilder.DropTable(
                name: "Voto");

            migrationBuilder.DropTable(
                name: "ReservaEspaco");

            migrationBuilder.DropTable(
                name: "Evento");

            migrationBuilder.DropTable(
                name: "Mensalidade");

            migrationBuilder.DropTable(
                name: "Interesse");

            migrationBuilder.DropTable(
                name: "Manifestacao");

            migrationBuilder.DropTable(
                name: "EleitorVotacao");

            migrationBuilder.DropTable(
                name: "OpcaoVotacao");

            migrationBuilder.DropTable(
                name: "Espaco");

            migrationBuilder.DropTable(
                name: "CategoriaManifestacao");

            migrationBuilder.DropTable(
                name: "Associado");

            migrationBuilder.DropTable(
                name: "Votacao");

            migrationBuilder.DropTable(
                name: "CategoriaAssociado");

            migrationBuilder.DropTable(
                name: "Usuario");

            migrationBuilder.DropTable(
                name: "Pessoa");

            migrationBuilder.DropTable(
                name: "Unidade");
        }
    }
}
