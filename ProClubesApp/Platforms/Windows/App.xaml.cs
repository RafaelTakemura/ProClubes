using Microsoft.UI.Xaml;

namespace ProClubesApp.WinUI;

public partial class App : MauiWinUIApplication
{
    public App()
    {
        this.RequestedTheme = ApplicationTheme.Light;
        this.InitializeComponent();
    }

    protected override MauiApp CreateMauiApp() => MauiProgram.CreateMauiApp();
}
