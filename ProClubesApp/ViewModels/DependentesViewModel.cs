using System.Collections.ObjectModel;
using System.Windows.Input;
using ProClubesApp.Models;
using ProClubesApp.Services;

namespace ProClubesApp.ViewModels;

/// <summary>
/// ViewModel da aba de Dependentes para consulta de vínculos e solicitação de novos dependentes.
/// </summary>
public class DependentesViewModel : BaseViewModel
{
    private const string VinculoFilhoLabel = "Filho(a) até 21 anos";
    private const string VinculoConjugeLabel = "Cônjuge";

    private readonly MobileSessionAndApiService _api;
    private DependenteMobileDto? _dependenteEmEdicao;
    private string _resumoContagem = "Carregando vínculos...";
    private bool _temDependentes;
    private bool _semDependentes;

    private string _tipoVinculoSelecionado = VinculoFilhoLabel;
    private string _nomeCompleto = string.Empty;
    private string _cpf = string.Empty;
    private string _cpfSomenteLeitura = string.Empty;
    private string? _sexo;
    private string? _estadoCivil;
    private DateTime _dataNascimento = DateTime.Today.AddYears(-10);
    private string _profissao = string.Empty;
    private string _telefone = string.Empty;
    private string _email = string.Empty;
    private string _logradouro = string.Empty;
    private string _numero = string.Empty;
    private string _bairro = string.Empty;

    public IReadOnlyList<string> OpcoesTipoVinculo { get; } = [VinculoFilhoLabel, VinculoConjugeLabel];
    public IReadOnlyList<string> OpcoesSexo => LoginCadastroViewModel.OpcoesSexoPadrao;
    public IReadOnlyList<string> OpcoesEstadoCivil => LoginCadastroViewModel.OpcoesEstadoCivilPadrao;
    public LocalidadesPickerController Localidades { get; } = new();

    public ObservableCollection<DependenteMobileDto> Dependentes { get; } = new();

    public string ResumoContagem { get => _resumoContagem; set => SetProperty(ref _resumoContagem, value); }
    public bool TemDependentes { get => _temDependentes; set => SetProperty(ref _temDependentes, value); }
    public bool SemDependentes { get => _semDependentes; set => SetProperty(ref _semDependentes, value); }

    public string TipoVinculoSelecionado { get => _tipoVinculoSelecionado; set => SetProperty(ref _tipoVinculoSelecionado, value); }
    public string NomeCompleto { get => _nomeCompleto; set => SetProperty(ref _nomeCompleto, value); }
    public string Cpf { get => _cpf; set => SetProperty(ref _cpf, LoginCadastroViewModel.FormatarCpfOuCnpj(value)); }
    public string CpfSomenteLeitura { get => _cpfSomenteLeitura; set => SetProperty(ref _cpfSomenteLeitura, value); }
    public string? Sexo { get => _sexo; set => SetProperty(ref _sexo, value); }
    public string? EstadoCivil { get => _estadoCivil; set => SetProperty(ref _estadoCivil, value); }
    public DateTime DataNascimento { get => _dataNascimento; set => SetProperty(ref _dataNascimento, value); }
    public string Profissao { get => _profissao; set => SetProperty(ref _profissao, value); }
    public string Telefone { get => _telefone; set => SetProperty(ref _telefone, value); }
    public string Email { get => _email; set => SetProperty(ref _email, value); }
    public string Logradouro { get => _logradouro; set => SetProperty(ref _logradouro, value); }
    public string Numero { get => _numero; set => SetProperty(ref _numero, value); }
    public string Bairro { get => _bairro; set => SetProperty(ref _bairro, value); }

    public ICommand AtualizarCommand { get; }
    public ICommand AlterarStatusCommand { get; }

    public DependentesViewModel(MobileSessionAndApiService api)
    {
        _api = api;
        AtualizarCommand = new Command(async () => await CarregarAsync());
        AlterarStatusCommand = new Command<DependenteMobileDto>(async dep => await AlterarStatusDependenteAsync(dep));
    }

    public async Task CarregarAsync()
    {
        if (IsBusy) return;
        IsBusy = true;
        ErrorMessage = null;

        try
        {
            Dependentes.Clear();
            var associadoId = _api.UsuarioAtual?.AssociadoId;
            if (!associadoId.HasValue)
            {
                ResumoContagem = "Disponível apenas para associados titulares";
                AtualizarContadores();
                return;
            }

            var (detalhe, err) = await _api.GetAsync<AssociadoDetalheMobileDto>($"api/v1/admin/associados/{associadoId.Value}");
            IsOffline = _api.IsOffline;
            if (err is not null)
            {
                ErrorMessage = err;
            }
            else if (detalhe?.Dependentes is not null)
            {
                foreach (var dep in detalhe.Dependentes)
                    Dependentes.Add(dep);
            }

            AtualizarContadores();
        }
        finally
        {
            IsBusy = false;
        }
    }

    public void LimparFormulario()
    {
        ErrorMessage = null;
        _dependenteEmEdicao = null;
        TipoVinculoSelecionado = VinculoFilhoLabel;
        NomeCompleto = string.Empty;
        Cpf = string.Empty;
        CpfSomenteLeitura = string.Empty;
        Sexo = null;
        EstadoCivil = null;
        DataNascimento = DateTime.Today.AddYears(-10);
        Profissao = string.Empty;
        Telefone = string.Empty;
        Email = string.Empty;
        Logradouro = string.Empty;
        Numero = string.Empty;
        Bairro = string.Empty;
        Localidades.LimparSelecao();
    }

    public async Task PrepararNovoDependenteAsync()
    {
        LimparFormulario();
        await Localidades.CarregarAsync(_api);
    }

    public async Task PrepararEdicaoDependenteAsync(DependenteMobileDto dep)
    {
        ErrorMessage = null;
        _dependenteEmEdicao = dep;
        await Localidades.CarregarAsync(_api);

        TipoVinculoSelecionado = dep.TipoVinculo == "Conjuge" ? VinculoConjugeLabel : VinculoFilhoLabel;
        NomeCompleto = dep.NomeCompleto;
        Cpf = string.Empty;
        CpfSomenteLeitura = dep.CpfMascarado;
        Sexo = OpcoesSexo.FirstOrDefault(s => string.Equals(s, dep.Sexo, StringComparison.OrdinalIgnoreCase)) ?? dep.Sexo;
        EstadoCivil = OpcoesEstadoCivil.FirstOrDefault(e => string.Equals(e, dep.EstadoCivil, StringComparison.OrdinalIgnoreCase)) ?? dep.EstadoCivil;
        DataNascimento = dep.DataNascimento != default
            ? dep.DataNascimento.ToDateTime(TimeOnly.MinValue)
            : DateTime.Today.AddYears(-10);
        Profissao = dep.Profissao;
        Telefone = dep.Fone;
        Email = dep.Email;
        Logradouro = dep.Logradouro;
        Numero = dep.Numero;
        Bairro = dep.Bairro;
        Localidades.DefinirValores(dep.UF, dep.Cidade);
    }

    private bool ValidarCamposObrigatorios(bool validarCpf, out bool ehConjuge, out DateOnly dob)
    {
        dob = DateOnly.FromDateTime(DataNascimento);
        ehConjuge = string.Equals(TipoVinculoSelecionado, VinculoConjugeLabel, StringComparison.Ordinal);

        if (string.IsNullOrWhiteSpace(TipoVinculoSelecionado)
            || string.IsNullOrWhiteSpace(NomeCompleto)
            || (validarCpf && string.IsNullOrWhiteSpace(Cpf))
            || string.IsNullOrWhiteSpace(Sexo)
            || string.IsNullOrWhiteSpace(EstadoCivil)
            || string.IsNullOrWhiteSpace(Profissao)
            || string.IsNullOrWhiteSpace(Telefone)
            || string.IsNullOrWhiteSpace(Email))
        {
            ErrorMessage = "Preencha todos os campos obrigatórios (*) antes de enviar.";
            return false;
        }

        if (validarCpf && !LoginCadastroViewModel.ValidarSintaxeCpf(Cpf))
        {
            ErrorMessage = "Sintaxe de CPF inválida. Informe 11 dígitos numéricos ou no formato 000.000.000-00.";
            return false;
        }

        var hoje = DateOnly.FromDateTime(DateTime.Today);
        if (dob > hoje)
        {
            ErrorMessage = "A data de nascimento não pode ser futura.";
            return false;
        }

        if (!ehConjuge)
        {
            var idade = hoje.Year - dob.Year - (dob > hoje.AddYears(-(hoje.Year - dob.Year)) ? 1 : 0);
            if (idade > 21)
            {
                ErrorMessage = "Filho(a) dependente deve ter até 21 anos, inclusive.";
                return false;
            }
        }

        if (!LoginCadastroViewModel.ValidarSintaxeTelefone(Telefone))
        {
            ErrorMessage = "Sintaxe de telefone inválida. Informe DDD + número (10 ou 11 dígitos).";
            return false;
        }

        if (!LoginCadastroViewModel.ValidarSintaxeEmail(Email))
        {
            ErrorMessage = "Sintaxe de e-mail inválida. Informe um endereço no formato usuario@dominio.com.";
            return false;
        }

        return true;
    }

    public async Task<bool> SolicitarDependenteAsync()
    {
        ErrorMessage = null;
        SuccessMessage = null;

        if (_api.UsuarioAtual?.AssociadoId is not int associadoId)
        {
            ErrorMessage = "Somente associados titulares ativos podem cadastrar dependentes.";
            return false;
        }

        if (!ValidarCamposObrigatorios(validarCpf: true, out var ehConjuge, out var dob))
            return false;

        var (cidadeSel, ufSel) = Localidades.ObterValores();

        var req = new CadastrarDependenteMobileRequest(
            NomeCompleto.Trim(),
            Cpf.Trim(),
            Sexo!,
            EstadoCivil!,
            dob,
            Logradouro.Trim(),
            Bairro.Trim(),
            Numero.Trim(),
            cidadeSel,
            ufSel,
            Telefone.Trim(),
            Email.Trim(),
            ehConjuge ? "Conjuge" : "Filho",
            Profissao: Profissao.Trim());

        var (_, err) = await _api.PostAsync<DependenteMobileDto>($"api/v1/admin/associados/{associadoId}/dependentes", req);
        if (err is not null)
        {
            ErrorMessage = err;
            return false;
        }

        LimparFormulario();
        await CarregarAsync();
        SuccessMessage = "Solicitação de dependente enviada para aprovação da diretoria!";
        return true;
    }

    public async Task<bool> SalvarEdicaoDependenteAsync()
    {
        ErrorMessage = null;
        SuccessMessage = null;

        if (_dependenteEmEdicao is null || _api.UsuarioAtual?.AssociadoId is not int associadoId)
        {
            ErrorMessage = "Nenhum dependente selecionado para edição.";
            return false;
        }

        if (!ValidarCamposObrigatorios(validarCpf: false, out var ehConjuge, out var dob))
            return false;

        var (cidadeSel, ufSel) = Localidades.ObterValores();

        var req = new AtualizarDadosCadastraisMobileRequest(
            NomeCompleto.Trim(),
            Sexo!,
            EstadoCivil!,
            dob,
            Profissao.Trim(),
            Telefone.Trim(),
            Email.Trim(),
            Logradouro.Trim(),
            Bairro.Trim(),
            Numero.Trim(),
            cidadeSel,
            ufSel,
            ehConjuge ? "Conjuge" : "Filho");

        var (_, err) = await _api.PutAsync<DependenteMobileDto>(
            $"api/v1/admin/associados/{associadoId}/dependentes/{_dependenteEmEdicao.DependenciaId}/dados-cadastrais",
            req);

        if (err is not null)
        {
            ErrorMessage = err;
            return false;
        }

        var nomeAtualizado = NomeCompleto.Trim();
        LimparFormulario();
        await CarregarAsync();
        SuccessMessage = $"Dados do dependente {nomeAtualizado} atualizados com sucesso!";
        return true;
    }

    public async Task AlterarStatusDependenteAsync(DependenteMobileDto? dep)
    {
        if (dep is null || dep.PendenteAprovacao)
            return;

        if (_api.UsuarioAtual?.AssociadoId is not int associadoId)
        {
            ErrorMessage = "Somente associados titulares ativos podem alterar o status de dependentes.";
            return;
        }

        ErrorMessage = null;
        SuccessMessage = null;

        var novoStatus = !dep.Ativo;
        var (_, err) = await _api.PatchAsync<DependenteMobileDto>(
            $"api/v1/admin/associados/{associadoId}/dependentes/{dep.DependenciaId}/status",
            new AlterarStatusDependenteMobileRequest(novoStatus));

        if (err is not null)
        {
            ErrorMessage = err;
            return;
        }

        await CarregarAsync();
        SuccessMessage = novoStatus
            ? $"Vínculo de {dep.NomeCompleto} reativado com sucesso!"
            : $"Vínculo de {dep.NomeCompleto} desativado com sucesso!";
    }

    private void AtualizarContadores()
    {
        TemDependentes = Dependentes.Count > 0;
        SemDependentes = !TemDependentes;

        var ativos = Dependentes.Count(d => d.Ativo);
        var pendentes = Dependentes.Count(d => d.PendenteAprovacao);
        var encerrados = Dependentes.Count - ativos - pendentes;

        var partes = new List<string>();
        if (ativos > 0) partes.Add(ativos == 1 ? "1 ativo" : $"{ativos} ativos");
        if (pendentes > 0) partes.Add(pendentes == 1 ? "1 pendente de aprovação" : $"{pendentes} pendentes de aprovação");
        if (encerrados > 0) partes.Add(encerrados == 1 ? "1 encerrado" : $"{encerrados} encerrados");

        ResumoContagem = partes.Count == 0
            ? "Nenhum dependente vinculado até o momento"
            : string.Join(" • ", partes);
    }
}
