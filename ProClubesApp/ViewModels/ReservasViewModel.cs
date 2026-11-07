using System.Collections.ObjectModel;
using System.Windows.Input;
using ProClubesApp.Models;
using ProClubesApp.Services;

namespace ProClubesApp.ViewModels;

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
