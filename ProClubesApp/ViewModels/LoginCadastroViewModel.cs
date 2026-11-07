using System.Collections.ObjectModel;
using System.Windows.Input;
using ProClubesApp.Models;
using ProClubesApp.Services;

namespace ProClubesApp.ViewModels;

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
    public ICommand IrParaEventosCommand { get; }
    public ICommand IrParaLoginCommand { get; }
    public ICommand SolicitarCadastroCommand { get; }
    public ICommand ReenviarCadastroCommand { get; }
    public ICommand SairCommand { get; }

    public LoginCadastroViewModel(MobileSessionAndApiService api)
    {
        _api = api;
        EntrarCommand = new Command(async () => await EntrarAsync());
        AlternarModoCommand = new Command(async () =>
        {
            if (Shell.Current is not null)
                await Shell.Current.GoToAsync("//Cadastro");
        });
        IrParaEventosCommand = new Command(async () =>
        {
            if (Shell.Current is not null)
                await Shell.Current.GoToAsync("//Eventos");
        });
        IrParaLoginCommand = new Command(async () =>
        {
            if (Shell.Current is not null)
                await Shell.Current.GoToAsync("//Login");
        });
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
            Senha = string.Empty;
            var (st, stErr) = await _api.GetAsync<StatusCadastroResponse>("api/v1/me/cadastro/status");
            StatusCadastroAtual = st;
            ExibirStatusRestrito = true;
            if (stErr is not null) ErrorMessage = stErr;
            else SuccessMessage = $"Conta em estado restrito de acompanhamento: {st?.Status}";
        }
        else
        {
            Senha = string.Empty;
            ExibirStatusRestrito = false;
            SuccessMessage = null;
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
        SenhaCadastro = string.Empty;
        SuccessMessage = "Solicitação enviada com sucesso! Faça login para acompanhar o status.";
        if (Shell.Current is not null)
            await Shell.Current.GoToAsync("//Login");
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
