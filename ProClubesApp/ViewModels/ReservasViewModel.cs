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
    private DateTime _dataFimReserva = DateTime.Today.AddDays(2);
    private string _horaInicio = "10:00";
    private string _horaFim = "12:00";
    private string _justificativaCancelamento = string.Empty;
    private AgendaEspacoDto? _agendaAtual;
    private bool _carregandoAgenda;

    public ObservableCollection<EspacoDto> Espacos { get; } = new();
    public ObservableCollection<ReservaEspacoDto> MinhasReservas { get; } = new();
    public ObservableCollection<SlotAgendaDto> SlotsAgenda { get; } = new();

    public EspacoDto? EspacoSelecionado
    {
        get => _espacoSelecionado;
        set
        {
            if (!SetProperty(ref _espacoSelecionado, value)) return;
            OnPropertyChanged(nameof(EspacoEhPorDia));
            OnPropertyChanged(nameof(EspacoEhPorHora));
            OnPropertyChanged(nameof(ResumoEspacoSelecionado));
            OnPropertyChanged(nameof(PrimeiraFotoEspacoSelecionado));
            OnPropertyChanged(nameof(EspacoSelecionadoTemFotos));
            if (EspacoEhPorHora && _dataFimReserva.Date != _dataReserva.Date)
                DataFimReserva = _dataReserva.Date;
            OnPropertyChanged(nameof(ValorEstimadoTexto));
        }
    }

    public DateTime DataReserva
    {
        get => _dataReserva;
        set
        {
            var normalized = value.Date;
            if (!SetProperty(ref _dataReserva, normalized)) return;
            if (EspacoEhPorHora || _dataFimReserva.Date < normalized)
                DataFimReserva = normalized;
            OnPropertyChanged(nameof(ValorEstimadoTexto));
        }
    }

    public DateTime DataFimReserva
    {
        get => _dataFimReserva;
        set
        {
            var normalized = value.Date;
            if (!SetProperty(ref _dataFimReserva, normalized)) return;
            OnPropertyChanged(nameof(ValorEstimadoTexto));
        }
    }

    public string HoraInicio
    {
        get => _horaInicio;
        set
        {
            if (!SetProperty(ref _horaInicio, value)) return;
            OnPropertyChanged(nameof(ValorEstimadoTexto));
        }
    }

    public string HoraFim
    {
        get => _horaFim;
        set
        {
            if (!SetProperty(ref _horaFim, value)) return;
            OnPropertyChanged(nameof(ValorEstimadoTexto));
        }
    }

    public string JustificativaCancelamento
    {
        get => _justificativaCancelamento;
        set => SetProperty(ref _justificativaCancelamento, value);
    }

    public AgendaEspacoDto? AgendaAtual
    {
        get => _agendaAtual;
        private set
        {
            if (!SetProperty(ref _agendaAtual, value)) return;
            OnPropertyChanged(nameof(CabecalhoAgenda));
        }
    }

    public bool CarregandoAgenda
    {
        get => _carregandoAgenda;
        private set => SetProperty(ref _carregandoAgenda, value);
    }

    public bool EspacoEhPorDia => EspacoSelecionado?.EhPorDia == true;
    public bool EspacoEhPorHora => !EspacoEhPorDia;
    public bool EspacoSelecionadoTemFotos => EspacoSelecionado?.TemFotos == true;
    public string? PrimeiraFotoEspacoSelecionado => EspacoSelecionado?.PrimeiraFotoUrlRelativa;
    public bool PermiteOperarReserva => _api.IsTitularPagante;
    public bool NaoPermiteOperarReserva => !PermiteOperarReserva;

    public string ResumoEspacoSelecionado => EspacoSelecionado is null
        ? "Selecione um espaço para visualizar detalhes."
        : $"{EspacoSelecionado.UnidadeNome} • {EspacoSelecionado.TipoLocacaoTexto} • {EspacoSelecionado.ValorFormatado} • {EspacoSelecionado.HorarioFuncionamentoTexto}";

    public string CabecalhoAgenda => AgendaAtual?.CabecalhoConsulta ?? "Selecione um espaço para consultar a agenda";

    public string ValorEstimadoTexto
    {
        get
        {
            if (EspacoSelecionado is null) return "Valor estimado: --";
            if (EspacoSelecionado.ValorReserva <= 0m) return "Valor da reserva: Gratuito";

            if (EspacoEhPorDia)
            {
                var dias = Math.Max(1, (DataFimReserva.Date - DataReserva.Date).Days + 1);
                var totalDias = dias * EspacoSelecionado.ValorReserva;
                return $"Valor estimado ({dias} dia(s)): R$ {totalDias:N2}";
            }

            if (TimeOnly.TryParse(HoraInicio, out var hIni) && TimeOnly.TryParse(HoraFim, out var hFim) && hFim > hIni)
            {
                var minutos = (decimal)(hFim - hIni).TotalMinutes;
                var horas = Math.Max(1m, Math.Ceiling(minutos / 60m));
                var totalHoras = horas * EspacoSelecionado.ValorReserva;
                return $"Valor estimado ({horas:0}h): R$ {totalHoras:N2}";
            }

            return $"Valor base: {EspacoSelecionado.ValorFormatado}";
        }
    }

    public ICommand AtualizarCommand { get; }

    public ReservasViewModel(MobileSessionAndApiService api)
    {
        _api = api;
        AtualizarCommand = new Command(async () => await CarregarAsync());
    }

    public string ResolveMediaUrl(string? urlRelativa) => _api.ResolveMediaUrl(urlRelativa);

    public void PrepararNovaReserva(EspacoDto? espacoInicial = null)
    {
        ErrorMessage = null;
        SuccessMessage = null;
        EspacoSelecionado = espacoInicial ?? EspacoSelecionado ?? Espacos.FirstOrDefault();
        if (EspacoEhPorHora)
            DataFimReserva = DataReserva.Date;
        OnPropertyChanged(nameof(ValorEstimadoTexto));
    }

    public void PrepararCancelamento()
    {
        ErrorMessage = null;
        SuccessMessage = null;
        JustificativaCancelamento = string.Empty;
    }

    public async Task CarregarAsync()
    {
        ErrorMessage = null;
        OnPropertyChanged(nameof(PermiteOperarReserva));
        OnPropertyChanged(nameof(NaoPermiteOperarReserva));

        var (espacos, _) = await _api.GetAsync<List<EspacoDto>>("api/v1/espacos");
        if (espacos is not null)
        {
            var selecionadoId = EspacoSelecionado?.Id;
            Espacos.Clear();
            foreach (var e in espacos) Espacos.Add(e);
            EspacoSelecionado = Espacos.FirstOrDefault(e => e.Id == selecionadoId) ?? Espacos.FirstOrDefault();
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

    public async Task CarregarAgendaAtualAsync()
    {
        if (EspacoSelecionado is null)
        {
            AgendaAtual = null;
            SlotsAgenda.Clear();
            return;
        }

        CarregandoAgenda = true;
        try
        {
            var dIni = DateOnly.FromDateTime(DataReserva.Date);
            var dFim = EspacoEhPorDia ? dIni.AddDays(13) : dIni;
            var url = $"api/v1/espacos/{EspacoSelecionado.Id}/agenda?dataInicio={dIni:yyyy-MM-dd}&dataFim={dFim:yyyy-MM-dd}";
            var (agenda, err) = await _api.GetAsync<AgendaEspacoDto>(url);

            SlotsAgenda.Clear();
            if (agenda is null)
            {
                AgendaAtual = null;
                if (!string.IsNullOrWhiteSpace(err))
                    ErrorMessage = err;
                return;
            }

            AgendaAtual = agenda;
            foreach (var slot in agenda.Slots)
                SlotsAgenda.Add(slot);
        }
        finally
        {
            CarregandoAgenda = false;
        }
    }

    public void SelecionarSlotAgenda(SlotAgendaDto? slot)
    {
        if (slot is null || !slot.Disponivel) return;

        ErrorMessage = null;
        if (EspacoEhPorDia)
        {
            DataReserva = slot.Inicio.Date;
            if (DataFimReserva.Date < DataReserva.Date)
                DataFimReserva = DataReserva.Date;
        }
        else
        {
            DataReserva = slot.Inicio.Date;
            DataFimReserva = slot.Inicio.Date;
            HoraInicio = slot.Inicio.ToString("HH:mm");
            HoraFim = slot.Fim.ToString("HH:mm");
        }
    }

    public async Task<bool> CriarReservaAsync()
    {
        ErrorMessage = null;
        SuccessMessage = null;
        if (EspacoSelecionado is null)
        {
            ErrorMessage = "Selecione um espaço para reservar.";
            return false;
        }

        DateTimeOffset inicio;
        DateTimeOffset fim;

        if (EspacoEhPorDia)
        {
            var dIni = DateOnly.FromDateTime(DataReserva.Date);
            var dFim = DateOnly.FromDateTime(DataFimReserva.Date);
            if (dFim < dIni)
            {
                ErrorMessage = "A data de término não pode ser anterior à data de início.";
                return false;
            }

            inicio = new DateTimeOffset(dIni.ToDateTime(EspacoSelecionado.HoraAbertura), TimeSpan.FromHours(-3));
            fim = new DateTimeOffset(dFim.ToDateTime(EspacoSelecionado.HoraFechamento), TimeSpan.FromHours(-3));
        }
        else
        {
            if (!TimeOnly.TryParse(HoraInicio, out var hIni) || !TimeOnly.TryParse(HoraFim, out var hFim))
            {
                ErrorMessage = "Horário inválido. Use o formato HH:mm.";
                return false;
            }

            if (hFim <= hIni)
            {
                ErrorMessage = "O horário de término deve ser posterior ao horário de início.";
                return false;
            }

            var d = DateOnly.FromDateTime(DataReserva.Date);
            inicio = new DateTimeOffset(d.ToDateTime(hIni), TimeSpan.FromHours(-3));
            fim = new DateTimeOffset(d.ToDateTime(hFim), TimeSpan.FromHours(-3));
        }

        var (res, err) = await _api.PostAsync<ReservaEspacoDto>("api/v1/reservas", new CriarReservaRequest(EspacoSelecionado.Id, inicio, fim));
        if (err is not null)
        {
            ErrorMessage = err;
            return false;
        }

        await CarregarAsync();
        SuccessMessage = res is { ValorTotal: > 0m }
            ? $"Reserva confirmada para {res.EspacoNome} (R$ {res.ValorTotal:N2})!"
            : $"Reserva confirmada automaticamente para {res?.EspacoNome ?? EspacoSelecionado.Nome}!";
        return true;
    }

    public async Task<bool> CancelarReservaAsync(ReservaEspacoDto? reserva)
    {
        if (reserva is null) return false;
        ErrorMessage = null;
        SuccessMessage = null;

        var motivo = string.IsNullOrWhiteSpace(JustificativaCancelamento)
            ? "Cancelado pelo associado no aplicativo"
            : JustificativaCancelamento.Trim();

        var (_, err) = await _api.PostAsync<ReservaEspacoDto>($"api/v1/reservas/{reserva.Id}/cancelamento", new { justificativa = motivo });
        if (err is not null)
        {
            ErrorMessage = err;
            return false;
        }

        await CarregarAsync();
        SuccessMessage = "Reserva cancelada com sucesso.";
        return true;
    }
}
