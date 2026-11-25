using System.Globalization;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Shapes;
using Microsoft.Maui.Graphics;
using ProClubesApp.Models;
using ProClubesApp.ViewModels;

namespace ProClubesApp.Views;

public sealed class EventosMobilePage : ContentPage
{
    private readonly EventosViewModel _vm;
    private readonly PopupOverlayHost _popupHost;
    private readonly MediaUrlImageSourceConverter _mediaConverter;

    public EventosMobilePage(EventosViewModel vm)
    {
        _vm = vm;
        _mediaConverter = new MediaUrlImageSourceConverter(vm.ResolveMediaUrl);
        BindingContext = _vm;
        Title = "Eventos";
        BackgroundColor = ProClubesTheme.SurfaceApp;

        var headerGrid = ProClubesTheme.CreatePageHeader(
            "AGENDA CULTURAL & ESPORTIVA",
            "Eventos do Clube",
            "Programação oficial das unidades e atividades abertas",
            MobileVectorIconKind.Ticket);

        var abrirFotosCmd = new Command<EventoDto>(AbrirPopupFotosEvento);

        var list = new CollectionView
        {
            ItemsSource = _vm.Eventos,
            ItemTemplate = new DataTemplate(() =>
            {
                var capaImg = new Image
                {
                    Aspect = Aspect.AspectFill,
                    HeightRequest = 160
                };
                capaImg.SetBinding(Image.SourceProperty, new Binding(nameof(EventoDto.PrimeiraFotoUrlRelativa), converter: _mediaConverter));

                var capaBorder = new Border
                {
                    StrokeThickness = 0,
                    StrokeShape = new RoundRectangle { CornerRadius = new CornerRadius(10) },
                    HeightRequest = 160,
                    Margin = new Thickness(0, 0, 0, 4),
                    Content = capaImg
                };
                capaBorder.SetBinding(VisualElement.IsVisibleProperty, nameof(EventoDto.TemFotos));

                var tit = new Label
                {
                    FontSize = 16,
                    FontAttributes = FontAttributes.Bold,
                    TextColor = ProClubesTheme.TextPrimary
                };
                tit.SetBinding(Label.TextProperty, nameof(EventoDto.Nome));

                var localLbl = new Label
                {
                    FontSize = 12,
                    FontAttributes = FontAttributes.Bold,
                    TextColor = ProClubesTheme.BrandSecondary
                };
                localLbl.SetBinding(Label.TextProperty, nameof(EventoDto.LocalEspacoTexto));

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

                var btnFotos = new Button
                {
                    BackgroundColor = ProClubesTheme.SurfaceSubtle,
                    TextColor = ProClubesTheme.BrandPrimary,
                    CornerRadius = 8,
                    FontSize = 12,
                    FontAttributes = FontAttributes.Bold,
                    HeightRequest = 36,
                    Margin = new Thickness(0, 2, 0, 0),
                    Command = abrirFotosCmd
                };
                btnFotos.SetBinding(Button.TextProperty, nameof(EventoDto.TextoBotaoFotos));
                btnFotos.SetBinding(Button.CommandParameterProperty, ".");
                btnFotos.SetBinding(VisualElement.IsVisibleProperty, nameof(EventoDto.TemFotos));

                return ProClubesTheme.CreateCard(new VerticalStackLayout
                {
                    Spacing = 6,
                    Children = { capaBorder, tit, localLbl, desc, pillBox, btnFotos }
                });
            })
        };

        var mainScroll = new ScrollView
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

        _popupHost = new PopupOverlayHost(mainScroll);
        Content = _popupHost.RootLayout;
    }

    private void AbrirPopupFotosEvento(EventoDto? evento)
    {
        if (evento?.Fotos is not { Count: > 0 }) return;

        var fotosStack = new VerticalStackLayout { Spacing = 10 };
        fotosStack.Children.Add(new Label
        {
            Text = evento.LocalEspacoTexto,
            FontSize = 12,
            FontAttributes = FontAttributes.Bold,
            TextColor = ProClubesTheme.TextSecondary
        });

        foreach (var foto in evento.Fotos)
        {
            var url = _vm.ResolveMediaUrl(foto.UrlRelativa);
            if (!Uri.TryCreate(url, UriKind.Absolute, out var uri)) continue;

            fotosStack.Children.Add(new Border
            {
                Stroke = ProClubesTheme.BorderDefault,
                StrokeThickness = 1,
                StrokeShape = new RoundRectangle { CornerRadius = new CornerRadius(10) },
                Content = new VerticalStackLayout
                {
                    Spacing = 4,
                    Children =
                    {
                        new Image
                        {
                            Source = ImageSource.FromUri(uri),
                            Aspect = Aspect.AspectFill,
                            HeightRequest = 190
                        },
                        new Label
                        {
                            Text = foto.NomeArquivo,
                            FontSize = 11,
                            TextColor = ProClubesTheme.TextMuted,
                            Padding = new Thickness(10, 4, 10, 8),
                            LineBreakMode = LineBreakMode.TailTruncation
                        }
                    }
                }
            });
        }

        var btnFechar = new Button
        {
            Text = "Fechar Galeria",
            BackgroundColor = ProClubesTheme.SurfaceSubtle,
            TextColor = ProClubesTheme.TextSecondary,
            CornerRadius = 10
        };
        btnFechar.Clicked += (_, _) => _popupHost.Close();
        fotosStack.Children.Add(btnFechar);

        _popupHost.Show($"Fotos — {evento.Nome}", fotosStack);
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
