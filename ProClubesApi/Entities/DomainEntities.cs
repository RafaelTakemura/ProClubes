namespace ProClubesApi.Entities;

// 1. Unidade — Organização
public class Unidade
{
    public int Id { get; set; }
    public string Nome { get; set; } = string.Empty;
    public string Sigla { get; set; } = string.Empty;
    public string Logradouro { get; set; } = string.Empty;
    public string Bairro { get; set; } = string.Empty;
    public string Numero { get; set; } = string.Empty;
    public string Cidade { get; set; } = string.Empty;
    public string UF { get; set; } = string.Empty;
    public string Fone { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public bool Ativa { get; set; } = true;
    public DateTime CriadaEmUtc { get; set; }
}

// 2. Pessoa — Identidade
public class Pessoa
{
    public int Id { get; set; }
    public string NomeCompleto { get; set; } = string.Empty;
    public string Cpf { get; set; } = string.Empty;
    public string Sexo { get; set; } = string.Empty;
    public string EstadoCivil { get; set; } = string.Empty;
    public DateOnly DataNascimento { get; set; }
    public string Logradouro { get; set; } = string.Empty;
    public string Bairro { get; set; } = string.Empty;
    public string Numero { get; set; } = string.Empty;
    public string Cidade { get; set; } = string.Empty;
    public string UF { get; set; } = string.Empty;
    public string Fone { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public DateTime CriadaEmUtc { get; set; }
    public DateTime AtualizadaEmUtc { get; set; }

    public Usuario? Usuario { get; set; }
    public Associado? Associado { get; set; }
    public ICollection<PessoaInteresse> Interesses { get; set; } = new List<PessoaInteresse>();
}

// 3. Interesse — Identidade
public class Interesse
{
    public int Id { get; set; }
    public string Nome { get; set; } = string.Empty;
    public bool Ativo { get; set; } = true;
}

// 4. PessoaInteresse — Identidade
public class PessoaInteresse
{
    public int PessoaId { get; set; }
    public Pessoa Pessoa { get; set; } = null!;
    public int InteresseId { get; set; }
    public Interesse Interesse { get; set; } = null!;
}

// 5. Usuario — Identidade
public class Usuario
{
    public int Id { get; set; }
    public int PessoaId { get; set; }
    public Pessoa Pessoa { get; set; } = null!;
    public string SenhaHash { get; set; } = string.Empty;
    public RoleAdministrativa RoleAdministrativa { get; set; }
    public int? UnidadeAdministrativaId { get; set; }
    public Unidade? UnidadeAdministrativa { get; set; }
    public EstadoConta EstadoConta { get; set; }
    public int VersaoSeguranca { get; set; } = 1;
    public int? AdminAtivoUnidadeId { get; set; }
    public DateTime CriadoEmUtc { get; set; }
    public DateTime? UltimoLoginUtc { get; set; }
}

// 6. SessaoRenovacao — Identidade
public class SessaoRenovacao
{
    public int Id { get; set; }
    public int UsuarioId { get; set; }
    public Usuario Usuario { get; set; } = null!;
    public string TokenHash { get; set; } = string.Empty;
    public DateTime CriadoEmUtc { get; set; }
    public DateTime ExpiraEmUtc { get; set; }
    public DateTime? RevogadoEmUtc { get; set; }
    public int? SubstituidoPorSessaoId { get; set; }
    public SessaoRenovacao? SubstituidoPorSessao { get; set; }
}

// 7. CategoriaAssociado — Associação
public class CategoriaAssociado
{
    public int Id { get; set; }
    public string Codigo { get; set; } = string.Empty;
    public string Nome { get; set; } = string.Empty;
    public bool TemDireitoVoto { get; set; }
    public bool IsentoMensalidade { get; set; }
    public bool Ativa { get; set; } = true;
}

// 8. Associado — Associação
public class Associado
{
    public int Id { get; set; }
    public int PessoaId { get; set; }
    public Pessoa Pessoa { get; set; } = null!;
    public int UnidadeId { get; set; }
    public Unidade Unidade { get; set; } = null!;
    public int CategoriaAssociadoId { get; set; }
    public CategoriaAssociado CategoriaAssociado { get; set; } = null!;
    public StatusAssociado Status { get; set; } = StatusAssociado.Ativo;
    public DateOnly DataIngresso { get; set; }
    public DateOnly? DataInicioCobranca { get; set; }
    public DateTime? DesativadoEmUtc { get; set; }

    public ICollection<Dependencia> Dependentes { get; set; } = new List<Dependencia>();
}

// 9. HistoricoCategoriaAssociado — Associação
public class HistoricoCategoriaAssociado
{
    public int Id { get; set; }
    public int AssociadoId { get; set; }
    public Associado Associado { get; set; } = null!;
    public int CategoriaAnteriorId { get; set; }
    public CategoriaAssociado CategoriaAnterior { get; set; } = null!;
    public int CategoriaNovaId { get; set; }
    public CategoriaAssociado CategoriaNova { get; set; } = null!;
    public DateOnly VigenteApartir { get; set; }
    public int? AlteradoPorUsuarioId { get; set; }
    public Usuario? AlteradoPorUsuario { get; set; }
    public DateTime AlteradoEmUtc { get; set; }
    public string? Motivo { get; set; }
}

// 10. Dependencia — Associação
public class Dependencia
{
    public int Id { get; set; }
    public int AssociadoTitularId { get; set; }
    public Associado AssociadoTitular { get; set; } = null!;
    public int PessoaDependenteId { get; set; }
    public Pessoa PessoaDependente { get; set; } = null!;
    public TipoVinculoDependente TipoVinculo { get; set; }
    public DateOnly DataInicio { get; set; }
    public DateOnly? DataFim { get; set; }
    public MotivoFimDependencia? MotivoFim { get; set; }
    public int? DependenteAtivoPessoaId { get; set; }
}

// 11. TransferenciaUnidade — Associação
public class TransferenciaUnidade
{
    public int Id { get; set; }
    public int AssociadoId { get; set; }
    public Associado Associado { get; set; } = null!;
    public int UnidadeOrigemId { get; set; }
    public Unidade UnidadeOrigem { get; set; } = null!;
    public int UnidadeDestinoId { get; set; }
    public Unidade UnidadeDestino { get; set; } = null!;
    public DateOnly DataEfetiva { get; set; }
    public DateOnly CompetenciaCobrancaDestino { get; set; }
    public int RealizadaPorUsuarioId { get; set; }
    public Usuario RealizadaPorUsuario { get; set; } = null!;
    public DateTime RealizadaEmUtc { get; set; }
}

// 12. SolicitacaoCadastro — Associação
public class SolicitacaoCadastro
{
    public int Id { get; set; }
    public int PessoaId { get; set; }
    public Pessoa Pessoa { get; set; } = null!;
    public int UsuarioId { get; set; }
    public Usuario Usuario { get; set; } = null!;
    public int UnidadePretendidaId { get; set; }
    public Unidade UnidadePretendida { get; set; } = null!;
    public StatusSolicitacaoCadastro Status { get; set; }
    public int? CategoriaAprovadaId { get; set; }
    public CategoriaAssociado? CategoriaAprovada { get; set; }
    public string? JustificativaRejeicao { get; set; }
    public int? AvaliadorUsuarioId { get; set; }
    public Usuario? AvaliadorUsuario { get; set; }
    public DateTime SolicitadaEmUtc { get; set; }
    public DateTime? DecididaEmUtc { get; set; }
    public int? PendentePessoaId { get; set; }
}

// 13. ConfiguracaoFinanceira — Financeiro
public class ConfiguracaoFinanceira
{
    public int Id { get; set; } = 1;
    public byte DiaGeracao { get; set; } = 1;
    public byte DiaVencimento { get; set; } = 10;
    public byte? DiaAvisoVencimento { get; set; } = 7;
    public decimal PercentualMulta { get; set; } = 2.0000m;
    public decimal PercentualJuroMensal { get; set; } = 1.0000m;
    public DateTime AtualizadaEmUtc { get; set; }
    public int AlteradaPorUsuarioId { get; set; }
    public Usuario AlteradaPorUsuario { get; set; } = null!;
}

// 14. ValorMensalidade — Financeiro
public class ValorMensalidade
{
    public int Id { get; set; }
    public int UnidadeId { get; set; }
    public Unidade Unidade { get; set; } = null!;
    public int CategoriaAssociadoId { get; set; }
    public CategoriaAssociado CategoriaAssociado { get; set; } = null!;
    public decimal Valor { get; set; }
    public DateOnly CompetenciaInicio { get; set; }
    public DateOnly? CompetenciaFim { get; set; }
    public int CriadoPorUsuarioId { get; set; }
    public Usuario CriadoPorUsuario { get; set; } = null!;
    public DateTime CriadoEmUtc { get; set; }
}

// 15. Mensalidade — Financeiro
public class Mensalidade
{
    public int Id { get; set; }
    public int AssociadoId { get; set; }
    public Associado Associado { get; set; } = null!;
    public int UnidadeEmissoraId { get; set; }
    public Unidade UnidadeEmissora { get; set; } = null!;
    public int CategoriaNaEmissaoId { get; set; }
    public CategoriaAssociado CategoriaNaEmissao { get; set; } = null!;
    public DateOnly Competencia { get; set; }
    public DateOnly DataGeracao { get; set; }
    public DateOnly DataVencimento { get; set; }
    public decimal ValorPrincipal { get; set; }
    public decimal MultaPercentualEmissao { get; set; }
    public decimal JuroMensalPercentualEmissao { get; set; }
    public StatusMensalidade Status { get; set; }
    public DateTime GeradaEmUtc { get; set; }

    public PagamentoMensalidade? Pagamento { get; set; }
}

// 16. PagamentoMensalidade — Financeiro
public class PagamentoMensalidade
{
    public int Id { get; set; }
    public int MensalidadeId { get; set; }
    public Mensalidade Mensalidade { get; set; } = null!;
    public decimal ValorPrincipalPago { get; set; }
    public decimal ValorMultaPago { get; set; }
    public decimal ValorJurosPago { get; set; }
    public decimal ValorTotalPago { get; set; }
    public FormaPagamento FormaPagamento { get; set; }
    public DateOnly DataPagamento { get; set; }
    public int RegistradoPorUsuarioId { get; set; }
    public Usuario RegistradoPorUsuario { get; set; } = null!;
    public DateTime RegistradoEmUtc { get; set; }
}

// 17. Evento — Eventos
public class Evento
{
    public int Id { get; set; }
    public int UnidadeOrganizadoraId { get; set; }
    public Unidade UnidadeOrganizadora { get; set; } = null!;
    public string Nome { get; set; } = string.Empty;
    public string? Descricao { get; set; }
    public PublicoAlvoEvento PublicoAlvo { get; set; }
    public DateTime DataHoraInicioUtc { get; set; }
    public DateTime DataHoraFimUtc { get; set; }
    public int? LimiteParticipantes { get; set; }
    public decimal TaxaParticipacao { get; set; }
    public StatusEvento Status { get; set; }
    public int CriadoPorUsuarioId { get; set; }
    public Usuario CriadoPorUsuario { get; set; } = null!;
    public DateTime CriadoEmUtc { get; set; }

    public ICollection<EventoUnidade> UnidadesParticipantes { get; set; } = new List<EventoUnidade>();
    public ICollection<OcupacaoEspaco> OcupacoesEspaco { get; set; } = new List<OcupacaoEspaco>();
}

// 18. EventoUnidade — Eventos
public class EventoUnidade
{
    public int EventoId { get; set; }
    public Evento Evento { get; set; } = null!;
    public int UnidadeId { get; set; }
    public Unidade Unidade { get; set; } = null!;
}

// 19. PagamentoEvento — Eventos
public class PagamentoEvento
{
    public int Id { get; set; }
    public int EventoId { get; set; }
    public Evento Evento { get; set; } = null!;
    public int? PessoaPagadoraId { get; set; }
    public Pessoa? PessoaPagadora { get; set; }
    public string? NomePagadorInformado { get; set; }
    public decimal ValorPago { get; set; }
    public FormaPagamento FormaPagamento { get; set; }
    public DateOnly DataPagamento { get; set; }
    public int RegistradoPorUsuarioId { get; set; }
    public Usuario RegistradoPorUsuario { get; set; } = null!;
    public DateTime RegistradoEmUtc { get; set; }
}

// 20. Espaco — Reservas
public class Espaco
{
    public int Id { get; set; }
    public int UnidadeId { get; set; }
    public Unidade Unidade { get; set; } = null!;
    public string Nome { get; set; } = string.Empty;
    public string? Descricao { get; set; }
    public bool Ativo { get; set; } = true;
    public TimeOnly HoraAbertura { get; set; }
    public TimeOnly HoraFechamento { get; set; }
    public int DuracaoMaximaMinutos { get; set; } = 240;
    public int AntecedenciaMaximaDias { get; set; } = 90;
}

// 21. ReservaEspaco — Reservas
public class ReservaEspaco
{
    public int Id { get; set; }
    public int EspacoId { get; set; }
    public Espaco Espaco { get; set; } = null!;
    public int AssociadoId { get; set; }
    public Associado Associado { get; set; } = null!;
    public DateTime InicioUtc { get; set; }
    public DateTime FimUtc { get; set; }
    public StatusReserva Status { get; set; }
    public DateTime CriadaEmUtc { get; set; }
    public DateTime? CanceladaEmUtc { get; set; }
    public int? CanceladaPorUsuarioId { get; set; }
    public Usuario? CanceladaPorUsuario { get; set; }
    public string? JustificativaCancelamento { get; set; }
}

// 22. OcupacaoEspaco — Reservas
public class OcupacaoEspaco
{
    public int Id { get; set; }
    public int EspacoId { get; set; }
    public Espaco Espaco { get; set; } = null!;
    public DateTime InicioUtc { get; set; }
    public DateTime FimUtc { get; set; }
    public OrigemOcupacaoEspaco Origem { get; set; }
    public int? ReservaEspacoId { get; set; }
    public ReservaEspaco? ReservaEspaco { get; set; }
    public int? EventoId { get; set; }
    public Evento? Evento { get; set; }
    public string? DescricaoBloqueio { get; set; }
    public bool Ativa { get; set; } = true;
    public int? CriadaPorUsuarioId { get; set; }
    public Usuario? CriadaPorUsuario { get; set; }
}

// 23. CategoriaManifestacao — Comunicação
public class CategoriaManifestacao
{
    public int Id { get; set; }
    public string Nome { get; set; } = string.Empty;
    public TipoManifestacao TipoManifestacao { get; set; }
    public string? Descricao { get; set; }
    public bool Ativa { get; set; } = true;
}

// 24. Manifestacao — Comunicação
public class Manifestacao
{
    public int Id { get; set; }
    public int UnidadeId { get; set; }
    public Unidade Unidade { get; set; } = null!;
    public int PessoaAutorId { get; set; }
    public Pessoa PessoaAutor { get; set; } = null!;
    public int CategoriaManifestacaoId { get; set; }
    public CategoriaManifestacao CategoriaManifestacao { get; set; } = null!;
    public TipoManifestacao TipoManifestacao { get; set; }
    public string Titulo { get; set; } = string.Empty;
    public string Descricao { get; set; } = string.Empty;
    public StatusManifestacao Status { get; set; }
    public DateTime CriadaEmUtc { get; set; }
    public DateTime? ConcluidaEmUtc { get; set; }

    public ICollection<RespostaManifestacao> Respostas { get; set; } = new List<RespostaManifestacao>();
}

// 25. RespostaManifestacao — Comunicação
public class RespostaManifestacao
{
    public int Id { get; set; }
    public int ManifestacaoId { get; set; }
    public Manifestacao Manifestacao { get; set; } = null!;
    public int AutorUsuarioId { get; set; }
    public Usuario AutorUsuario { get; set; } = null!;
    public string Mensagem { get; set; } = string.Empty;
    public DateTime CriadaEmUtc { get; set; }
}

// 26. Notificacao — Comunicação
public class Notificacao
{
    public int Id { get; set; }
    public int DestinatarioUsuarioId { get; set; }
    public Usuario DestinatarioUsuario { get; set; } = null!;
    public TipoNotificacao Tipo { get; set; }
    public string Titulo { get; set; } = string.Empty;
    public string Mensagem { get; set; } = string.Empty;
    public string? ReferenciaTipo { get; set; }
    public int? ReferenciaId { get; set; }
    public DateTime CriadaEmUtc { get; set; }
    public DateTime? LidaEmUtc { get; set; }
    public string? ChaveIdempotencia { get; set; }
}

// 27. Votacao — Votações
public class Votacao
{
    public int Id { get; set; }
    public string Titulo { get; set; } = string.Empty;
    public string? Descricao { get; set; }
    public EscopoVotacao Escopo { get; set; }
    public int? UnidadeId { get; set; }
    public Unidade? Unidade { get; set; }
    public DateTime InicioUtc { get; set; }
    public DateTime FimPrevistoUtc { get; set; }
    public EstadoVotacao Estado { get; set; }
    public DateTime? SnapshotConcluidoEmUtc { get; set; }
    public DateTime? EncerradaEmUtc { get; set; }
    public DateTime? ResultadosDivulgadosEmUtc { get; set; }
    public string? JustificativaCancelamento { get; set; }
    public int CriadaPorUsuarioId { get; set; }
    public Usuario CriadaPorUsuario { get; set; } = null!;
    public DateTime CriadaEmUtc { get; set; }

    public ICollection<OpcaoVotacao> Opcoes { get; set; } = new List<OpcaoVotacao>();
    public ICollection<EleitorVotacao> Eleitores { get; set; } = new List<EleitorVotacao>();
    public ICollection<Voto> Votos { get; set; } = new List<Voto>();
}

// 28. OpcaoVotacao — Votações
public class OpcaoVotacao
{
    public int Id { get; set; }
    public int VotacaoId { get; set; }
    public Votacao Votacao { get; set; } = null!;
    public string Texto { get; set; } = string.Empty;
    public int Ordem { get; set; }
}

// 29. EleitorVotacao — Votações
public class EleitorVotacao
{
    public int Id { get; set; }
    public int VotacaoId { get; set; }
    public Votacao Votacao { get; set; } = null!;
    public int AssociadoId { get; set; }
    public Associado Associado { get; set; } = null!;
    public int PessoaId { get; set; }
    public Pessoa Pessoa { get; set; } = null!;
    public int UnidadeIdNaAbertura { get; set; }
    public Unidade UnidadeNaAbertura { get; set; } = null!;
    public int CategoriaIdNaAbertura { get; set; }
    public CategoriaAssociado CategoriaNaAbertura { get; set; } = null!;
    public DateTime CriadoEmUtc { get; set; }
}

// 30. Voto — Votações
public class Voto
{
    public int Id { get; set; }
    public int VotacaoId { get; set; }
    public Votacao Votacao { get; set; } = null!;
    public int EleitorVotacaoId { get; set; }
    public EleitorVotacao EleitorVotacao { get; set; } = null!;
    public int OpcaoVotacaoId { get; set; }
    public OpcaoVotacao OpcaoVotacao { get; set; } = null!;
    public DateTime RegistradoEmUtc { get; set; }
}

// 31. RegistroAuditoria — Controle
public class RegistroAuditoria
{
    public long Id { get; set; }
    public int? UsuarioAutorId { get; set; }
    public Usuario? UsuarioAutor { get; set; }
    public int? UnidadeId { get; set; }
    public Unidade? Unidade { get; set; }
    public string TipoOperacao { get; set; } = string.Empty;
    public string Entidade { get; set; } = string.Empty;
    public int? EntidadeId { get; set; }
    public string? DescricaoSegura { get; set; }
    public DateTime CriadaEmUtc { get; set; }
}

// 32. ExecucaoRotina — Controle
public class ExecucaoRotina
{
    public long Id { get; set; }
    public string NomeRotina { get; set; } = string.Empty;
    public string ChaveCompetencia { get; set; } = string.Empty;
    public EstadoExecucaoRotina Estado { get; set; }
    public DateTime IniciadaEmUtc { get; set; }
    public DateTime? ConcluidaEmUtc { get; set; }
    public string? ErroResumo { get; set; }
}
