using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Shapes;
using Microsoft.Maui.Graphics;
using ProClubesApp.Models;
using ProClubesApp.ViewModels;

namespace ProClubesApp.Views;

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
