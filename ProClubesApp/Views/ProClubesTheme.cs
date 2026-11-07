using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Shapes;
using Microsoft.Maui.Graphics;
using ProClubesApp.ViewModels;

namespace ProClubesApp.Views;

public static class ProClubesTheme
{
    public static readonly Color BrandPrimary = Color.FromArgb("#0F2C59");
    public static readonly Color BrandPrimaryHover = Color.FromArgb("#163E7A");
    public static readonly Color BrandSecondary = Color.FromArgb("#1E7E5A");
    public static readonly Color BrandAccent = Color.FromArgb("#D97706");
    public static readonly Color SurfaceApp = Color.FromArgb("#F8FAFC");
    public static readonly Color SurfaceCard = Color.FromArgb("#FFFFFF");
    public static readonly Color SurfaceSubtle = Color.FromArgb("#F1F5F9");
    public static readonly Color TextPrimary = Color.FromArgb("#0F172A");
    public static readonly Color TextSecondary = Color.FromArgb("#475569");
    public static readonly Color TextMuted = Color.FromArgb("#64748B");
    public static readonly Color BorderDefault = Color.FromArgb("#E2E8F0");
    public static readonly Color StatusSuccess = Color.FromArgb("#15803D");
    public static readonly Color StatusSuccessBg = Color.FromArgb("#DCFCE7");
    public static readonly Color StatusWarning = Color.FromArgb("#B45309");
    public static readonly Color StatusWarningBg = Color.FromArgb("#FEF3C7");
    public static readonly Color StatusDanger = Color.FromArgb("#B91C1C");
    public static readonly Color StatusDangerBg = Color.FromArgb("#FEE2E2");
    public static readonly Color StatusInfo = Color.FromArgb("#1D4ED8");
    public static readonly Color StatusInfoBg = Color.FromArgb("#DBEAFE");

    public static Border CreateCard(View content, Color? bg = null, Color? border = null)
    {
        return new Border
        {
            BackgroundColor = bg ?? SurfaceCard,
            Stroke = border ?? BorderDefault,
            StrokeThickness = 1,
            StrokeShape = new RoundRectangle { CornerRadius = new CornerRadius(16) },
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
}
