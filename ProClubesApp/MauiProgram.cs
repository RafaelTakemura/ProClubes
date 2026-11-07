using Microsoft.Extensions.Logging;
using Microsoft.Maui.Handlers;
using ProClubesApp.Data;
using ProClubesApp.Services;
using ProClubesApp.ViewModels;
using ProClubesApp.Views;

namespace ProClubesApp;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
            });

#if WINDOWS
        EntryHandler.Mapper.AppendToMapping("ForceLightEntryTheme", (handler, view) =>
        {
            handler.PlatformView.RequestedTheme = Microsoft.UI.Xaml.ElementTheme.Light;
        });
        EditorHandler.Mapper.AppendToMapping("ForceLightEditorTheme", (handler, view) =>
        {
            handler.PlatformView.RequestedTheme = Microsoft.UI.Xaml.ElementTheme.Light;
        });
        PickerHandler.Mapper.AppendToMapping("ForceLightPickerTheme", (handler, view) =>
        {
            handler.PlatformView.RequestedTheme = Microsoft.UI.Xaml.ElementTheme.Light;
        });
        DatePickerHandler.Mapper.AppendToMapping("ForceLightDatePickerTheme", (handler, view) =>
        {
            handler.PlatformView.RequestedTheme = Microsoft.UI.Xaml.ElementTheme.Light;
        });
#endif

        builder.Services.AddSingleton<SqliteCacheRepository>();
        builder.Services.AddSingleton(sp =>
        {
            var baseUrl = DeviceInfo.Platform == DevicePlatform.Android
                ? "http://10.0.2.2:5180/"
                : "http://localhost:5180/";
            var http = new HttpClient { BaseAddress = new Uri(baseUrl), Timeout = TimeSpan.FromSeconds(15) };
            return new MobileSessionAndApiService(http, sp.GetRequiredService<SqliteCacheRepository>());
        });

        builder.Services.AddSingleton<AppShell>();

        builder.Services.AddTransient<LoginCadastroViewModel>();
        builder.Services.AddTransient<InicioViewModel>();
        builder.Services.AddTransient<EventosViewModel>();
        builder.Services.AddTransient<ReservasViewModel>();
        builder.Services.AddTransient<FinanceiroViewModel>();
        builder.Services.AddTransient<OuvidoriaViewModel>();
        builder.Services.AddTransient<MaisGovernancaViewModel>();

        builder.Services.AddTransient<LoginCadastroPage>();
        builder.Services.AddTransient<InicioMobilePage>();
        builder.Services.AddTransient<EventosMobilePage>();
        builder.Services.AddTransient<ReservasMobilePage>();
        builder.Services.AddTransient<FinanceiroMobilePage>();
        builder.Services.AddTransient<OuvidoriaMobilePage>();
        builder.Services.AddTransient<MaisGovernancaMobilePage>();

#if DEBUG
        builder.Logging.AddDebug();
#endif

        return builder.Build();
    }
}
