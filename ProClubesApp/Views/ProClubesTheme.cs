using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Shapes;
using Microsoft.Maui.Graphics;
using ProClubesApp.ViewModels;

namespace ProClubesApp.Views;

public sealed class PopupOverlayHost
{
    private readonly Grid _overlayGrid;
    private readonly Label _titleLabel;
    private readonly ScrollView _bodyScroll;
    private Action? _onCloseCallback;

    public Grid RootLayout { get; }

    public PopupOverlayHost(View mainPageContent)
    {
        _titleLabel = new Label
        {
            FontSize = 17,
            FontAttributes = FontAttributes.Bold,
            TextColor = ProClubesTheme.BrandPrimary,
            VerticalOptions = LayoutOptions.Center
        };

        var closeBtn = new Button
        {
            Text = "✕",
            FontSize = 14,
            FontAttributes = FontAttributes.Bold,
            BackgroundColor = ProClubesTheme.SurfaceSubtle,
            TextColor = ProClubesTheme.TextSecondary,
            CornerRadius = 16,
            WidthRequest = 34,
            HeightRequest = 34,
            Padding = Thickness.Zero,
            VerticalOptions = LayoutOptions.Center
        };
        closeBtn.Clicked += (_, _) => Close();

        var headerGrid = new Grid
        {
            ColumnDefinitions =
            {
                new ColumnDefinition { Width = GridLength.Star },
                new ColumnDefinition { Width = GridLength.Auto }
            },
            Children = { _titleLabel, closeBtn }
        };
        Grid.SetColumn(_titleLabel, 0);
        Grid.SetColumn(closeBtn, 1);

        _bodyScroll = new ScrollView
        {
            MaximumHeightRequest = 560
        };

        var popupCard = new Border
        {
            BackgroundColor = ProClubesTheme.SurfaceCard,
            Stroke = ProClubesTheme.BorderDefault,
            StrokeThickness = 1,
            StrokeShape = new RoundRectangle { CornerRadius = new CornerRadius(20) },
            Padding = new Thickness(18),
            VerticalOptions = LayoutOptions.Center,
            HorizontalOptions = LayoutOptions.Fill,
            MaximumHeightRequest = 680,
            Shadow = new Shadow
            {
                Brush = Brush.Black,
                Offset = new Point(0, 8),
                Radius = 24,
                Opacity = 0.35f
            },
            Content = new VerticalStackLayout
            {
                Spacing = 12,
                Children =
                {
                    headerGrid,
                    new BoxView { HeightRequest = 1, Color = ProClubesTheme.BorderDefault },
                    _bodyScroll
                }
            }
        };

        _overlayGrid = new Grid
        {
            BackgroundColor = ProClubesTheme.ModalBackdrop,
            Padding = new Thickness(18, 28),
            VerticalOptions = LayoutOptions.Fill,
            HorizontalOptions = LayoutOptions.Fill,
            IsVisible = false,
            ZIndex = 100,
            Children = { popupCard }
        };

        // Impede que toques no fundo escuro passem para a tela de trás
        var backdropTap = new TapGestureRecognizer();
        _overlayGrid.GestureRecognizers.Add(backdropTap);

        RootLayout = new Grid
        {
            VerticalOptions = LayoutOptions.Fill,
            HorizontalOptions = LayoutOptions.Fill,
            Children =
            {
                mainPageContent,
                _overlayGrid
            }
        };
    }

    public void Show(string title, View bodyContent, Action? onClose = null)
    {
        _titleLabel.Text = title;
        _bodyScroll.Content = bodyContent;
        _onCloseCallback = onClose;
        _overlayGrid.IsVisible = true;
    }

    public void Close()
    {
        _overlayGrid.IsVisible = false;
        _bodyScroll.Content = null;
        var cb = _onCloseCallback;
        _onCloseCallback = null;
        cb?.Invoke();
    }
}

public static class ProClubesTheme
{
    public static readonly Color BrandPrimary = Color.FromArgb("#0B192C");
    public static readonly Color BrandPrimaryHover = Color.FromArgb("#153448");
    public static readonly Color BrandSecondary = Color.FromArgb("#1E5128");
    public static readonly Color EmeraldAction = Color.FromArgb("#10B981");
    public static readonly Color BrandAccent = Color.FromArgb("#D97706");
    public static readonly Color AmberSoft = Color.FromArgb("#FEF3C7");
    public static readonly Color SurfaceApp = Color.FromArgb("#F8FAFC");
    public static readonly Color SurfaceCard = Color.FromArgb("#FFFFFF");
    public static readonly Color SurfaceSubtle = Color.FromArgb("#F1F5F9");
    public static readonly Color TextPrimary = Color.FromArgb("#0F172A");
    public static readonly Color TextSecondary = Color.FromArgb("#475569");
    public static readonly Color TextMuted = Color.FromArgb("#64748B");
    public static readonly Color BorderDefault = Color.FromArgb("#E2E8F0");
    public static readonly Color StatusSuccess = Color.FromArgb("#15803D");
    public static readonly Color StatusSuccessBg = Color.FromArgb("#DCFCE7");
    public static readonly Color StatusWarning = Color.FromArgb("#854D0E");
    public static readonly Color StatusWarningBg = Color.FromArgb("#FEF9C3");
    public static readonly Color StatusDanger = Color.FromArgb("#DC2626");
    public static readonly Color StatusDangerBg = Color.FromArgb("#FEE2E2");
    public static readonly Color StatusInfo = Color.FromArgb("#1D4ED8");
    public static readonly Color StatusInfoBg = Color.FromArgb("#DBEAFE");
    public static readonly Color ModalBackdrop = Color.FromArgb("#990F172A");

    public static Border CreateCard(View content, Color? bg = null, Color? border = null)
    {
        return new Border
        {
            BackgroundColor = bg ?? SurfaceCard,
            Stroke = border ?? BorderDefault,
            StrokeThickness = 1,
            StrokeShape = new RoundRectangle { CornerRadius = new CornerRadius(14) },
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

    public static Border CreateVectorIconTile(MobileVectorIconKind kind, Color? iconColor = null)
    {
        return new Border
        {
            WidthRequest = 34,
            HeightRequest = 34,
            BackgroundColor = SurfaceApp,
            Stroke = BorderDefault,
            StrokeThickness = 1,
            StrokeShape = new RoundRectangle { CornerRadius = new CornerRadius(9) },
            HorizontalOptions = LayoutOptions.Start,
            VerticalOptions = LayoutOptions.Center,
            Content = CreateVectorIcon(kind, iconColor ?? BrandPrimary, 18)
        };
    }

    public static GraphicsView CreateVectorIcon(MobileVectorIconKind kind, Color strokeColor, double size = 18)
    {
        return new GraphicsView
        {
            WidthRequest = size,
            HeightRequest = size,
            HorizontalOptions = LayoutOptions.Center,
            VerticalOptions = LayoutOptions.Center,
            InputTransparent = true,
            Drawable = new VectorIconDrawable(kind, strokeColor)
        };
    }
}

public enum MobileVectorIconKind
{
    Calendar,
    CreditCard,
    Ticket,
    Chat,
    QrCode
}

internal sealed class VectorIconDrawable : IDrawable
{
    private readonly MobileVectorIconKind _kind;
    private readonly Color _color;

    public VectorIconDrawable(MobileVectorIconKind kind, Color color)
    {
        _kind = kind;
        _color = color;
    }

    public void Draw(ICanvas canvas, RectF dirtyRect)
    {
        var w = dirtyRect.Width;
        var h = dirtyRect.Height;
        canvas.StrokeColor = _color;
        canvas.StrokeSize = 1.8f;
        canvas.StrokeLineCap = LineCap.Round;
        canvas.StrokeLineJoin = LineJoin.Round;

        switch (_kind)
        {
            case MobileVectorIconKind.Calendar:
                // Calendário (Reservas)
                canvas.DrawRoundedRectangle(w * 0.14f, h * 0.22f, w * 0.72f, h * 0.64f, 2.5f);
                canvas.DrawLine(w * 0.14f, h * 0.42f, w * 0.86f, h * 0.42f);
                canvas.DrawLine(w * 0.34f, h * 0.12f, w * 0.34f, h * 0.28f);
                canvas.DrawLine(w * 0.66f, h * 0.12f, w * 0.66f, h * 0.28f);
                break;

            case MobileVectorIconKind.CreditCard:
                // Cartão / Faturamento (Mensalidade)
                canvas.DrawRoundedRectangle(w * 0.10f, h * 0.22f, w * 0.80f, h * 0.56f, 2.5f);
                canvas.DrawLine(w * 0.10f, h * 0.42f, w * 0.90f, h * 0.42f);
                canvas.DrawLine(w * 0.24f, h * 0.60f, w * 0.46f, h * 0.60f);
                break;

            case MobileVectorIconKind.Ticket:
                // Ingresso / Agenda (Eventos)
                canvas.DrawRoundedRectangle(w * 0.12f, h * 0.25f, w * 0.76f, h * 0.50f, 2.5f);
                canvas.DrawLine(w * 0.40f, h * 0.25f, w * 0.40f, h * 0.75f);
                canvas.DrawLine(w * 0.54f, h * 0.43f, w * 0.74f, h * 0.43f);
                canvas.DrawLine(w * 0.54f, h * 0.57f, w * 0.68f, h * 0.57f);
                break;

            case MobileVectorIconKind.Chat:
                // Balão de diálogo (Ouvidoria)
                var path = new PathF();
                path.MoveTo(w * 0.15f, h * 0.22f);
                path.LineTo(w * 0.85f, h * 0.22f);
                path.LineTo(w * 0.85f, h * 0.66f);
                path.LineTo(w * 0.42f, h * 0.66f);
                path.LineTo(w * 0.20f, h * 0.84f);
                path.LineTo(w * 0.20f, h * 0.66f);
                path.LineTo(w * 0.15f, h * 0.66f);
                path.Close();
                canvas.DrawPath(path);
                break;

            case MobileVectorIconKind.QrCode:
                // QR Code (Acesso Catraca)
                canvas.DrawRoundedRectangle(w * 0.14f, h * 0.14f, w * 0.28f, h * 0.28f, 1.5f);
                canvas.DrawRoundedRectangle(w * 0.58f, h * 0.14f, w * 0.28f, h * 0.28f, 1.5f);
                canvas.DrawRoundedRectangle(w * 0.14f, h * 0.58f, w * 0.28f, h * 0.28f, 1.5f);
                canvas.DrawLine(w * 0.58f, h * 0.58f, w * 0.86f, h * 0.58f);
                canvas.DrawLine(w * 0.58f, h * 0.72f, w * 0.72f, h * 0.72f);
                canvas.DrawLine(w * 0.86f, h * 0.72f, w * 0.86f, h * 0.86f);
                canvas.DrawLine(w * 0.58f, h * 0.86f, w * 0.74f, h * 0.86f);
                break;
        }
    }
}
