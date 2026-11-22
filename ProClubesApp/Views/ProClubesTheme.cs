using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Shapes;
using Microsoft.Maui.Graphics;
using ProClubesApp.Models;
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
    public static readonly Color StatusSuccess = Color.FromArgb("#047857");
    public static readonly Color StatusSuccessBg = Color.FromArgb("#ECFDF5");
    public static readonly Color StatusSuccessBorder = Color.FromArgb("#A7F3D0");
    public static readonly Color StatusWarning = Color.FromArgb("#B45309");
    public static readonly Color StatusWarningBg = Color.FromArgb("#FFFBEB");
    public static readonly Color StatusWarningBorder = Color.FromArgb("#FDE68A");
    public static readonly Color StatusDanger = Color.FromArgb("#B91C1C");
    public static readonly Color StatusDangerBg = Color.FromArgb("#FEF2F2");
    public static readonly Color StatusDangerBorder = Color.FromArgb("#FECACA");
    public static readonly Color StatusInfo = Color.FromArgb("#1D4ED8");
    public static readonly Color StatusInfoBg = Color.FromArgb("#EFF6FF");
    public static readonly Color StatusInfoBorder = Color.FromArgb("#BFDBFE");
    public static readonly Color ModalBackdrop = Color.FromArgb("#990F172A");

    public static Border CreateSoftTag(View content, Color bg, Color border, Thickness? padding = null)
    {
        return new Border
        {
            BackgroundColor = bg,
            Stroke = border,
            StrokeThickness = 1,
            StrokeShape = new RoundRectangle { CornerRadius = new CornerRadius(999) },
            Padding = padding ?? new Thickness(10, 3),
            HorizontalOptions = LayoutOptions.Start,
            VerticalOptions = LayoutOptions.Center,
            Content = content
        };
    }

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

    public static void ApplyCpfCnpjMask(Entry entry)
    {
        entry.Keyboard = Keyboard.Numeric;
        entry.MaxLength = 18;
        if (string.IsNullOrWhiteSpace(entry.Placeholder))
            entry.Placeholder = "000.000.000-00";

        var updating = false;
        entry.TextChanged += (_, e) =>
        {
            if (updating) return;
            var formatted = LoginCadastroViewModel.FormatarCpfOuCnpj(e.NewTextValue);
            if (!string.Equals(e.NewTextValue ?? string.Empty, formatted, StringComparison.Ordinal))
            {
                updating = true;
                entry.Text = formatted;
                entry.CursorPosition = formatted.Length;
                updating = false;
            }
        };
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

    public static Grid CreatePageHeader(string eyebrow, string title, string subtitle, MobileVectorIconKind iconKind)
    {
        var grid = new Grid
        {
            ColumnDefinitions =
            {
                new ColumnDefinition { Width = GridLength.Star },
                new ColumnDefinition { Width = GridLength.Auto }
            },
            ColumnSpacing = 12,
            VerticalOptions = LayoutOptions.Center,
            Margin = new Thickness(0, 0, 0, 4)
        };

        var stack = new VerticalStackLayout
        {
            Spacing = 3,
            Children =
            {
                new Label
                {
                    Text = eyebrow,
                    FontSize = 10,
                    FontAttributes = FontAttributes.Bold,
                    TextColor = TextMuted,
                    CharacterSpacing = 1.2
                },
                new Label
                {
                    Text = title,
                    FontSize = 22,
                    FontAttributes = FontAttributes.Bold,
                    TextColor = TextPrimary
                },
                new Label
                {
                    Text = subtitle,
                    FontSize = 13,
                    TextColor = TextMuted
                }
            }
        };

        var iconTile = CreateVectorIconTile(iconKind);
        iconTile.BackgroundColor = SurfaceCard;

        grid.Add(stack, 0, 0);
        grid.Add(iconTile, 1, 0);
        return grid;
    }

    public static VerticalStackLayout CreateFormField(string rotulo, View controle)
    {
        return new VerticalStackLayout
        {
            Spacing = 3,
            Children =
            {
                new Label
                {
                    Text = rotulo,
                    FontSize = 12,
                    FontAttributes = FontAttributes.Bold,
                    TextColor = TextPrimary
                },
                controle
            }
        };
    }

    public static VerticalStackLayout CreateLocalidadePickers(LocalidadesPickerController localidades)
    {
        var estadoPicker = new Picker
        {
            Title = "-- Selecione o Estado --",
            TextColor = TextPrimary,
            TitleColor = TextMuted,
            ItemDisplayBinding = new Binding(nameof(EstadoMobileDto.Exibicao))
        };
        estadoPicker.SetBinding(Picker.ItemsSourceProperty, nameof(LocalidadesPickerController.EstadosDisponiveis));
        estadoPicker.SetBinding(Picker.SelectedItemProperty, nameof(LocalidadesPickerController.EstadoSelecionado));

        var cidadePicker = new Picker
        {
            Title = "-- Selecione a Cidade --",
            TextColor = TextPrimary,
            TitleColor = TextMuted,
            ItemDisplayBinding = new Binding(nameof(CidadeMobileDto.Exibicao))
        };
        cidadePicker.SetBinding(Picker.ItemsSourceProperty, nameof(LocalidadesPickerController.CidadesDisponiveis));
        cidadePicker.SetBinding(Picker.SelectedItemProperty, nameof(LocalidadesPickerController.CidadeSelecionada));

        var btnLimpar = new Button
        {
            Text = "Limpar seleção de Cidade/Estado",
            BackgroundColor = SurfaceSubtle,
            TextColor = TextSecondary,
            CornerRadius = 8,
            HeightRequest = 34,
            FontSize = 11,
            FontAttributes = FontAttributes.Bold
        };
        btnLimpar.SetBinding(Button.CommandProperty, nameof(LocalidadesPickerController.LimparSelecaoCommand));

        return new VerticalStackLayout
        {
            BindingContext = localidades,
            Spacing = 8,
            Children =
            {
                CreateFormField("Estado (UF)", estadoPicker),
                CreateFormField("Cidade", cidadePicker),
                btnLimpar
            }
        };
    }
}

public enum MobileVectorIconKind
{
    Calendar,
    CreditCard,
    Ticket,
    Chat,
    QrCode,
    CheckVote,
    Bell
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

            case MobileVectorIconKind.CheckVote:
                // Urna / Votação Auditada
                canvas.DrawRoundedRectangle(w * 0.14f, h * 0.16f, w * 0.72f, h * 0.68f, 2.5f);
                canvas.DrawLine(w * 0.32f, h * 0.52f, w * 0.45f, h * 0.65f);
                canvas.DrawLine(w * 0.45f, h * 0.65f, w * 0.70f, h * 0.36f);
                break;

            case MobileVectorIconKind.Bell:
                // Sino de Notificações
                var bellPath = new PathF();
                bellPath.MoveTo(w * 0.22f, h * 0.68f);
                bellPath.LineTo(w * 0.28f, h * 0.38f);
                bellPath.QuadTo(w * 0.50f, h * 0.12f, w * 0.72f, h * 0.38f);
                bellPath.LineTo(w * 0.78f, h * 0.68f);
                bellPath.Close();
                canvas.DrawPath(bellPath);
                canvas.DrawLine(w * 0.16f, h * 0.68f, w * 0.84f, h * 0.68f);
                canvas.DrawLine(w * 0.42f, h * 0.82f, w * 0.58f, h * 0.82f);
                break;
        }
    }
}
