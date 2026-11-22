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

public sealed class LocalidadesPickerController : BaseViewModel
{
    private IReadOnlyList<EstadoMobileDto> _todosEstados = Array.Empty<EstadoMobileDto>();
    private IReadOnlyList<CidadeMobileDto> _todasCidades = Array.Empty<CidadeMobileDto>();
    private EstadoMobileDto? _estadoSelecionado;
    private CidadeMobileDto? _cidadeSelecionada;
    private bool _atualizandoInternamente;

    public ObservableCollection<EstadoMobileDto> EstadosDisponiveis { get; } = new();
    public ObservableCollection<CidadeMobileDto> CidadesDisponiveis { get; } = new();

    public ICommand LimparSelecaoCommand { get; }

    public LocalidadesPickerController()
    {
        LimparSelecaoCommand = new Command(LimparSelecao);
    }

    public EstadoMobileDto? EstadoSelecionado
    {
        get => _estadoSelecionado;
        set
        {
            if (EqualityComparer<EstadoMobileDto?>.Default.Equals(_estadoSelecionado, value))
                return;

            _estadoSelecionado = value;
            OnPropertyChanged();

            if (_atualizandoInternamente)
                return;

            _atualizandoInternamente = true;
            try
            {
                var cidadesFiltradas = LocalidadesMobileHelper.FiltrarCidadesPorEstado(_todasCidades, value?.UF);
                SyncCollection(CidadesDisponiveis, cidadesFiltradas);

                if (value is not null && _cidadeSelecionada is not null &&
                    !string.Equals(_cidadeSelecionada.EstadoUF, value.UF, StringComparison.OrdinalIgnoreCase))
                {
                    _cidadeSelecionada = null;
                }

                if (_cidadeSelecionada is null)
                    SyncCollection(EstadosDisponiveis, _todosEstados);

                OnPropertyChanged(nameof(CidadeSelecionada));
                OnPropertyChanged(nameof(EstadoSelecionado));
            }
            finally
            {
                _atualizandoInternamente = false;
            }
        }
    }

    public CidadeMobileDto? CidadeSelecionada
    {
        get => _cidadeSelecionada;
        set
        {
            if (EqualityComparer<CidadeMobileDto?>.Default.Equals(_cidadeSelecionada, value))
                return;

            _cidadeSelecionada = value;
            OnPropertyChanged();

            if (_atualizandoInternamente)
                return;

            _atualizandoInternamente = true;
            try
            {
                if (value is not null)
                {
                    var estadosFiltrados = LocalidadesMobileHelper.FiltrarEstadosPorCidade(_todosEstados, _todasCidades, value.Nome);
                    SyncCollection(EstadosDisponiveis, estadosFiltrados);

                    if (estadosFiltrados.Count == 1)
                    {
                        _estadoSelecionado = estadosFiltrados[0];
                    }
                    else if (_estadoSelecionado is not null && !estadosFiltrados.Any(e => string.Equals(e.UF, _estadoSelecionado.UF, StringComparison.OrdinalIgnoreCase)))
                    {
                        _estadoSelecionado = null;
                    }
                }
                else
                {
                    SyncCollection(EstadosDisponiveis, _todosEstados);
                    var cidadesFiltradas = LocalidadesMobileHelper.FiltrarCidadesPorEstado(_todasCidades, _estadoSelecionado?.UF);
                    SyncCollection(CidadesDisponiveis, cidadesFiltradas);
                }

                OnPropertyChanged(nameof(EstadoSelecionado));
                OnPropertyChanged(nameof(CidadeSelecionada));
            }
            finally
            {
                _atualizandoInternamente = false;
            }
        }
    }

    public async Task CarregarAsync(MobileSessionAndApiService api)
    {
        var (estados, _) = await api.GetAsync<List<EstadoMobileDto>>("api/v1/localidades/estados?apenasAtivos=true", anonymous: true);
        var (cidades, _) = await api.GetAsync<List<CidadeMobileDto>>("api/v1/localidades/cidades?apenasAtivas=true", anonymous: true);

        if (estados is not null)
            _todosEstados = estados;
        if (cidades is not null)
            _todasCidades = cidades;

        _atualizandoInternamente = true;
        try
        {
            var estadosFiltrados = _cidadeSelecionada is not null
                ? LocalidadesMobileHelper.FiltrarEstadosPorCidade(_todosEstados, _todasCidades, _cidadeSelecionada.Nome)
                : _todosEstados;

            var cidadesFiltradas = _estadoSelecionado is not null
                ? LocalidadesMobileHelper.FiltrarCidadesPorEstado(_todasCidades, _estadoSelecionado.UF)
                : _todasCidades;

            SyncCollection(EstadosDisponiveis, estadosFiltrados);
            SyncCollection(CidadesDisponiveis, cidadesFiltradas);
            OnPropertyChanged(nameof(EstadoSelecionado));
            OnPropertyChanged(nameof(CidadeSelecionada));
        }
        finally
        {
            _atualizandoInternamente = false;
        }
    }

    public void DefinirValores(string? uf, string? cidade)
    {
        _atualizandoInternamente = true;
        try
        {
            var ufNorm = (uf ?? string.Empty).Trim();
            var cidNorm = (cidade ?? string.Empty).Trim();

            var estadoMatch = !string.IsNullOrEmpty(ufNorm)
                ? _todosEstados.FirstOrDefault(e => string.Equals(e.UF, ufNorm, StringComparison.OrdinalIgnoreCase))
                : null;

            var cidadeMatch = !string.IsNullOrEmpty(cidNorm)
                ? _todasCidades.FirstOrDefault(c =>
                    string.Equals(c.Nome, cidNorm, StringComparison.OrdinalIgnoreCase)
                    && (estadoMatch is null || string.Equals(c.EstadoUF, estadoMatch.UF, StringComparison.OrdinalIgnoreCase)))
                : null;

            if (estadoMatch is null && cidadeMatch is not null)
                estadoMatch = _todosEstados.FirstOrDefault(e => string.Equals(e.UF, cidadeMatch.EstadoUF, StringComparison.OrdinalIgnoreCase));

            var estadosLista = cidadeMatch is not null
                ? LocalidadesMobileHelper.FiltrarEstadosPorCidade(_todosEstados, _todasCidades, cidadeMatch.Nome)
                : _todosEstados;

            var cidadesLista = estadoMatch is not null
                ? LocalidadesMobileHelper.FiltrarCidadesPorEstado(_todasCidades, estadoMatch.UF)
                : _todasCidades;

            SyncCollection(EstadosDisponiveis, estadosLista);
            SyncCollection(CidadesDisponiveis, cidadesLista);

            _estadoSelecionado = estadoMatch;
            _cidadeSelecionada = cidadeMatch;

            OnPropertyChanged(nameof(EstadoSelecionado));
            OnPropertyChanged(nameof(CidadeSelecionada));
        }
        finally
        {
            _atualizandoInternamente = false;
        }
    }

    public void LimparSelecao()
    {
        _atualizandoInternamente = true;
        try
        {
            _estadoSelecionado = null;
            _cidadeSelecionada = null;
            SyncCollection(EstadosDisponiveis, _todosEstados);
            SyncCollection(CidadesDisponiveis, _todasCidades);
            OnPropertyChanged(nameof(EstadoSelecionado));
            OnPropertyChanged(nameof(CidadeSelecionada));
        }
        finally
        {
            _atualizandoInternamente = false;
        }
    }

    public (string Cidade, string UF) ObterValores() =>
        (_cidadeSelecionada?.Nome ?? string.Empty, _estadoSelecionado?.UF ?? string.Empty);

    private static void SyncCollection<T>(ObservableCollection<T> target, IReadOnlyList<T> next)
    {
        if (target.SequenceEqual(next))
            return;

        target.Clear();
        foreach (var item in next)
            target.Add(item);
    }
}
