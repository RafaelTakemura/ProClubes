using System.Collections.ObjectModel;
using ProClubesApp.Models;
using ProClubesApp.Services;

namespace ProClubesApp.ViewModels;

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
