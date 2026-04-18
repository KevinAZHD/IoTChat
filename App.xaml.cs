using Microsoft.Extensions.DependencyInjection;

namespace IoTChat
{
    public partial class App : Application
    {
        public App()
        {
            InitializeComponent();
            ApplySystemAccentColor(); // Carga color de acento del sistema
        }

        // Aplica el color de acento del SO al tema de la app
        private void ApplySystemAccentColor()
        {
            try
            {
                Color accentColor = Color.FromArgb("#0078D7");
#if WINDOWS
                var uiSettings = new global::Windows.UI.ViewManagement.UISettings();
                var color = uiSettings.GetColorValue(global::Windows.UI.ViewManagement.UIColorType.Accent);
                accentColor = Color.FromRgba(color.R, color.G, color.B, color.A);
#elif ANDROID
                if (Android.OS.Build.VERSION.SdkInt >= Android.OS.BuildVersionCodes.S)
                {
                    var context = Android.App.Application.Context;
                    int colorHandle = context.Resources?.GetIdentifier("system_accent1_500", "color", "android") ?? 0;
                    if (colorHandle != 0)
                    {
                        var nativeColor = AndroidX.Core.Content.ContextCompat.GetColor(context, colorHandle);
                        accentColor = Color.FromRgba(
                            Android.Graphics.Color.GetRedComponent(nativeColor),
                            Android.Graphics.Color.GetGreenComponent(nativeColor),
                            Android.Graphics.Color.GetBlueComponent(nativeColor),
                            Android.Graphics.Color.GetAlphaComponent(nativeColor));
                    }
                }
#endif
                if (Resources != null)
                {
                    Resources["PrimaryColor"] = accentColor;
                    Resources["UserBubbleLight"] = accentColor;
                    Resources["UserBubbleDark"] = accentColor;
                }
            }
            catch { }
        }

        // Crea la ventana principal y persiste su posicion/tamaño (solo en Desktop)
        protected override Window CreateWindow(IActivationState? activationState)
        {
            var window = new Window(new AppShell());
#if WINDOWS || MACCATALYST
            window.MinimumWidth  = 400.0;
            window.MinimumHeight = 700.0;

            window.Width  = Preferences.Default.Get("WindowWidth",  400.0);
            window.Height = Preferences.Default.Get("WindowHeight", 700.0);
            window.X      = Preferences.Default.Get("WindowX",      100.0);
            window.Y      = Preferences.Default.Get("WindowY",      100.0);

            window.Destroying += (s, e) =>
            {
                Preferences.Default.Set("WindowWidth",  window.Width);
                Preferences.Default.Set("WindowHeight", window.Height);
                Preferences.Default.Set("WindowX",      window.X);
                Preferences.Default.Set("WindowY",      window.Y);
            };
#endif
            return window;
        }
    }
}