using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Shapes;
using Microsoft.Maui.Graphics;
using ProClubesApp.ViewModels;

namespace ProClubesApp.Views;

public sealed class LoginCadastroPage : ContentPage
{
    private readonly LoginCadastroViewModel _vm;

    public LoginCadastroPage(LoginCadastroViewModel vm)
    {
        _vm = vm;
        BindingContext = _vm;
        Title = "Login";
        BackgroundColor = ProClubesTheme.SurfaceApp;

        var headerCard = ProClubesTheme.CreateCard(new VerticalStackLayout
        {
            Spacing = 6,
            Children =
            {
                new HorizontalStackLayout
                {
                    Spacing = 12,
                    VerticalOptions = LayoutOptions.Center,
                    Children =
                    {
                        new Image { Source = "proclubes_logo.png", WidthRequest = 44, HeightRequest = 44 },
                        new VerticalStackLayout
                        {
                            Children =
                            {
                                new Label { Text = "ProClubes Mobile", FontSize = 20, FontAttributes = FontAttributes.Bold, TextColor = Colors.White },
                                new Label { Text = "Autosserviço do Associado e Portal do Clube", FontSize = 12, TextColor = Color.FromArgb("#CBD5E1") }
                            }
                        }
                    }
                }
            }
        }, ProClubesTheme.BrandPrimary, ProClubesTheme.BrandPrimaryHover);

        var emailEntry = new Entry { Placeholder = "E-mail ou CPF", Keyboard = Keyboard.Email, TextColor = ProClubesTheme.TextPrimary, PlaceholderColor = ProClubesTheme.TextMuted };
        emailEntry.SetBinding(Entry.TextProperty, nameof(LoginCadastroViewModel.LoginOuEmail));

        var senhaEntry = new Entry { Placeholder = "Senha", IsPassword = true, TextColor = ProClubesTheme.TextPrimary, PlaceholderColor = ProClubesTheme.TextMuted };
        senhaEntry.SetBinding(Entry.TextProperty, nameof(LoginCadastroViewModel.Senha));

        var btnEntrar = new Button
        {
            Text = "Entrar no App",
            BackgroundColor = ProClubesTheme.BrandPrimary,
            TextColor = Colors.White,
            CornerRadius = 10,
            FontAttributes = FontAttributes.Bold,
            Command = _vm.EntrarCommand
        };

        var btnIrParaCadastro = new Button
        {
            Text = "Solicitar Novo Cadastro de Associado",
            BackgroundColor = ProClubesTheme.SurfaceSubtle,
            TextColor = ProClubesTheme.BrandPrimary,
            CornerRadius = 10,
            FontAttributes = FontAttributes.Bold,
            Command = _vm.AlternarModoCommand
        };

        var btnIrParaEventos = new Button
        {
            Text = "Consultar Eventos Públicos do Clube",
            BackgroundColor = ProClubesTheme.SurfaceSubtle,
            TextColor = ProClubesTheme.BrandSecondary,
            CornerRadius = 10,
            Command = _vm.IrParaEventosCommand
        };

        var loginCard = ProClubesTheme.CreateCard(new VerticalStackLayout
        {
            Spacing = 10,
            Children =
            {
                new Label { Text = "Entrar na Conta", FontSize = 16, FontAttributes = FontAttributes.Bold, TextColor = ProClubesTheme.TextPrimary },
                emailEntry,
                senhaEntry,
                btnEntrar,
                new BoxView { HeightRequest = 1, Color = ProClubesTheme.BorderDefault, Margin = new Thickness(0, 4) },
                btnIrParaCadastro,
                btnIrParaEventos
            }
        });

        var statusCard = new Border
        {
            BackgroundColor = ProClubesTheme.StatusWarningBg,
            Stroke = ProClubesTheme.BrandAccent,
            StrokeThickness = 1,
            StrokeShape = new RoundRectangle { CornerRadius = new CornerRadius(16) },
            Padding = new Thickness(16),
            Margin = new Thickness(0, 0, 0, 12)
        };
        var lblStatusTit = new Label { Text = "Acompanhamento de Solicitação de Cadastro", FontSize = 16, FontAttributes = FontAttributes.Bold, TextColor = ProClubesTheme.StatusWarning };
        var lblStatusVal = new Label { FontSize = 14, FontAttributes = FontAttributes.Bold, TextColor = ProClubesTheme.TextPrimary };
        lblStatusVal.SetBinding(Label.TextProperty, "StatusCadastroAtual.Status", stringFormat: "Status atual: {0}");
        var lblMotivoRej = new Label { FontSize = 13, TextColor = ProClubesTheme.StatusDanger };
        lblMotivoRej.SetBinding(Label.TextProperty, "StatusCadastroAtual.JustificativaRejeicao", stringFormat: "Justificativa: {0}");

        var btnReenviar = new Button
        {
            Text = "Reenviar Solicitação Corrigida (mesmo CPF)",
            BackgroundColor = ProClubesTheme.BrandPrimary,
            TextColor = Colors.White,
            CornerRadius = 10,
            Command = _vm.ReenviarCadastroCommand
        };
        var btnSairRestrito = new Button
        {
            Text = "Sair da Conta",
            BackgroundColor = ProClubesTheme.SurfaceCard,
            TextColor = ProClubesTheme.StatusDanger,
            CornerRadius = 10,
            Command = _vm.SairCommand
        };

        statusCard.Content = new VerticalStackLayout
        {
            Spacing = 8,
            Children = { lblStatusTit, lblStatusVal, lblMotivoRej, btnReenviar, btnSairRestrito }
        };
        statusCard.SetBinding(VisualElement.IsVisibleProperty, nameof(LoginCadastroViewModel.ExibirStatusRestrito));

        Content = new ScrollView
        {
            Padding = new Thickness(16),
            Content = new VerticalStackLayout
            {
                Children =
                {
                    headerCard,
                    ProClubesTheme.CreateStatusBanner(),
                    statusCard,
                    loginCard
                }
            }
        };
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _vm.InicializarAsync();
    }
}
