namespace ProClubesApp.Models;

public record LoginRequest(string Identificador, string Senha);

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
    string RoleAdministrativa,
    int? UnidadeAdministrativaId,
    string? UnidadeAdministrativaNome,
    string EstadoConta,
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
    IReadOnlyList<string> Interesses)
{
    public string UnidadeContextualNome =>
        UnidadeAssociadoNome ?? UnidadeTitularNome ?? UnidadeAdministrativaNome ?? "Sem unidade";

    public string CategoriaContextualNome =>
        EhDependenteAtivo ? $"Dependente (Titular: {TitularNome})" : (CategoriaNome ?? "Visitante");
}

public record StatusCadastroResponse(
    int SolicitacaoId,
    int PessoaId,
    string NomeCompleto,
    string Email,
    int UnidadePretendidaId,
    string UnidadePretendidaNome,
    string Status,
    string? JustificativaRejeicao,
    string? CategoriaAprovadaNome,
    DateTimeOffset SolicitadaEm,
    DateTimeOffset? DecididaEm);

public record SolicitarCadastroRequest(
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
    int UnidadePretendidaId,
    string Senha);

public record ReenviarSolicitacaoCadastroRequest(
    string NomeCompleto,
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
    int UnidadePretendidaId);

public record UnidadePublicaDto(
    int Id,
    string Nome,
    string Sigla,
    string Cidade,
    string UF);

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
    string Status,
    string SituacaoExibicao,
    int DiasAtraso)
{
    public string ResumoFormatado => $"Competência {Competencia:MM/yyyy} • Venc.: {DataVencimento:dd/MM/yyyy} • R$ {ValorTotalAtualizado:N2} ({SituacaoExibicao})";

    public Microsoft.Maui.Graphics.Color SituacaoCorTexto => SituacaoExibicao switch
    {
        "Paga" => Microsoft.Maui.Graphics.Color.FromArgb("#15803D"),
        "Vencida" => Microsoft.Maui.Graphics.Color.FromArgb("#DC2626"),
        _ => Microsoft.Maui.Graphics.Color.FromArgb("#D97706")
    };
}

public record EventoDto(
    int Id,
    int UnidadeOrganizadoraId,
    string UnidadeOrganizadoraNome,
    string Nome,
    string? Descricao,
    string PublicoAlvo,
    DateTimeOffset Inicio,
    DateTimeOffset Fim,
    int? LimiteParticipantes,
    decimal TaxaParticipacao,
    string Status)
{
    public string ResumoDataTaxa =>
        $"{UnidadeOrganizadoraNome} • {Inicio:dd/MM/yyyy HH:mm} • {(TaxaParticipacao > 0 ? $"Taxa: R$ {TaxaParticipacao:N2}" : "Gratuito")} • {(PublicoAlvo == "Publico" ? "Aberto ao público" : "Somente associados")}";
}

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
    string Status,
    DateTimeOffset CriadaEm,
    DateTimeOffset? CanceladaEm,
    string? JustificativaCancelamento)
{
    public bool PodeCancelar => string.Equals(Status, "Confirmada", StringComparison.OrdinalIgnoreCase);
    public bool EstaCancelada => string.Equals(Status, "Cancelada", StringComparison.OrdinalIgnoreCase);
    public bool TemJustificativa => EstaCancelada && !string.IsNullOrWhiteSpace(JustificativaCancelamento);
    public string PeriodoFormatado => $"{Inicio:dd/MM/yyyy HH:mm} às {Fim:HH:mm}";
    public string StatusBadgeTexto => EstaCancelada ? "✖ RESERVA CANCELADA" : "✔ RESERVA CONFIRMADA";
    public string MotivoCancelamentoFormatado =>
        CanceladaEm.HasValue
            ? $"Cancelada em {CanceladaEm.Value:dd/MM/yyyy HH:mm} • Motivo: {JustificativaCancelamento}"
            : $"Motivo do cancelamento: {JustificativaCancelamento}";
}

public record CriarReservaRequest(int EspacoId, DateTimeOffset Inicio, DateTimeOffset Fim);

public record CategoriaManifestacaoDto(int Id, string Nome, string TipoManifestacao, string? Descricao, bool Ativa);

public record RespostaManifestacaoDto(
    int Id,
    int AutorUsuarioId,
    string AutorNome,
    string Mensagem,
    DateTimeOffset CriadaEm)
{
    public string CabecalhoFormatado => $"{AutorNome} • {CriadaEm:dd/MM/yyyy HH:mm}";
}

public record ManifestacaoDto(
    int Id,
    int UnidadeId,
    string UnidadeNome,
    string CategoriaNome,
    string TipoManifestacao,
    string Titulo,
    string Descricao,
    string Status,
    DateTimeOffset CriadaEm,
    DateTimeOffset? ConcluidaEm,
    IReadOnlyList<RespostaManifestacaoDto>? Respostas)
{
    public IReadOnlyList<RespostaManifestacaoDto> ListaRespostas => Respostas ?? Array.Empty<RespostaManifestacaoDto>();
    public int QuantidadeRespostas => ListaRespostas.Count;
    public string ResumoMeta => $"{CategoriaNome} ({TipoManifestacao}) • {CriadaEm:dd/MM/yyyy HH:mm}";
    public string StatusBadge => $"Status: {Status} • {QuantidadeRespostas} resposta(s)";
}

public record CriarManifestacaoRequest(
    int CategoriaManifestacaoId,
    string TipoManifestacao,
    string Titulo,
    string Descricao);

public record VotacaoDto(
    int Id,
    string Titulo,
    string? Descricao,
    string Escopo,
    string? UnidadeNome,
    DateTimeOffset Inicio,
    DateTimeOffset FimPrevisto,
    string Estado,
    int TotalEleitoresAptos,
    int TotalVotosRegistrados,
    bool UsuarioAtualElegivel,
    bool UsuarioAtualJaVotou,
    IReadOnlyList<OpcaoVotacaoDto> Opcoes);

public record OpcaoVotacaoDto(int Id, string Texto, int Ordem);
public record RegistrarVotoRequest(int OpcaoVotacaoId);

public record ResultadoVotacaoDto(
    int VotacaoId,
    string Titulo,
    string Estado,
    int TotalEleitoresSnapshot,
    int TotalVotosComputados,
    IReadOnlyList<ResultadoOpcaoDto> Opcoes);

public record ResultadoOpcaoDto(int OpcaoId, string Texto, int Ordem, int QuantidadeVotos, decimal Percentual);

public record NotificacaoDto(
    int Id,
    string Tipo,
    string Titulo,
    string Mensagem,
    DateTimeOffset CriadaEm,
    bool Lida);

public record PagedResponse<T>(IReadOnlyList<T> Items, int TotalCount, int Page, int PageSize);

public record ResumoSincronizacaoAppDto(
    DateTimeOffset SincronizadoEm,
    PerfilUsuarioDto Perfil,
    int QuantidadeMensalidadesVencidas,
    MensalidadeDto? ProximaMensalidade,
    IReadOnlyList<EventoDto> ProximosEventos,
    IReadOnlyList<ReservaEspacoDto> ReservasFuturas,
    IReadOnlyList<NotificacaoDto> NotificacoesRecentes,
    int NotificacoesNaoLidas);
