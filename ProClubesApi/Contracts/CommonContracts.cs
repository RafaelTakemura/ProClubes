namespace ProClubesApi.Contracts;

public static class CodigosErro
{
    public const string CredenciaisInvalidas = "CREDENCIAIS_INVALIDAS";
    public const string SessaoInvalidaOuRevogada = "SESSAO_INVALIDA_OU_REVOGADA";
    public const string ContaBloqueadaOuDesativada = "CONTA_BLOQUEADA_OU_DESATIVADA";
    public const string CadastroPendente = "CADASTRO_PENDENTE";
    public const string CadastroRejeitado = "CADASTRO_REJEITADO";
    public const string UnidadeSemPermissao = "UNIDADE_SEM_PERMISSAO";
    public const string RecursoSemPermissao = "RECURSO_SEM_PERMISSAO";
    public const string RecursoNaoEncontrado = "RECURSO_NAO_ENCONTRADO";
    public const string DadosInvalidos = "DADOS_INVALIDOS";
    public const string CpfJaCadastrado = "CPF_JA_CADASTRADO";
    public const string CpfInvalido = "CPF_INVALIDO";
    public const string SolicitacaoJaPendente = "SOLICITACAO_JA_PENDENTE";
    public const string UnidadeJaPossuiAdminAtivo = "UNIDADE_JA_POSSUI_ADMIN_ATIVO";
    public const string SiglaUnidadeDuplicada = "SIGLA_UNIDADE_DUPLICADA";
    public const string TransferenciaMesmaUnidade = "TRANSFERENCIA_MESMA_UNIDADE";
    public const string DependenteIdadeInvalida = "DEPENDENTE_IDADE_INVALIDA";
    public const string DependenteJaVinculado = "DEPENDENTE_JA_VINCULADO";
    public const string VigenciaValorSobreposta = "VIGENCIA_VALOR_SOBREPOSTA";
    public const string MensalidadeJaPaga = "MENSALIDADE_JA_PAGA";
    public const string DataPagamentoFutura = "DATA_PAGAMENTO_FUTURA";
    public const string ReservaHorarioOcupado = "RESERVA_HORARIO_OCUPADO";
    public const string ReservaForaRegrasEspaco = "RESERVA_FORA_REGRAS_ESPACO";
    public const string ReservaCancelamentoForaPrazo = "RESERVA_CANCELAMENTO_FORA_PRAZO";
    public const string CategoriaManifestacaoInativaOuIncompativel = "CATEGORIA_MANIFESTACAO_INVALIDA";
    public const string VotacaoNaoElegivel = "VOTACAO_NAO_ELEGIVEL";
    public const string VotacaoForaPeriodo = "VOTACAO_FORA_PERIODO";
    public const string VotoJaRegistrado = "VOTO_JA_REGISTRADO";
    public const string VotosIndividuaisAcessoNegado = "VOTOS_INDIVIDUAIS_ACESSO_NEGADO";
    public const string ConflitoConcorrencia = "CONFLITO_CONCORRENCIA";
}

public class ApiException : Exception
{
    public int StatusCode { get; }
    public string ErrorCode { get; }
    public string Title { get; }

    public ApiException(int statusCode, string errorCode, string message, string? title = null)
        : base(message)
    {
        StatusCode = statusCode;
        ErrorCode = errorCode;
        Title = title ?? ErrorCode;
    }
}

public record PaginationQuery(int Page = 1, int PageSize = 20)
{
    public int NormalizedPage => Page < 1 ? 1 : Page;
    public int NormalizedPageSize => PageSize < 1 ? 20 : (PageSize > 100 ? 100 : PageSize);
    public int Skip => (NormalizedPage - 1) * NormalizedPageSize;
}

public record PagedResponse<T>(
    IReadOnlyList<T> Items,
    int TotalCount,
    int Page,
    int PageSize)
{
    public int TotalPages => PageSize <= 0 ? 0 : (int)Math.Ceiling((double)TotalCount / PageSize);
}
