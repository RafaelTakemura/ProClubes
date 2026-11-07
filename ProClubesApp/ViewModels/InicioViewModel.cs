using System.Collections.ObjectModel;
using System.Windows.Input;
using ProClubesApp.Models;
using ProClubesApp.Services;

namespace ProClubesApp.ViewModels;

public class InicioViewModel : BaseViewModel
{
    private readonly MobileSessionAndApiService _api;
    private string _saudacao = "Bem-vindo(a) ao ProClubes";
    private string _unidadeTexto = string.Empty;
    private string _categoriaTexto = string.Empty;
    private bool _exibirResumoFinanceiro;
    private string _situacaoFinanceira = "Em dia";
    private string _proximaMensalidadeResumo = "Nenhuma mensalidade em aberto";
    private int _notificacoesNaoLidas;

    public ObservableCollection<EventoDto> ProximosEventos { get; } = new();
    public ObservableCollection<ReservaEspacoDto> ReservasFuturas { get; } = new();
    public ObservableCollection<NotificacaoDto> NotificacoesRecentes { get; } = new();

    public string Saudacao { get => _saudacao; set => SetProperty(ref _saudacao, value); }
    public string UnidadeTexto { get => _unidadeTexto; set => SetProperty(ref _unidadeTexto, value); }
    public string CategoriaTexto { get => _categoriaTexto; set => SetProperty(ref _categoriaTexto, value); }
    public bool ExibirResumoFinanceiro { get => _exibirResumoFinanceiro; set => SetProperty(ref _exibirResumoFinanceiro, value); }
    public string SituacaoFinanceira { get => _situacaoFinanceira; set => SetProperty(ref _situacaoFinanceira, value); }
    public string ProximaMensalidadeResumo { get => _proximaMensalidadeResumo; set => SetProperty(ref _proximaMensalidadeResumo, value); }
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

        Saudacao = $"Olá, {resumo.Perfil.NomeCompleto}";
        UnidadeTexto = $"Unidade: {resumo.Perfil.UnidadeContextualNome}";
        CategoriaTexto = resumo.Perfil.CategoriaContextualNome;
        NotificacoesNaoLidas = resumo.NotificacoesNaoLidas;

        ExibirResumoFinanceiro = _api.IsTitularPagante;
        if (ExibirResumoFinanceiro)
        {
            SituacaoFinanceira = resumo.QuantidadeMensalidadesVencidas > 0
                ? $"Em atraso ({resumo.QuantidadeMensalidadesVencidas} vencida(s))"
                : "Em dia";
            ProximaMensalidadeResumo = resumo.ProximaMensalidade?.ResumoFormatado ?? "Nenhuma mensalidade pendente";
        }

        ProximosEventos.Clear();
        foreach (var ev in resumo.ProximosEventos) ProximosEventos.Add(ev);

        ReservasFuturas.Clear();
        foreach (var r in resumo.ReservasFuturas) ReservasFuturas.Add(r);

        NotificacoesRecentes.Clear();
        foreach (var n in resumo.NotificacoesRecentes) NotificacoesRecentes.Add(n);

        IsBusy = false;
    }
}
