using Microsoft.Maui.Controls;
using ProClubesApp.Models;
using ProClubesApp.ViewModels;

namespace ProClubesApp.Views;

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

                var status = new Label { FontSize = 12, FontAttributes = FontAttributes.Bold, TextColor = ProClubesTheme.BrandAccent };
                status.SetBinding(Label.TextProperty, nameof(MensalidadeDto.SituacaoExibicao), stringFormat: "Situação: {0}");
                status.SetBinding(Label.TextColorProperty, nameof(MensalidadeDto.SituacaoCorTexto));
                status.BindingContextChanged += (_, _) =>
                {
                    if (status.BindingContext is MensalidadeDto m)
                        status.TextColor = m.SituacaoCorTexto;
                };

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
