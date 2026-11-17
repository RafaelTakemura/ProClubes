using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Shapes;
using ProClubesApp.Models;
using ProClubesApp.ViewModels;

namespace ProClubesApp.Views;

public sealed class EventosMobilePage : ContentPage
{
    private readonly EventosViewModel _vm;

    public EventosMobilePage(EventosViewModel vm)
    {
        _vm = vm;
        BindingContext = _vm;
        Title = "Eventos";
        BackgroundColor = ProClubesTheme.SurfaceApp;

        var headerGrid = ProClubesTheme.CreatePageHeader(
            "AGENDA CULTURAL & ESPORTIVA",
            "Eventos do Clube",
            "Programação oficial das unidades e atividades abertas",
            MobileVectorIconKind.Ticket);

        var list = new CollectionView
        {
            ItemsSource = _vm.Eventos,
            ItemTemplate = new DataTemplate(() =>
            {
                var tit = new Label
                {
                    FontSize = 16,
                    FontAttributes = FontAttributes.Bold,
                    TextColor = ProClubesTheme.TextPrimary
                };
                tit.SetBinding(Label.TextProperty, nameof(EventoDto.Nome));

                var desc = new Label
                {
                    FontSize = 13,
                    TextColor = ProClubesTheme.TextMuted
                };
                desc.SetBinding(Label.TextProperty, nameof(EventoDto.Descricao));

                var resumo = new Label
                {
                    FontSize = 12,
                    FontAttributes = FontAttributes.Bold,
                    TextColor = ProClubesTheme.BrandPrimary
                };
                resumo.SetBinding(Label.TextProperty, nameof(EventoDto.ResumoDataTaxa));

                var pillBox = new Border
                {
                    BackgroundColor = ProClubesTheme.SurfaceApp,
                    StrokeThickness = 0,
                    StrokeShape = new RoundRectangle { CornerRadius = new CornerRadius(8) },
                    Padding = new Thickness(10, 6),
                    Margin = new Thickness(0, 4, 0, 0),
                    Content = resumo
                };

                return ProClubesTheme.CreateCard(new VerticalStackLayout
                {
                    Spacing = 6,
                    Children = { tit, desc, pillBox }
                });
            })
        };

        Content = new ScrollView
        {
            Content = new VerticalStackLayout
            {
                Spacing = 16,
                Padding = new Thickness(20, 18, 20, 36),
                Children =
                {
                    ProClubesTheme.CreateOfflineBanner(),
                    ProClubesTheme.CreateStatusBanner(),
                    headerGrid,
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
