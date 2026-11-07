using Microsoft.Maui.Controls;
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
