using System.Collections.ObjectModel;
using System.Windows.Input;
using ProClubesApp.Models;
using ProClubesApp.Services;

namespace ProClubesApp.ViewModels;

public class InicioViewModel : BaseViewModel
{
    private readonly MobileSessionAndApiService _api;
    private string _saudacao = "Olá, Associado";
    private string _unidadeTexto = string.Empty;
    private string _categoriaTexto = string.Empty;
    private string _nomeCredencial = "ASSOCIADO PROCLUBES";
    private string _subtituloCredencial = "SÓCIO ASSOCIADO • TITULAR";
    private string _validadeCredencial = "12/2027";
    private string _matriculaCredencial = "MATRÍCULA #00001";
    private string _codigoQrCredencial = "PC-00001";
    private string _reservasBadgeTexto = "0 ativas";
    private string _mensalidadeBadgeTexto = "Em dia";
    private string _eventosBadgeTexto = "0 disp.";
    private bool _exibirResumoFinanceiro;
    private string _situacaoFinanceira = "Em dia";
    private string _proximaMensalidadeResumo = "Nenhuma mensalidade em aberto";
    private bool _temProximoCompromisso;
    private string _compromissoTitulo = "Nenhum compromisso agendado";
    private string _compromissoSubtitulo = "Consulte espaços e eventos disponíveis na sua unidade";
    private string _compromissoDataHora = "Sem reservas ou eventos próximos";
    private string _compromissoRotaDestino = "//Eventos";
    private string _dependentesContagemTexto = "0 ativos";
    private bool _temDependentes;
    private int _notificacoesNaoLidas;

    public ObservableCollection<EventoDto> ProximosEventos { get; } = new();
    public ObservableCollection<ReservaEspacoDto> ReservasFuturas { get; } = new();
    public ObservableCollection<NotificacaoDto> NotificacoesRecentes { get; } = new();
    public ObservableCollection<DependenteMobileDto> DependentesAtivos { get; } = new();

    public string Saudacao { get => _saudacao; set => SetProperty(ref _saudacao, value); }
    public string UnidadeTexto { get => _unidadeTexto; set => SetProperty(ref _unidadeTexto, value); }
    public string CategoriaTexto { get => _categoriaTexto; set => SetProperty(ref _categoriaTexto, value); }
    public string NomeCredencial { get => _nomeCredencial; set => SetProperty(ref _nomeCredencial, value); }
    public string SubtituloCredencial { get => _subtituloCredencial; set => SetProperty(ref _subtituloCredencial, value); }
    public string ValidadeCredencial { get => _validadeCredencial; set => SetProperty(ref _validadeCredencial, value); }
    public string MatriculaCredencial { get => _matriculaCredencial; set => SetProperty(ref _matriculaCredencial, value); }
    public string CodigoQrCredencial { get => _codigoQrCredencial; set => SetProperty(ref _codigoQrCredencial, value); }
    public string ReservasBadgeTexto { get => _reservasBadgeTexto; set => SetProperty(ref _reservasBadgeTexto, value); }
    public string MensalidadeBadgeTexto { get => _mensalidadeBadgeTexto; set => SetProperty(ref _mensalidadeBadgeTexto, value); }
    public string EventosBadgeTexto { get => _eventosBadgeTexto; set => SetProperty(ref _eventosBadgeTexto, value); }
    public bool ExibirResumoFinanceiro { get => _exibirResumoFinanceiro; set => SetProperty(ref _exibirResumoFinanceiro, value); }
    public string SituacaoFinanceira { get => _situacaoFinanceira; set => SetProperty(ref _situacaoFinanceira, value); }
    public string ProximaMensalidadeResumo { get => _proximaMensalidadeResumo; set => SetProperty(ref _proximaMensalidadeResumo, value); }
    public bool TemProximoCompromisso { get => _temProximoCompromisso; set => SetProperty(ref _temProximoCompromisso, value); }
    public string CompromissoTitulo { get => _compromissoTitulo; set => SetProperty(ref _compromissoTitulo, value); }
    public string CompromissoSubtitulo { get => _compromissoSubtitulo; set => SetProperty(ref _compromissoSubtitulo, value); }
    public string CompromissoDataHora { get => _compromissoDataHora; set => SetProperty(ref _compromissoDataHora, value); }
    public string CompromissoRotaDestino { get => _compromissoRotaDestino; set => SetProperty(ref _compromissoRotaDestino, value); }
    public string DependentesContagemTexto { get => _dependentesContagemTexto; set => SetProperty(ref _dependentesContagemTexto, value); }
    public bool TemDependentes { get => _temDependentes; set => SetProperty(ref _temDependentes, value); }
    public bool IsTitularPagante => _api.IsTitularPagante;
    public int NotificacoesNaoLidas { get => _notificacoesNaoLidas; set => SetProperty(ref _notificacoesNaoLidas, value); }

    public ICommand AtualizarCommand { get; }

    public InicioViewModel(MobileSessionAndApiService api)
    {
        _api = api;
        AtualizarCommand = new Command(async () => await CarregarAsync());
    }

    public async Task SairAsync()
    {
        await _api.LogoutAsync();
        if (Shell.Current is not null)
            await Shell.Current.GoToAsync("//Login");
    }

    public async Task CarregarAsync()
    {
        if (IsBusy) return;
        IsBusy = true;
        ErrorMessage = null;

        var (resumo, doCache, err) = await _api.SincronizarOuLerCacheAsync();
        if (resumo is null)
        {
            ErrorMessage = err;
            IsBusy = false;
            return;
        }

        IsOffline = doCache;
        SyncBannerText = doCache
            ? $"Dados de {resumo.SincronizadoEm:dd/MM/yyyy HH:mm} — somente consulta"
            : $"Sincronizado em {resumo.SincronizadoEm:dd/MM/yyyy HH:mm}";

        var matriculaNum = resumo.Perfil.AssociadoId ?? resumo.Perfil.TitularAssociadoId ?? resumo.Perfil.PessoaId;
        var primeiroSegundoNome = ObterNomeSaudacao(resumo.Perfil.NomeCompleto);

        Saudacao = $"Olá, {primeiroSegundoNome}";
        UnidadeTexto = $"{resumo.Perfil.UnidadeContextualNome} • Matrícula #{matriculaNum:D5}";
        CategoriaTexto = resumo.Perfil.EhDependenteAtivo ? "Dependente" : (resumo.Perfil.CategoriaNome ?? "Associado");

        NomeCredencial = resumo.Perfil.NomeCompleto.ToUpperInvariant();
        var vinculoCred = resumo.Perfil.EhDependenteAtivo ? "DEPENDENTE" : "TITULAR";
        var catCred = (resumo.Perfil.CategoriaNome ?? "ASSOCIADO").ToUpperInvariant();
        SubtituloCredencial = $"{catCred} • {vinculoCred}";
        ValidadeCredencial = $"12/{DateTime.Today.Year + 1}";
        MatriculaCredencial = $"MATRÍCULA #{matriculaNum:D5}";
        CodigoQrCredencial = $"PC-{matriculaNum:D5}-{resumo.Perfil.PessoaId:D4}";

        NotificacoesNaoLidas = resumo.NotificacoesNaoLidas;

        ExibirResumoFinanceiro = _api.IsTitularPagante;
        if (ExibirResumoFinanceiro)
        {
            if (resumo.QuantidadeMensalidadesVencidas > 0)
            {
                SituacaoFinanceira = $"Em atraso ({resumo.QuantidadeMensalidadesVencidas} vencida(s))";
                MensalidadeBadgeTexto = $"{resumo.QuantidadeMensalidadesVencidas} vencida(s)";
            }
            else if (resumo.ProximaMensalidade is not null)
            {
                SituacaoFinanceira = "Em dia (1 em aberto)";
                MensalidadeBadgeTexto = "Em dia";
            }
            else
            {
                SituacaoFinanceira = "Em dia";
                MensalidadeBadgeTexto = "Em dia";
            }
            ProximaMensalidadeResumo = resumo.ProximaMensalidade?.ResumoFormatado ?? "Nenhuma mensalidade pendente";
        }
        else
        {
            MensalidadeBadgeTexto = "Isento";
        }

        ProximosEventos.Clear();
        foreach (var ev in resumo.ProximosEventos) ProximosEventos.Add(ev);

        ReservasFuturas.Clear();
        foreach (var r in resumo.ReservasFuturas) ReservasFuturas.Add(r);

        ReservasBadgeTexto = ReservasFuturas.Count == 1 ? "1 ativa" : $"{ReservasFuturas.Count} ativas";
        EventosBadgeTexto = $"{ProximosEventos.Count} disp.";

        if (ReservasFuturas.Count > 0)
        {
            var r = ReservasFuturas[0];
            TemProximoCompromisso = true;
            CompromissoTitulo = r.EspacoNome;
            CompromissoSubtitulo = $"Reserva Confirmada • {r.UnidadeNome}";
            CompromissoDataHora = $"{r.Inicio:dd/MM/yyyy} • {r.Inicio:HH:mm} às {r.Fim:HH:mm}";
            CompromissoRotaDestino = "//Reservas";
        }
        else if (ProximosEventos.Count > 0)
        {
            var ev = ProximosEventos[0];
            TemProximoCompromisso = true;
            CompromissoTitulo = ev.Nome;
            CompromissoSubtitulo = $"Evento do Clube • {ev.UnidadeOrganizadoraNome}";
            CompromissoDataHora = $"{ev.Inicio:dd/MM/yyyy • HH:mm} ({(ev.TaxaParticipacao > 0 ? $"R$ {ev.TaxaParticipacao:N2}" : "Gratuito")})";
            CompromissoRotaDestino = "//Eventos";
        }
        else
        {
            TemProximoCompromisso = false;
            CompromissoTitulo = "Nenhum compromisso agendado";
            CompromissoSubtitulo = $"Unidade {resumo.Perfil.UnidadeContextualNome}";
            CompromissoDataHora = "Explore a agenda de espaços e eventos do clube";
            CompromissoRotaDestino = "//Eventos";
        }

        DependentesAtivos.Clear();
        if (resumo.Perfil.AssociadoId.HasValue && !doCache)
        {
            var (assocDetalhe, _) = await _api.GetAsync<AssociadoDetalheMobileDto>($"api/v1/admin/associados/{resumo.Perfil.AssociadoId.Value}");
            if (assocDetalhe?.Dependentes is not null)
            {
                foreach (var d in assocDetalhe.Dependentes.Where(x => x.Ativo))
                    DependentesAtivos.Add(d);
            }
        }
        TemDependentes = DependentesAtivos.Count > 0;
        DependentesContagemTexto = DependentesAtivos.Count == 1 ? "1 ativo" : $"{DependentesAtivos.Count} ativos";

        NotificacoesRecentes.Clear();
        foreach (var n in resumo.NotificacoesRecentes) NotificacoesRecentes.Add(n);

        IsBusy = false;
    }

    private static string ObterNomeSaudacao(string nomeCompleto)
    {
        if (string.IsNullOrWhiteSpace(nomeCompleto)) return "Associado";
        var partes = nomeCompleto.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (partes.Length <= 2) return nomeCompleto;
        return $"{partes[0]} {partes[1]}";
    }
}
