using Microsoft.AspNetCore.Components.Forms;

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
    Inativo = 2,
    AguardandoAceiteContrato = 3
}

public enum StatusContrato : byte
{
    PendenteAceite = 1,
    Aceito = 2,
    Recusado = 3,
    Cancelado = 4
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

public enum TipoLocacaoEspaco : byte
{
    PorHora = 1,
    PorDia = 2
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
    IReadOnlyList<string> Interesses,
    string Profissao = "",
    string EstadoCivil = "",
    string Sexo = "",
    DateOnly DataNascimento = default,
    string Logradouro = "",
    string Bairro = "",
    string Numero = "",
    string Cidade = "",
    string UF = "",
    DateTimeOffset? CriadoEm = null,
    DateTimeOffset? AtualizadoEm = null,
    string? UltimaAlteracaoPorNome = null,
    StatusAssociado? StatusAssociado = null,
    bool PossuiContratoPendente = false,
    int? ContratoPendenteId = null,
    string? ContratoPendenteNumero = null);

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

public record EstadoDto(
    int Id,
    string Nome,
    string UF,
    bool Ativo,
    int QuantidadeCidades,
    DateTimeOffset CriadoEm,
    DateTimeOffset AtualizadoEm,
    string? UltimaAlteracaoPorNome = null);

public record CriarOuAtualizarEstadoRequest(
    string Nome,
    string UF,
    bool Ativo = true);

public record CidadeDto(
    int Id,
    string Nome,
    int EstadoId,
    string EstadoNome,
    string EstadoUF,
    bool Ativa,
    DateTimeOffset CriadaEm,
    DateTimeOffset AtualizadaEm,
    string? UltimaAlteracaoPorNome = null);

public record CriarOuAtualizarCidadeRequest(
    string Nome,
    int EstadoId,
    bool Ativa = true);

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
    DateTimeOffset? DecididaEm,
    string Sexo = "",
    string EstadoCivil = "",
    DateOnly DataNascimento = default,
    string Logradouro = "",
    string Bairro = "",
    string Numero = "",
    string Cidade = "",
    string UF = "",
    string Profissao = "",
    DateTimeOffset? CriadoEm = null,
    DateTimeOffset? AtualizadoEm = null,
    string? UltimaAlteracaoPorNome = null);

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
    string Logradouro,
    string Bairro,
    string Numero,
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
    IReadOnlyList<DependenteDto> Dependentes,
    string Profissao = "",
    DateTimeOffset? CriadoEm = null,
    DateTimeOffset? AtualizadoEm = null,
    string? UltimaAlteracaoPorNome = null,
    string? NumeroContratoAtual = null,
    StatusContrato? StatusContratoAtual = null);

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
    RoleAdministrativa RoleInicial = RoleAdministrativa.Nenhuma,
    string Profissao = "Não informada");

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
    bool Ativo,
    bool PendenteAprovacao = false,
    string EstadoCivil = "",
    string Profissao = "",
    string Logradouro = "",
    string Bairro = "",
    string Numero = "",
    string Cidade = "",
    string UF = "",
    DateTimeOffset? CriadoEm = null,
    DateTimeOffset? AtualizadoEm = null,
    string? UltimaAlteracaoPorNome = null);

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
    IReadOnlyList<int>? InteressesIds = null,
    string Profissao = "Não informada");

public record AlterarStatusDependenteRequest(bool Ativo);

public record AtualizarDadosCadastraisRequest(
    string NomeCompleto,
    string Sexo,
    string EstadoCivil,
    DateOnly DataNascimento,
    string Profissao,
    string Fone,
    string Email,
    string Logradouro,
    string Bairro,
    string Numero,
    string Cidade,
    string UF,
    TipoVinculoDependente? TipoVinculo = null);

public record ContratoAssociadoDto(
    int Id,
    string NumeroContrato,
    int AssociadoId,
    int PessoaId,
    string AssociadoNome,
    string AssociadoCpfMascarado,
    string AssociadoEmail,
    int? SolicitacaoCadastroId,
    int UnidadeId,
    string UnidadeNome,
    int CategoriaAssociadoId,
    string CategoriaCodigo,
    string CategoriaNome,
    string Titulo,
    string Conteudo,
    string VersaoTermos,
    decimal ValorMensalidadeReferencia,
    StatusContrato Status,
    DateTimeOffset GeradoEm,
    DateTimeOffset AtualizadoEm,
    DateTimeOffset? RespondidoEm,
    string? JustificativaRecusa,
    string? ObservacoesAdmin,
    string? GeradoPorNome,
    string? UltimaAlteracaoPorNome,
    string? RespondidoPorNome);

public record AtualizarContratoRequest(
    string Titulo,
    string Conteudo,
    string? VersaoTermos = null,
    string? ObservacoesAdmin = null);

public record ResponderContratoRequest(string? Motivo = null);

public record RegerarContratoRequest(string? ObservacoesAdmin = null);

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

public record FotoItemDto(
    int Id,
    string NomeArquivo,
    string ContentType,
    string UrlRelativa,
    int Ordem,
    DateTimeOffset CriadaEm);

public record UploadFotoRequest(
    string NomeArquivo,
    string ContentType,
    string ConteudoBase64);

public record SlotAgendaDto(
    DateTimeOffset Inicio,
    DateTimeOffset Fim,
    string Rotulo,
    bool Disponivel,
    OrigemOcupacaoEspaco? OrigemOcupacao = null,
    string? DescricaoOcupacao = null);

public record AgendaEspacoDto(
    int EspacoId,
    string EspacoNome,
    TipoLocacaoEspaco TipoLocacao,
    decimal ValorReserva,
    TimeOnly HoraAbertura,
    TimeOnly HoraFechamento,
    DateOnly DataInicioConsulta,
    DateOnly DataFimConsulta,
    IReadOnlyList<SlotAgendaDto> Slots,
    IReadOnlyList<OcupacaoEspacoDto> Ocupacoes);

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
    IReadOnlyList<string> EspacosOcupadosNomes,
    decimal ValorReservaEspaco = 0m,
    IReadOnlyList<FotoItemDto>? Fotos = null);

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
    int AntecedenciaMaximaDias,
    TipoLocacaoEspaco TipoLocacao = TipoLocacaoEspaco.PorHora,
    decimal ValorReserva = 0m,
    IReadOnlyList<FotoItemDto>? Fotos = null);

public record CriarOuAtualizarEspacoRequest(
    int UnidadeId,
    string Nome,
    string? Descricao,
    bool Ativo,
    TimeOnly HoraAbertura,
    TimeOnly HoraFechamento,
    int DuracaoMaximaMinutos = 240,
    int AntecedenciaMaximaDias = 90,
    TipoLocacaoEspaco TipoLocacao = TipoLocacaoEspaco.PorHora,
    decimal ValorReserva = 0m);

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
    string? JustificativaCancelamento,
    TipoLocacaoEspaco TipoLocacao = TipoLocacaoEspaco.PorHora,
    decimal ValorTotal = 0m);

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

public static class WebFormatHelpers
{
    public static string FormatarCpfOuCnpj(string? valor)
    {
        var digitos = new string((valor ?? string.Empty).Where(char.IsDigit).Take(14).ToArray());
        if (digitos.Length == 0)
            return string.Empty;

        if (digitos.Length <= 11)
        {
            if (digitos.Length <= 3) return digitos;
            if (digitos.Length <= 6) return $"{digitos[..3]}.{digitos[3..]}";
            if (digitos.Length <= 9) return $"{digitos[..3]}.{digitos[3..6]}.{digitos[6..]}";
            return $"{digitos[..3]}.{digitos[3..6]}.{digitos[6..9]}-{digitos[9..]}";
        }

        if (digitos.Length <= 12) return $"{digitos[..2]}.{digitos[2..5]}.{digitos[5..8]}/{digitos[8..]}";
        return $"{digitos[..2]}.{digitos[2..5]}.{digitos[5..8]}/{digitos[8..12]}-{digitos[12..]}";
    }

    public static string FormatarEndereco(string? logradouro, string? numero, string? bairro, string? cidade, string? uf)
    {
        var partes = new List<string>();
        if (!string.IsNullOrWhiteSpace(logradouro))
        {
            var ruaNum = !string.IsNullOrWhiteSpace(numero) ? $"{logradouro}, {numero}" : logradouro;
            partes.Add(ruaNum);
        }
        else if (!string.IsNullOrWhiteSpace(numero))
        {
            partes.Add($"Nº {numero}");
        }

        if (!string.IsNullOrWhiteSpace(bairro))
            partes.Add(bairro);

        var cidadeUf = (!string.IsNullOrWhiteSpace(cidade), !string.IsNullOrWhiteSpace(uf)) switch
        {
            (true, true) => $"{cidade}/{uf}",
            (true, false) => cidade!,
            (false, true) => uf!,
            _ => string.Empty
        };
        if (!string.IsNullOrWhiteSpace(cidadeUf))
            partes.Add(cidadeUf);

        return partes.Count > 0 ? string.Join(" — ", partes) : "Não informado";
    }

    public static string ObterRotuloStatusAssociado(StatusAssociado status) => status switch
    {
        StatusAssociado.Ativo => "Ativo",
        StatusAssociado.Inativo => "Inativo",
        StatusAssociado.AguardandoAceiteContrato => "Aguardando Aceite de Contrato",
        _ => status.ToString()
    };

    public static string ObterVarianteStatusAssociado(StatusAssociado status) => status switch
    {
        StatusAssociado.Ativo => "ativo",
        StatusAssociado.Inativo => "inativo",
        StatusAssociado.AguardandoAceiteContrato => "pendente",
        _ => "info"
    };

    public static string ObterRotuloStatusContrato(StatusContrato status) => status switch
    {
        StatusContrato.PendenteAceite => "Pendente de Aceite",
        StatusContrato.Aceito => "Aceito",
        StatusContrato.Recusado => "Recusado",
        StatusContrato.Cancelado => "Cancelado",
        _ => status.ToString()
    };

    public static string ObterVarianteStatusContrato(StatusContrato status) => status switch
    {
        StatusContrato.PendenteAceite => "pendente",
        StatusContrato.Aceito => "ativo",
        StatusContrato.Recusado => "danger",
        StatusContrato.Cancelado => "neutral",
        _ => "info"
    };

    public static string ObterRotuloTipoLocacao(TipoLocacaoEspaco tipo) => tipo switch
    {
        TipoLocacaoEspaco.PorDia => "Por Dia",
        _ => "Por Hora"
    };

    public static string ObterSufixoValorLocacao(TipoLocacaoEspaco tipo) => tipo switch
    {
        TipoLocacaoEspaco.PorDia => "/dia",
        _ => "/hora"
    };

    public static string FormatarPeriodoPorTipo(TipoLocacaoEspaco tipo, DateTimeOffset inicio, DateTimeOffset fim)
    {
        if (tipo == TipoLocacaoEspaco.PorDia)
        {
            var dataIni = inicio.Date;
            var dataFim = fim.TimeOfDay == TimeSpan.Zero && fim.Date > dataIni ? fim.Date.AddDays(-1) : fim.Date;
            return dataIni == dataFim
                ? $"{dataIni:dd/MM/yyyy} (Dia inteiro)"
                : $"{dataIni:dd/MM/yyyy} até {dataFim:dd/MM/yyyy}";
        }

        return inicio.Date == fim.Date
            ? $"{inicio:dd/MM/yyyy HH:mm} — {fim:HH:mm}"
            : $"{inicio:dd/MM/yyyy HH:mm} até {fim:dd/MM/yyyy HH:mm}";
    }

    public static async Task<(UploadFotoRequest? Request, string? Error)> LerArquivoFotoAsync(IBrowserFile file)
    {
        const long maxBytes = 5 * 1024 * 1024;
        if (file.Size <= 0 || file.Size > maxBytes)
            return (null, $"A imagem '{file.Name}' deve ter no máximo 5 MB.");

        var contentType = (file.ContentType ?? string.Empty).Trim().ToLowerInvariant();
        if (contentType != "image/jpeg" && contentType != "image/jpg" && contentType != "image/png" && contentType != "image/webp")
            return (null, $"Formato inválido para '{file.Name}'. Utilize JPG, PNG ou WEBP.");

        await using var stream = file.OpenReadStream(maxBytes);
        using var ms = new MemoryStream();
        await stream.CopyToAsync(ms);
        var base64 = Convert.ToBase64String(ms.ToArray());
        return (new UploadFotoRequest(file.Name, contentType, base64), null);
    }
}
