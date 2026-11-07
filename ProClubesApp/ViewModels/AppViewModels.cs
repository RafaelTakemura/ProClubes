using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using ProClubesApp.Models;
using ProClubesApp.Services;

namespace ProClubesApp.ViewModels;

public abstract class BaseViewModel : INotifyPropertyChanged
{
    private bool _isBusy;
    private string? _errorMessage;
    private string? _successMessage;
    private bool _isOffline;
    private string _syncBannerText = string.Empty;

    public event PropertyChangedEventHandler? PropertyChanged;

    public bool IsBusy
    {
        get => _isBusy;
        set => SetProperty(ref _isBusy, value);
    }

    public string? ErrorMessage
    {
        get => _errorMessage;
        set { SetProperty(ref _errorMessage, value); OnPropertyChanged(nameof(HasError)); }
    }

    public bool HasError => !string.IsNullOrWhiteSpace(ErrorMessage);

    public string? SuccessMessage
    {
        get => _successMessage;
        set { SetProperty(ref _successMessage, value); OnPropertyChanged(nameof(HasSuccess)); }
    }

    public bool HasSuccess => !string.IsNullOrWhiteSpace(SuccessMessage);

    public bool IsOffline
    {
        get => _isOffline;
        set => SetProperty(ref _isOffline, value);
    }

    public string SyncBannerText
    {
        get => _syncBannerText;
        set => SetProperty(ref _syncBannerText, value);
    }

    protected bool SetProperty<T>(ref T backingStore, T value, [CallerMemberName] string propertyName = "")
    {
        if (EqualityComparer<T>.Default.Equals(backingStore, value))
            return false;
        backingStore = value;
        OnPropertyChanged(propertyName);
        return true;
    }

    protected void OnPropertyChanged([CallerMemberName] string propertyName = "")
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}

public class LoginCadastroViewModel : BaseViewModel
{
    private readonly MobileSessionAndApiService _api;
    private string _loginOuEmail = string.Empty;
    private string _senha = string.Empty;
    private bool _modoCadastro;
    private bool _exibirStatusRestrito;
    private StatusCadastroResponse? _statusCadastroAtual;
    private string _nomeCompleto = string.Empty;
    private string _cpf = string.Empty;
    private string _email = string.Empty;
    private string _telefone = string.Empty;
    private string _senhaCadastro = string.Empty;
    private UnidadePublicaDto? _unidadeSelecionada;

    public ObservableCollection<EventoDto> EventosPublicos { get; } = new();
    public ObservableCollection<UnidadePublicaDto> UnidadesPublicas { get; } = new();

    public string LoginOuEmail { get => _loginOuEmail; set => SetProperty(ref _loginOuEmail, value); }
    public string Senha { get => _senha; set => SetProperty(ref _senha, value); }
    public bool ModoCadastro { get => _modoCadastro; set => SetProperty(ref _modoCadastro, value); }
    public bool ExibirStatusRestrito { get => _exibirStatusRestrito; set => SetProperty(ref _exibirStatusRestrito, value); }
    public StatusCadastroResponse? StatusCadastroAtual { get => _statusCadastroAtual; set => SetProperty(ref _statusCadastroAtual, value); }
    public string NomeCompleto { get => _nomeCompleto; set => SetProperty(ref _nomeCompleto, value); }
    public string Cpf { get => _cpf; set => SetProperty(ref _cpf, value); }
    public string Email { get => _email; set => SetProperty(ref _email, value); }
    public string Telefone { get => _telefone; set => SetProperty(ref _telefone, value); }
    public string SenhaCadastro { get => _senhaCadastro; set => SetProperty(ref _senhaCadastro, value); }
    public UnidadePublicaDto? UnidadeSelecionada { get => _unidadeSelecionada; set => SetProperty(ref _unidadeSelecionada, value); }

    public ICommand EntrarCommand { get; }
    public ICommand AlternarModoCommand { get; }
    public ICommand SolicitarCadastroCommand { get; }
    public ICommand ReenviarCadastroCommand { get; }
    public ICommand SairCommand { get; }

    public LoginCadastroViewModel(MobileSessionAndApiService api)
    {
        _api = api;
        EntrarCommand = new Command(async () => await EntrarAsync());
        AlternarModoCommand = new Command(() => ModoCadastro = !ModoCadastro);
        SolicitarCadastroCommand = new Command(async () => await SolicitarCadastroAsync());
        ReenviarCadastroCommand = new Command(async () => await ReenviarCadastroAsync());
        SairCommand = new Command(async () => await SairAsync());
    }

    public async Task InicializarAsync()
    {
        var (unidades, _) = await _api.GetAsync<List<UnidadePublicaDto>>("api/v1/unidades/publicas", anonymous: true);
        if (unidades is not null)
        {
            UnidadesPublicas.Clear();
            foreach (var u in unidades) UnidadesPublicas.Add(u);
            UnidadeSelecionada ??= UnidadesPublicas.FirstOrDefault();
        }

        var (pubEv, _) = await _api.GetAsync<List<EventoDto>>("api/v1/eventos/publicos", anonymous: true);
        if (pubEv is not null)
        {
            EventosPublicos.Clear();
            foreach (var e in pubEv) EventosPublicos.Add(e);
        }
    }

    public async Task EntrarAsync()
    {
        ErrorMessage = null;
        SuccessMessage = null;
        var (ok, err) = await _api.LoginAsync(LoginOuEmail, Senha);
        if (!ok)
        {
            ErrorMessage = err;
            return;
        }

        if (_api.IsRestritaCadastro)
        {
            var (st, stErr) = await _api.GetAsync<StatusCadastroResponse>("api/v1/me/cadastro/status");
            StatusCadastroAtual = st;
            ExibirStatusRestrito = true;
            if (stErr is not null) ErrorMessage = stErr;
            else SuccessMessage = $"Conta em estado restrito de acompanhamento: {st?.Status}";
        }
        else
        {
            ExibirStatusRestrito = false;
            SuccessMessage = "Autenticação realizada com sucesso.";
            if (Shell.Current is not null)
                await Shell.Current.GoToAsync("//Inicio");
        }
    }

    public async Task SolicitarCadastroAsync()
    {
        ErrorMessage = null;
        SuccessMessage = null;
        var unidadeId = UnidadeSelecionada?.Id ?? 1;
        var req = new SolicitarCadastroRequest(
            NomeCompleto,
            Cpf,
            "NaoInformado",
            "Solteiro",
            new DateOnly(1995, 1, 1),
            "Rua Principal",
            "Centro",
            "100",
            "Belo Horizonte",
            "MG",
            Telefone,
            Email,
            unidadeId,
            SenhaCadastro);

        var (res, err) = await _api.PostAsync<StatusCadastroResponse>("api/v1/cadastros/solicitacoes", req, anonymous: true);
        if (err is not null)
        {
            ErrorMessage = err;
            return;
        }

        StatusCadastroAtual = res;
        ModoCadastro = false;
        SuccessMessage = "Solicitação enviada com sucesso! Faça login para acompanhar o status.";
    }

    public async Task ReenviarCadastroAsync()
    {
        if (StatusCadastroAtual is null) return;
        ErrorMessage = null;
        SuccessMessage = null;

        var req = new ReenviarSolicitacaoCadastroRequest(
            string.IsNullOrWhiteSpace(NomeCompleto) ? StatusCadastroAtual.NomeCompleto : NomeCompleto,
            "NaoInformado",
            "Solteiro",
            new DateOnly(1995, 1, 1),
            "Rua Atualizada",
            "Centro",
            "100",
            "Belo Horizonte",
            "MG",
            string.IsNullOrWhiteSpace(Telefone) ? "31999990000" : Telefone,
            string.IsNullOrWhiteSpace(Email) ? StatusCadastroAtual.Email : Email,
            StatusCadastroAtual.UnidadePretendidaId);

        var (res, err) = await _api.PutAsync<StatusCadastroResponse>("api/v1/me/cadastro/reenvio", req);
        if (err is not null)
        {
            ErrorMessage = err;
            return;
        }

        StatusCadastroAtual = res;
        SuccessMessage = "Solicitação reenviada com sucesso sem duplicar CPF!";
    }

    public async Task SairAsync()
    {
        await _api.LogoutAsync();
        ExibirStatusRestrito = false;
        StatusCadastroAtual = null;
        SuccessMessage = "Sessão encerrada.";
    }
}

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

public class EventosViewModel : BaseViewModel
{
    private readonly MobileSessionAndApiService _api;
    public ObservableCollection<EventoDto> Eventos { get; } = new();

    public EventosViewModel(MobileSessionAndApiService api)
    {
        _api = api;
    }

    public async Task CarregarAsync()
    {
        ErrorMessage = null;
        var url = _api.IsAuthenticated ? "api/v1/eventos" : "api/v1/eventos/publicos";
        var (items, err) = await _api.GetAsync<List<EventoDto>>(url, anonymous: !_api.IsAuthenticated);
        if (items is not null)
        {
            Eventos.Clear();
            foreach (var ev in items) Eventos.Add(ev);
            return;
        }

        var (cache, doCache, _) = await _api.SincronizarOuLerCacheAsync();
        if (cache is not null && doCache)
        {
            IsOffline = true;
            SyncBannerText = $"Dados de {cache.SincronizadoEm:dd/MM/yyyy HH:mm} — somente consulta";
            Eventos.Clear();
            foreach (var ev in cache.ProximosEventos) Eventos.Add(ev);
            return;
        }

        ErrorMessage = err;
    }
}

public class ReservasViewModel : BaseViewModel
{
    private readonly MobileSessionAndApiService _api;
    private EspacoDto? _espacoSelecionado;
    private DateTime _dataReserva = DateTime.Today.AddDays(2);
    private string _horaInicio = "10:00";
    private string _horaFim = "12:00";

    public ObservableCollection<EspacoDto> Espacos { get; } = new();
    public ObservableCollection<ReservaEspacoDto> MinhasReservas { get; } = new();

    public EspacoDto? EspacoSelecionado { get => _espacoSelecionado; set => SetProperty(ref _espacoSelecionado, value); }
    public DateTime DataReserva { get => _dataReserva; set => SetProperty(ref _dataReserva, value); }
    public string HoraInicio { get => _horaInicio; set => SetProperty(ref _horaInicio, value); }
    public string HoraFim { get => _horaFim; set => SetProperty(ref _horaFim, value); }
    public bool PermiteOperarReserva => _api.IsTitularPagante;

    public ICommand CriarReservaCommand { get; }
    public ICommand CancelarReservaCommand { get; }

    public ReservasViewModel(MobileSessionAndApiService api)
    {
        _api = api;
        CriarReservaCommand = new Command(async () => await CriarReservaAsync());
        CancelarReservaCommand = new Command<ReservaEspacoDto>(async r => await CancelarReservaAsync(r));
    }

    public async Task CarregarAsync()
    {
        ErrorMessage = null;
        OnPropertyChanged(nameof(PermiteOperarReserva));

        var (espacos, _) = await _api.GetAsync<List<EspacoDto>>("api/v1/espacos");
        if (espacos is not null)
        {
            Espacos.Clear();
            foreach (var e in espacos) Espacos.Add(e);
            EspacoSelecionado ??= Espacos.FirstOrDefault();
        }

        var (reservas, err) = await _api.GetAsync<List<ReservaEspacoDto>>("api/v1/reservas");
        if (reservas is not null)
        {
            MinhasReservas.Clear();
            foreach (var r in reservas) MinhasReservas.Add(r);
            return;
        }

        var (cache, doCache, _) = await _api.SincronizarOuLerCacheAsync();
        if (cache is not null && doCache)
        {
            IsOffline = true;
            SyncBannerText = $"Dados de {cache.SincronizadoEm:dd/MM/yyyy HH:mm} — somente consulta";
            MinhasReservas.Clear();
            foreach (var r in cache.ReservasFuturas) MinhasReservas.Add(r);
            return;
        }

        ErrorMessage = err;
    }

    public async Task CriarReservaAsync()
    {
        ErrorMessage = null;
        SuccessMessage = null;
        if (EspacoSelecionado is null)
        {
            ErrorMessage = "Selecione um espaço para reservar.";
            return;
        }

        if (!TimeOnly.TryParse(HoraInicio, out var hIni) || !TimeOnly.TryParse(HoraFim, out var hFim))
        {
            ErrorMessage = "Horário inválido. Use o formato HH:mm.";
            return;
        }

        var d = DateOnly.FromDateTime(DataReserva);
        var inicio = new DateTimeOffset(d.ToDateTime(hIni), TimeSpan.FromHours(-3));
        var fim = new DateTimeOffset(d.ToDateTime(hFim), TimeSpan.FromHours(-3));

        var (res, err) = await _api.PostAsync<ReservaEspacoDto>("api/v1/reservas", new CriarReservaRequest(EspacoSelecionado.Id, inicio, fim));
        if (err is not null)
        {
            ErrorMessage = err;
            return;
        }

        SuccessMessage = $"Reserva confirmada automaticamente para {res?.EspacoNome}!";
        await CarregarAsync();
    }

    public async Task CancelarReservaAsync(ReservaEspacoDto? reserva)
    {
        if (reserva is null) return;
        ErrorMessage = null;
        SuccessMessage = null;

        var (_, err) = await _api.PostAsync<ReservaEspacoDto>($"api/v1/reservas/{reserva.Id}/cancelamento", new { justificativa = "Cancelado pelo associado no aplicativo" });
        if (err is not null)
        {
            ErrorMessage = err;
            return;
        }

        SuccessMessage = "Reserva cancelada com sucesso.";
        await CarregarAsync();
    }
}

public class FinanceiroViewModel : BaseViewModel
{
    private readonly MobileSessionAndApiService _api;
    public ObservableCollection<MensalidadeDto> Mensalidades { get; } = new();

    public FinanceiroViewModel(MobileSessionAndApiService api)
    {
        _api = api;
    }

    public async Task CarregarAsync()
    {
        ErrorMessage = null;
        if (!_api.IsTitularPagante)
        {
            ErrorMessage = "Consulta financeira disponível apenas para o Associado Titular.";
            return;
        }

        var (items, err) = await _api.GetAsync<List<MensalidadeDto>>("api/v1/me/mensalidades");
        if (items is not null)
        {
            Mensalidades.Clear();
            foreach (var m in items) Mensalidades.Add(m);
            return;
        }

        ErrorMessage = err;
    }
}

public class OuvidoriaViewModel : BaseViewModel
{
    private readonly MobileSessionAndApiService _api;
    private CategoriaManifestacaoDto? _categoriaSelecionada;
    private string _assuntoManifestacao = string.Empty;
    private string _descricaoManifestacao = string.Empty;
    private ManifestacaoDto? _manifestacaoSelecionada;
    private bool _exibirFormularioNova;

    public ObservableCollection<CategoriaManifestacaoDto> CategoriasManifestacao { get; } = new();
    public ObservableCollection<ManifestacaoDto> MinhasManifestacoes { get; } = new();
    public ObservableCollection<RespostaManifestacaoDto> RespostasDaSelecionada { get; } = new();

    public CategoriaManifestacaoDto? CategoriaSelecionada { get => _categoriaSelecionada; set => SetProperty(ref _categoriaSelecionada, value); }
    public string AssuntoManifestacao { get => _assuntoManifestacao; set => SetProperty(ref _assuntoManifestacao, value); }
    public string DescricaoManifestacao { get => _descricaoManifestacao; set => SetProperty(ref _descricaoManifestacao, value); }
    public bool ExibirFormularioNova { get => _exibirFormularioNova; set => SetProperty(ref _exibirFormularioNova, value); }

    public ManifestacaoDto? ManifestacaoSelecionada
    {
        get => _manifestacaoSelecionada;
        set
        {
            SetProperty(ref _manifestacaoSelecionada, value);
            OnPropertyChanged(nameof(TemManifestacaoSelecionada));
            OnPropertyChanged(nameof(SemRespostasNaSelecionada));
        }
    }

    public bool TemManifestacaoSelecionada => ManifestacaoSelecionada is not null;
    public bool SemRespostasNaSelecionada => ManifestacaoSelecionada is not null && RespostasDaSelecionada.Count == 0;
    public bool PodeManifestar => _api.IsTitularPagante;

    public ICommand AlternarFormularioNovaCommand { get; }
    public ICommand EnviarManifestacaoCommand { get; }
    public ICommand SelecionarManifestacaoCommand { get; }
    public ICommand FecharDetalhesCommand { get; }
    public ICommand AtualizarCommand { get; }

    public OuvidoriaViewModel(MobileSessionAndApiService api)
    {
        _api = api;
        AlternarFormularioNovaCommand = new Command(() => ExibirFormularioNova = !ExibirFormularioNova);
        EnviarManifestacaoCommand = new Command(async () => await EnviarManifestacaoAsync());
        SelecionarManifestacaoCommand = new Command<ManifestacaoDto>(async m => await SelecionarManifestacaoAsync(m));
        FecharDetalhesCommand = new Command(() =>
        {
            ManifestacaoSelecionada = null;
            RespostasDaSelecionada.Clear();
        });
        AtualizarCommand = new Command(async () => await CarregarAsync());
    }

    public async Task CarregarAsync()
    {
        ErrorMessage = null;
        OnPropertyChanged(nameof(PodeManifestar));
        if (!PodeManifestar)
            return;

        var (cats, _) = await _api.GetAsync<List<CategoriaManifestacaoDto>>("api/v1/manifestacoes/categorias");
        if (cats is not null)
        {
            CategoriasManifestacao.Clear();
            foreach (var c in cats.Where(x => x.Ativa)) CategoriasManifestacao.Add(c);
            CategoriaSelecionada ??= CategoriasManifestacao.FirstOrDefault();
        }

        var (paged, err) = await _api.GetAsync<PagedResponse<ManifestacaoDto>>("api/v1/manifestacoes?page=1&pageSize=30");
        if (paged is not null)
        {
            MinhasManifestacoes.Clear();
            foreach (var m in paged.Items) MinhasManifestacoes.Add(m);

            if (ManifestacaoSelecionada is not null)
            {
                var atualizada = paged.Items.FirstOrDefault(x => x.Id == ManifestacaoSelecionada.Id);
                if (atualizada is not null)
                    await SelecionarManifestacaoAsync(atualizada);
            }
        }
        else if (err is not null)
        {
            ErrorMessage = err;
        }
    }

    public async Task SelecionarManifestacaoAsync(ManifestacaoDto? item)
    {
        if (item is null) return;
        ErrorMessage = null;

        var (detalhe, err) = await _api.GetAsync<ManifestacaoDto>($"api/v1/manifestacoes/{item.Id}");
        var alvo = detalhe ?? item;

        RespostasDaSelecionada.Clear();
        foreach (var r in alvo.ListaRespostas)
            RespostasDaSelecionada.Add(r);

        ManifestacaoSelecionada = alvo;
        OnPropertyChanged(nameof(SemRespostasNaSelecionada));

        if (err is not null && detalhe is null)
            ErrorMessage = err;
    }

    public async Task EnviarManifestacaoAsync()
    {
        ErrorMessage = null;
        SuccessMessage = null;
        if (CategoriaSelecionada is null)
        {
            ErrorMessage = "Selecione uma categoria ativa.";
            return;
        }

        var req = new CriarManifestacaoRequest(
            CategoriaSelecionada.Id,
            CategoriaSelecionada.TipoManifestacao,
            AssuntoManifestacao,
            DescricaoManifestacao);

        var (criada, err) = await _api.PostAsync<ManifestacaoDto>("api/v1/manifestacoes", req);
        if (err is not null)
        {
            ErrorMessage = err;
            return;
        }

        AssuntoManifestacao = string.Empty;
        DescricaoManifestacao = string.Empty;
        ExibirFormularioNova = false;
        SuccessMessage = "Manifestação registrada com sucesso!";
        await CarregarAsync();
        if (criada is not null)
            await SelecionarManifestacaoAsync(criada);
    }
}

public class MaisGovernancaViewModel : BaseViewModel
{
    private readonly MobileSessionAndApiService _api;

    public ObservableCollection<VotacaoDto> Votacoes { get; } = new();

    public bool PodeVotar => _api.TemDireitoVoto;

    public ICommand SairCommand { get; }

    public MaisGovernancaViewModel(MobileSessionAndApiService api)
    {
        _api = api;
        SairCommand = new Command(async () =>
        {
            await _api.LogoutAsync();
            if (Shell.Current is not null)
                await Shell.Current.GoToAsync("//Login");
        });
    }

    public async Task CarregarAsync()
    {
        ErrorMessage = null;
        OnPropertyChanged(nameof(PodeVotar));

        if (PodeVotar)
        {
            var (vots, _) = await _api.GetAsync<List<VotacaoDto>>("api/v1/votacoes");
            if (vots is not null)
            {
                Votacoes.Clear();
                foreach (var v in vots) Votacoes.Add(v);
            }
        }
    }

    public async Task RegistrarVotoDiretoAsync(int votacaoId, int opcaoId)
    {
        ErrorMessage = null;
        SuccessMessage = null;
        var (_, err) = await _api.PostAsync<object>($"api/v1/votacoes/{votacaoId}/votos", new RegistrarVotoRequest(opcaoId));
        if (err is not null)
        {
            ErrorMessage = err;
            return;
        }
        SuccessMessage = "Voto registrado com sucesso!";
        await CarregarAsync();
    }
}
