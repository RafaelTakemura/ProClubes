using System.Collections.ObjectModel;
using System.Windows.Input;
using ProClubesApp.Models;
using ProClubesApp.Services;

namespace ProClubesApp.ViewModels;

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
