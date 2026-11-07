using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Shapes;
using Microsoft.Maui.Graphics;
using ProClubesApp.Models;
using ProClubesApp.ViewModels;

namespace ProClubesApp.Views;

public sealed class ReservasMobilePage : ContentPage
{
    private readonly ReservasViewModel _vm;
    private readonly PopupOverlayHost _popupHost;

    public ReservasMobilePage(ReservasViewModel vm)
    {
        _vm = vm;
        BindingContext = _vm;
        Title = "Reservas";
        BackgroundColor = ProClubesTheme.SurfaceApp;

        var btnNovaReserva = new Button
        {
            Text = "+ Nova Reserva de Espaço",
            BackgroundColor = ProClubesTheme.BrandPrimary,
            TextColor = Colors.White,
            CornerRadius = 10,
            FontAttributes = FontAttributes.Bold
        };
        btnNovaReserva.SetBinding(VisualElement.IsVisibleProperty, nameof(ReservasViewModel.PermiteOperarReserva));
        btnNovaReserva.Clicked += (_, _) => AbrirPopupNovaReserva();

        var btnAtualizar = new Button
        {
            Text = "Atualizar Reservas",
            BackgroundColor = ProClubesTheme.SurfaceSubtle,
            TextColor = ProClubesTheme.BrandPrimary,
            CornerRadius = 10,
            FontSize = 12,
            Command = _vm.AtualizarCommand
        };

        var abrirCancelamentoCmd = new Command<ReservaEspacoDto>(reserva => AbrirPopupCancelarReserva(reserva));

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

                // Botão Cancelar só aparece para reservas ativas/confirmadas e abre o Pop-up de confirmação
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
                    Children = { esp, per, badgeConfirmada, boxCancelada, btnCancel }
                });
            })
        };

        var mainScroll = new ScrollView
        {
            Padding = new Thickness(16),
            Content = new VerticalStackLayout
            {
                Spacing = 8,
                Children =
                {
                    ProClubesTheme.CreateOfflineBanner(),
                    ProClubesTheme.CreateStatusBanner(),
                    new Label { Text = "Reservas de Espaços do Clube", FontSize = 18, FontAttributes = FontAttributes.Bold, TextColor = ProClubesTheme.TextPrimary },
                    new Label { Text = "Consulte suas reservas agendadas ou solicite um novo horário com confirmação imediata.", FontSize = 12, TextColor = ProClubesTheme.TextSecondary },
                    btnNovaReserva,
                    btnAtualizar,
                    new Label { Text = "Minhas Reservas", FontSize = 16, FontAttributes = FontAttributes.Bold, TextColor = ProClubesTheme.TextPrimary, Margin = new Thickness(0, 6, 0, 2) },
                    minhasList
                }
            }
        };

        _popupHost = new PopupOverlayHost(mainScroll);
        Content = _popupHost.RootLayout;
    }

    private void AbrirPopupNovaReserva()
    {
        _vm.PrepararNovaReserva();

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

        var btnConfirmar = new Button
        {
            Text = "Confirmar Reserva Automática",
            BackgroundColor = ProClubesTheme.BrandSecondary,
            TextColor = Colors.White,
            CornerRadius = 10,
            FontAttributes = FontAttributes.Bold
        };
        btnConfirmar.Clicked += async (_, _) =>
        {
            var ok = await _vm.CriarReservaAsync();
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
                new Label { Text = "Confirmação imediata se o horário estiver livre e dentro das regras do espaço.", FontSize = 12, TextColor = ProClubesTheme.TextSecondary },
                new Label { Text = "Espaço:", FontSize = 12, FontAttributes = FontAttributes.Bold, TextColor = ProClubesTheme.TextPrimary },
                espacoPicker,
                new Label { Text = "Data da Reserva:", FontSize = 12, FontAttributes = FontAttributes.Bold, TextColor = ProClubesTheme.TextPrimary },
                datePicker,
                new Label { Text = "Horário de Início e Término:", FontSize = 12, FontAttributes = FontAttributes.Bold, TextColor = ProClubesTheme.TextPrimary },
                horaIniEntry,
                horaFimEntry,
                btnConfirmar,
                btnCancelar
            }
        };

        _popupHost.Show("Nova Reserva de Espaço", body, () => _vm.ErrorMessage = null);
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
            {
                _popupHost.Close();
            }
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
                            new Label { Text = reserva.PeriodoFormatado, FontSize = 13, TextColor = ProClubesTheme.TextSecondary }
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
}
