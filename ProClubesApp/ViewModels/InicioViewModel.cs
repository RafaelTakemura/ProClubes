using System.Collections.ObjectModel;
using System.Windows.Input;
using Microsoft.Maui.Graphics;
using ProClubesApp.Models;
using ProClubesApp.Services;

namespace ProClubesApp.ViewModels;

public class InicioViewModel : BaseViewModel
{
    private readonly MobileSessionAndApiService _api;
    private PerfilUsuarioDto? _perfilAtual;
    private ContratoAssociadoMobileDto? _contratoAtual;
    private string _saudacao = "Olá, Associado";
    private string _unidadeTexto = string.Empty;
    private string _categoriaTexto = string.Empty;
    private string _nomeCredencial = "ASSOCIADO PROCLUBES";
    private string _subtituloCredencial = "SÓCIO ASSOCIADO • TITULAR";
    private string _validadeCredencial = "12/2027";
    private string _matriculaCredencial = "MATRÍCULA #00001";
    private string _codigoQrCredencial = "PC-00001";
    private string _statusCredencialTexto = "ATIVO";
    private Color _credencialStatusBg = Color.FromArgb("#ECFDF5");
    private Color _credencialStatusBorder = Color.FromArgb("#A7F3D0");
    private Color _credencialStatusTextColor = Color.FromArgb("#047857");
    private string _reservasBadgeTexto = "0 ativas";
    private string _mensalidadeBadgeTexto = "Em dia";
    private string _eventosBadgeTexto = "0 disp.";
    private bool _exibirResumoFinanceiro;
    private string _situacaoFinanceira = "Em dia";
    private string _proximaMensalidadeResumo = "Nenhuma mensalidade em aberto";
    private bool _temProximoCompromisso;
    private string _compromissoTitulo = "Nenhum compromisso agendado";
    private string _compromissoSubtitulo = "Consulte espaços e eventos disponíveis na sua unidade";
    private string _compromissoDataHora = "Sem reservas ou eventos próximos";
    private string _compromissoRotaDestino = "//Eventos";
    private string _dependentesContagemTexto = "0 ativos";
    private bool _temDependentes;
    private int _notificacoesNaoLidas;
    private bool _possuiContratoPendente;
    private bool _possuiContratoConsultavel;
    private string _contratoResumoTexto = string.Empty;
    private string _motivoRecusaContrato = string.Empty;

    private string _perfilCpf = string.Empty;
    private string _perfilProfissao = "Não informada";
    private string _perfilEmail = string.Empty;
    private string _perfilTelefone = string.Empty;
    private string _perfilEndereco = "Não informado";
    private string _perfilAuditoria = string.Empty;

    private string _editNomeCompleto = string.Empty;
    private string _editCpfSomenteLeitura = string.Empty;
    private string? _editSexo;
    private string? _editEstadoCivil;
    private DateTime _editDataNascimento = new(2000, 1, 1);
    private string _editProfissao = string.Empty;
    private string _editTelefone = string.Empty;
    private string _editEmail = string.Empty;
    private string _editLogradouro = string.Empty;
    private string _editNumero = string.Empty;
    private string _editBairro = string.Empty;

    public IReadOnlyList<string> OpcoesSexo => LoginCadastroViewModel.OpcoesSexoPadrao;
    public IReadOnlyList<string> OpcoesEstadoCivil => LoginCadastroViewModel.OpcoesEstadoCivilPadrao;
    public LocalidadesPickerController EditLocalidades { get; } = new();

    public ObservableCollection<EventoDto> ProximosEventos { get; } = new();
    public ObservableCollection<ReservaEspacoDto> ReservasFuturas { get; } = new();
    public ObservableCollection<NotificacaoDto> NotificacoesRecentes { get; } = new();
    public ObservableCollection<DependenteMobileDto> DependentesAtivos { get; } = new();

    public string Saudacao { get => _saudacao; set => SetProperty(ref _saudacao, value); }
    public string UnidadeTexto { get => _unidadeTexto; set => SetProperty(ref _unidadeTexto, value); }
    public string CategoriaTexto { get => _categoriaTexto; set => SetProperty(ref _categoriaTexto, value); }
    public string NomeCredencial { get => _nomeCredencial; set => SetProperty(ref _nomeCredencial, value); }
    public string SubtituloCredencial { get => _subtituloCredencial; set => SetProperty(ref _subtituloCredencial, value); }
    public string ValidadeCredencial { get => _validadeCredencial; set => SetProperty(ref _validadeCredencial, value); }
    public string MatriculaCredencial { get => _matriculaCredencial; set => SetProperty(ref _matriculaCredencial, value); }
    public string CodigoQrCredencial { get => _codigoQrCredencial; set => SetProperty(ref _codigoQrCredencial, value); }
    public string StatusCredencialTexto { get => _statusCredencialTexto; set => SetProperty(ref _statusCredencialTexto, value); }
    public Color CredencialStatusBg { get => _credencialStatusBg; set => SetProperty(ref _credencialStatusBg, value); }
    public Color CredencialStatusBorder { get => _credencialStatusBorder; set => SetProperty(ref _credencialStatusBorder, value); }
    public Color CredencialStatusTextColor { get => _credencialStatusTextColor; set => SetProperty(ref _credencialStatusTextColor, value); }
    public string ReservasBadgeTexto { get => _reservasBadgeTexto; set => SetProperty(ref _reservasBadgeTexto, value); }
    public string MensalidadeBadgeTexto { get => _mensalidadeBadgeTexto; set => SetProperty(ref _mensalidadeBadgeTexto, value); }
    public string EventosBadgeTexto { get => _eventosBadgeTexto; set => SetProperty(ref _eventosBadgeTexto, value); }
    public bool ExibirResumoFinanceiro { get => _exibirResumoFinanceiro; set => SetProperty(ref _exibirResumoFinanceiro, value); }
    public string SituacaoFinanceira { get => _situacaoFinanceira; set => SetProperty(ref _situacaoFinanceira, value); }
    public string ProximaMensalidadeResumo { get => _proximaMensalidadeResumo; set => SetProperty(ref _proximaMensalidadeResumo, value); }
    public bool TemProximoCompromisso { get => _temProximoCompromisso; set => SetProperty(ref _temProximoCompromisso, value); }
    public string CompromissoTitulo { get => _compromissoTitulo; set => SetProperty(ref _compromissoTitulo, value); }
    public string CompromissoSubtitulo { get => _compromissoSubtitulo; set => SetProperty(ref _compromissoSubtitulo, value); }
    public string CompromissoDataHora { get => _compromissoDataHora; set => SetProperty(ref _compromissoDataHora, value); }
    public string CompromissoRotaDestino { get => _compromissoRotaDestino; set => SetProperty(ref _compromissoRotaDestino, value); }
    public string DependentesContagemTexto { get => _dependentesContagemTexto; set => SetProperty(ref _dependentesContagemTexto, value); }
    public bool TemDependentes { get => _temDependentes; set => SetProperty(ref _temDependentes, value); }
    public bool IsTitularPagante => _api.IsTitularPagante;

    public ContratoAssociadoMobileDto? ContratoAtual { get => _contratoAtual; set => SetProperty(ref _contratoAtual, value); }
    public bool PossuiContratoPendente { get => _possuiContratoPendente; set => SetProperty(ref _possuiContratoPendente, value); }
    public bool PossuiContratoConsultavel { get => _possuiContratoConsultavel; set => SetProperty(ref _possuiContratoConsultavel, value); }
    public string ContratoResumoTexto { get => _contratoResumoTexto; set => SetProperty(ref _contratoResumoTexto, value); }
    public string MotivoRecusaContrato { get => _motivoRecusaContrato; set => SetProperty(ref _motivoRecusaContrato, value); }

    public string PerfilCpf { get => _perfilCpf; set => SetProperty(ref _perfilCpf, value); }
    public string PerfilProfissao { get => _perfilProfissao; set => SetProperty(ref _perfilProfissao, value); }
    public string PerfilEmail { get => _perfilEmail; set => SetProperty(ref _perfilEmail, value); }
    public string PerfilTelefone { get => _perfilTelefone; set => SetProperty(ref _perfilTelefone, value); }
    public string PerfilEndereco { get => _perfilEndereco; set => SetProperty(ref _perfilEndereco, value); }
    public string PerfilAuditoria { get => _perfilAuditoria; set => SetProperty(ref _perfilAuditoria, value); }

    public string EditNomeCompleto { get => _editNomeCompleto; set => SetProperty(ref _editNomeCompleto, value); }
    public string EditCpfSomenteLeitura { get => _editCpfSomenteLeitura; set => SetProperty(ref _editCpfSomenteLeitura, value); }
    public string? EditSexo { get => _editSexo; set => SetProperty(ref _editSexo, value); }
    public string? EditEstadoCivil { get => _editEstadoCivil; set => SetProperty(ref _editEstadoCivil, value); }
    public DateTime EditDataNascimento { get => _editDataNascimento; set => SetProperty(ref _editDataNascimento, value); }
    public string EditProfissao { get => _editProfissao; set => SetProperty(ref _editProfissao, value); }
    public string EditTelefone { get => _editTelefone; set => SetProperty(ref _editTelefone, value); }
    public string EditEmail { get => _editEmail; set => SetProperty(ref _editEmail, value); }
    public string EditLogradouro { get => _editLogradouro; set => SetProperty(ref _editLogradouro, value); }
    public string EditNumero { get => _editNumero; set => SetProperty(ref _editNumero, value); }
    public string EditBairro { get => _editBairro; set => SetProperty(ref _editBairro, value); }

    public int NotificacoesNaoLidas
    {
        get => _notificacoesNaoLidas;
        set
        {
            if (SetProperty(ref _notificacoesNaoLidas, value))
                OnPropertyChanged(nameof(TemNotificacoesNaoLidas));
        }
    }
    public bool TemNotificacoesNaoLidas => _notificacoesNaoLidas > 0;

    public ICommand AtualizarCommand { get; }

    public InicioViewModel(MobileSessionAndApiService api)
    {
        _api = api;
        AtualizarCommand = new Command(async () => await CarregarAsync());
    }

    public async Task SairAsync()
    {
        await _api.LogoutAsync();
        if (Shell.Current is not null)
            await Shell.Current.GoToAsync("//Login");
    }

    public async Task CarregarAsync()
    {
        if (IsBusy) return;
        IsBusy = true;
        ErrorMessage = null;

        try
        {
            await CarregarDadosInternoAsync();
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task CarregarDadosInternoAsync()
    {
        var (resumo, doCache, err) = await _api.SincronizarOuLerCacheAsync();
        if (resumo is null)
        {
            ErrorMessage = err;
            return;
        }

        _perfilAtual = resumo.Perfil;
        IsOffline = doCache;
        SyncBannerText = doCache
            ? $"Dados de {resumo.SincronizadoEm:dd/MM/yyyy HH:mm} — somente consulta"
            : $"Sincronizado em {resumo.SincronizadoEm:dd/MM/yyyy HH:mm}";

        var matriculaNum = resumo.Perfil.AssociadoId ?? resumo.Perfil.TitularAssociadoId ?? resumo.Perfil.PessoaId;
        var primeiroSegundoNome = ObterNomeSaudacao(resumo.Perfil.NomeCompleto);

        Saudacao = $"Olá, {primeiroSegundoNome}";
        UnidadeTexto = $"{resumo.Perfil.UnidadeContextualNome} • Matrícula #{matriculaNum:D5}";
        CategoriaTexto = resumo.Perfil.CategoriaContextualNome;

        NomeCredencial = resumo.Perfil.NomeCompleto.ToUpperInvariant();
        var vinculoCred = resumo.Perfil.EhDependenteAtivo ? "DEPENDENTE" : "TITULAR";
        var catCred = (resumo.Perfil.AguardandoAceiteContrato ? "AGUARDANDO ACEITE" : (resumo.Perfil.CategoriaNome ?? "ASSOCIADO")).ToUpperInvariant();
        SubtituloCredencial = $"{catCred} • {vinculoCred}";
        ValidadeCredencial = $"12/{DateTime.Today.Year + 1}";
        MatriculaCredencial = $"MATRÍCULA #{matriculaNum:D5}";
        CodigoQrCredencial = $"PC-{matriculaNum:D5}-{resumo.Perfil.PessoaId:D4}";

        PerfilCpf = $"{resumo.Perfil.CpfMascarado} (imutável)";
        PerfilProfissao = string.IsNullOrWhiteSpace(resumo.Perfil.Profissao) ? "Não informada" : resumo.Perfil.Profissao;
        PerfilEmail = resumo.Perfil.Email;
        PerfilTelefone = resumo.Perfil.Fone;
        PerfilEndereco = FormatarEndereco(resumo.Perfil);
        PerfilAuditoria = resumo.Perfil.AuditoriaResumo;

        if (!doCache && (resumo.Perfil.AguardandoAceiteContrato || resumo.Perfil.AssociadoId.HasValue))
        {
            var (contrato, _) = await _api.GetAsync<ContratoAssociadoMobileDto>("api/v1/me/contrato");
            ContratoAtual = contrato;
        }
        else if (doCache)
        {
            ContratoAtual = null;
        }

        PossuiContratoConsultavel = ContratoAtual is not null;
        PossuiContratoPendente = resumo.Perfil.AguardandoAceiteContrato || (ContratoAtual?.EstaPendente ?? false);

        if (PossuiContratoPendente)
        {
            StatusCredencialTexto = "PENDENTE CONTRATO";
            CredencialStatusBg = Color.FromArgb("#FFFBEB");
            CredencialStatusBorder = Color.FromArgb("#FDE68A");
            CredencialStatusTextColor = Color.FromArgb("#B45309");
            ContratoResumoTexto = ContratoAtual is not null
                ? $"Contrato {ContratoAtual.NumeroContrato} • {ContratoAtual.ValorFormatado}. Leia e aceite os termos para liberar suas permissões e gerar sua mensalidade."
                : $"Contrato {resumo.Perfil.ContratoPendenteNumero} aguarda sua leitura e aceite para liberar suas permissões.";
        }
        else
        {
            StatusCredencialTexto = "ATIVO";
            CredencialStatusBg = Color.FromArgb("#ECFDF5");
            CredencialStatusBorder = Color.FromArgb("#A7F3D0");
            CredencialStatusTextColor = Color.FromArgb("#047857");
            ContratoResumoTexto = ContratoAtual is not null
                ? $"Contrato {ContratoAtual.NumeroContrato} ({ContratoAtual.Status})"
                : string.Empty;
        }

        OnPropertyChanged(nameof(IsTitularPagante));
        NotificacoesNaoLidas = resumo.NotificacoesNaoLidas;

        ExibirResumoFinanceiro = _api.IsTitularPagante;
        if (ExibirResumoFinanceiro)
        {
            if (resumo.QuantidadeMensalidadesVencidas > 0)
            {
                SituacaoFinanceira = $"Em atraso ({resumo.QuantidadeMensalidadesVencidas} vencida(s))";
                MensalidadeBadgeTexto = $"{resumo.QuantidadeMensalidadesVencidas} vencida(s)";
            }
            else if (resumo.ProximaMensalidade is not null)
            {
                SituacaoFinanceira = "Em dia (1 em aberto)";
                MensalidadeBadgeTexto = "Em dia";
            }
            else
            {
                SituacaoFinanceira = "Em dia";
                MensalidadeBadgeTexto = "Em dia";
            }
            ProximaMensalidadeResumo = resumo.ProximaMensalidade?.ResumoFormatado ?? "Nenhuma mensalidade pendente";
        }
        else
        {
            MensalidadeBadgeTexto = "Isento";
        }

        ProximosEventos.Clear();
        foreach (var ev in resumo.ProximosEventos) ProximosEventos.Add(ev);

        ReservasFuturas.Clear();
        foreach (var r in resumo.ReservasFuturas) ReservasFuturas.Add(r);

        ReservasBadgeTexto = ReservasFuturas.Count == 1 ? "1 ativa" : $"{ReservasFuturas.Count} ativas";
        EventosBadgeTexto = $"{ProximosEventos.Count} disp.";

        if (ReservasFuturas.Count > 0)
        {
            var r = ReservasFuturas[0];
            TemProximoCompromisso = true;
            CompromissoTitulo = r.EspacoNome;
            CompromissoSubtitulo = $"Reserva Confirmada • {r.UnidadeNome}";
            CompromissoDataHora = $"{r.Inicio:dd/MM/yyyy} • {r.Inicio:HH:mm} às {r.Fim:HH:mm}";
            CompromissoRotaDestino = "//Reservas";
        }
        else if (ProximosEventos.Count > 0)
        {
            var ev = ProximosEventos[0];
            TemProximoCompromisso = true;
            CompromissoTitulo = ev.Nome;
            CompromissoSubtitulo = $"Evento do Clube • {ev.UnidadeOrganizadoraNome}";
            CompromissoDataHora = $"{ev.Inicio:dd/MM/yyyy • HH:mm} ({(ev.TaxaParticipacao > 0 ? $"R$ {ev.TaxaParticipacao:N2}" : "Gratuito")})";
            CompromissoRotaDestino = "//Eventos";
        }
        else
        {
            TemProximoCompromisso = false;
            CompromissoTitulo = "Nenhum compromisso agendado";
            CompromissoSubtitulo = $"Unidade {resumo.Perfil.UnidadeContextualNome}";
            CompromissoDataHora = "Explore a agenda de espaços e eventos do clube";
            CompromissoRotaDestino = "//Eventos";
        }

        DependentesAtivos.Clear();
        if (resumo.Perfil.AssociadoId.HasValue && !doCache)
        {
            var (assocDetalhe, _) = await _api.GetAsync<AssociadoDetalheMobileDto>($"api/v1/admin/associados/{resumo.Perfil.AssociadoId.Value}");
            if (assocDetalhe?.Dependentes is not null)
            {
                foreach (var d in assocDetalhe.Dependentes.Where(x => x.Ativo || x.PendenteAprovacao))
                    DependentesAtivos.Add(d);
            }
        }
        TemDependentes = DependentesAtivos.Count > 0;
        var depAtivosCount = DependentesAtivos.Count(x => x.Ativo);
        var depPendentesCount = DependentesAtivos.Count(x => x.PendenteAprovacao);
        DependentesContagemTexto = depPendentesCount > 0
            ? $"{depAtivosCount} ativo(s), {depPendentesCount} pendente(s)"
            : (depAtivosCount == 1 ? "1 ativo" : $"{depAtivosCount} ativos");

        NotificacoesRecentes.Clear();
        foreach (var n in resumo.NotificacoesRecentes) NotificacoesRecentes.Add(n);
    }

    public async Task<bool> AceitarContratoAsync()
    {
        if (ContratoAtual is null || !ContratoAtual.EstaPendente)
        {
            ErrorMessage = "Nenhum contrato pendente encontrado para aceite.";
            return false;
        }

        ErrorMessage = null;
        SuccessMessage = null;

        var (atualizado, err) = await _api.PostAsync<ContratoAssociadoMobileDto>($"api/v1/me/contratos/{ContratoAtual.Id}/aceitar");
        if (err is not null)
        {
            ErrorMessage = err;
            return false;
        }

        ContratoAtual = atualizado;
        await CarregarDadosInternoAsync();
        SuccessMessage = $"Contrato {atualizado?.NumeroContrato} aceito com sucesso! Suas permissões e cobrança foram ativadas.";
        return true;
    }

    public async Task<bool> RecusarContratoAsync()
    {
        if (ContratoAtual is null || !ContratoAtual.EstaPendente)
        {
            ErrorMessage = "Nenhum contrato pendente encontrado para recusa.";
            return false;
        }

        ErrorMessage = null;
        SuccessMessage = null;

        var (_, err) = await _api.PostAsync<ContratoAssociadoMobileDto>(
            $"api/v1/me/contratos/{ContratoAtual.Id}/recusar",
            new ResponderContratoMobileRequest(string.IsNullOrWhiteSpace(MotivoRecusaContrato) ? null : MotivoRecusaContrato.Trim()));

        if (err is not null)
        {
            ErrorMessage = err;
            return false;
        }

        MotivoRecusaContrato = string.Empty;
        await SairAsync();
        return true;
    }

    public async Task PrepararEdicaoMeusDadosAsync()
    {
        ErrorMessage = null;
        SuccessMessage = null;
        await EditLocalidades.CarregarAsync(_api);

        if (_perfilAtual is null)
        {
            var (me, _) = await _api.GetAsync<PerfilUsuarioDto>("api/v1/me");
            if (me is not null)
                _perfilAtual = me;
        }

        if (_perfilAtual is null)
            return;

        EditNomeCompleto = _perfilAtual.NomeCompleto;
        EditCpfSomenteLeitura = _perfilAtual.CpfMascarado;
        EditSexo = OpcoesSexo.FirstOrDefault(s => string.Equals(s, _perfilAtual.Sexo, StringComparison.OrdinalIgnoreCase)) ?? OpcoesSexo[0];
        EditEstadoCivil = OpcoesEstadoCivil.FirstOrDefault(e => string.Equals(e, _perfilAtual.EstadoCivil, StringComparison.OrdinalIgnoreCase)) ?? OpcoesEstadoCivil[0];
        EditDataNascimento = _perfilAtual.DataNascimento != default
            ? _perfilAtual.DataNascimento.ToDateTime(TimeOnly.MinValue)
            : new DateTime(2000, 1, 1);
        EditProfissao = _perfilAtual.Profissao;
        EditTelefone = _perfilAtual.Fone;
        EditEmail = _perfilAtual.Email;
        EditLogradouro = _perfilAtual.Logradouro;
        EditNumero = _perfilAtual.Numero;
        EditBairro = _perfilAtual.Bairro;
        EditLocalidades.DefinirValores(_perfilAtual.UF, _perfilAtual.Cidade);
    }

    public async Task<bool> SalvarMeusDadosAsync()
    {
        ErrorMessage = null;
        SuccessMessage = null;

        if (string.IsNullOrWhiteSpace(EditNomeCompleto)
            || string.IsNullOrWhiteSpace(EditSexo)
            || string.IsNullOrWhiteSpace(EditEstadoCivil)
            || string.IsNullOrWhiteSpace(EditProfissao)
            || string.IsNullOrWhiteSpace(EditTelefone)
            || string.IsNullOrWhiteSpace(EditEmail))
        {
            ErrorMessage = "Preencha todos os campos obrigatórios (*) antes de salvar.";
            return false;
        }

        var dob = DateOnly.FromDateTime(EditDataNascimento);
        if (dob > DateOnly.FromDateTime(DateTime.Today))
        {
            ErrorMessage = "A data de nascimento não pode ser futura.";
            return false;
        }

        if (!LoginCadastroViewModel.ValidarSintaxeTelefone(EditTelefone))
        {
            ErrorMessage = "Sintaxe de telefone inválida. Informe DDD + número (10 ou 11 dígitos).";
            return false;
        }

        if (!LoginCadastroViewModel.ValidarSintaxeEmail(EditEmail))
        {
            ErrorMessage = "Sintaxe de e-mail inválida. Informe um endereço no formato usuario@dominio.com.";
            return false;
        }

        var (cidadeSel, ufSel) = EditLocalidades.ObterValores();

        var req = new AtualizarDadosCadastraisMobileRequest(
            EditNomeCompleto.Trim(),
            EditSexo,
            EditEstadoCivil,
            dob,
            EditProfissao.Trim(),
            EditTelefone.Trim(),
            EditEmail.Trim(),
            EditLogradouro.Trim(),
            EditBairro.Trim(),
            EditNumero.Trim(),
            cidadeSel,
            ufSel);

        var (perfilAtualizado, err) = await _api.PutAsync<PerfilUsuarioDto>("api/v1/me/dados-cadastrais", req);
        if (err is not null)
        {
            ErrorMessage = err;
            return false;
        }

        if (perfilAtualizado is not null)
            _perfilAtual = perfilAtualizado;

        await CarregarDadosInternoAsync();
        SuccessMessage = "Seus dados cadastrais foram atualizados com sucesso!";
        return true;
    }

    private static string FormatarEndereco(PerfilUsuarioDto p)
    {
        var partes = new List<string>();
        if (!string.IsNullOrWhiteSpace(p.Logradouro))
            partes.Add(string.IsNullOrWhiteSpace(p.Numero) ? p.Logradouro : $"{p.Logradouro}, {p.Numero}");
        if (!string.IsNullOrWhiteSpace(p.Bairro))
            partes.Add(p.Bairro);
        var cidUf = (!string.IsNullOrWhiteSpace(p.Cidade), !string.IsNullOrWhiteSpace(p.UF)) switch
        {
            (true, true) => $"{p.Cidade}/{p.UF}",
            (true, false) => p.Cidade,
            (false, true) => p.UF,
            _ => string.Empty
        };
        if (!string.IsNullOrWhiteSpace(cidUf))
            partes.Add(cidUf);

        return partes.Count > 0 ? string.Join(" — ", partes) : "Não informado";
    }

    public async Task MarcarTodasNotificacoesComoLidasAsync()
    {
        if (NotificacoesNaoLidas == 0 && !NotificacoesRecentes.Any(n => !n.Lida))
            return;

        NotificacoesNaoLidas = 0;

        if (IsOffline)
            return;

        var naoLidasIds = NotificacoesRecentes
            .Where(n => !n.Lida)
            .Select(n => n.Id)
            .ToHashSet();

        var (paginaNaoLidas, _) = await _api.GetAsync<PagedResponse<NotificacaoDto>>("api/v1/notificacoes?apenasNaoLidas=true&page=1&pageSize=50");
        if (paginaNaoLidas?.Items is not null)
        {
            foreach (var item in paginaNaoLidas.Items)
                naoLidasIds.Add(item.Id);
        }

        foreach (var id in naoLidasIds)
        {
            await _api.PatchAsync<NotificacaoDto>($"api/v1/notificacoes/{id}/leitura");
        }

        for (var i = 0; i < NotificacoesRecentes.Count; i++)
        {
            if (!NotificacoesRecentes[i].Lida)
                NotificacoesRecentes[i] = NotificacoesRecentes[i] with { Lida = true };
        }
    }

    private static string ObterNomeSaudacao(string nomeCompleto)
    {
        if (string.IsNullOrWhiteSpace(nomeCompleto)) return "Associado";
        var partes = nomeCompleto.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (partes.Length <= 2) return nomeCompleto;
        return $"{partes[0]} {partes[1]}";
    }
}
