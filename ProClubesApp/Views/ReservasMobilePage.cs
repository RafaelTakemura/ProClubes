using System.Globalization;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Shapes;
using Microsoft.Maui.Graphics;
using Microsoft.Maui.Layouts;
using ProClubesApp.Models;
using ProClubesApp.ViewModels;

namespace ProClubesApp.Views;

public sealed class ReservasMobilePage : ContentPage
{
    private readonly ReservasViewModel _vm;
    private readonly PopupOverlayHost _popupHost;
    private readonly MediaUrlImageSourceConverter _mediaConverter;

    public ReservasMobilePage(ReservasViewModel vm)
    {
        _vm = vm;
        _mediaConverter = new MediaUrlImageSourceConverter(vm.ResolveMediaUrl);
        BindingContext = _vm;
        Title = "Reservas";
        BackgroundColor = ProClubesTheme.SurfaceApp;

        var headerGrid = ProClubesTheme.CreatePageHeader(
            "ESPAÇOS & LAZER",
            "Reservas do Clube",
            "Consulte fotos, valores, agenda disponível ou faça sua reserva",
            MobileVectorIconKind.Calendar);

        var btnNovaReserva = new Button
        {
            Text = "+ Nova Reserva",
            BackgroundColor = ProClubesTheme.BrandPrimary,
            TextColor = Colors.White,
            CornerRadius = 10,
            HeightRequest = 42,
            FontSize = 13,
            FontAttributes = FontAttributes.Bold
        };
        btnNovaReserva.SetBinding(VisualElement.IsVisibleProperty, nameof(ReservasViewModel.PermiteOperarReserva));
        btnNovaReserva.Clicked += (_, _) => _ = AbrirPopupNovaReservaAsync(null);

        var btnAtualizar = new Button
        {
            Text = "Atualizar",
            BackgroundColor = ProClubesTheme.SurfaceCard,
            TextColor = ProClubesTheme.BrandPrimary,
            BorderColor = ProClubesTheme.BorderDefault,
            BorderWidth = 1,
            CornerRadius = 10,
            HeightRequest = 42,
            FontSize = 13,
            FontAttributes = FontAttributes.Bold,
            Command = _vm.AtualizarCommand
        };

        var actionsGrid = new Grid
        {
            ColumnDefinitions =
            {
                new ColumnDefinition { Width = GridLength.Star },
                new ColumnDefinition { Width = GridLength.Auto }
            },
            ColumnSpacing = 10
        };
        actionsGrid.Add(btnNovaReserva, 0, 0);
        actionsGrid.Add(btnAtualizar, 1, 0);

        var abrirAgendaEspacoCmd = new Command<EspacoDto>(esp => _ = AbrirPopupNovaReservaAsync(esp));

        var espacosCollection = new CollectionView
        {
            ItemsSource = _vm.Espacos,
            HeightRequest = 248,
            ItemsLayout = new LinearItemsLayout(ItemsLayoutOrientation.Horizontal) { ItemSpacing = 12 },
            ItemTemplate = new DataTemplate(() =>
            {
                var capaImg = new Image
                {
                    Aspect = Aspect.AspectFill,
                    HeightRequest = 92
                };
                capaImg.SetBinding(Image.SourceProperty, new Binding(nameof(EspacoDto.PrimeiraFotoUrlRelativa), converter: _mediaConverter));

                var capaBorder = new Border
                {
                    StrokeThickness = 0,
                    StrokeShape = new RoundRectangle { CornerRadius = new CornerRadius(10) },
                    HeightRequest = 92,
                    Margin = new Thickness(0, 0, 0, 4),
                    Content = capaImg
                };
                capaBorder.SetBinding(VisualElement.IsVisibleProperty, nameof(EspacoDto.TemFotos));

                var lblNome = new Label
                {
                    FontSize = 14,
                    FontAttributes = FontAttributes.Bold,
                    TextColor = ProClubesTheme.TextPrimary,
                    LineBreakMode = LineBreakMode.TailTruncation
                };
                lblNome.SetBinding(Label.TextProperty, nameof(EspacoDto.Nome));

                var lblTipoValor = new Label
                {
                    FontSize = 12,
                    FontAttributes = FontAttributes.Bold,
                    TextColor = ProClubesTheme.BrandSecondary
                };
                lblTipoValor.SetBinding(Label.TextProperty, new Binding(nameof(EspacoDto.ValorFormatado), stringFormat: "{0}"));

                var lblTipoTag = new Label
                {
                    FontSize = 10,
                    FontAttributes = FontAttributes.Bold,
                    TextColor = ProClubesTheme.BrandPrimary
                };
                lblTipoTag.SetBinding(Label.TextProperty, nameof(EspacoDto.TipoLocacaoTexto));

                var tagTipo = ProClubesTheme.CreateSoftTag(
                    lblTipoTag,
                    ProClubesTheme.SurfaceSubtle,
                    ProClubesTheme.BorderDefault,
                    new Thickness(8, 2));

                var lblHorario = new Label
                {
                    FontSize = 11,
                    TextColor = ProClubesTheme.TextMuted,
                    LineBreakMode = LineBreakMode.TailTruncation
                };
                lblHorario.SetBinding(Label.TextProperty, nameof(EspacoDto.HorarioFuncionamentoTexto));

                var btnVerAgenda = new Button
                {
                    Text = "Ver Agenda / Reservar",
                    BackgroundColor = ProClubesTheme.BrandPrimary,
                    TextColor = Colors.White,
                    CornerRadius = 8,
                    FontSize = 11,
                    FontAttributes = FontAttributes.Bold,
                    HeightRequest = 34,
                    Command = abrirAgendaEspacoCmd
                };
                btnVerAgenda.SetBinding(Button.CommandParameterProperty, ".");

                return new Border
                {
                    WidthRequest = 232,
                    BackgroundColor = ProClubesTheme.SurfaceCard,
                    Stroke = ProClubesTheme.BorderDefault,
                    StrokeThickness = 1,
                    StrokeShape = new RoundRectangle { CornerRadius = new CornerRadius(14) },
                    Padding = new Thickness(12),
                    Content = new VerticalStackLayout
                    {
                        Spacing = 5,
                        Children = { capaBorder, lblNome, tagTipo, lblTipoValor, lblHorario, btnVerAgenda }
                    }
                };
            })
        };

        var abrirCancelamentoCmd = new Command<ReservaEspacoDto>(AbrirPopupCancelarReserva);

        var minhasList = new CollectionView
        {
            ItemsSource = _vm.MinhasReservas,
            ItemTemplate = new DataTemplate(() =>
            {
                var esp = new Label { FontAttributes = FontAttributes.Bold, FontSize = 15, TextColor = ProClubesTheme.TextPrimary };
                esp.SetBinding(Label.TextProperty, nameof(ReservaEspacoDto.EspacoNome));

                var per = new Label { FontSize = 13, TextColor = ProClubesTheme.TextSecondary };
                per.SetBinding(Label.TextProperty, nameof(ReservaEspacoDto.PeriodoFormatado));

                var lblTipo = new Label { FontSize = 12, TextColor = ProClubesTheme.TextMuted };
                lblTipo.SetBinding(Label.TextProperty, nameof(ReservaEspacoDto.TipoLocacaoTexto));

                var lblValor = new Label { FontSize = 12, FontAttributes = FontAttributes.Bold, TextColor = ProClubesTheme.BrandPrimary };
                lblValor.SetBinding(Label.TextProperty, nameof(ReservaEspacoDto.ValorTotalFormatado));

                var metaRow = new HorizontalStackLayout
                {
                    Spacing = 10,
                    Children = { lblTipo, lblValor }
                };

                var badgeConfirmada = ProClubesTheme.CreateSoftTag(
                    new Label
                    {
                        Text = "RESERVA CONFIRMADA",
                        FontSize = 10,
                        FontAttributes = FontAttributes.Bold,
                        TextColor = ProClubesTheme.StatusSuccess
                    },
                    ProClubesTheme.StatusSuccessBg,
                    ProClubesTheme.StatusSuccessBorder,
                    new Thickness(10, 4));
                badgeConfirmada.SetBinding(VisualElement.IsVisibleProperty, nameof(ReservaEspacoDto.PodeCancelar));

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
                                Text = "RESERVA CANCELADA",
                                FontSize = 11,
                                FontAttributes = FontAttributes.Bold,
                                TextColor = ProClubesTheme.StatusDanger
                            },
                            lblMotivo
                        }
                    }
                };
                boxCancelada.SetBinding(VisualElement.IsVisibleProperty, nameof(ReservaEspacoDto.EstaCancelada));

                var btnCancel = new Button
                {
                    Text = "Cancelar Reserva (mín. 24h)",
                    BackgroundColor = ProClubesTheme.StatusDangerBg,
                    TextColor = ProClubesTheme.StatusDanger,
                    CornerRadius = 8,
                    FontSize = 12,
                    FontAttributes = FontAttributes.Bold,
                    Command = abrirCancelamentoCmd
                };
                btnCancel.SetBinding(Button.CommandParameterProperty, ".");
                btnCancel.SetBinding(VisualElement.IsVisibleProperty, nameof(ReservaEspacoDto.PodeCancelar));

                return ProClubesTheme.CreateCard(new VerticalStackLayout
                {
                    Spacing = 6,
                    Children = { esp, per, metaRow, badgeConfirmada, boxCancelada, btnCancel }
                });
            })
        };

        var mainScroll = new ScrollView
        {
            Content = new VerticalStackLayout
            {
                Spacing = 14,
                Padding = new Thickness(20, 18, 20, 36),
                Children =
                {
                    ProClubesTheme.CreateOfflineBanner(),
                    ProClubesTheme.CreateStatusBanner(),
                    headerGrid,
                    actionsGrid,
                    new Label { Text = "ESPAÇOS DO CLUBE", FontSize = 11, FontAttributes = FontAttributes.Bold, TextColor = ProClubesTheme.TextMuted, CharacterSpacing = 1, Margin = new Thickness(0, 4, 0, 0) },
                    espacosCollection,
                    new Label { Text = "MINHAS RESERVAS", FontSize = 11, FontAttributes = FontAttributes.Bold, TextColor = ProClubesTheme.TextMuted, CharacterSpacing = 1, Margin = new Thickness(0, 4, 0, 0) },
                    minhasList
                }
            }
        };

        _popupHost = new PopupOverlayHost(mainScroll);
        Content = _popupHost.RootLayout;
    }

    private async Task AbrirPopupNovaReservaAsync(EspacoDto? espacoInicial)
    {
        _vm.PrepararNovaReserva(espacoInicial);

        var espacoPicker = new Picker
        {
            Title = "Selecione o Espaço do Clube",
            TextColor = ProClubesTheme.TextPrimary,
            TitleColor = ProClubesTheme.TextMuted,
            ItemsSource = _vm.Espacos,
            ItemDisplayBinding = new Binding(nameof(EspacoDto.NomeComDetalhes))
        };
        espacoPicker.SetBinding(Picker.SelectedItemProperty, nameof(ReservasViewModel.EspacoSelecionado));
        espacoPicker.SelectedIndexChanged += (_, _) => _ = _vm.CarregarAgendaAtualAsync();

        var previewImg = new Image
        {
            Aspect = Aspect.AspectFill,
            HeightRequest = 140
        };
        previewImg.SetBinding(Image.SourceProperty, new Binding(nameof(ReservasViewModel.PrimeiraFotoEspacoSelecionado), converter: _mediaConverter));

        var previewBorder = new Border
        {
            Stroke = ProClubesTheme.BorderDefault,
            StrokeThickness = 1,
            StrokeShape = new RoundRectangle { CornerRadius = new CornerRadius(10) },
            HeightRequest = 140,
            Content = previewImg
        };
        previewBorder.SetBinding(VisualElement.IsVisibleProperty, nameof(ReservasViewModel.EspacoSelecionadoTemFotos));

        var lblResumoEspaco = new Label
        {
            FontSize = 12,
            TextColor = ProClubesTheme.TextSecondary
        };
        lblResumoEspaco.SetBinding(Label.TextProperty, nameof(ReservasViewModel.ResumoEspacoSelecionado));

        var datePickerInicio = new DatePicker { Format = "dd/MM/yyyy", TextColor = ProClubesTheme.TextPrimary };
        datePickerInicio.SetBinding(DatePicker.DateProperty, nameof(ReservasViewModel.DataReserva));
        datePickerInicio.DateSelected += (_, _) => _ = _vm.CarregarAgendaAtualAsync();

        var datePickerFim = new DatePicker { Format = "dd/MM/yyyy", TextColor = ProClubesTheme.TextPrimary };
        datePickerFim.SetBinding(DatePicker.DateProperty, nameof(ReservasViewModel.DataFimReserva));

        var blocoPorDia = new VerticalStackLayout
        {
            Spacing = 4,
            Children =
            {
                new Label { Text = "Data de Término:", FontSize = 12, FontAttributes = FontAttributes.Bold, TextColor = ProClubesTheme.TextPrimary },
                datePickerFim
            }
        };
        blocoPorDia.SetBinding(VisualElement.IsVisibleProperty, nameof(ReservasViewModel.EspacoEhPorDia));

        var horaIniEntry = new Entry { Placeholder = "Hora início (HH:mm)", TextColor = ProClubesTheme.TextPrimary, PlaceholderColor = ProClubesTheme.TextMuted };
        horaIniEntry.SetBinding(Entry.TextProperty, nameof(ReservasViewModel.HoraInicio));

        var horaFimEntry = new Entry { Placeholder = "Hora fim (HH:mm)", TextColor = ProClubesTheme.TextPrimary, PlaceholderColor = ProClubesTheme.TextMuted };
        horaFimEntry.SetBinding(Entry.TextProperty, nameof(ReservasViewModel.HoraFim));

        var blocoPorHora = new VerticalStackLayout
        {
            Spacing = 6,
            Children =
            {
                new Label { Text = "Horário de Início e Término:", FontSize = 12, FontAttributes = FontAttributes.Bold, TextColor = ProClubesTheme.TextPrimary },
                horaIniEntry,
                horaFimEntry
            }
        };
        blocoPorHora.SetBinding(VisualElement.IsVisibleProperty, nameof(ReservasViewModel.EspacoEhPorHora));

        var lblCabecalhoAgenda = new Label
        {
            FontSize = 12,
            FontAttributes = FontAttributes.Bold,
            TextColor = ProClubesTheme.BrandPrimary
        };
        lblCabecalhoAgenda.SetBinding(Label.TextProperty, nameof(ReservasViewModel.CabecalhoAgenda));

        var slotsFlex = new FlexLayout
        {
            Wrap = FlexWrap.Wrap,
            JustifyContent = FlexJustify.Start,
            AlignItems = FlexAlignItems.Start
        };
        var slotCmd = new Command<SlotAgendaDto>(slot => _vm.SelecionarSlotAgenda(slot));
        BindableLayout.SetItemsSource(slotsFlex, _vm.SlotsAgenda);
        BindableLayout.SetItemTemplate(slotsFlex, new DataTemplate(() =>
        {
            var lblSlot = new Label
            {
                FontSize = 11,
                FontAttributes = FontAttributes.Bold
            };
            lblSlot.SetBinding(Label.TextProperty, new MultiBinding
            {
                StringFormat = "{0} • {1}",
                Bindings =
                {
                    new Binding(nameof(SlotAgendaDto.Rotulo)),
                    new Binding(nameof(SlotAgendaDto.StatusTexto))
                }
            });
            lblSlot.SetBinding(Label.TextColorProperty, nameof(SlotAgendaDto.CorTexto));

            var chip = new Border
            {
                StrokeThickness = 1,
                StrokeShape = new RoundRectangle { CornerRadius = new CornerRadius(8) },
                Padding = new Thickness(9, 6),
                Margin = new Thickness(0, 0, 6, 6),
                Content = lblSlot
            };
            chip.SetBinding(Border.BackgroundColorProperty, nameof(SlotAgendaDto.CorFundo));
            chip.SetBinding(Border.StrokeProperty, nameof(SlotAgendaDto.CorBorda));

            var tap = new TapGestureRecognizer { Command = slotCmd };
            tap.SetBinding(TapGestureRecognizer.CommandParameterProperty, ".");
            chip.GestureRecognizers.Add(tap);

            return chip;
        }));

        var agendaBox = new Border
        {
            BackgroundColor = ProClubesTheme.SurfaceApp,
            Stroke = ProClubesTheme.BorderDefault,
            StrokeThickness = 1,
            StrokeShape = new RoundRectangle { CornerRadius = new CornerRadius(10) },
            Padding = new Thickness(10),
            Content = new VerticalStackLayout
            {
                Spacing = 6,
                Children =
                {
                    lblCabecalhoAgenda,
                    new Label { Text = "Toque em um horário/dia livre para preencher automaticamente:", FontSize = 11, TextColor = ProClubesTheme.TextMuted },
                    slotsFlex
                }
            }
        };

        var lblValorEstimado = new Label
        {
            FontSize = 13,
            FontAttributes = FontAttributes.Bold,
            TextColor = ProClubesTheme.BrandSecondary
        };
        lblValorEstimado.SetBinding(Label.TextProperty, nameof(ReservasViewModel.ValorEstimadoTexto));

        var valorBox = new Border
        {
            BackgroundColor = ProClubesTheme.StatusSuccessBg,
            Stroke = ProClubesTheme.StatusSuccessBorder,
            StrokeThickness = 1,
            StrokeShape = new RoundRectangle { CornerRadius = new CornerRadius(10) },
            Padding = new Thickness(12, 8),
            Content = lblValorEstimado
        };

        var lblSomenteConsulta = new Label
        {
            Text = "Apenas titulares pagantes podem efetivar reservas. Você pode consultar os valores e a agenda livre.",
            FontSize = 12,
            TextColor = ProClubesTheme.StatusWarning
        };
        lblSomenteConsulta.SetBinding(VisualElement.IsVisibleProperty, nameof(ReservasViewModel.NaoPermiteOperarReserva));

        var btnConfirmar = new Button
        {
            Text = "Confirmar Reserva Automática",
            BackgroundColor = ProClubesTheme.BrandSecondary,
            TextColor = Colors.White,
            CornerRadius = 10,
            FontAttributes = FontAttributes.Bold
        };
        btnConfirmar.SetBinding(VisualElement.IsVisibleProperty, nameof(ReservasViewModel.PermiteOperarReserva));
        btnConfirmar.Clicked += async (_, _) =>
        {
            var ok = await _vm.CriarReservaAsync();
            if (ok)
                _popupHost.Close();
        };

        var btnCancelar = new Button
        {
            Text = "Fechar",
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
                new Label { Text = "Espaço:", FontSize = 12, FontAttributes = FontAttributes.Bold, TextColor = ProClubesTheme.TextPrimary },
                espacoPicker,
                previewBorder,
                lblResumoEspaco,
                new Label { Text = "Data de Início / Consulta:", FontSize = 12, FontAttributes = FontAttributes.Bold, TextColor = ProClubesTheme.TextPrimary },
                datePickerInicio,
                blocoPorDia,
                blocoPorHora,
                agendaBox,
                valorBox,
                lblSomenteConsulta,
                btnConfirmar,
                btnCancelar
            }
        };

        _popupHost.Show("Agenda & Reserva de Espaço", body, () => _vm.ErrorMessage = null);
        await _vm.CarregarAgendaAtualAsync();
    }

    private void AbrirPopupCancelarReserva(ReservaEspacoDto? reserva)
    {
        if (reserva is null) return;
        _vm.PrepararCancelamento();

        var motivoEntry = new Entry
        {
            Placeholder = "Informe o motivo do cancelamento (opcional)",
            TextColor = ProClubesTheme.TextPrimary,
            PlaceholderColor = ProClubesTheme.TextMuted
        };
        motivoEntry.SetBinding(Entry.TextProperty, nameof(ReservasViewModel.JustificativaCancelamento));

        var btnConfirmarCancel = new Button
        {
            Text = "Confirmar Cancelamento da Reserva",
            BackgroundColor = ProClubesTheme.StatusDanger,
            TextColor = Colors.White,
            CornerRadius = 10,
            FontAttributes = FontAttributes.Bold
        };
        btnConfirmarCancel.Clicked += async (_, _) =>
        {
            var ok = await _vm.CancelarReservaAsync(reserva);
            if (ok)
                _popupHost.Close();
        };

        var btnVoltar = new Button
        {
            Text = "Voltar (Manter Reserva)",
            BackgroundColor = ProClubesTheme.SurfaceSubtle,
            TextColor = ProClubesTheme.TextSecondary,
            CornerRadius = 10
        };
        btnVoltar.Clicked += (_, _) =>
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
                new Border
                {
                    BackgroundColor = ProClubesTheme.SurfaceSubtle,
                    Stroke = ProClubesTheme.BorderDefault,
                    StrokeThickness = 1,
                    StrokeShape = new RoundRectangle { CornerRadius = new CornerRadius(10) },
                    Padding = new Thickness(12, 8),
                    Content = new VerticalStackLayout
                    {
                        Spacing = 4,
                        Children =
                        {
                            new Label { Text = reserva.EspacoNome, FontSize = 15, FontAttributes = FontAttributes.Bold, TextColor = ProClubesTheme.TextPrimary },
                            new Label { Text = $"{reserva.PeriodoFormatado} • {reserva.ValorTotalFormatado}", FontSize = 13, TextColor = ProClubesTheme.TextSecondary }
                        }
                    }
                },
                new Label { Text = "Deseja realmente cancelar esta reserva? O cancelamento pelo aplicativo exige antecedência mínima de 24 horas.", FontSize = 12, TextColor = ProClubesTheme.TextSecondary },
                motivoEntry,
                btnConfirmarCancel,
                btnVoltar
            }
        };

        _popupHost.Show("Cancelar Reserva", body, () => _vm.ErrorMessage = null);
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _vm.CarregarAsync();
    }

    private sealed class MediaUrlImageSourceConverter : IValueConverter
    {
        private readonly Func<string?, string> _resolveUrl;

        public MediaUrlImageSourceConverter(Func<string?, string> resolveUrl)
        {
            _resolveUrl = resolveUrl;
        }

        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            var raw = value as string;
            var url = _resolveUrl(raw);
            return Uri.TryCreate(url, UriKind.Absolute, out var uri)
                ? ImageSource.FromUri(uri)
                : null;
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
            throw new NotSupportedException();
    }
}
