using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Shapes;
using Microsoft.Maui.Graphics;
using ProClubesApp.Models;
using ProClubesApp.ViewModels;

namespace ProClubesApp.Views;

public sealed class LoginCadastroPage : ContentPage
{
    private readonly LoginCadastroViewModel _vm;
    private readonly PopupOverlayHost _popupHost;

    public LoginCadastroPage(LoginCadastroViewModel vm)
    {
        _vm = vm;
        BindingContext = _vm;
        Title = "Login";
        BackgroundColor = ProClubesTheme.SurfaceApp;

        var headerCard = new Border
        {
            StrokeThickness = 0,
            StrokeShape = new RoundRectangle { CornerRadius = new CornerRadius(16) },
            Background = new LinearGradientBrush
            {
                StartPoint = new Point(0, 0),
                EndPoint = new Point(1, 1),
                GradientStops =
                {
                    new GradientStop(ProClubesTheme.BrandPrimary, 0.0f),
                    new GradientStop(ProClubesTheme.BrandPrimaryHover, 1.0f)
                }
            },
            Shadow = new Shadow
            {
                Brush = new SolidColorBrush(ProClubesTheme.BrandPrimary),
                Offset = new Point(0, 6),
                Radius = 14,
                Opacity = 0.22f
            },
            Padding = new Thickness(20, 18),
            Content = new HorizontalStackLayout
            {
                Spacing = 14,
                VerticalOptions = LayoutOptions.Center,
                Children =
                {
                    new Image { Source = "proclubes_logo.png", WidthRequest = 44, HeightRequest = 44 },
                    new VerticalStackLayout
                    {
                        Spacing = 2,
                        Children =
                        {
                            new Label
                            {
                                Text = "PROCLUBES ENTERPRISE",
                                FontSize = 10,
                                FontAttributes = FontAttributes.Bold,
                                TextColor = Color.FromArgb("#94A3B8"),
                                CharacterSpacing = 1.4
                            },
                            new Label
                            {
                                Text = "Portal Mobile do Associado",
                                FontSize = 19,
                                FontAttributes = FontAttributes.Bold,
                                TextColor = Colors.White
                            },
                            new Label
                            {
                                Text = "Credencial digital, reservas, eventos e governança",
                                FontSize = 12,
                                TextColor = Color.FromArgb("#CBD5E1")
                            }
                        }
                    }
                }
            }
        };

        var emailEntry = new Entry
        {
            Placeholder = "E-mail ou CPF",
            Keyboard = Keyboard.Email,
            TextColor = ProClubesTheme.TextPrimary,
            PlaceholderColor = ProClubesTheme.TextMuted,
            HeightRequest = 44
        };
        emailEntry.SetBinding(Entry.TextProperty, nameof(LoginCadastroViewModel.LoginOuEmail));

        var senhaEntry = new Entry
        {
            Placeholder = "Senha",
            IsPassword = true,
            TextColor = ProClubesTheme.TextPrimary,
            PlaceholderColor = ProClubesTheme.TextMuted,
            HeightRequest = 44
        };
        senhaEntry.SetBinding(Entry.TextProperty, nameof(LoginCadastroViewModel.Senha));

        var btnEntrar = new Button
        {
            Text = "Entrar na Conta",
            BackgroundColor = ProClubesTheme.BrandPrimary,
            TextColor = Colors.White,
            CornerRadius = 10,
            HeightRequest = 44,
            FontAttributes = FontAttributes.Bold,
            Command = _vm.EntrarCommand
        };

        var btnIrParaCadastro = new Button
        {
            Text = "Solicitar Novo Cadastro de Associado",
            BackgroundColor = ProClubesTheme.SurfaceSubtle,
            TextColor = ProClubesTheme.BrandPrimary,
            CornerRadius = 10,
            HeightRequest = 42,
            FontAttributes = FontAttributes.Bold,
            Command = _vm.AlternarModoCommand
        };

        var btnIrParaEventos = new Button
        {
            Text = "Consultar Eventos Públicos do Clube",
            BackgroundColor = ProClubesTheme.SurfaceApp,
            TextColor = ProClubesTheme.BrandSecondary,
            BorderColor = ProClubesTheme.BorderDefault,
            BorderWidth = 1,
            CornerRadius = 10,
            HeightRequest = 42,
            FontAttributes = FontAttributes.Bold,
            Command = _vm.IrParaEventosCommand
        };

        var loginCard = ProClubesTheme.CreateCard(new VerticalStackLayout
        {
            Spacing = 12,
            Children =
            {
                new Label
                {
                    Text = "ACESSO À CONTA",
                    FontSize = 11,
                    FontAttributes = FontAttributes.Bold,
                    TextColor = ProClubesTheme.TextMuted,
                    CharacterSpacing = 1
                },
                new Label
                {
                    Text = "Identifique-se com seu e-mail ou CPF",
                    FontSize = 16,
                    FontAttributes = FontAttributes.Bold,
                    TextColor = ProClubesTheme.TextPrimary
                },
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
            StrokeShape = new RoundRectangle { CornerRadius = new CornerRadius(14) },
            Padding = new Thickness(16),
            Margin = new Thickness(0, 0, 0, 12)
        };
        var lblStatusTit = new Label { Text = "Acompanhamento de Solicitação de Cadastro", FontSize = 15, FontAttributes = FontAttributes.Bold, TextColor = ProClubesTheme.StatusWarning };
        var lblStatusVal = new Label { FontSize = 14, FontAttributes = FontAttributes.Bold, TextColor = ProClubesTheme.TextPrimary };
        lblStatusVal.SetBinding(Label.TextProperty, "StatusCadastroAtual.Status", stringFormat: "Status atual: {0}");
        var lblMotivoRej = new Label { FontSize = 13, TextColor = ProClubesTheme.StatusDanger };
        lblMotivoRej.SetBinding(Label.TextProperty, "StatusCadastroAtual.JustificativaRejeicao", stringFormat: "Justificativa: {0}");

        var btnAbrirPopupReenvio = new Button
        {
            Text = "Corrigir e Reenviar Solicitação (mesmo CPF)",
            BackgroundColor = ProClubesTheme.BrandPrimary,
            TextColor = Colors.White,
            CornerRadius = 10,
            FontAttributes = FontAttributes.Bold
        };
        btnAbrirPopupReenvio.Clicked += (_, _) => AbrirPopupReenvioCadastro();

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
            Children = { lblStatusTit, lblStatusVal, lblMotivoRej, btnAbrirPopupReenvio, btnSairRestrito }
        };
        statusCard.SetBinding(VisualElement.IsVisibleProperty, nameof(LoginCadastroViewModel.ExibirStatusRestrito));

        var mainScroll = new ScrollView
        {
            Content = new VerticalStackLayout
            {
                Spacing = 18,
                Padding = new Thickness(20, 18, 20, 36),
                Children =
                {
                    headerCard,
                    ProClubesTheme.CreateStatusBanner(),
                    statusCard,
                    loginCard
                }
            }
        };

        _popupHost = new PopupOverlayHost(mainScroll);
        Content = _popupHost.RootLayout;
    }

    private void AbrirPopupReenvioCadastro()
    {
        _vm.PrepararReenvioCadastro();

        var lblMotivo = new Label { FontSize = 12, FontAttributes = FontAttributes.Bold, TextColor = ProClubesTheme.StatusDanger };
        lblMotivo.SetBinding(Label.TextProperty, "StatusCadastroAtual.JustificativaRejeicao", stringFormat: "Orientação da Secretaria: {0}");

        var unidadePicker = new Picker
        {
            Title = "Unidade do Clube pretendida",
            TextColor = ProClubesTheme.TextPrimary,
            TitleColor = ProClubesTheme.TextMuted,
            ItemsSource = _vm.UnidadesPublicas,
            ItemDisplayBinding = new Binding(nameof(UnidadePublicaDto.Nome))
        };
        unidadePicker.SetBinding(Picker.SelectedItemProperty, nameof(LoginCadastroViewModel.UnidadeSelecionada));

        var nomeEntry = new Entry { Placeholder = "Nome completo corrigido", TextColor = ProClubesTheme.TextPrimary, PlaceholderColor = ProClubesTheme.TextMuted };
        nomeEntry.SetBinding(Entry.TextProperty, nameof(LoginCadastroViewModel.NomeCompleto));

        var emailEntry = new Entry { Placeholder = "E-mail corrigido", Keyboard = Keyboard.Email, TextColor = ProClubesTheme.TextPrimary, PlaceholderColor = ProClubesTheme.TextMuted };
        emailEntry.SetBinding(Entry.TextProperty, nameof(LoginCadastroViewModel.Email));

        var telEntry = new Entry { Placeholder = "Telefone celular atualizado", Keyboard = Keyboard.Telephone, TextColor = ProClubesTheme.TextPrimary, PlaceholderColor = ProClubesTheme.TextMuted };
        telEntry.SetBinding(Entry.TextProperty, nameof(LoginCadastroViewModel.Telefone));

        var btnConfirmarReenvio = new Button
        {
            Text = "Salvar e Reenviar para Análise",
            BackgroundColor = ProClubesTheme.BrandSecondary,
            TextColor = Colors.White,
            CornerRadius = 10,
            FontAttributes = FontAttributes.Bold
        };
        btnConfirmarReenvio.Clicked += async (_, _) =>
        {
            var ok = await _vm.ReenviarCadastroAsync();
            if (ok)
            {
                _popupHost.Close();
            }
        };

        var btnCancelar = new Button
        {
            Text = "Cancelar",
            BackgroundColor = ProClubesTheme.SurfaceSubtle,
            TextColor = ProClubesTheme.TextSecondary,
            CornerRadius = 10
        };
        btnCancelar.Clicked += (_, _) =>
        {
            _vm.ErrorMessage = null;
            _popupHost.Close();
        };

        var body = new VerticalStackLayout
        {
            Spacing = 10,
            Children =
            {
                ProClubesTheme.CreateStatusBanner(),
                lblMotivo,
                new Label { Text = "Atualize os dados solicitados abaixo para reenviar seu cadastro mantendo o mesmo CPF.", FontSize = 12, TextColor = ProClubesTheme.TextSecondary },
                unidadePicker,
                nomeEntry,
                emailEntry,
                telEntry,
                btnConfirmarReenvio,
                btnCancelar
            }
        };

        _popupHost.Show("Corrigir Solicitação de Cadastro", body, () => _vm.ErrorMessage = null);
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _vm.InicializarAsync();
    }
}
