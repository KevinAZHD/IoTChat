using IoTChat.Services;
using IoTChat.ViewModels;
using Microsoft.Extensions.Logging;
using CommunityToolkit.Maui;

namespace IoTChat
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();

            builder
                .UseMauiApp<App>()
                .UseMauiCommunityToolkit()
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                    fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
                });

            // Servicios inyectados
            builder.Services.AddSingleton<HttpClient>();
            builder.Services.AddSingleton<LlmService>();
            builder.Services.AddSingleton<NodeRedService>();
            builder.Services.AddTransient<MainViewModel>();
            builder.Services.AddTransient<Views.MainPage>();

            // Quita el borde del Entry en cada plataforma
            Microsoft.Maui.Handlers.EntryHandler.Mapper.AppendToMapping("NoBorder", (h, v) =>
            {
#if WINDOWS
                h.PlatformView.BorderThickness = new Microsoft.UI.Xaml.Thickness(0);
                h.PlatformView.Background = null;
                h.PlatformView.Resources.Add("TextControlBorderThemeThicknessFocused", new Microsoft.UI.Xaml.Thickness(0));
#elif ANDROID
                h.PlatformView.BackgroundTintList = Android.Content.Res.ColorStateList.ValueOf(Android.Graphics.Color.Transparent);
#elif IOS || MACCATALYST
                h.PlatformView.BorderStyle = UIKit.UITextBorderStyle.None;
#endif
            });

#if DEBUG
            builder.Logging.AddDebug();
#endif
            return builder.Build();
        }
    }
}
