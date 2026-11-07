using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Shapes;
using Microsoft.Maui.Graphics;
using ProClubesApp.Models;
using ProClubesApp.ViewModels;

namespace ProClubesApp.Views;

public sealed class InicioMobilePage : ContentPage
{
    private readonly InicioViewModel _vm;
    private readonly PopupOverlayHost _popupHost;

    public InicioMobilePage(InicioViewModel vm)
    {
        _vm = vm;
        BindingContext = _vm;
        Title = "Início";
        BackgroundColor = ProClubesTheme.SurfaceApp;

        var lblNome = new Label { FontSize = 18, FontAttributes = FontAttributes.Bold, TextColor = Colors.White };
        lblNome.SetBinding(Label.TextProperty, nameof(InicioViewModel.Saudacao));

        var lblUnidade = new Label { FontSize = 13, TextColor = Color.FromArgb("#CBD5E1") };
        lblUnidade.SetBinding(Label.TextProperty, nameof(InicioViewModel.UnidadeTexto));

        var lblCategoria = new Label { FontSize = 12, FontAttributes = FontAttributes.Bold, TextColor = Colors.White };
        lblCategoria.SetBinding(Label.TextProperty, nameof(InicioViewModel.CategoriaTexto));

        var badgeBorder = new Border
        {
            BackgroundColor = ProClubesTheme.BrandSecondary,
            StrokeThickness = 0,
            StrokeShape = new RoundRectangle { CornerRadius = new CornerRadius(999) },
            Padding = new Thickness(10, 4),
            HorizontalOptions = LayoutOptions.Start,
            VerticalOptions = LayoutOptions.Center,
            Content = lblCategoria
        };

        var btnSairCard = new Button
        {
            Text = "Sair da Conta",
            BackgroundColor = ProClubesTheme.StatusDangerBg,
            TextColor = ProClubesTheme.StatusDanger,
            CornerRadius = 8,
            FontSize = 12,
            FontAttributes = FontAttributes.Bold,
            Padding = new Thickness(12, 6),
            HeightRequest = 32,
            HorizontalOptions = LayoutOptions.End,
            VerticalOptions = LayoutOptions.Center
        };
        btnSairCard.Clicked += (_, _) => AbrirPopupConfirmarLogout();

        var bottomRowGrid = new Grid
        {
            ColumnDefinitions =
            {
                new ColumnDefinition { Width = GridLength.Star },
                new ColumnDefinition { Width = GridLength.Auto }
            },
            Margin = new Thickness(0, 4, 0, 0),
            Children = { badgeBorder, btnSairCard }
        };
        Grid.SetColumn(badgeBorder, 0);
        Grid.SetColumn(btnSairCard, 1);

        var memberCard = ProClubesTheme.CreateCard(new VerticalStackLayout
        {
            Spacing = 8,
            Children =
            {
                new HorizontalStackLayout
                {
                    Spacing = 10,
                    Children =
                    {
                        new Image { Source = "proclubes_logo.png", WidthRequest = 36, HeightRequest = 36 },
                        new VerticalStackLayout { Children = { lblNome, lblUnidade } }
                    }
                },
                bottomRowGrid
            }
        }, ProClubesTheme.BrandPrimary, ProClubesTheme.BrandPrimaryHover);

        var lblSitFin = new Label { FontSize = 15, FontAttributes = FontAttributes.Bold, TextColor = ProClubesTheme.BrandSecondary };
        lblSitFin.SetBinding(Label.TextProperty, nameof(InicioViewModel.SituacaoFinanceira));
        var lblProxMens = new Label { FontSize = 13, TextColor = ProClubesTheme.TextPrimary };
        lblProxMens.SetBinding(Label.TextProperty, nameof(InicioViewModel.ProximaMensalidadeResumo));

        var finCard = ProClubesTheme.CreateCard(new VerticalStackLayout
        {
            Spacing = 4,
            Children =
            {
                new Label { Text = "Resumo Financeiro do Associado", FontSize = 13, TextColor = ProClubesTheme.TextSecondary },
                lblSitFin,
                lblProxMens
            }
        });
        finCard.SetBinding(VisualElement.IsVisibleProperty, nameof(InicioViewModel.ExibirResumoFinanceiro));

        var btnSincronizar = new Button
        {
            Text = "Sincronizar Dados Agora",
            BackgroundColor = ProClubesTheme.BrandSecondary,
            TextColor = Colors.White,
            CornerRadius = 10,
            FontAttributes = FontAttributes.Bold,
            Command = _vm.AtualizarCommand
        };

        var notifCollection = new CollectionView
        {
            ItemsSource = _vm.NotificacoesRecentes,
            ItemTemplate = new DataTemplate(() =>
            {
                var t = new Label { FontAttributes = FontAttributes.Bold, FontSize = 13, TextColor = ProClubesTheme.TextPrimary };
                t.SetBinding(Label.TextProperty, nameof(NotificacaoDto.Titulo));
                var m = new Label { FontSize = 12, TextColor = ProClubesTheme.TextSecondary };
                m.SetBinding(Label.TextProperty, nameof(NotificacaoDto.Mensagem));
                return ProClubesTheme.CreateCard(new VerticalStackLayout { Spacing = 2, Children = { t, m } }, ProClubesTheme.SurfaceSubtle);
            })
        };

        var notifCard = ProClubesTheme.CreateCard(new VerticalStackLayout
        {
            Spacing = 8,
            Children =
            {
                new Label { Text = "Notificações e Avisos Recentes", FontSize = 15, FontAttributes = FontAttributes.Bold, TextColor = ProClubesTheme.TextPrimary },
                notifCollection
            }
        });

        var mainScroll = new ScrollView
        {
            Padding = new Thickness(16),
            Content = new VerticalStackLayout
            {
                Children =
                {
                    ProClubesTheme.CreateOfflineBanner(),
                    ProClubesTheme.CreateStatusBanner(),
                    memberCard,
                    finCard,
                    btnSincronizar,
                    notifCard
                }
            }
        };

        _popupHost = new PopupOverlayHost(mainScroll);
        Content = _popupHost.RootLayout;
    }

    private void AbrirPopupConfirmarLogout()
    {
        var btnConfirmar = new Button
        {
            Text = "Encerrar Sessão e Sair",
            BackgroundColor = ProClubesTheme.StatusDanger,
            TextColor = Colors.White,
            CornerRadius = 10,
            FontAttributes = FontAttributes.Bold
        };
        btnConfirmar.Clicked += async (_, _) =>
        {
            _popupHost.Close();
            await _vm.SairAsync();
        };

        var btnCancelar = new Button
        {
            Text = "Cancelar (Continuar Conectado)",
            BackgroundColor = ProClubesTheme.SurfaceSubtle,
            TextColor = ProClubesTheme.TextSecondary,
            CornerRadius = 10
        };
        btnCancelar.Clicked += (_, _) => _popupHost.Close();

        var body = new VerticalStackLayout
        {
            Spacing = 12,
            Children =
            {
                new Label
                {
                    Text = "Deseja realmente encerrar sua sessão no aplicativo ProClubes?",
                    FontSize = 14,
                    TextColor = ProClubesTheme.TextPrimary
                },
                new Label
                {
                    Text = "Seus dados pessoais sincronizados em cache local serão limpos por segurança.",
                    FontSize = 12,
                    TextColor = ProClubesTheme.TextSecondary
                },
                btnConfirmar,
                btnCancelar
            }
        };

        _popupHost.Show("Sair da Conta", body);
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _vm.CarregarAsync();
    }
}
