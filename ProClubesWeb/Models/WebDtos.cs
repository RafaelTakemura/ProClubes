namespace ProClubesWeb.Models;

public enum RoleAdministrativa : byte
{
    Nenhuma = 0,
    Tesoureiro = 1,
    Admin = 2,
    SuperAdmin = 3
}

public enum EstadoConta : byte
{
    RestritaCadastro = 0,
    Ativa = 1,
    Desativada = 2,
    Bloqueada = 3
}

public enum StatusAssociado : byte
{
    Ativo = 1,
    Inativo = 2
}

public enum TipoVinculoDependente : byte
{
    Conjuge = 1,
    Filho = 2
}

public enum MotivoFimDependencia : byte
{
    Idade22Anos = 1,
    SolicitacaoTitular = 2,
    Administrativo = 3
}

public enum StatusSolicitacaoCadastro : byte
{
    Pendente = 0,
    Aprovada = 1,
    Rejeitada = 2
}

public enum StatusMensalidade : byte
{
    Aberta = 0,
    Paga = 1
}

public enum FormaPagamento : byte
{
    Pix = 1,
    Dinheiro = 2,
    CartaoCredito = 3,
    CartaoDebito = 4,
    Transferencia = 5,
    Boleto = 6
}

public enum PublicoAlvoEvento : byte
{
    Publico = 0,
    Associados = 1
}

public enum StatusEvento : byte
{
    Programado = 0,
    Cancelado = 1,
    Concluido = 2
}

public enum StatusReserva : byte
{
    Confirmada = 0,
    Cancelada = 1
}

public enum OrigemOcupacaoEspaco : byte
{
    Reserva = 1,
    Evento = 2,
    Manutencao = 3
}

public enum TipoManifestacao : byte
{
    Solicitacao = 1,
    Reclamacao = 2,
    Sugestao = 3
}

public enum StatusManifestacao : byte
{
    Aberta = 0,
    EmAtendimento = 1,
    Concluida = 2
}

public enum TipoNotificacao : byte
{
    SolicitacaoCadastroRecebida = 1,
    SolicitacaoCadastroAnalisada = 2,
    ReservaCancelada = 3,
    ManifestacaoAtualizada = 4,
    MensalidadeGerada = 5,
    MensalidadeVencida = 6,
    VotacaoAberta = 7,
    VotacaoResultado = 8,
    TransicaoDependente = 9,
    Sistema = 10
}

public enum EscopoVotacao : byte
{
    Unidade = 1,
    Global = 2
}

public enum EstadoVotacao : byte
{
    Programada = 0,
    Aberta = 1,
    Encerrada = 2,
    Cancelada = 3
}

public record PagedResponse<T>(
    IReadOnlyList<T> Items,
    int TotalCount,
    int Page,
    int PageSize,
    int TotalPages);

public record LoginRequest(string Identificador, string Senha);
public record RefreshRequest(string RefreshToken);

public record LoginResponse(
    string AccessToken,
    string RefreshToken,
    DateTimeOffset ExpiraEm,
    PerfilUsuarioDto Usuario);

public record PerfilUsuarioDto(
    int UsuarioId,
    int PessoaId,
    string NomeCompleto,
    string CpfMascarado,
    string Email,
    string Fone,
    RoleAdministrativa RoleAdministrativa,
    int? UnidadeAdministrativaId,
    string? UnidadeAdministrativaNome,
    EstadoConta EstadoConta,
    int? AssociadoId,
    int? UnidadeAssociadoId,
    string? UnidadeAssociadoNome,
    string? CategoriaCodigo,
    string? CategoriaNome,
    bool EhDependenteAtivo,
    int? TitularAssociadoId,
    string? TitularNome,
    int? UnidadeTitularId,
    string? UnidadeTitularNome,
    IReadOnlyList<string> Interesses);

public record UnidadeDto(
    int Id,
    string Nome,
    string Sigla,
    string Logradouro,
    string Bairro,
    string Numero,
    string Cidade,
    string UF,
    string Fone,
    string Email,
    bool Ativa,
    int? AdminAtivoUsuarioId,
    string? AdminAtivoNome,
    IReadOnlyList<MembroEquipeDto> Tesoureiros);

public record MembroEquipeDto(int UsuarioId, int PessoaId, string NomeCompleto, string Email, RoleAdministrativa Role);

public record CriarOuAtualizarUnidadeRequest(
    string Nome,
    string Sigla,
    string Logradouro,
    string Bairro,
    string Numero,
    string Cidade,
    string UF,
    string Fone,
    string Email,
    bool Ativa = true);

public record DesignarRoleUnidadeRequest(int? UsuarioId, RoleAdministrativa Role);

public record SolicitacaoCadastroListItemDto(
    int Id,
    int PessoaId,
    string NomeCompleto,
    string CpfMascarado,
    string Email,
    string Fone,
    int UnidadePretendidaId,
    string UnidadePretendidaNome,
    StatusSolicitacaoCadastro Status,
    string? JustificativaRejeicao,
    string? CategoriaAprovadaNome,
    DateTimeOffset SolicitadaEm,
    DateTimeOffset? DecididaEm);

public record AprovarSolicitacaoRequest(string CodigoCategoria);
public record RejeitarSolicitacaoRequest(string Justificativa);

public record AssociadoDto(
    int Id,
    int PessoaId,
    int? UsuarioId,
    string NomeCompleto,
    string CpfMascarado,
    string Sexo,
    string EstadoCivil,
    DateOnly DataNascimento,
    string Email,
    string Fone,
    string Cidade,
    string UF,
    int UnidadeId,
    string UnidadeNome,
    int CategoriaAssociadoId,
    string CategoriaCodigo,
    string CategoriaNome,
    StatusAssociado Status,
    DateOnly DataIngresso,
    DateOnly? DataInicioCobranca,
    RoleAdministrativa RoleAdministrativa,
    IReadOnlyList<string> Interesses,
    IReadOnlyList<DependenteDto> Dependentes);

public record CriarAssociadoAdminRequest(
    string NomeCompleto,
    string Cpf,
    string Sexo,
    string EstadoCivil,
    DateOnly DataNascimento,
    string Logradouro,
    string Bairro,
    string Numero,
    string Cidade,
    string UF,
    string Fone,
    string Email,
    int UnidadeId,
    string CodigoCategoria,
    string SenhaInicial,
    IReadOnlyList<int>? InteressesIds = null,
    RoleAdministrativa RoleInicial = RoleAdministrativa.Nenhuma);

public record AlterarCategoriaAssociadoRequest(string NovoCodigoCategoria, string? Motivo = null);
public record TransferirAssociadoUnidadeRequest(int UnidadeDestinoId);
public record AlterarStatusAssociadoRequest(bool Ativo);

public record DependenteDto(
    int DependenciaId,
    int AssociadoTitularId,
    int PessoaDependenteId,
    int? UsuarioDependenteId,
    string NomeCompleto,
    string CpfMascarado,
    string Sexo,
    DateOnly DataNascimento,
    string Email,
    string Fone,
    TipoVinculoDependente TipoVinculo,
    DateOnly DataInicio,
    DateOnly? DataFim,
    MotivoFimDependencia? MotivoFim,
    bool Ativo);

public record CadastrarDependenteRequest(
    string NomeCompleto,
    string Cpf,
    string Sexo,
    string EstadoCivil,
    DateOnly DataNascimento,
    string Logradouro,
    string Bairro,
    string Numero,
    string Cidade,
    string UF,
    string Fone,
    string Email,
    TipoVinculoDependente TipoVinculo,
    string? SenhaContaPropria = null,
    IReadOnlyList<int>? InteressesIds = null);

public record ConfiguracaoFinanceiraDto(
    byte DiaGeracao,
    byte DiaVencimento,
    byte? DiaAvisoVencimento,
    decimal PercentualMulta,
    decimal PercentualJuroMensal,
    DateTimeOffset AtualizadaEm);

public record AtualizarConfiguracaoFinanceiraRequest(
    byte DiaGeracao,
    byte DiaVencimento,
    byte? DiaAvisoVencimento,
    decimal PercentualMulta,
    decimal PercentualJuroMensal);

public record ValorMensalidadeDto(
    int Id,
    int UnidadeId,
    string UnidadeNome,
    int CategoriaAssociadoId,
    string CategoriaCodigo,
    string CategoriaNome,
    decimal Valor,
    DateOnly CompetenciaInicio,
    DateOnly? CompetenciaFim);

public record DefinirValorMensalidadeRequest(
    int UnidadeId,
    string CodigoCategoria,
    decimal Valor,
    DateOnly? CompetenciaInicio = null);

public record MensalidadeDto(
    int Id,
    int AssociadoId,
    string AssociadoNome,
    string CpfMascarado,
    int UnidadeEmissoraId,
    string UnidadeEmissoraNome,
    string CategoriaNaEmissaoNome,
    DateOnly Competencia,
    DateOnly DataGeracao,
    DateOnly DataVencimento,
    decimal ValorPrincipal,
    decimal ValorMultaCalculado,
    decimal ValorJurosCalculado,
    decimal ValorTotalAtualizado,
    StatusMensalidade Status,
    string SituacaoExibicao,
    int DiasAtraso,
    PagamentoMensalidadeDto? Pagamento);

public record PagamentoMensalidadeDto(
    int Id,
    decimal ValorPrincipalPago,
    decimal ValorMultaPago,
    decimal ValorJurosPago,
    decimal ValorTotalPago,
    FormaPagamento FormaPagamento,
    DateOnly DataPagamento,
    DateTimeOffset RegistradoEm);

public record RegistrarPagamentoMensalidadeRequest(DateOnly DataPagamento, FormaPagamento FormaPagamento);

public record EventoDto(
    int Id,
    int UnidadeOrganizadoraId,
    string UnidadeOrganizadoraNome,
    string Nome,
    string? Descricao,
    PublicoAlvoEvento PublicoAlvo,
    DateTimeOffset Inicio,
    DateTimeOffset Fim,
    int? LimiteParticipantes,
    decimal TaxaParticipacao,
    StatusEvento Status,
    IReadOnlyList<int> UnidadesParticipantesIds,
    IReadOnlyList<string> UnidadesParticipantesNomes,
    IReadOnlyList<int> EspacosOcupadosIds,
    IReadOnlyList<string> EspacosOcupadosNomes);

public record CriarEventoRequest(
    int UnidadeOrganizadoraId,
    string Nome,
    string? Descricao,
    PublicoAlvoEvento PublicoAlvo,
    DateTimeOffset Inicio,
    DateTimeOffset Fim,
    int? LimiteParticipantes,
    decimal TaxaParticipacao,
    IReadOnlyList<int>? UnidadesCompartilhadasIds = null,
    IReadOnlyList<int>? EspacosIds = null);

public record RegistrarPagamentoEventoRequest(
    int? PessoaPagadoraId,
    string? NomePagadorInformado,
    decimal ValorPago,
    FormaPagamento FormaPagamento,
    DateOnly DataPagamento);

public record PagamentoEventoDto(
    int Id,
    int EventoId,
    string EventoNome,
    int? PessoaPagadoraId,
    string NomePagadorExibicao,
    decimal ValorPago,
    FormaPagamento FormaPagamento,
    DateOnly DataPagamento,
    DateTimeOffset RegistradoEm);

public record EspacoDto(
    int Id,
    int UnidadeId,
    string UnidadeNome,
    string Nome,
    string? Descricao,
    bool Ativo,
    TimeOnly HoraAbertura,
    TimeOnly HoraFechamento,
    int DuracaoMaximaMinutos,
    int AntecedenciaMaximaDias);

public record CriarOuAtualizarEspacoRequest(
    int UnidadeId,
    string Nome,
    string? Descricao,
    bool Ativo,
    TimeOnly HoraAbertura,
    TimeOnly HoraFechamento,
    int DuracaoMaximaMinutos = 240,
    int AntecedenciaMaximaDias = 90);

public record CriarBloqueioManutencaoRequest(
    int EspacoId,
    DateTimeOffset Inicio,
    DateTimeOffset Fim,
    string DescricaoBloqueio);

public record OcupacaoEspacoDto(
    int Id,
    int EspacoId,
    string EspacoNome,
    DateTimeOffset Inicio,
    DateTimeOffset Fim,
    OrigemOcupacaoEspaco Origem,
    string DescricaoPublica,
    bool Ativa);

public record ReservaEspacoDto(
    int Id,
    int EspacoId,
    string EspacoNome,
    int UnidadeId,
    string UnidadeNome,
    int AssociadoId,
    string AssociadoNome,
    DateTimeOffset Inicio,
    DateTimeOffset Fim,
    StatusReserva Status,
    DateTimeOffset CriadaEm,
    DateTimeOffset? CanceladaEm,
    string? JustificativaCancelamento);

public record CancelarReservaRequest(string? Justificativa = null);

public record CategoriaManifestacaoDto(
    int Id,
    string Nome,
    TipoManifestacao TipoManifestacao,
    string? Descricao,
    bool Ativa);

public record CriarOuAtualizarCategoriaManifestacaoRequest(
    string Nome,
    TipoManifestacao TipoManifestacao,
    string? Descricao,
    bool Ativa = true);

public record ManifestacaoDto(
    int Id,
    int UnidadeId,
    string UnidadeNome,
    int PessoaAutorId,
    string AutorNome,
    int CategoriaManifestacaoId,
    string CategoriaNome,
    TipoManifestacao TipoManifestacao,
    string Titulo,
    string Descricao,
    StatusManifestacao Status,
    DateTimeOffset CriadaEm,
    DateTimeOffset? ConcluidaEm,
    IReadOnlyList<RespostaManifestacaoDto> Respostas);

public record RespostaManifestacaoDto(
    int Id,
    int AutorUsuarioId,
    string AutorNome,
    string Mensagem,
    DateTimeOffset CriadaEm);

public record ResponderManifestacaoRequest(string Mensagem, StatusManifestacao? NovoStatus = null);

public record NotificacaoDto(
    int Id,
    TipoNotificacao Tipo,
    string Titulo,
    string Mensagem,
    string? ReferenciaTipo,
    int? ReferenciaId,
    DateTimeOffset CriadaEm,
    DateTimeOffset? LidaEm,
    bool Lida);

public record VotacaoDto(
    int Id,
    string Titulo,
    string? Descricao,
    EscopoVotacao Escopo,
    int? UnidadeId,
    string? UnidadeNome,
    DateTimeOffset Inicio,
    DateTimeOffset FimPrevisto,
    EstadoVotacao Estado,
    DateTimeOffset? SnapshotConcluidoEm,
    DateTimeOffset? EncerradaEm,
    DateTimeOffset? ResultadosDivulgadosEm,
    string? JustificativaCancelamento,
    int TotalEleitoresAptos,
    int TotalVotosRegistrados,
    bool UsuarioAtualElegivel,
    bool UsuarioAtualJaVotou,
    IReadOnlyList<OpcaoVotacaoDto> Opcoes);

public record OpcaoVotacaoDto(int Id, string Texto, int Ordem);

public record CriarVotacaoRequest(
    string Titulo,
    string? Descricao,
    EscopoVotacao Escopo,
    int? UnidadeId,
    DateTimeOffset Inicio,
    DateTimeOffset FimPrevisto,
    IReadOnlyList<string> Opcoes,
    bool AbrirImediatamente = false);

public record CancelarVotacaoRequest(string Justificativa);

public record ResultadoVotacaoDto(
    int VotacaoId,
    string Titulo,
    EstadoVotacao Estado,
    DateTimeOffset? ResultadosDivulgadosEm,
    int TotalEleitoresSnapshot,
    int TotalVotosComputados,
    IReadOnlyList<ResultadoOpcaoDto> Opcoes);

public record ResultadoOpcaoDto(
    int OpcaoId,
    string Texto,
    int Ordem,
    int QuantidadeVotos,
    decimal Percentual);

public record VotoIndividualAuditoriaDto(
    int VotoId,
    int AssociadoId,
    int PessoaId,
    string EleitorNome,
    string CpfMascarado,
    string UnidadeNaAberturaNome,
    string CategoriaNaAberturaNome,
    int OpcaoVotacaoId,
    string OpcaoEscolhidaTexto,
    DateTimeOffset RegistradoEm);

public record DashboardAdminDto(
    int? UnidadeEscopoId,
    string EscopoNome,
    int TotalUnidadesAtivas,
    int TotalAssociadosAtivos,
    int CadastrosPendentes,
    int MensalidadesEmAbertoMesAtual,
    int MensalidadesVencidas,
    decimal TotalRecebidoMesAtual,
    int EventosFuturos,
    int ReservasFuturasConfirmadas,
    int ManifestacoesPendentes,
    int VotacoesAbertas);
