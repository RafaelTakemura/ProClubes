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
    string? StatusAssociado = null,
    bool PossuiContratoPendente = false,
    int? ContratoPendenteId = null,
    string? ContratoPendenteNumero = null)
{
    public bool AguardandoAceiteContrato =>
        PossuiContratoPendente || string.Equals(StatusAssociado, "AguardandoAceiteContrato", StringComparison.OrdinalIgnoreCase);

    public string UnidadeContextualNome =>
        UnidadeAssociadoNome ?? UnidadeTitularNome ?? UnidadeAdministrativaNome ?? "Sem unidade";

    public string CategoriaContextualNome =>
        AguardandoAceiteContrato
            ? "Aguardando Aceite de Contrato"
            : (EhDependenteAtivo ? $"Dependente (Titular: {TitularNome})" : (CategoriaNome ?? "Visitante"));

    public string AuditoriaResumo
    {
        get
        {
            var cadastro = CriadoEm.HasValue ? CriadoEm.Value.ToString("dd/MM/yyyy HH:mm") : "Não informado";
            var alterado = AtualizadoEm.HasValue ? AtualizadoEm.Value.ToString("dd/MM/yyyy HH:mm") : "Sem alterações";
            var usuario = string.IsNullOrWhiteSpace(UltimaAlteracaoPorNome) ? "Cadastro inicial" : UltimaAlteracaoPorNome;
            return $"Cadastro: {cadastro} • Última alteração: {alterado} • Por: {usuario}";
        }
    }
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
    string Senha,
    IReadOnlyList<int>? InteressesIds = null,
    string? CodigoCategoriaPretendida = null,
    string Profissao = "Não informada");

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
    int UnidadePretendidaId,
    IReadOnlyList<int>? InteressesIds = null,
    string? CodigoCategoriaPretendida = null,
    string Profissao = "Não informada");

public record EstadoMobileDto(int Id, string Nome, string UF, bool Ativo = true)
{
    public string Exibicao => $"{Nome} ({UF})";
}

public record CidadeMobileDto(int Id, string Nome, int EstadoId, string EstadoNome, string EstadoUF, bool Ativa = true)
{
    public string Exibicao => $"{Nome} ({EstadoUF})";
}

public static class LocalidadesMobileHelper
{
    public static IReadOnlyList<EstadoMobileDto> FiltrarEstadosPorCidade(
        IReadOnlyList<EstadoMobileDto> todosEstados,
        IReadOnlyList<CidadeMobileDto> todasCidades,
        string? cidadeNome)
    {
        if (string.IsNullOrWhiteSpace(cidadeNome))
            return todosEstados;

        var ufs = todasCidades
            .Where(c => string.Equals(c.Nome, cidadeNome.Trim(), StringComparison.OrdinalIgnoreCase))
            .Select(c => c.EstadoUF)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        if (ufs.Count == 0)
            return todosEstados;

        return todosEstados.Where(e => ufs.Contains(e.UF)).ToList();
    }

    public static IReadOnlyList<CidadeMobileDto> FiltrarCidadesPorEstado(
        IReadOnlyList<CidadeMobileDto> todasCidades,
        string? uf)
    {
        if (string.IsNullOrWhiteSpace(uf))
            return todasCidades;

        return todasCidades
            .Where(c => string.Equals(c.EstadoUF, uf.Trim(), StringComparison.OrdinalIgnoreCase))
            .ToList();
    }
}

public record AtualizarDadosCadastraisMobileRequest(
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
    string? TipoVinculo = null);

public record ContratoAssociadoMobileDto(
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
    string Status,
    DateTimeOffset GeradoEm,
    DateTimeOffset AtualizadoEm,
    DateTimeOffset? RespondidoEm = null,
    string? JustificativaRecusa = null,
    string? ObservacoesAdmin = null,
    string? GeradoPorNome = null,
    string? UltimaAlteracaoPorNome = null,
    string? RespondidoPorNome = null)
{
    public bool EstaPendente => string.Equals(Status, "PendenteAceite", StringComparison.OrdinalIgnoreCase);
    public string ValorFormatado => ValorMensalidadeReferencia > 0 ? $"R$ {ValorMensalidadeReferencia:N2}/mês" : "Isento de mensalidade";
}

public record ResponderContratoMobileRequest(string? Motivo = null);

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
    public string CompetenciaTitulo => $"Competência {Competencia:MM/yyyy}";
    public string VencimentoEUnidade => $"Vencimento: {DataVencimento:dd/MM/yyyy} • {UnidadeEmissoraNome}";
    public string TotalAtualizadoTexto => $"Total atualizado: R$ {ValorTotalAtualizado:N2}";

    public Microsoft.Maui.Graphics.Color SituacaoCorTexto => SituacaoExibicao switch
    {
        "Paga" => Microsoft.Maui.Graphics.Color.FromArgb("#047857"),
        "Vencida" => Microsoft.Maui.Graphics.Color.FromArgb("#B91C1C"),
        _ => Microsoft.Maui.Graphics.Color.FromArgb("#B45309")
    };

    public Microsoft.Maui.Graphics.Color SituacaoCorFundo => SituacaoExibicao switch
    {
        "Paga" => Microsoft.Maui.Graphics.Color.FromArgb("#ECFDF5"),
        "Vencida" => Microsoft.Maui.Graphics.Color.FromArgb("#FEF2F2"),
        _ => Microsoft.Maui.Graphics.Color.FromArgb("#FFFBEB")
    };

    public Microsoft.Maui.Graphics.Color SituacaoCorBorda => SituacaoExibicao switch
    {
        "Paga" => Microsoft.Maui.Graphics.Color.FromArgb("#A7F3D0"),
        "Vencida" => Microsoft.Maui.Graphics.Color.FromArgb("#FECACA"),
        _ => Microsoft.Maui.Graphics.Color.FromArgb("#FDE68A")
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

public record DependenteMobileDto(
    int DependenciaId,
    int AssociadoTitularId,
    int PessoaDependenteId,
    string NomeCompleto,
    string CpfMascarado,
    string TipoVinculo,
    bool Ativo,
    DateOnly DataNascimento = default,
    string Email = "",
    string Fone = "",
    bool PendenteAprovacao = false,
    string Sexo = "",
    string EstadoCivil = "",
    string Profissao = "",
    string Logradouro = "",
    string Bairro = "",
    string Numero = "",
    string Cidade = "",
    string UF = "",
    DateTimeOffset? CriadoEm = null,
    DateTimeOffset? AtualizadoEm = null,
    string? UltimaAlteracaoPorNome = null)
{
    public string Iniciais
    {
        get
        {
            if (string.IsNullOrWhiteSpace(NomeCompleto)) return "DP";
            var partes = NomeCompleto.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (partes.Length == 1) return partes[0][..Math.Min(2, partes[0].Length)].ToUpperInvariant();
            return $"{char.ToUpperInvariant(partes[0][0])}{char.ToUpperInvariant(partes[^1][0])}";
        }
    }

    public string VinculoFormatado => TipoVinculo == "Conjuge" ? "Cônjuge" : "Filho(a)";

    public string ProfissaoFormatada =>
        string.IsNullOrWhiteSpace(Profissao) ? "Profissão não informada" : Profissao;

    public string SubtituloFormatado =>
        DataNascimento != default
            ? $"{VinculoFormatado} • {CpfMascarado} • Nasc.: {DataNascimento:dd/MM/yyyy}"
            : $"{VinculoFormatado} • {CpfMascarado}";

    public string AuditoriaResumo
    {
        get
        {
            var cadastro = CriadoEm.HasValue ? CriadoEm.Value.ToString("dd/MM/yyyy HH:mm") : "Não informado";
            var alterado = AtualizadoEm.HasValue ? AtualizadoEm.Value.ToString("dd/MM/yyyy HH:mm") : "Sem alterações";
            var usuario = string.IsNullOrWhiteSpace(UltimaAlteracaoPorNome) ? "Cadastro inicial" : UltimaAlteracaoPorNome;
            return $"Cadastro: {cadastro} • Última alt.: {alterado} • Por: {usuario}";
        }
    }

    public string StatusTexto =>
        PendenteAprovacao ? "Pendente de Aprovação" : (Ativo ? "Ativo" : "Encerrado");

    public Microsoft.Maui.Graphics.Color StatusCorTexto =>
        PendenteAprovacao
            ? Microsoft.Maui.Graphics.Color.FromArgb("#B45309")
            : (Ativo ? Microsoft.Maui.Graphics.Color.FromArgb("#047857") : Microsoft.Maui.Graphics.Color.FromArgb("#475569"));

    public Microsoft.Maui.Graphics.Color StatusCorFundo =>
        PendenteAprovacao
            ? Microsoft.Maui.Graphics.Color.FromArgb("#FFFBEB")
            : (Ativo ? Microsoft.Maui.Graphics.Color.FromArgb("#ECFDF5") : Microsoft.Maui.Graphics.Color.FromArgb("#F1F5F9"));

    public Microsoft.Maui.Graphics.Color StatusCorBorda =>
        PendenteAprovacao
            ? Microsoft.Maui.Graphics.Color.FromArgb("#FDE68A")
            : (Ativo ? Microsoft.Maui.Graphics.Color.FromArgb("#A7F3D0") : Microsoft.Maui.Graphics.Color.FromArgb("#CBD5E1"));

    public bool PodeAlterarStatus => !PendenteAprovacao;

    public string AcaoStatusTexto => Ativo ? "Desativar Vínculo" : "Reativar Vínculo";

    public Microsoft.Maui.Graphics.Color AcaoStatusCorTexto =>
        Ativo ? Microsoft.Maui.Graphics.Color.FromArgb("#B91C1C") : Microsoft.Maui.Graphics.Color.FromArgb("#0B192C");

    public Microsoft.Maui.Graphics.Color AcaoStatusCorFundo =>
        Ativo ? Microsoft.Maui.Graphics.Color.FromArgb("#FEF2F2") : Microsoft.Maui.Graphics.Color.FromArgb("#F1F5F9");
}

public record CadastrarDependenteMobileRequest(
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
    string TipoVinculo,
    string? SenhaContaPropria = null,
    IReadOnlyList<int>? InteressesIds = null,
    string Profissao = "Não informada");

public record AlterarStatusDependenteMobileRequest(bool Ativo);

public record AssociadoDetalheMobileDto(
    int Id,
    string NomeCompleto,
    IReadOnlyList<DependenteMobileDto> Dependentes);
