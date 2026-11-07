using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Shapes;
using Microsoft.Maui.Graphics;
using ProClubesApp.Models;
using ProClubesApp.ViewModels;

namespace ProClubesApp.Views;

public static class ProClubesTheme
{
    public static readonly Color BrandPrimary = Color.FromArgb("#0F2C59");
    public static readonly Color BrandPrimaryHover = Color.FromArgb("#163E7A");
    public static readonly Color BrandSecondary = Color.FromArgb("#1E7E5A");
    public static readonly Color BrandAccent = Color.FromArgb("#D97706");
    public static readonly Color SurfaceApp = Color.FromArgb("#F8FAFC");
    public static readonly Color SurfaceCard = Color.FromArgb("#FFFFFF");
    public static readonly Color SurfaceSubtle = Color.FromArgb("#F1F5F9");
    public static readonly Color TextPrimary = Color.FromArgb("#0F172A");
    public static readonly Color TextSecondary = Color.FromArgb("#475569");
    public static readonly Color TextMuted = Color.FromArgb("#64748B");
    public static readonly Color BorderDefault = Color.FromArgb("#E2E8F0");
    public static readonly Color StatusSuccess = Color.FromArgb("#15803D");
    public static readonly Color StatusSuccessBg = Color.FromArgb("#DCFCE7");
    public static readonly Color StatusWarning = Color.FromArgb("#B45309");
    public static readonly Color StatusWarningBg = Color.FromArgb("#FEF3C7");
    public static readonly Color StatusDanger = Color.FromArgb("#B91C1C");
    public static readonly Color StatusDangerBg = Color.FromArgb("#FEE2E2");
    public static readonly Color StatusInfo = Color.FromArgb("#1D4ED8");
    public static readonly Color StatusInfoBg = Color.FromArgb("#DBEAFE");

    public static Border CreateCard(View content, Color? bg = null, Color? border = null)
    {
        return new Border
        {
            BackgroundColor = bg ?? SurfaceCard,
            Stroke = border ?? BorderDefault,
            StrokeThickness = 1,
            StrokeShape = new RoundRectangle { CornerRadius = new CornerRadius(16) },
            Padding = new Thickness(16),
            Margin = new Thickness(0, 0, 0, 12),
            Content = content
        };
    }

    public static Border CreateOfflineBanner(string bindingPath = nameof(BaseViewModel.SyncBannerText), string visibilityPath = nameof(BaseViewModel.IsOffline))
    {
        var label = new Label
        {
            TextColor = StatusWarning,
            FontAttributes = FontAttributes.Bold,
            FontSize = 12
        };
        label.SetBinding(Label.TextProperty, bindingPath);

        var banner = new Border
        {
            BackgroundColor = StatusWarningBg,
            Stroke = Color.FromArgb("#FDE68A"),
            StrokeThickness = 1,
            StrokeShape = new RoundRectangle { CornerRadius = new CornerRadius(12) },
            Padding = new Thickness(12, 8),
            Margin = new Thickness(0, 0, 0, 12),
            Content = label
        };
        banner.SetBinding(VisualElement.IsVisibleProperty, visibilityPath);
        return banner;
    }

    public static Border CreateStatusBanner()
    {
        var errLbl = new Label { TextColor = StatusDanger, FontSize = 13, FontAttributes = FontAttributes.Bold };
        errLbl.SetBinding(Label.TextProperty, nameof(BaseViewModel.ErrorMessage));
        var errBorder = new Border
        {
            BackgroundColor = StatusDangerBg,
            Stroke = Color.FromArgb("#FECACA"),
            StrokeShape = new RoundRectangle { CornerRadius = new CornerRadius(12) },
            Padding = new Thickness(12, 8),
            Margin = new Thickness(0, 0, 0, 8),
            Content = errLbl
        };
        errBorder.SetBinding(VisualElement.IsVisibleProperty, nameof(BaseViewModel.HasError));

        var okLbl = new Label { TextColor = StatusSuccess, FontSize = 13, FontAttributes = FontAttributes.Bold };
        okLbl.SetBinding(Label.TextProperty, nameof(BaseViewModel.SuccessMessage));
        var okBorder = new Border
        {
            BackgroundColor = StatusSuccessBg,
            Stroke = Color.FromArgb("#BBF7D0"),
            StrokeShape = new RoundRectangle { CornerRadius = new CornerRadius(12) },
            Padding = new Thickness(12, 8),
            Margin = new Thickness(0, 0, 0, 8),
            Content = okLbl
        };
        okBorder.SetBinding(VisualElement.IsVisibleProperty, nameof(BaseViewModel.HasSuccess));

        var stack = new VerticalStackLayout { Children = { errBorder, okBorder } };
        return new Border { StrokeThickness = 0, BackgroundColor = Colors.Transparent, Content = stack };
    }
}

public sealed class LoginCadastroPage : ContentPage
{
    private readonly LoginCadastroViewModel _vm;

    public LoginCadastroPage(LoginCadastroViewModel vm)
    {
        _vm = vm;
        BindingContext = _vm;
        Title = "Acesso e Cadastro";
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
                                new Label { Text = "Autosserviço do Associado e Eventos Públicos", FontSize = 12, TextColor = Color.FromArgb("#CBD5E1") }
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

        var btnAlternarForm = new Button
        {
            Text = "Solicitar Novo Cadastro de Associado",
            BackgroundColor = ProClubesTheme.SurfaceSubtle,
            TextColor = ProClubesTheme.BrandPrimary,
            CornerRadius = 10,
            Command = _vm.AlternarModoCommand
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
                btnAlternarForm
            }
        });

        var unidadePicker = new Picker
        {
            Title = "Unidade do Clube pretendida",
            TextColor = ProClubesTheme.TextPrimary,
            TitleColor = ProClubesTheme.TextMuted,
            ItemsSource = _vm.UnidadesPublicas,
            ItemDisplayBinding = new Binding(nameof(UnidadePublicaDto.Nome))
        };
        unidadePicker.SetBinding(Picker.SelectedItemProperty, nameof(LoginCadastroViewModel.UnidadeSelecionada));

        var nomeEntry = new Entry { Placeholder = "Nome completo", TextColor = ProClubesTheme.TextPrimary, PlaceholderColor = ProClubesTheme.TextMuted };
        nomeEntry.SetBinding(Entry.TextProperty, nameof(LoginCadastroViewModel.NomeCompleto));

        var cpfEntry = new Entry { Placeholder = "CPF (11 dígitos)", Keyboard = Keyboard.Numeric, TextColor = ProClubesTheme.TextPrimary, PlaceholderColor = ProClubesTheme.TextMuted };
        cpfEntry.SetBinding(Entry.TextProperty, nameof(LoginCadastroViewModel.Cpf));

        var emailCadEntry = new Entry { Placeholder = "E-mail", Keyboard = Keyboard.Email, TextColor = ProClubesTheme.TextPrimary, PlaceholderColor = ProClubesTheme.TextMuted };
        emailCadEntry.SetBinding(Entry.TextProperty, nameof(LoginCadastroViewModel.Email));

        var telEntry = new Entry { Placeholder = "Telefone celular", Keyboard = Keyboard.Telephone, TextColor = ProClubesTheme.TextPrimary, PlaceholderColor = ProClubesTheme.TextMuted };
        telEntry.SetBinding(Entry.TextProperty, nameof(LoginCadastroViewModel.Telefone));

        var senhaCadEntry = new Entry { Placeholder = "Senha de acesso (mín. 8 caracteres)", IsPassword = true, TextColor = ProClubesTheme.TextPrimary, PlaceholderColor = ProClubesTheme.TextMuted };
        senhaCadEntry.SetBinding(Entry.TextProperty, nameof(LoginCadastroViewModel.SenhaCadastro));

        var btnEnviarCadastro = new Button
        {
            Text = "Enviar Solicitação de Cadastro",
            BackgroundColor = ProClubesTheme.BrandSecondary,
            TextColor = Colors.White,
            CornerRadius = 10,
            FontAttributes = FontAttributes.Bold,
            Command = _vm.SolicitarCadastroCommand
        };

        var cadastroCard = ProClubesTheme.CreateCard(new VerticalStackLayout
        {
            Spacing = 10,
            Children =
            {
                new Label { Text = "Solicitar Cadastro de Associado", FontSize = 16, FontAttributes = FontAttributes.Bold, TextColor = ProClubesTheme.TextPrimary },
                new Label { Text = "Após o envio, você poderá acompanhar o status da sua solicitação com seu login e senha.", FontSize = 12, TextColor = ProClubesTheme.TextSecondary },
                unidadePicker,
                nomeEntry,
                cpfEntry,
                emailCadEntry,
                telEntry,
                senhaCadEntry,
                btnEnviarCadastro
            }
        });
        cadastroCard.SetBinding(VisualElement.IsVisibleProperty, nameof(LoginCadastroViewModel.ModoCadastro));

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

        var eventosPublicosList = new CollectionView
        {
            ItemsSource = _vm.EventosPublicos,
            ItemTemplate = new DataTemplate(() =>
            {
                var tit = new Label { FontAttributes = FontAttributes.Bold, FontSize = 14, TextColor = ProClubesTheme.TextPrimary };
                tit.SetBinding(Label.TextProperty, nameof(EventoDto.Nome));
                var resumo = new Label { FontSize = 12, TextColor = ProClubesTheme.TextSecondary };
                resumo.SetBinding(Label.TextProperty, nameof(EventoDto.ResumoDataTaxa));

                return ProClubesTheme.CreateCard(new VerticalStackLayout { Spacing = 4, Children = { tit, resumo } }, ProClubesTheme.SurfaceSubtle);
            })
        };

        var publicosCard = ProClubesTheme.CreateCard(new VerticalStackLayout
        {
            Spacing = 8,
            Children =
            {
                new Label { Text = "Eventos Públicos do Clube (Consulta Aberta)", FontSize = 15, FontAttributes = FontAttributes.Bold, TextColor = ProClubesTheme.BrandPrimary },
                eventosPublicosList
            }
        });

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
                    loginCard,
                    cadastroCard,
                    publicosCard
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

public sealed class InicioMobilePage : ContentPage
{
    private readonly InicioViewModel _vm;

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
            Content = lblCategoria
        };

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
                badgeBorder
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

        Content = new ScrollView
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
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _vm.CarregarAsync();
    }
}

public sealed class EventosMobilePage : ContentPage
{
    private readonly EventosViewModel _vm;

    public EventosMobilePage(EventosViewModel vm)
    {
        _vm = vm;
        BindingContext = _vm;
        Title = "Eventos";
        BackgroundColor = ProClubesTheme.SurfaceApp;

        var list = new CollectionView
        {
            ItemsSource = _vm.Eventos,
            ItemTemplate = new DataTemplate(() =>
            {
                var tit = new Label { FontSize = 16, FontAttributes = FontAttributes.Bold, TextColor = ProClubesTheme.TextPrimary };
                tit.SetBinding(Label.TextProperty, nameof(EventoDto.Nome));

                var desc = new Label { FontSize = 13, TextColor = ProClubesTheme.TextSecondary };
                desc.SetBinding(Label.TextProperty, nameof(EventoDto.Descricao));

                var resumo = new Label { FontSize = 12, FontAttributes = FontAttributes.Bold, TextColor = ProClubesTheme.BrandSecondary };
                resumo.SetBinding(Label.TextProperty, nameof(EventoDto.ResumoDataTaxa));

                return ProClubesTheme.CreateCard(new VerticalStackLayout
                {
                    Spacing = 6,
                    Children = { tit, desc, resumo }
                });
            })
        };

        Content = new ScrollView
        {
            Padding = new Thickness(16),
            Content = new VerticalStackLayout
            {
                Children =
                {
                    ProClubesTheme.CreateOfflineBanner(),
                    ProClubesTheme.CreateStatusBanner(),
                    new Label { Text = "Agenda de Eventos do Clube", FontSize = 18, FontAttributes = FontAttributes.Bold, TextColor = ProClubesTheme.TextPrimary, Margin = new Thickness(0,0,0,8) },
                    list
                }
            }
        };
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _vm.CarregarAsync();
    }
}

public sealed class ReservasMobilePage : ContentPage
{
    private readonly ReservasViewModel _vm;

    public ReservasMobilePage(ReservasViewModel vm)
    {
        _vm = vm;
        BindingContext = _vm;
        Title = "Reservas";
        BackgroundColor = ProClubesTheme.SurfaceApp;

        var espacoPicker = new Picker
        {
            Title = "Selecione o Espaço do Clube",
            TextColor = ProClubesTheme.TextPrimary,
            TitleColor = ProClubesTheme.TextMuted,
            ItemsSource = _vm.Espacos,
            ItemDisplayBinding = new Binding(nameof(EspacoDto.Nome))
        };
        espacoPicker.SetBinding(Picker.SelectedItemProperty, nameof(ReservasViewModel.EspacoSelecionado));

        var datePicker = new DatePicker { Format = "dd/MM/yyyy", TextColor = ProClubesTheme.TextPrimary };
        datePicker.SetBinding(DatePicker.DateProperty, nameof(ReservasViewModel.DataReserva));

        var horaIniEntry = new Entry { Placeholder = "Hora início (HH:mm)", TextColor = ProClubesTheme.TextPrimary, PlaceholderColor = ProClubesTheme.TextMuted };
        horaIniEntry.SetBinding(Entry.TextProperty, nameof(ReservasViewModel.HoraInicio));

        var horaFimEntry = new Entry { Placeholder = "Hora fim (HH:mm)", TextColor = ProClubesTheme.TextPrimary, PlaceholderColor = ProClubesTheme.TextMuted };
        horaFimEntry.SetBinding(Entry.TextProperty, nameof(ReservasViewModel.HoraFim));

        var btnReservar = new Button
        {
            Text = "Confirmar Reserva Automática",
            BackgroundColor = ProClubesTheme.BrandPrimary,
            TextColor = Colors.White,
            CornerRadius = 10,
            FontAttributes = FontAttributes.Bold,
            Command = _vm.CriarReservaCommand
        };

        var novaReservaCard = ProClubesTheme.CreateCard(new VerticalStackLayout
        {
            Spacing = 8,
            Children =
            {
                new Label { Text = "Nova Reserva de Espaço", FontSize = 16, FontAttributes = FontAttributes.Bold, TextColor = ProClubesTheme.TextPrimary },
                new Label { Text = "Confirmação imediata se o horário estiver livre e dentro das regras do espaço.", FontSize = 12, TextColor = ProClubesTheme.TextSecondary },
                espacoPicker,
                datePicker,
                horaIniEntry,
                horaFimEntry,
                btnReservar
            }
        });
        novaReservaCard.SetBinding(VisualElement.IsVisibleProperty, nameof(ReservasViewModel.PermiteOperarReserva));

        var minhasList = new CollectionView
        {
            ItemsSource = _vm.MinhasReservas,
            ItemTemplate = new DataTemplate(() =>
            {
                var esp = new Label { FontAttributes = FontAttributes.Bold, FontSize = 15, TextColor = ProClubesTheme.TextPrimary };
                esp.SetBinding(Label.TextProperty, nameof(ReservaEspacoDto.EspacoNome));

                var per = new Label { FontSize = 13, TextColor = ProClubesTheme.TextSecondary };
                per.SetBinding(Label.TextProperty, nameof(ReservaEspacoDto.PeriodoFormatado));

                // Badge verde quando Confirmada
                var badgeConfirmada = new Border
                {
                    BackgroundColor = ProClubesTheme.StatusSuccessBg,
                    Stroke = Color.FromArgb("#BBF7D0"),
                    StrokeThickness = 1,
                    StrokeShape = new RoundRectangle { CornerRadius = new CornerRadius(999) },
                    Padding = new Thickness(10, 4),
                    HorizontalOptions = LayoutOptions.Start,
                    Content = new Label
                    {
                        Text = "✔ RESERVA CONFIRMADA",
                        FontSize = 11,
                        FontAttributes = FontAttributes.Bold,
                        TextColor = ProClubesTheme.StatusSuccess
                    }
                };
                badgeConfirmada.SetBinding(VisualElement.IsVisibleProperty, nameof(ReservaEspacoDto.PodeCancelar));

                // Alerta vermelho destacado quando Cancelada
                var lblMotivo = new Label
                {
                    FontSize = 12,
                    TextColor = ProClubesTheme.StatusDanger
                };
                lblMotivo.SetBinding(Label.TextProperty, nameof(ReservaEspacoDto.MotivoCancelamentoFormatado));
                lblMotivo.SetBinding(VisualElement.IsVisibleProperty, nameof(ReservaEspacoDto.TemJustificativa));

                var boxCancelada = new Border
                {
                    BackgroundColor = ProClubesTheme.StatusDangerBg,
                    Stroke = Color.FromArgb("#FECACA"),
                    StrokeThickness = 1,
                    StrokeShape = new RoundRectangle { CornerRadius = new CornerRadius(10) },
                    Padding = new Thickness(10, 8),
                    Margin = new Thickness(0, 2, 0, 2),
                    Content = new VerticalStackLayout
                    {
                        Spacing = 4,
                        Children =
                        {
                            new Label
                            {
                                Text = "✖ RESERVA CANCELADA",
                                FontSize = 12,
                                FontAttributes = FontAttributes.Bold,
                                TextColor = ProClubesTheme.StatusDanger
                            },
                            lblMotivo
                        }
                    }
                };
                boxCancelada.SetBinding(VisualElement.IsVisibleProperty, nameof(ReservaEspacoDto.EstaCancelada));

                // Botão Cancelar só aparece para reservas ativas/confirmadas
                var btnCancel = new Button
                {
                    Text = "Cancelar Reserva (mín. 24h)",
                    BackgroundColor = ProClubesTheme.StatusDangerBg,
                    TextColor = ProClubesTheme.StatusDanger,
                    CornerRadius = 8,
                    FontSize = 12,
                    FontAttributes = FontAttributes.Bold,
                    Command = _vm.CancelarReservaCommand
                };
                btnCancel.SetBinding(Button.CommandParameterProperty, ".");
                btnCancel.SetBinding(VisualElement.IsVisibleProperty, nameof(ReservaEspacoDto.PodeCancelar));

                return ProClubesTheme.CreateCard(new VerticalStackLayout
                {
                    Spacing = 6,
                    Children = { esp, per, badgeConfirmada, boxCancelada, btnCancel }
                });
            })
        };

        Content = new ScrollView
        {
            Padding = new Thickness(16),
            Content = new VerticalStackLayout
            {
                Children =
                {
                    ProClubesTheme.CreateOfflineBanner(),
                    ProClubesTheme.CreateStatusBanner(),
                    novaReservaCard,
                    new Label { Text = "Minhas Reservas", FontSize = 16, FontAttributes = FontAttributes.Bold, TextColor = ProClubesTheme.TextPrimary, Margin = new Thickness(0,4,0,8) },
                    minhasList
                }
            }
        };
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _vm.CarregarAsync();
    }
}

public sealed class FinanceiroMobilePage : ContentPage
{
    private readonly FinanceiroViewModel _vm;

    public FinanceiroMobilePage(FinanceiroViewModel vm)
    {
        _vm = vm;
        BindingContext = _vm;
        Title = "Financeiro";
        BackgroundColor = ProClubesTheme.SurfaceApp;

        var list = new CollectionView
        {
            ItemsSource = _vm.Mensalidades,
            ItemTemplate = new DataTemplate(() =>
            {
                var resumo = new Label { FontSize = 14, FontAttributes = FontAttributes.Bold, TextColor = ProClubesTheme.TextPrimary };
                resumo.SetBinding(Label.TextProperty, nameof(MensalidadeDto.ResumoFormatado));

                var valOrig = new Label { FontSize = 12, TextColor = ProClubesTheme.TextSecondary };
                valOrig.SetBinding(Label.TextProperty, nameof(MensalidadeDto.ValorPrincipal), stringFormat: "Principal: R$ {0:N2}");

                var status = new Label { FontSize = 12, FontAttributes = FontAttributes.Bold, TextColor = ProClubesTheme.BrandSecondary };
                status.SetBinding(Label.TextProperty, nameof(MensalidadeDto.SituacaoExibicao), stringFormat: "Situação: {0}");

                return ProClubesTheme.CreateCard(new VerticalStackLayout
                {
                    Spacing = 4,
                    Children = { resumo, valOrig, status }
                });
            })
        };

        Content = new ScrollView
        {
            Padding = new Thickness(16),
            Content = new VerticalStackLayout
            {
                Children =
                {
                    ProClubesTheme.CreateOfflineBanner(),
                    ProClubesTheme.CreateStatusBanner(),
                    new Label { Text = "Histórico de Mensalidades", FontSize = 18, FontAttributes = FontAttributes.Bold, TextColor = ProClubesTheme.TextPrimary },
                    new Label { Text = "Consulta de cobranças, encargos e pagamentos registrados pela tesouraria.", FontSize = 12, TextColor = ProClubesTheme.TextSecondary, Margin = new Thickness(0,0,0,12) },
                    list
                }
            }
        };
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _vm.CarregarAsync();
    }
}

public sealed class OuvidoriaMobilePage : ContentPage
{
    private readonly OuvidoriaViewModel _vm;

    public OuvidoriaMobilePage(OuvidoriaViewModel vm)
    {
        _vm = vm;
        BindingContext = _vm;
        Title = "Ouvidoria";
        BackgroundColor = ProClubesTheme.SurfaceApp;

        var btnNova = new Button
        {
            Text = "+ Nova Manifestação",
            BackgroundColor = ProClubesTheme.BrandPrimary,
            TextColor = Colors.White,
            CornerRadius = 10,
            FontAttributes = FontAttributes.Bold,
            Command = _vm.AlternarFormularioNovaCommand
        };

        var btnAtualizar = new Button
        {
            Text = "Atualizar Lista e Respostas",
            BackgroundColor = ProClubesTheme.SurfaceSubtle,
            TextColor = ProClubesTheme.BrandPrimary,
            CornerRadius = 10,
            FontSize = 12,
            Command = _vm.AtualizarCommand
        };

        var catPicker = new Picker
        {
            Title = "Categoria da Manifestação",
            TextColor = ProClubesTheme.TextPrimary,
            TitleColor = ProClubesTheme.TextMuted,
            ItemsSource = _vm.CategoriasManifestacao,
            ItemDisplayBinding = new Binding(nameof(CategoriaManifestacaoDto.Nome))
        };
        catPicker.SetBinding(Picker.SelectedItemProperty, nameof(OuvidoriaViewModel.CategoriaSelecionada));

        var assuntoEntry = new Entry { Placeholder = "Assunto da manifestação", TextColor = ProClubesTheme.TextPrimary, PlaceholderColor = ProClubesTheme.TextMuted };
        assuntoEntry.SetBinding(Entry.TextProperty, nameof(OuvidoriaViewModel.AssuntoManifestacao));

        var descManifEditor = new Editor { Placeholder = "Descreva sua dúvida, sugestão ou reclamação...", HeightRequest = 90, TextColor = ProClubesTheme.TextPrimary, PlaceholderColor = ProClubesTheme.TextMuted };
        descManifEditor.SetBinding(Editor.TextProperty, nameof(OuvidoriaViewModel.DescricaoManifestacao));

        var btnEnviarManif = new Button
        {
            Text = "Enviar Manifestação",
            BackgroundColor = ProClubesTheme.BrandSecondary,
            TextColor = Colors.White,
            CornerRadius = 10,
            FontAttributes = FontAttributes.Bold,
            Command = _vm.EnviarManifestacaoCommand
        };

        var formCard = ProClubesTheme.CreateCard(new VerticalStackLayout
        {
            Spacing = 8,
            Children =
            {
                new Label { Text = "Registrar Nova Manifestação", FontSize = 16, FontAttributes = FontAttributes.Bold, TextColor = ProClubesTheme.TextPrimary },
                catPicker,
                assuntoEntry,
                descManifEditor,
                btnEnviarManif
            }
        });
        formCard.SetBinding(VisualElement.IsVisibleProperty, nameof(OuvidoriaViewModel.ExibirFormularioNova));

        // Card de Detalhes + Respostas da Diretoria quando o usuário clica em uma manifestação
        var lblDetTitulo = new Label { FontSize = 16, FontAttributes = FontAttributes.Bold, TextColor = ProClubesTheme.BrandPrimary };
        lblDetTitulo.SetBinding(Label.TextProperty, "ManifestacaoSelecionada.Titulo");

        var lblDetStatus = new Label { FontSize = 12, FontAttributes = FontAttributes.Bold, TextColor = ProClubesTheme.BrandSecondary };
        lblDetStatus.SetBinding(Label.TextProperty, "ManifestacaoSelecionada.StatusBadge");

        var lblDetDesc = new Label { FontSize = 13, TextColor = ProClubesTheme.TextPrimary };
        lblDetDesc.SetBinding(Label.TextProperty, "ManifestacaoSelecionada.Descricao");

        var lblSemResp = new Label
        {
            Text = "Ainda não há respostas registradas pela diretoria para esta manifestação.",
            FontSize = 12,
            FontAttributes = FontAttributes.Italic,
            TextColor = ProClubesTheme.TextSecondary
        };
        lblSemResp.SetBinding(VisualElement.IsVisibleProperty, nameof(OuvidoriaViewModel.SemRespostasNaSelecionada));

        var respostasContainer = new VerticalStackLayout { Spacing = 8 };
        respostasContainer.SetBinding(BindableLayout.ItemsSourceProperty, nameof(OuvidoriaViewModel.RespostasDaSelecionada));
        BindableLayout.SetItemTemplate(respostasContainer, new DataTemplate(() =>
        {
            var cab = new Label { FontSize = 11, FontAttributes = FontAttributes.Bold, TextColor = ProClubesTheme.BrandPrimary };
            cab.SetBinding(Label.TextProperty, nameof(RespostaManifestacaoDto.CabecalhoFormatado));

            var msg = new Label { FontSize = 13, TextColor = ProClubesTheme.TextPrimary };
            msg.SetBinding(Label.TextProperty, nameof(RespostaManifestacaoDto.Mensagem));

            return new Border
            {
                BackgroundColor = ProClubesTheme.SurfaceCard,
                Stroke = ProClubesTheme.BorderDefault,
                StrokeThickness = 1,
                StrokeShape = new RoundRectangle { CornerRadius = new CornerRadius(10) },
                Padding = new Thickness(12, 8),
                Content = new VerticalStackLayout { Spacing = 4, Children = { cab, msg } }
            };
        }));

        var btnFecharDet = new Button
        {
            Text = "Fechar Respostas",
            BackgroundColor = ProClubesTheme.SurfaceCard,
            TextColor = ProClubesTheme.TextSecondary,
            CornerRadius = 8,
            FontSize = 12,
            Command = _vm.FecharDetalhesCommand
        };

        var detalhesCard = ProClubesTheme.CreateCard(new VerticalStackLayout
        {
            Spacing = 8,
            Children =
            {
                new Label { Text = "Respostas da Diretoria (Portal Web)", FontSize = 13, FontAttributes = FontAttributes.Bold, TextColor = ProClubesTheme.StatusInfo },
                lblDetTitulo,
                lblDetStatus,
                lblDetDesc,
                new BoxView { HeightRequest = 1, Color = ProClubesTheme.BorderDefault },
                lblSemResp,
                respostasContainer,
                btnFecharDet
            }
        }, ProClubesTheme.StatusInfoBg, Color.FromArgb("#BFDBFE"));
        detalhesCard.SetBinding(VisualElement.IsVisibleProperty, nameof(OuvidoriaViewModel.TemManifestacaoSelecionada));

        // Lista de cards clicáveis com as manifestações do associado
        var list = new CollectionView
        {
            ItemsSource = _vm.MinhasManifestacoes,
            ItemTemplate = new DataTemplate(() =>
            {
                var tit = new Label { FontSize = 15, FontAttributes = FontAttributes.Bold, TextColor = ProClubesTheme.TextPrimary };
                tit.SetBinding(Label.TextProperty, nameof(ManifestacaoDto.Titulo));

                var meta = new Label { FontSize = 12, TextColor = ProClubesTheme.TextSecondary };
                meta.SetBinding(Label.TextProperty, nameof(ManifestacaoDto.ResumoMeta));

                var statusBadge = new Label { FontSize = 12, FontAttributes = FontAttributes.Bold, TextColor = ProClubesTheme.BrandSecondary };
                statusBadge.SetBinding(Label.TextProperty, nameof(ManifestacaoDto.StatusBadge));

                var btnVerRespostas = new Button
                {
                    Text = "Ver Respostas da Diretoria",
                    BackgroundColor = ProClubesTheme.SurfaceSubtle,
                    TextColor = ProClubesTheme.BrandPrimary,
                    CornerRadius = 8,
                    FontSize = 12,
                    FontAttributes = FontAttributes.Bold,
                    Command = _vm.SelecionarManifestacaoCommand
                };
                btnVerRespostas.SetBinding(Button.CommandParameterProperty, ".");

                var card = ProClubesTheme.CreateCard(new VerticalStackLayout
                {
                    Spacing = 6,
                    Children = { tit, meta, statusBadge, btnVerRespostas }
                });

                var tap = new TapGestureRecognizer { Command = _vm.SelecionarManifestacaoCommand };
                tap.SetBinding(TapGestureRecognizer.CommandParameterProperty, ".");
                card.GestureRecognizers.Add(tap);

                return card;
            })
        };

        Content = new ScrollView
        {
            Padding = new Thickness(16),
            Content = new VerticalStackLayout
            {
                Spacing = 8,
                Children =
                {
                    ProClubesTheme.CreateOfflineBanner(),
                    ProClubesTheme.CreateStatusBanner(),
                    new Label { Text = "Canal de Ouvidoria", FontSize = 18, FontAttributes = FontAttributes.Bold, TextColor = ProClubesTheme.TextPrimary },
                    new Label { Text = "Envie solicitações, sugestões ou reclamações e toque em qualquer card abaixo para consultar as respostas da diretoria.", FontSize = 12, TextColor = ProClubesTheme.TextSecondary },
                    btnNova,
                    btnAtualizar,
                    formCard,
                    detalhesCard,
                    new Label { Text = "Minhas Manifestações", FontSize = 16, FontAttributes = FontAttributes.Bold, TextColor = ProClubesTheme.TextPrimary, Margin = new Thickness(0, 6, 0, 2) },
                    list
                }
            }
        };
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _vm.CarregarAsync();
    }
}

public sealed class MaisGovernancaMobilePage : ContentPage
{
    private readonly MaisGovernancaViewModel _vm;

    public MaisGovernancaMobilePage(MaisGovernancaViewModel vm)
    {
        _vm = vm;
        BindingContext = _vm;
        Title = "Votações e Conta";
        BackgroundColor = ProClubesTheme.SurfaceApp;

        var votacoesList = new CollectionView
        {
            ItemsSource = _vm.Votacoes,
            ItemTemplate = new DataTemplate(() =>
            {
                var tit = new Label { FontAttributes = FontAttributes.Bold, FontSize = 15, TextColor = ProClubesTheme.TextPrimary };
                tit.SetBinding(Label.TextProperty, nameof(VotacaoDto.Titulo));

                var desc = new Label { FontSize = 12, TextColor = ProClubesTheme.TextSecondary };
                desc.SetBinding(Label.TextProperty, nameof(VotacaoDto.Descricao));

                var st = new Label { FontSize = 12, FontAttributes = FontAttributes.Bold, TextColor = ProClubesTheme.BrandSecondary };
                st.SetBinding(Label.TextProperty, nameof(VotacaoDto.Estado), stringFormat: "Estado: {0}");

                var opcoesContainer = new VerticalStackLayout { Spacing = 6 };
                opcoesContainer.SetBinding(BindableLayout.ItemsSourceProperty, nameof(VotacaoDto.Opcoes));
                BindableLayout.SetItemTemplate(opcoesContainer, new DataTemplate(() =>
                {
                    var btnOpcao = new Button
                    {
                        BackgroundColor = ProClubesTheme.BrandPrimary,
                        TextColor = Colors.White,
                        CornerRadius = 8,
                        FontSize = 12
                    };
                    btnOpcao.SetBinding(Button.TextProperty, nameof(OpcaoVotacaoDto.Texto), stringFormat: "Votar em: {0}");
                    btnOpcao.Clicked += async (s, _) =>
                    {
                        if (s is Button b && b.BindingContext is OpcaoVotacaoDto op && b.Parent?.BindingContext is VotacaoDto vot)
                        {
                            await _vm.RegistrarVotoDiretoAsync(vot.Id, op.Id);
                        }
                    };
                    return btnOpcao;
                }));

                return ProClubesTheme.CreateCard(new VerticalStackLayout
                {
                    Spacing = 6,
                    Children = { tit, desc, st, opcoesContainer }
                });
            })
        };

        var votacoesSection = ProClubesTheme.CreateCard(new VerticalStackLayout
        {
            Spacing = 8,
            Children =
            {
                new Label { Text = "Assembleias e Votações Digitais", FontSize = 16, FontAttributes = FontAttributes.Bold, TextColor = ProClubesTheme.BrandPrimary },
                new Label { Text = "Exclusivo para Associado Contribuinte elegível no congelamento do colégio eleitoral.", FontSize = 12, TextColor = ProClubesTheme.TextSecondary },
                votacoesList
            }
        });
        votacoesSection.SetBinding(VisualElement.IsVisibleProperty, nameof(MaisGovernancaViewModel.PodeVotar));

        var btnSair = new Button
        {
            Text = "Encerrar Sessão e Sair",
            BackgroundColor = ProClubesTheme.StatusDangerBg,
            TextColor = ProClubesTheme.StatusDanger,
            CornerRadius = 10,
            FontAttributes = FontAttributes.Bold,
            Command = _vm.SairCommand
        };

        Content = new ScrollView
        {
            Padding = new Thickness(16),
            Content = new VerticalStackLayout
            {
                Children =
                {
                    ProClubesTheme.CreateOfflineBanner(),
                    ProClubesTheme.CreateStatusBanner(),
                    votacoesSection,
                    btnSair
                }
            }
        };
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _vm.CarregarAsync();
    }
}
