using System.Collections.ObjectModel;
using System.Text.RegularExpressions;
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
    private string? _sexo;
    private string? _estadoCivil;
    private DateTime _dataNascimento = new(2000, 1, 1);
    private string _profissao = string.Empty;
    private string _email = string.Empty;
    private string _telefone = string.Empty;
    private string _logradouro = string.Empty;
    private string _numero = string.Empty;
    private string _bairro = string.Empty;
    private string _senhaCadastro = string.Empty;
    private string _categoriaSelecionada = "Sócio Associado";
    private UnidadePublicaDto? _unidadeSelecionada;

    internal static readonly IReadOnlyList<string> OpcoesSexoPadrao = ["Masculino", "Feminino", "Outro"];
    internal static readonly IReadOnlyList<string> OpcoesEstadoCivilPadrao = ["Solteiro(a)", "Casado(a)", "Divorciado(a)", "Viúvo(a)", "União Estável"];

    public IReadOnlyList<string> OpcoesSexo => OpcoesSexoPadrao;
    public IReadOnlyList<string> OpcoesEstadoCivil => OpcoesEstadoCivilPadrao;
    public IReadOnlyList<string> OpcoesCategoria { get; } = ["Sócio Associado", "Sócio Fundador", "Sócio Convidado", "Simpatizante"];

    public LocalidadesPickerController Localidades { get; } = new();
    public ObservableCollection<EventoDto> EventosPublicos { get; } = new();
    public ObservableCollection<UnidadePublicaDto> UnidadesPublicas { get; } = new();

    public string LoginOuEmail { get => _loginOuEmail; set => SetProperty(ref _loginOuEmail, value); }
    public string Senha { get => _senha; set => SetProperty(ref _senha, value); }
    public bool ModoCadastro { get => _modoCadastro; set => SetProperty(ref _modoCadastro, value); }
    public bool ExibirStatusRestrito { get => _exibirStatusRestrito; set => SetProperty(ref _exibirStatusRestrito, value); }
    public StatusCadastroResponse? StatusCadastroAtual { get => _statusCadastroAtual; set => SetProperty(ref _statusCadastroAtual, value); }

    public string NomeCompleto { get => _nomeCompleto; set => SetProperty(ref _nomeCompleto, value); }
    public string Cpf { get => _cpf; set => SetProperty(ref _cpf, FormatarCpfOuCnpj(value)); }
    public string? Sexo { get => _sexo; set => SetProperty(ref _sexo, value); }
    public string? EstadoCivil { get => _estadoCivil; set => SetProperty(ref _estadoCivil, value); }
    public DateTime DataNascimento { get => _dataNascimento; set => SetProperty(ref _dataNascimento, value); }
    public string Profissao { get => _profissao; set => SetProperty(ref _profissao, value); }
    public string Email { get => _email; set => SetProperty(ref _email, value); }
    public string Telefone { get => _telefone; set => SetProperty(ref _telefone, value); }
    public string Logradouro { get => _logradouro; set => SetProperty(ref _logradouro, value); }
    public string Numero { get => _numero; set => SetProperty(ref _numero, value); }
    public string Bairro { get => _bairro; set => SetProperty(ref _bairro, value); }
    public string SenhaCadastro { get => _senhaCadastro; set => SetProperty(ref _senhaCadastro, value); }
    public string CategoriaSelecionada { get => _categoriaSelecionada; set => SetProperty(ref _categoriaSelecionada, value); }
    public UnidadePublicaDto? UnidadeSelecionada { get => _unidadeSelecionada; set => SetProperty(ref _unidadeSelecionada, value); }

    public ICommand EntrarCommand { get; }
    public ICommand AlternarModoCommand { get; }
    public ICommand IrParaEventosCommand { get; }
    public ICommand IrParaLoginCommand { get; }
    public ICommand SolicitarCadastroCommand { get; }
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

        await Localidades.CarregarAsync(_api);
    }

    public async Task PrepararReenvioCadastroAsync()
    {
        ErrorMessage = null;
        SuccessMessage = null;
        await Localidades.CarregarAsync(_api);

        var perfil = _api.UsuarioAtual;
        if (perfil is not null)
        {
            NomeCompleto = perfil.NomeCompleto;
            Email = perfil.Email;
            Telefone = perfil.Fone;
            Sexo = OpcoesSexo.FirstOrDefault(s => string.Equals(s, perfil.Sexo, StringComparison.OrdinalIgnoreCase)) ?? OpcoesSexo[0];
            EstadoCivil = OpcoesEstadoCivil.FirstOrDefault(e => string.Equals(e, perfil.EstadoCivil, StringComparison.OrdinalIgnoreCase)) ?? OpcoesEstadoCivil[0];
            if (perfil.DataNascimento != default)
                DataNascimento = perfil.DataNascimento.ToDateTime(TimeOnly.MinValue);
            Profissao = perfil.Profissao;
            Logradouro = perfil.Logradouro;
            Numero = perfil.Numero;
            Bairro = perfil.Bairro;
            Localidades.DefinirValores(perfil.UF, perfil.Cidade);
        }

        if (StatusCadastroAtual is null) return;

        NomeCompleto = StatusCadastroAtual.NomeCompleto;
        Email = StatusCadastroAtual.Email;
        Sexo ??= OpcoesSexo[0];
        EstadoCivil ??= OpcoesEstadoCivil[0];
        CategoriaSelecionada = OpcoesCategoria.FirstOrDefault(c =>
            string.Equals(c, StatusCadastroAtual.CategoriaAprovadaNome, StringComparison.OrdinalIgnoreCase))
            ?? "Sócio Associado";
        UnidadeSelecionada = UnidadesPublicas.FirstOrDefault(u => u.Id == StatusCadastroAtual.UnidadePretendidaId)
                             ?? UnidadesPublicas.FirstOrDefault();
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

        if (UnidadeSelecionada is null
            || string.IsNullOrWhiteSpace(CategoriaSelecionada)
            || string.IsNullOrWhiteSpace(NomeCompleto)
            || string.IsNullOrWhiteSpace(Cpf)
            || string.IsNullOrWhiteSpace(Sexo)
            || string.IsNullOrWhiteSpace(EstadoCivil)
            || string.IsNullOrWhiteSpace(Profissao)
            || string.IsNullOrWhiteSpace(Telefone)
            || string.IsNullOrWhiteSpace(Email)
            || string.IsNullOrWhiteSpace(SenhaCadastro))
        {
            ErrorMessage = "Preencha todos os campos obrigatórios (*) antes de enviar.";
            return;
        }

        if (!ValidarSintaxeCpf(Cpf))
        {
            ErrorMessage = "Sintaxe de CPF inválida. Informe 11 dígitos numéricos ou no formato 000.000.000-00.";
            return;
        }

        if (!ValidarSintaxeTelefone(Telefone))
        {
            ErrorMessage = "Sintaxe de telefone inválida. Informe DDD + número (10 ou 11 dígitos).";
            return;
        }

        if (!ValidarSintaxeEmail(Email))
        {
            ErrorMessage = "Sintaxe de e-mail inválida. Informe um endereço no formato usuario@dominio.com.";
            return;
        }

        var (cidadeSel, ufSel) = Localidades.ObterValores();

        var req = new SolicitarCadastroRequest(
            NomeCompleto.Trim(),
            Cpf.Trim(),
            Sexo,
            EstadoCivil,
            DateOnly.FromDateTime(DataNascimento),
            Logradouro.Trim(),
            Bairro.Trim(),
            Numero.Trim(),
            cidadeSel,
            ufSel,
            Telefone.Trim(),
            Email.Trim(),
            UnidadeSelecionada.Id,
            SenhaCadastro,
            CodigoCategoriaPretendida: MapearCategoriaParaCodigo(CategoriaSelecionada),
            Profissao: Profissao.Trim());

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

    public async Task<bool> ReenviarCadastroAsync()
    {
        if (StatusCadastroAtual is null) return false;
        ErrorMessage = null;
        SuccessMessage = null;

        if (UnidadeSelecionada is null
            || string.IsNullOrWhiteSpace(CategoriaSelecionada)
            || string.IsNullOrWhiteSpace(NomeCompleto)
            || string.IsNullOrWhiteSpace(Sexo)
            || string.IsNullOrWhiteSpace(EstadoCivil)
            || string.IsNullOrWhiteSpace(Profissao)
            || string.IsNullOrWhiteSpace(Telefone)
            || string.IsNullOrWhiteSpace(Email))
        {
            ErrorMessage = "Preencha todos os campos obrigatórios (*) antes de reenviar.";
            return false;
        }

        if (!ValidarSintaxeTelefone(Telefone))
        {
            ErrorMessage = "Sintaxe de telefone inválida. Informe DDD + número (10 ou 11 dígitos).";
            return false;
        }

        if (!ValidarSintaxeEmail(Email))
        {
            ErrorMessage = "Sintaxe de e-mail inválida. Informe um endereço no formato usuario@dominio.com.";
            return false;
        }

        var (cidadeSel, ufSel) = Localidades.ObterValores();

        var req = new ReenviarSolicitacaoCadastroRequest(
            NomeCompleto.Trim(),
            Sexo,
            EstadoCivil,
            DateOnly.FromDateTime(DataNascimento),
            Logradouro.Trim(),
            Bairro.Trim(),
            Numero.Trim(),
            cidadeSel,
            ufSel,
            Telefone.Trim(),
            Email.Trim(),
            UnidadeSelecionada.Id,
            CodigoCategoriaPretendida: MapearCategoriaParaCodigo(CategoriaSelecionada),
            Profissao: Profissao.Trim());

        var (res, err) = await _api.PutAsync<StatusCadastroResponse>("api/v1/me/cadastro/reenvio", req);
        if (err is not null)
        {
            ErrorMessage = err;
            return false;
        }

        StatusCadastroAtual = res;
        SuccessMessage = "Solicitação corrigida e reenviada com sucesso!";
        return true;
    }

    public async Task SairAsync()
    {
        await _api.LogoutAsync();
        ExibirStatusRestrito = false;
        StatusCadastroAtual = null;
    }

    private static string MapearCategoriaParaCodigo(string? categoriaNome)
    {
        if (string.IsNullOrWhiteSpace(categoriaNome)) return "ASSOCIADO";
        if (categoriaNome.Contains("Fundador", StringComparison.OrdinalIgnoreCase)) return "FUNDADOR";
        if (categoriaNome.Contains("Convidado", StringComparison.OrdinalIgnoreCase)) return "CONVIDADO";
        if (categoriaNome.Contains("Simpatizante", StringComparison.OrdinalIgnoreCase)) return "SIMPATIZANTE";
        return "ASSOCIADO";
    }

    internal static string FormatarCpfOuCnpj(string? valor)
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

    internal static bool ValidarSintaxeCpf(string? cpf)
    {
        if (string.IsNullOrWhiteSpace(cpf)) return false;
        var s = cpf.Trim();
        if (!Regex.IsMatch(s, @"^(\d{11}|\d{3}\.\d{3}\.\d{3}-\d{2})$")) return false;
        var digitos = new string(s.Where(char.IsDigit).ToArray());
        return digitos.Length == 11 && digitos.Distinct().Count() > 1;
    }

    internal static bool ValidarSintaxeTelefone(string? fone)
    {
        if (string.IsNullOrWhiteSpace(fone)) return false;
        var s = fone.Trim();
        if (!Regex.IsMatch(s, @"^(\(\d{2}\)\s?\d{4,5}-?\d{4}|\d{2}\s?\d{4,5}-?\d{4}|\d{10,11})$")) return false;
        var digitos = new string(s.Where(char.IsDigit).ToArray());
        return digitos.Length is 10 or 11;
    }

    internal static bool ValidarSintaxeEmail(string? email)
    {
        if (string.IsNullOrWhiteSpace(email)) return false;
        return Regex.IsMatch(email.Trim(), @"^[^@\s]+@[^@\s]+\.[^@\s]{2,}$");
    }
}
