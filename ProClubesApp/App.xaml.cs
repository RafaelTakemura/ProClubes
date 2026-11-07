namespace ProClubesApp;

public partial class App : Application
{
    private readonly AppShell _shell;

    public App(AppShell shell)
    {
        InitializeComponent();
        UserAppTheme = AppTheme.Light;
        _shell = shell;
    }

    protected override Window CreateWindow(IActivationState? activationState)
    {
        var window = new Window(_shell)
        {
            Title = "ProClubes Mobile",
            Width = 410,
            Height = 860,
            MinimumWidth = 375,
            MaximumWidth = 430,
            MinimumHeight = 700,
            MaximumHeight = 920
        };

        var displayInfo = DeviceDisplay.Current.MainDisplayInfo;
        if (displayInfo.Width > 0 && displayInfo.Density > 0)
        {
            var screenWidthDip = displayInfo.Width / displayInfo.Density;
            var screenHeightDip = displayInfo.Height / displayInfo.Density;
            window.X = Math.Max(0, (screenWidthDip - window.Width) / 2);
            window.Y = Math.Max(20, (screenHeightDip - window.Height) / 2);
        }

        return window;
    }
}
