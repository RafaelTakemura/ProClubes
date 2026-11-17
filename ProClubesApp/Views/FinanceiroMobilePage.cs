using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Shapes;
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

        var headerGrid = ProClubesTheme.CreatePageHeader(
            "TESOURARIA & EXTRATO",
            "Minhas Mensalidades",
            "Competências, encargos e situação de pagamento",
            MobileVectorIconKind.CreditCard);

        var list = new CollectionView
        {
            ItemsSource = _vm.Mensalidades,
            ItemTemplate = new DataTemplate(() =>
            {
                var lblComp = new Label
                {
                    FontSize = 15,
                    FontAttributes = FontAttributes.Bold,
                    TextColor = ProClubesTheme.TextPrimary,
                    VerticalOptions = LayoutOptions.Center
                };
                lblComp.SetBinding(Label.TextProperty, nameof(MensalidadeDto.CompetenciaTitulo));

                var lblStatus = new Label
                {
                    FontSize = 11,
                    FontAttributes = FontAttributes.Bold,
                    TextColor = ProClubesTheme.BrandAccent
                };
                lblStatus.SetBinding(Label.TextProperty, nameof(MensalidadeDto.SituacaoExibicao));
                lblStatus.SetBinding(Label.TextColorProperty, nameof(MensalidadeDto.SituacaoCorTexto));

                var statusPill = new Border
                {
                    BackgroundColor = ProClubesTheme.StatusWarningBg,
                    Stroke = ProClubesTheme.StatusWarningBorder,
                    StrokeThickness = 1,
                    StrokeShape = new RoundRectangle { CornerRadius = new CornerRadius(999) },
                    Padding = new Thickness(10, 3),
                    VerticalOptions = LayoutOptions.Center,
                    Content = lblStatus
                };
                statusPill.SetBinding(VisualElement.BackgroundColorProperty, nameof(MensalidadeDto.SituacaoCorFundo));
                statusPill.SetBinding(Border.StrokeProperty, nameof(MensalidadeDto.SituacaoCorBorda));
                statusPill.BindingContextChanged += (_, _) =>
                {
                    if (statusPill.BindingContext is MensalidadeDto m)
                    {
                        lblStatus.TextColor = m.SituacaoCorTexto;
                        statusPill.BackgroundColor = m.SituacaoCorFundo;
                        statusPill.Stroke = m.SituacaoCorBorda;
                    }
                };

                var topGrid = new Grid
                {
                    ColumnDefinitions =
                    {
                        new ColumnDefinition { Width = GridLength.Star },
                        new ColumnDefinition { Width = GridLength.Auto }
                    }
                };
                topGrid.Add(lblComp, 0, 0);
                topGrid.Add(statusPill, 1, 0);

                var lblVenc = new Label
                {
                    FontSize = 12,
                    TextColor = ProClubesTheme.TextMuted
                };
                lblVenc.SetBinding(Label.TextProperty, nameof(MensalidadeDto.VencimentoEUnidade));

                var valOrig = new Label
                {
                    FontSize = 12,
                    TextColor = ProClubesTheme.TextSecondary
                };
                valOrig.SetBinding(Label.TextProperty, nameof(MensalidadeDto.ValorPrincipal), stringFormat: "Valor principal: R$ {0:N2}");

                var lblTotal = new Label
                {
                    FontSize = 13,
                    FontAttributes = FontAttributes.Bold,
                    TextColor = ProClubesTheme.BrandPrimary
                };
                lblTotal.SetBinding(Label.TextProperty, nameof(MensalidadeDto.TotalAtualizadoTexto));

                var bottomBar = new Border
                {
                    BackgroundColor = ProClubesTheme.SurfaceApp,
                    StrokeThickness = 0,
                    StrokeShape = new RoundRectangle { CornerRadius = new CornerRadius(8) },
                    Padding = new Thickness(10, 6),
                    Margin = new Thickness(0, 4, 0, 0),
                    Content = lblTotal
                };

                return ProClubesTheme.CreateCard(new VerticalStackLayout
                {
                    Spacing = 6,
                    Children = { topGrid, lblVenc, valOrig, bottomBar }
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
