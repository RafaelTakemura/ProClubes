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

        // 1. Saudação ao Associado Despoluída + Botão de Notificações no Canto Superior Direito
        var lblSaudacao = new Label
        {
            FontSize = 21,
            FontAttributes = FontAttributes.Bold,
            TextColor = ProClubesTheme.TextPrimary,
            LineBreakMode = LineBreakMode.TailTruncation
        };
        lblSaudacao.SetBinding(Label.TextProperty, nameof(InicioViewModel.Saudacao));

        var lblUnidadeMatricula = new Label
        {
            FontSize = 12,
            TextColor = ProClubesTheme.TextMuted,
            LineBreakMode = LineBreakMode.TailTruncation
        };
        lblUnidadeMatricula.SetBinding(Label.TextProperty, nameof(InicioViewModel.UnidadeTexto));

        var lblCategoriaPill = new Label
        {
            FontSize = 11,
            FontAttributes = FontAttributes.Bold,
            TextColor = ProClubesTheme.StatusWarning
        };
        lblCategoriaPill.SetBinding(Label.TextProperty, nameof(InicioViewModel.CategoriaTexto));

        var categoriaBadge = ProClubesTheme.CreateSoftTag(
            lblCategoriaPill,
            ProClubesTheme.StatusWarningBg,
            ProClubesTheme.StatusWarningBorder,
            new Thickness(10, 3));

        var lblContadorSino = new Label
        {
            FontSize = 9,
            FontAttributes = FontAttributes.Bold,
            TextColor = Colors.White,
            HorizontalOptions = LayoutOptions.Center,
            VerticalOptions = LayoutOptions.Center
        };
        lblContadorSino.SetBinding(Label.TextProperty, nameof(InicioViewModel.NotificacoesNaoLidas));

        var badgeContadorSino = new Border
        {
            BackgroundColor = ProClubesTheme.BrandAccent,
            StrokeThickness = 0,
            StrokeShape = new RoundRectangle { CornerRadius = new CornerRadius(999) },
            MinimumWidthRequest = 16,
            HeightRequest = 16,
            Padding = new Thickness(4, 0),
            HorizontalOptions = LayoutOptions.End,
            VerticalOptions = LayoutOptions.Start,
            Margin = new Thickness(0, -4, -4, 0),
            InputTransparent = true,
            Content = lblContadorSino
        };
        badgeContadorSino.SetBinding(VisualElement.IsVisibleProperty, nameof(InicioViewModel.TemNotificacoesNaoLidas));

        var sinoBtnBorder = new Border
        {
            WidthRequest = 40,
            HeightRequest = 40,
            BackgroundColor = ProClubesTheme.SurfaceCard,
            Stroke = ProClubesTheme.BorderDefault,
            StrokeThickness = 1,
            StrokeShape = new RoundRectangle { CornerRadius = new CornerRadius(11) },
            HorizontalOptions = LayoutOptions.Center,
            VerticalOptions = LayoutOptions.Center,
            Content = ProClubesTheme.CreateVectorIcon(MobileVectorIconKind.Bell, ProClubesTheme.BrandPrimary, 19)
        };

        var sinoContainer = new Grid
        {
            WidthRequest = 42,
            HeightRequest = 42,
            VerticalOptions = LayoutOptions.Center,
            Children = { sinoBtnBorder, badgeContadorSino }
        };
        var tapSino = new TapGestureRecognizer();
        tapSino.Tapped += (_, _) => AbrirPopupNotificacoes();
        sinoContainer.GestureRecognizers.Add(tapSino);
        sinoBtnBorder.GestureRecognizers.Add(tapSino);

        var topoDireitaStack = new HorizontalStackLayout
        {
            Spacing = 8,
            VerticalOptions = LayoutOptions.Center,
            Children = { categoriaBadge, sinoContainer }
        };

        var headerGrid = new Grid
        {
            ColumnDefinitions =
            {
                new ColumnDefinition { Width = GridLength.Star },
                new ColumnDefinition { Width = GridLength.Auto }
            },
            ColumnSpacing = 10,
            VerticalOptions = LayoutOptions.Center
        };
        var saudacaoStack = new VerticalStackLayout
        {
            Spacing = 3,
            Children = { lblSaudacao, lblUnidadeMatricula }
        };
        headerGrid.Add(saudacaoStack, 0, 0);
        headerGrid.Add(topoDireitaStack, 1, 0);

        // 2. Credencial Digital Minimalista
        var topoCredencialGrid = new Grid
        {
            ColumnDefinitions =
            {
                new ColumnDefinition { Width = GridLength.Star },
                new ColumnDefinition { Width = GridLength.Auto }
            }
        };
        topoCredencialGrid.Add(new Label
        {
            Text = "CREDENCIAL DIGITAL",
            FontSize = 10,
            FontAttributes = FontAttributes.Bold,
            TextColor = Color.FromArgb("#94A3B8"),
            CharacterSpacing = 1.5,
            VerticalOptions = LayoutOptions.Center
        }, 0, 0);

        topoCredencialGrid.Add(new Border
        {
            BackgroundColor = ProClubesTheme.StatusSuccessBg,
            Stroke = ProClubesTheme.StatusSuccessBorder,
            StrokeThickness = 1,
            StrokeShape = new RoundRectangle { CornerRadius = new CornerRadius(999) },
            Padding = new Thickness(10, 3),
            Content = new Label
            {
                Text = "ATIVO",
                FontSize = 10,
                FontAttributes = FontAttributes.Bold,
                TextColor = ProClubesTheme.StatusSuccess
            }
        }, 1, 0);

        var lblNomeCred = new Label
        {
            FontSize = 19,
            FontAttributes = FontAttributes.Bold,
            TextColor = Colors.White,
            CharacterSpacing = 1
        };
        lblNomeCred.SetBinding(Label.TextProperty, nameof(InicioViewModel.NomeCredencial));

        var lblSubCred = new Label
        {
            FontSize = 11,
            TextColor = Color.FromArgb("#CBD5E1")
        };
        lblSubCred.SetBinding(Label.TextProperty, nameof(InicioViewModel.SubtituloCredencial));

        var titularStack = new VerticalStackLayout
        {
            Spacing = 3,
            Children = { lblNomeCred, lblSubCred }
        };

        var lblValidadeValor = new Label
        {
            FontSize = 13,
            FontAttributes = FontAttributes.Bold,
            TextColor = Colors.White
        };
        lblValidadeValor.SetBinding(Label.TextProperty, nameof(InicioViewModel.ValidadeCredencial));

        var lblMatriculaCred = new Label
        {
            FontSize = 11,
            TextColor = Color.FromArgb("#CBD5E1"),
            Margin = new Thickness(0, 3, 0, 0)
        };
        lblMatriculaCred.SetBinding(Label.TextProperty, nameof(InicioViewModel.MatriculaCredencial));

        var validadeStack = new VerticalStackLayout
        {
            Spacing = 2,
            Children =
            {
                new Label { Text = "VALIDADE", FontSize = 9, TextColor = Color.FromArgb("#94A3B8") },
                lblValidadeValor,
                lblMatriculaCred
            }
        };

        var btnAcessoCatraca = new Border
        {
            BackgroundColor = Colors.White,
            StrokeShape = new RoundRectangle { CornerRadius = new CornerRadius(10) },
            StrokeThickness = 0,
            Padding = new Thickness(10, 7),
            VerticalOptions = LayoutOptions.End,
            Content = new HorizontalStackLayout
            {
                Spacing = 6,
                VerticalOptions = LayoutOptions.Center,
                Children =
                {
                    ProClubesTheme.CreateVectorIcon(MobileVectorIconKind.QrCode, ProClubesTheme.BrandPrimary, 16),
                    new Label { Text = "Acesso Catraca", FontSize = 11, FontAttributes = FontAttributes.Bold, TextColor = ProClubesTheme.BrandPrimary, VerticalOptions = LayoutOptions.Center }
                }
            }
        };
        var tapCatraca = new TapGestureRecognizer();
        tapCatraca.Tapped += (_, _) => AbrirPopupQrCodeCatraca();
        btnAcessoCatraca.GestureRecognizers.Add(tapCatraca);

        var rodapeCredencialGrid = new Grid
        {
            ColumnDefinitions =
            {
                new ColumnDefinition { Width = GridLength.Star },
                new ColumnDefinition { Width = GridLength.Auto }
            },
            VerticalOptions = LayoutOptions.End
        };
        rodapeCredencialGrid.Add(validadeStack, 0, 0);
        rodapeCredencialGrid.Add(btnAcessoCatraca, 1, 0);

        var credencialCard = new Border
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
            Content = new VerticalStackLayout
            {
                Padding = new Thickness(22, 20),
                Spacing = 18,
                Children =
                {
                    topoCredencialGrid,
                    titularStack,
                    rodapeCredencialGrid
                }
            }
        };

        // 3. Acesso Rápido (4 Botões em Grid 2x2 Clean)
        var quickGrid = new Grid
        {
            ColumnDefinitions =
            {
                new ColumnDefinition { Width = GridLength.Star },
                new ColumnDefinition { Width = GridLength.Star }
            },
            RowDefinitions =
            {
                new RowDefinition { Height = GridLength.Auto },
                new RowDefinition { Height = GridLength.Auto }
            },
            ColumnSpacing = 12,
            RowSpacing = 12
        };

        quickGrid.Add(CriarTileAcessoRapido(
            MobileVectorIconKind.Calendar,
            "Reservas",
            nameof(InicioViewModel.ReservasBadgeTexto),
            ProClubesTheme.StatusInfoBg,
            ProClubesTheme.StatusInfo,
            ProClubesTheme.StatusInfoBorder,
            "//Reservas",
            somenteTitular: true), 0, 0);

        quickGrid.Add(CriarTileAcessoRapido(
            MobileVectorIconKind.CreditCard,
            "Mensalidade",
            nameof(InicioViewModel.MensalidadeBadgeTexto),
            ProClubesTheme.StatusSuccessBg,
            ProClubesTheme.StatusSuccess,
            ProClubesTheme.StatusSuccessBorder,
            "//Financeiro",
            somenteTitular: true), 1, 0);

        quickGrid.Add(CriarTileAcessoRapido(
            MobileVectorIconKind.Ticket,
            "Eventos",
            nameof(InicioViewModel.EventosBadgeTexto),
            ProClubesTheme.SurfaceApp,
            ProClubesTheme.TextSecondary,
            ProClubesTheme.BorderDefault,
            "//Eventos",
            somenteTitular: false), 0, 1);

        quickGrid.Add(CriarTileAcessoRapido(
            MobileVectorIconKind.Chat,
            "Ouvidoria",
            null,
            ProClubesTheme.SurfaceApp,
            ProClubesTheme.TextSecondary,
            ProClubesTheme.BorderDefault,
            "//Ouvidoria",
            somenteTitular: true), 1, 1);

        var secaoAcessoRapido = new VerticalStackLayout
        {
            Spacing = 12,
            Children =
            {
                new Label
                {
                    Text = "ACESSO RÁPIDO",
                    FontSize = 11,
                    FontAttributes = FontAttributes.Bold,
                    TextColor = ProClubesTheme.TextMuted,
                    CharacterSpacing = 1
                },
                quickGrid
            }
        };

        // 4. Próximo Compromisso (Card Direto e Limpo)
        var lblVerDetalhes = new Label
        {
            Text = "Ver detalhes ›",
            FontSize = 12,
            FontAttributes = FontAttributes.Bold,
            TextColor = Color.FromArgb("#2563EB")
        };
        var tapDetalhes = new TapGestureRecognizer();
        tapDetalhes.Tapped += async (_, _) =>
        {
            if (Shell.Current is not null && !string.IsNullOrWhiteSpace(_vm.CompromissoRotaDestino))
                await Shell.Current.GoToAsync(_vm.CompromissoRotaDestino);
        };
        lblVerDetalhes.GestureRecognizers.Add(tapDetalhes);

        var headerCompromisso = new Grid
        {
            ColumnDefinitions =
            {
                new ColumnDefinition { Width = GridLength.Star },
                new ColumnDefinition { Width = GridLength.Auto }
            }
        };
        headerCompromisso.Add(new Label
        {
            Text = "PRÓXIMO COMPROMISSO",
            FontSize = 11,
            FontAttributes = FontAttributes.Bold,
            TextColor = ProClubesTheme.TextMuted,
            CharacterSpacing = 1
        }, 0, 0);
        headerCompromisso.Add(lblVerDetalhes, 1, 0);

        var lblCompTitulo = new Label { FontSize = 16, FontAttributes = FontAttributes.Bold, TextColor = ProClubesTheme.TextPrimary };
        lblCompTitulo.SetBinding(Label.TextProperty, nameof(InicioViewModel.CompromissoTitulo));

        var lblCompSub = new Label { FontSize = 13, TextColor = ProClubesTheme.TextMuted };
        lblCompSub.SetBinding(Label.TextProperty, nameof(InicioViewModel.CompromissoSubtitulo));

        var lblCompData = new Label { FontSize = 12, FontAttributes = FontAttributes.Bold, TextColor = ProClubesTheme.BrandPrimary };
        lblCompData.SetBinding(Label.TextProperty, nameof(InicioViewModel.CompromissoDataHora));

        var cardCompromisso = new Border
        {
            BackgroundColor = ProClubesTheme.SurfaceCard,
            Stroke = ProClubesTheme.BorderDefault,
            StrokeThickness = 1,
            StrokeShape = new RoundRectangle { CornerRadius = new CornerRadius(14) },
            Padding = new Thickness(16),
            Content = new VerticalStackLayout
            {
                Spacing = 8,
                Children =
                {
                    lblCompTitulo,
                    lblCompSub,
                    new Border
                    {
                        BackgroundColor = ProClubesTheme.SurfaceApp,
                        StrokeThickness = 0,
                        StrokeShape = new RoundRectangle { CornerRadius = new CornerRadius(8) },
                        Padding = new Thickness(10, 6),
                        Margin = new Thickness(0, 4, 0, 0),
                        Content = lblCompData
                    }
                }
            }
        };

        var secaoCompromisso = new VerticalStackLayout
        {
            Spacing = 10,
            Children = { headerCompromisso, cardCompromisso }
        };

        // 5. Dependentes Cadastrados
        var lblDepCount = new Label { FontSize = 11, TextColor = ProClubesTheme.TextMuted, VerticalOptions = LayoutOptions.Center };
        lblDepCount.SetBinding(Label.TextProperty, nameof(InicioViewModel.DependentesContagemTexto), stringFormat: "({0})");

        var lblGerenciarDeps = new Label
        {
            Text = "Gerenciar ›",
            FontSize = 12,
            FontAttributes = FontAttributes.Bold,
            TextColor = Color.FromArgb("#2563EB"),
            VerticalOptions = LayoutOptions.Center
        };
        var tapGerenciarDeps = new TapGestureRecognizer();
        tapGerenciarDeps.Tapped += async (_, _) =>
        {
            if (Shell.Current is not null)
                await Shell.Current.GoToAsync("//Dependentes");
        };
        lblGerenciarDeps.GestureRecognizers.Add(tapGerenciarDeps);

        var headerDependentes = new Grid
        {
            ColumnDefinitions =
            {
                new ColumnDefinition { Width = GridLength.Star },
                new ColumnDefinition { Width = GridLength.Auto }
            }
        };
        headerDependentes.Add(new HorizontalStackLayout
        {
            Spacing = 6,
            VerticalOptions = LayoutOptions.Center,
            Children =
            {
                new Label
                {
                    Text = "DEPENDENTES CADASTRADOS",
                    FontSize = 11,
                    FontAttributes = FontAttributes.Bold,
                    TextColor = ProClubesTheme.TextMuted,
                    CharacterSpacing = 1,
                    VerticalOptions = LayoutOptions.Center
                },
                lblDepCount
            }
        }, 0, 0);
        headerDependentes.Add(lblGerenciarDeps, 1, 0);

        var depStack = new VerticalStackLayout { Spacing = 12 };
        BindableLayout.SetItemsSource(depStack, _vm.DependentesAtivos);
        BindableLayout.SetItemTemplate(depStack, new DataTemplate(() =>
        {
            var lblIniciais = new Label
            {
                HorizontalOptions = LayoutOptions.Center,
                VerticalOptions = LayoutOptions.Center,
                FontSize = 11,
                FontAttributes = FontAttributes.Bold,
                TextColor = ProClubesTheme.TextPrimary
            };
            lblIniciais.SetBinding(Label.TextProperty, nameof(DependenteMobileDto.Iniciais));

            var avatarBorder = new Border
            {
                BackgroundColor = ProClubesTheme.SurfaceSubtle,
                StrokeThickness = 0,
                StrokeShape = new RoundRectangle { CornerRadius = new CornerRadius(999) },
                WidthRequest = 34,
                HeightRequest = 34,
                Content = lblIniciais
            };

            var lblDepNome = new Label { FontSize = 13, FontAttributes = FontAttributes.Bold, TextColor = ProClubesTheme.TextPrimary };
            lblDepNome.SetBinding(Label.TextProperty, nameof(DependenteMobileDto.NomeCompleto));

            var lblDepVinculo = new Label { FontSize = 11, TextColor = ProClubesTheme.TextMuted };
            lblDepVinculo.SetBinding(Label.TextProperty, nameof(DependenteMobileDto.VinculoFormatado));

            var rowGrid = new Grid
            {
                ColumnDefinitions =
                {
                    new ColumnDefinition { Width = GridLength.Auto },
                    new ColumnDefinition { Width = GridLength.Star },
                    new ColumnDefinition { Width = GridLength.Auto }
                },
                ColumnSpacing = 12,
                VerticalOptions = LayoutOptions.Center
            };
            rowGrid.Add(avatarBorder, 0, 0);
            rowGrid.Add(new VerticalStackLayout { Children = { lblDepNome, lblDepVinculo } }, 1, 0);
            rowGrid.Add(DependentesMobilePage.CreateStatusPill(new Thickness(9, 2), 10), 2, 0);

            return rowGrid;
        }));

        var cardDependentes = new Border
        {
            BackgroundColor = ProClubesTheme.SurfaceCard,
            Stroke = ProClubesTheme.BorderDefault,
            StrokeThickness = 1,
            StrokeShape = new RoundRectangle { CornerRadius = new CornerRadius(14) },
            Padding = new Thickness(14, 12),
            Content = depStack
        };

        var secaoDependentes = new VerticalStackLayout
        {
            Spacing = 10,
            Children = { headerDependentes, cardDependentes }
        };
        secaoDependentes.SetBinding(VisualElement.IsVisibleProperty, nameof(InicioViewModel.TemDependentes));

        // 6. Ações de Sessão
        var btnSincronizar = new Button
        {
            Text = "Sincronizar Dados",
            BackgroundColor = ProClubesTheme.SurfaceCard,
            TextColor = ProClubesTheme.BrandPrimary,
            BorderColor = ProClubesTheme.BorderDefault,
            BorderWidth = 1,
            CornerRadius = 10,
            FontSize = 13,
            FontAttributes = FontAttributes.Bold,
            HeightRequest = 42,
            Command = _vm.AtualizarCommand
        };

        var btnSairCard = new Button
        {
            Text = "Sair da Conta",
            BackgroundColor = ProClubesTheme.StatusDangerBg,
            TextColor = ProClubesTheme.StatusDanger,
            CornerRadius = 10,
            FontSize = 13,
            FontAttributes = FontAttributes.Bold,
            HeightRequest = 42
        };
        btnSairCard.Clicked += (_, _) => AbrirPopupConfirmarLogout();

        var acoesContaGrid = new Grid
        {
            ColumnDefinitions =
            {
                new ColumnDefinition { Width = GridLength.Star },
                new ColumnDefinition { Width = GridLength.Star }
            },
            ColumnSpacing = 12
        };
        acoesContaGrid.Add(btnSincronizar, 0, 0);
        acoesContaGrid.Add(btnSairCard, 1, 0);

        var mainScroll = new ScrollView
        {
            Content = new VerticalStackLayout
            {
                Spacing = 22,
                Padding = new Thickness(20, 18, 20, 36),
                Children =
                {
                    ProClubesTheme.CreateOfflineBanner(),
                    ProClubesTheme.CreateStatusBanner(),
                    headerGrid,
                    credencialCard,
                    secaoAcessoRapido,
                    secaoCompromisso,
                    secaoDependentes,
                    acoesContaGrid
                }
            }
        };

        _popupHost = new PopupOverlayHost(mainScroll);
        Content = _popupHost.RootLayout;
    }

    private Border CriarTileAcessoRapido(
        MobileVectorIconKind icone,
        string titulo,
        string? bindingBadge,
        Color badgeBg,
        Color badgeFg,
        Color badgeBorder,
        string rotaDestino,
        bool somenteTitular)
    {
        var topRow = new Grid
        {
            ColumnDefinitions =
            {
                new ColumnDefinition { Width = GridLength.Star },
                new ColumnDefinition { Width = GridLength.Auto }
            }
        };
        topRow.Add(ProClubesTheme.CreateVectorIconTile(icone), 0, 0);

        if (!string.IsNullOrWhiteSpace(bindingBadge))
        {
            var lblBadge = new Label
            {
                FontSize = 10,
                TextColor = badgeFg,
                FontAttributes = FontAttributes.Bold
            };
            lblBadge.SetBinding(Label.TextProperty, bindingBadge);

            topRow.Add(ProClubesTheme.CreateSoftTag(lblBadge, badgeBg, badgeBorder, new Thickness(8, 2)), 1, 0);
        }

        var tile = new Border
        {
            BackgroundColor = ProClubesTheme.SurfaceCard,
            Stroke = ProClubesTheme.BorderDefault,
            StrokeThickness = 1,
            StrokeShape = new RoundRectangle { CornerRadius = new CornerRadius(14) },
            Padding = new Thickness(16),
            Content = new VerticalStackLayout
            {
                Spacing = 8,
                Children =
                {
                    topRow,
                    new Label
                    {
                        Text = titulo,
                        FontAttributes = FontAttributes.Bold,
                        FontSize = 15,
                        TextColor = ProClubesTheme.TextPrimary
                    }
                }
            }
        };

        var tap = new TapGestureRecognizer();
        tap.Tapped += async (_, _) =>
        {
            if (somenteTitular && !_vm.IsTitularPagante)
            {
                await DisplayAlertAsync("Acesso Restrito", "Este módulo é exclusivo para associados titulares pagantes.", "OK");
                return;
            }
            if (Shell.Current is not null)
                await Shell.Current.GoToAsync(rotaDestino);
        };
        tile.GestureRecognizers.Add(tap);

        return tile;
    }

    private void AbrirPopupQrCodeCatraca()
    {
        var qrBox = new Border
        {
            BackgroundColor = ProClubesTheme.SurfaceCard,
            Stroke = ProClubesTheme.BrandPrimary,
            StrokeThickness = 2,
            StrokeShape = new RoundRectangle { CornerRadius = new CornerRadius(16) },
            Padding = new Thickness(24),
            HorizontalOptions = LayoutOptions.Center,
            Content = new VerticalStackLayout
            {
                Spacing = 8,
                HorizontalOptions = LayoutOptions.Center,
                Children =
                {
                    new Label
                    {
                        Text = "▣ ▤ ▥\n▤ ▣ ▤\n▥ ▤ ▣",
                        FontSize = 36,
                        FontAttributes = FontAttributes.Bold,
                        TextColor = ProClubesTheme.BrandPrimary,
                        HorizontalTextAlignment = TextAlignment.Center
                    },
                    new Label
                    {
                        Text = _vm.CodigoQrCredencial,
                        FontSize = 13,
                        FontAttributes = FontAttributes.Bold,
                        TextColor = ProClubesTheme.TextSecondary,
                        HorizontalTextAlignment = TextAlignment.Center
                    }
                }
            }
        };

        var btnFechar = new Button
        {
            Text = "Fechar Credencial",
            BackgroundColor = ProClubesTheme.BrandPrimary,
            TextColor = Colors.White,
            CornerRadius = 10,
            FontAttributes = FontAttributes.Bold
        };
        btnFechar.Clicked += (_, _) => _popupHost.Close();

        var body = new VerticalStackLayout
        {
            Spacing = 14,
            Children =
            {
                new Label
                {
                    Text = _vm.NomeCredencial,
                    FontSize = 16,
                    FontAttributes = FontAttributes.Bold,
                    TextColor = ProClubesTheme.TextPrimary,
                    HorizontalTextAlignment = TextAlignment.Center
                },
                new Label
                {
                    Text = $"{_vm.SubtituloCredencial} • {_vm.MatriculaCredencial}",
                    FontSize = 12,
                    TextColor = ProClubesTheme.TextMuted,
                    HorizontalTextAlignment = TextAlignment.Center
                },
                qrBox,
                new Label
                {
                    Text = "Apresente este código óptico no leitor da portaria ou catraca da sua unidade.",
                    FontSize = 12,
                    TextColor = ProClubesTheme.TextSecondary,
                    HorizontalTextAlignment = TextAlignment.Center
                },
                btnFechar
            }
        };

        _popupHost.Show("Credencial de Acesso — Catraca", body);
    }

    private void AbrirPopupNotificacoes()
    {
        _ = _vm.MarcarTodasNotificacoesComoLidasAsync();
        var body = new VerticalStackLayout { Spacing = 10 };

        if (_vm.NotificacoesRecentes.Count == 0)
        {
            body.Children.Add(new Border
            {
                BackgroundColor = ProClubesTheme.SurfaceApp,
                Stroke = ProClubesTheme.BorderDefault,
                StrokeThickness = 1,
                StrokeShape = new RoundRectangle { CornerRadius = new CornerRadius(10) },
                Padding = new Thickness(16),
                Content = new Label
                {
                    Text = "Nenhuma notificação recente registrada na sua conta.",
                    FontSize = 13,
                    TextColor = ProClubesTheme.TextSecondary,
                    HorizontalTextAlignment = TextAlignment.Center
                }
            });
        }
        else
        {
            foreach (var n in _vm.NotificacoesRecentes)
            {
                body.Children.Add(new Border
                {
                    BackgroundColor = ProClubesTheme.SurfaceApp,
                    Stroke = ProClubesTheme.BorderDefault,
                    StrokeThickness = 1,
                    StrokeShape = new RoundRectangle { CornerRadius = new CornerRadius(10) },
                    Padding = new Thickness(12, 10),
                    Content = new VerticalStackLayout
                    {
                        Spacing = 4,
                        Children =
                        {
                            new Label
                            {
                                Text = n.Titulo,
                                FontAttributes = FontAttributes.Bold,
                                FontSize = 13,
                                TextColor = ProClubesTheme.TextPrimary
                            },
                            new Label
                            {
                                Text = n.Mensagem,
                                FontSize = 12,
                                TextColor = ProClubesTheme.TextSecondary
                            },
                            new Label
                            {
                                Text = n.CriadaEm.ToString("dd/MM/yyyy HH:mm"),
                                FontSize = 11,
                                TextColor = ProClubesTheme.TextMuted
                            }
                        }
                    }
                });
            }
        }

        var btnFechar = new Button
        {
            Text = "Fechar",
            BackgroundColor = ProClubesTheme.BrandPrimary,
            TextColor = Colors.White,
            CornerRadius = 10,
            FontAttributes = FontAttributes.Bold
        };
        btnFechar.Clicked += (_, _) => _popupHost.Close();
        body.Children.Add(btnFechar);

        _popupHost.Show("Notificações e Avisos", body);
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
