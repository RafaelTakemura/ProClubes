namespace ProClubesApi.Entities;

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

public enum EstadoExecucaoRotina : byte
{
    EmExecucao = 0,
    Concluida = 1,
    Falhou = 2
}

public static class CodigosCategoriaAssociado
{
    public const string Associado = "ASSOCIADO";
    public const string Fundador = "FUNDADOR";
    public const string Convidado = "CONVIDADO";
    public const string Simpatizante = "SIMPATIZANTE";
}
