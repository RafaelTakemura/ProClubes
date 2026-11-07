using System.Collections.ObjectModel;
using System.Windows.Input;
using ProClubesApp.Models;
using ProClubesApp.Services;

namespace ProClubesApp.ViewModels;

public class MaisGovernancaViewModel : BaseViewModel
{
    private readonly MobileSessionAndApiService _api;

    public ObservableCollection<VotacaoDto> Votacoes { get; } = new();

    public bool PodeVotar => _api.TemDireitoVoto;

    public ICommand AtualizarCommand { get; }

    public MaisGovernancaViewModel(MobileSessionAndApiService api)
    {
        _api = api;
        AtualizarCommand = new Command(async () => await CarregarAsync());
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

    public async Task<bool> RegistrarVotoDiretoAsync(int votacaoId, int opcaoId)
    {
        ErrorMessage = null;
        SuccessMessage = null;
        var (_, err) = await _api.PostAsync<object>($"api/v1/votacoes/{votacaoId}/votos", new RegistrarVotoRequest(opcaoId));
        if (err is not null)
        {
            ErrorMessage = err;
            return false;
        }
        await CarregarAsync();
        SuccessMessage = "Voto secreto registrado com sucesso!";
        return true;
    }
}
