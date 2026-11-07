using ProClubesApp.Services;

namespace ProClubesApp;

public partial class AppShell : Shell
{
    private readonly MobileSessionAndApiService _session;

    public AppShell(MobileSessionAndApiService session)
    {
        _session = session;
        InitializeComponent();
        _session.StateChanged += AtualizarVisibilidadeTabsPorPerfil;
        AtualizarVisibilidadeTabsPorPerfil();
    }

    private void AtualizarVisibilidadeTabsPorPerfil()
    {
        var autenticadoPleno = _session.IsAuthenticated && !_session.IsRestritaCadastro;
        var titularPagante = _session.IsTitularPagante;

        TabInicio.IsVisible = autenticadoPleno;
        TabEventos.IsVisible = true; // Eventos públicos podem ser consultados por todos
        TabReservas.IsVisible = autenticadoPleno && titularPagante;
        TabFinanceiro.IsVisible = autenticadoPleno && titularPagante;
        TabOuvidoria.IsVisible = autenticadoPleno && titularPagante;
        TabMais.IsVisible = autenticadoPleno;
    }
}
