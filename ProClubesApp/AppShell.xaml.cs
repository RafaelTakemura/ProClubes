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
        var currentContent = CurrentItem?.CurrentItem?.CurrentItem;

        if (autenticadoPleno)
        {
            TabInicio.IsVisible = true;
            TabEventos.IsVisible = true;
            TabReservas.IsVisible = titularPagante;
            TabFinanceiro.IsVisible = titularPagante;
            TabOuvidoria.IsVisible = titularPagante;
            TabMais.IsVisible = true;

            if (currentContent == TabLogin || currentContent == TabCadastro)
            {
                CurrentItem = TabInicio;
            }

            TabLogin.IsVisible = false;
            TabCadastro.IsVisible = false;
        }
        else
        {
            TabLogin.IsVisible = true;
            TabCadastro.IsVisible = !_session.IsRestritaCadastro;
            TabEventos.IsVisible = true;

            if (currentContent != TabLogin && currentContent != TabCadastro && currentContent != TabEventos)
            {
                CurrentItem = TabLogin;
            }

            TabInicio.IsVisible = false;
            TabReservas.IsVisible = false;
            TabFinanceiro.IsVisible = false;
            TabOuvidoria.IsVisible = false;
            TabMais.IsVisible = false;
        }
    }
}
