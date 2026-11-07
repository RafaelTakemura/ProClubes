using System.Collections.ObjectModel;
using ProClubesApp.Models;
using ProClubesApp.Services;

namespace ProClubesApp.ViewModels;

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
        var autenticadoPleno = _api.IsAuthenticated && !_api.IsRestritaCadastro;
        var url = autenticadoPleno ? "api/v1/eventos" : "api/v1/eventos/publicos";
        var (items, err) = await _api.GetAsync<List<EventoDto>>(url, anonymous: !autenticadoPleno);
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
