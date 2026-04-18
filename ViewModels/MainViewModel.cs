using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using IoTChat.Models;
using IoTChat.Services;
using MQTTnet;
using MQTTnet.Client;
#if WINDOWS
using Windows.Media.SpeechRecognition;
#endif

namespace IoTChat.ViewModels
{
    public class MainViewModel : INotifyPropertyChanged
    {
        private readonly LlmService _llm;
        private readonly NodeRedService _nodeRed;

        // Campos privados de estado
        private string _userMessage = "";
        private string _statusText = "Esperando comando...";
        private bool _redOn, _yellowOn, _greenOn, _isBusy, _isListening, _lastMessageWasVoice;
        private CancellationTokenSource? _listenCts;

#if WINDOWS
        private SpeechRecognizer? _speechRecognizer;
#endif

        // Coleccion de mensajes del chat
        public ObservableCollection<ChatMessage> Messages { get; } = new();

        // Propiedades de UI enlazadas a la vista
        public string UserMessage    { get => _userMessage; set { _userMessage = value; Notify(); } }
        public string StatusText     { get => _statusText;  set { _statusText  = value; Notify(); } }
        public bool   RedOn          { get => _redOn;       set { _redOn       = value; Notify(); } }
        public bool   YellowOn       { get => _yellowOn;    set { _yellowOn    = value; Notify(); } }
        public bool   GreenOn        { get => _greenOn;     set { _greenOn     = value; Notify(); } }
        public bool   IsBusy         { get => _isBusy;      set { _isBusy      = value; Notify(); Notify(nameof(IsNotBusy)); } }
        public bool   IsNotBusy      => !_isBusy;
        public bool   IsListening    { get => _isListening; set { _isListening = value; Notify(); Notify(nameof(MicButtonColor)); } }
        public string MicButtonColor => _isListening ? "#FF1744" : "Transparent";

        // Posicion del semaforo (izquierda/derecha), persiste entre sesiones
        public bool SemaphoreLeft
        {
            get => Preferences.Default.Get("SemaphoreLeft", false);
            set { Preferences.Default.Set("SemaphoreLeft", value); Notify(); Notify(nameof(LayoutDirection)); }
        }
        public Microsoft.Maui.Layouts.FlexDirection LayoutDirection =>
            SemaphoreLeft ? Microsoft.Maui.Layouts.FlexDirection.RowReverse : Microsoft.Maui.Layouts.FlexDirection.Row;

        // Comandos de la UI
        public ICommand SendCommand        { get; }
        public ICommand ClearChatCommand   { get; }
        public ICommand ToggleLayoutCommand { get; }
        public ICommand ToggleThemeCommand  { get; }
        public ICommand ToggleListenCommand { get; }
        public ICommand ToggleLightCommand  { get; }

        // Icono SVG del tema segun si esta en modo oscuro
        public string ThemeIconPath => IsDarkTheme
            ? "M12 7c-2.76 0-5 2.24-5 5s2.24 5 5 5 5-2.24 5-5-2.24-5-5-5zM2 13h2c.55 0 1-.45 1-1s-.45-1-1-1H2c-.55 0-1 .45-1 1s.45 1 1 1zm18 0h2c.55 0 1-.45 1-1s-.45-1-1-1h-2c-.55 0-1 .45-1 1s.45 1 1 1zM11 2v2c0 .55.45 1 1 1s1-.45 1-1V2c0-.55-.45-1-1-1s-1 .45-1 1zm0 18v2c0 .55.45 1 1 1s1-.45 1-1v-2c0-.55-.45-1-1-1s-1 .45-1 1zM5.99 4.58c-.39-.39-1.03-.39-1.41 0-.39.39-.39 1.03 0 1.41l1.06 1.06c.39.39 1.03.39 1.41 0s.39-1.03 0-1.41L5.99 4.58zm12.37 12.37c-.39-.39-1.03-.39-1.41 0-.39.39-.39 1.03 0 1.41l1.06 1.06c.39.39 1.03.39 1.41 0 .39-.39.39-1.03 0-1.41l-1.06-1.06zm1.06-10.96c.39-.39.39-1.03 0-1.41-.39-.39-1.03-.39-1.41 0l-1.06 1.06c-.39.39-.39 1.03 0 1.41s1.03.39 1.41 0l1.06-1.06zM7.05 18.36c.39-.39.39-1.03 0-1.41-.39-.39-1.03-.39-1.41 0l-1.06 1.06c-.39.39-.39 1.03 0 1.41s1.03.39 1.41 0l1.06-1.06z"
            : "M9.37,5.51C9.19,6.15,9.1,6.82,9.1,7.5c0,4.08,3.32,7.4,7.4,7.4c0.68,0,1.35-0.09,1.99-0.27C17.45,17.19,14.93,19,12,19 c-3.86,0-7-3.14-7-7C5,9.07,6.81,6.55,9.37,5.51z M12,3c-4.97,0-9,4.03-9,9s4.03,9,9,9s9-4.03,9-9c0-0.46-0.04-0.92-0.1-1.36 c-0.98,1.37-2.58,2.26-4.4,2.26c-2.98,0-5.4-2.42-5.4-5.4c0-1.81,0.89-3.42,2.26-4.4C12.92,3.04,12.46,3,12,3L12,3z";

        // Tema oscuro/claro, persiste entre sesiones
        public bool IsDarkTheme
        {
            get => Preferences.Default.Get("IsDarkTheme", true);
            set
            {
                Preferences.Default.Set("IsDarkTheme", value);
                if (Application.Current != null)
                    Application.Current.UserAppTheme = value ? AppTheme.Dark : AppTheme.Light;
                Notify(); Notify(nameof(ThemeIconPath)); Notify(nameof(IsLightTheme));
            }
        }
        public bool IsLightTheme => !IsDarkTheme;

        public MainViewModel(LlmService llm, NodeRedService nodeRed)
        {
            _llm = llm;
            _nodeRed = nodeRed;

            if (Application.Current != null)
                Application.Current.UserAppTheme = IsDarkTheme ? AppTheme.Dark : AppTheme.Light;

            SendCommand         = new Command(async () => await SendAsync(), () => IsNotBusy);
            ClearChatCommand    = new Command(() => Messages.Clear());
            ToggleLayoutCommand = new Command(() => SemaphoreLeft = !SemaphoreLeft);
            ToggleThemeCommand  = new Command(() => IsDarkTheme = !IsDarkTheme);
            ToggleListenCommand = new Command(async () => await ToggleListenAsync());
            ToggleLightCommand  = new Command<string>(async (color) => await ToggleLightAsync(color));

            _ = SetupMqtt(); // Conecta MQTT para recibir telemetria de la placa
        }

        // Activa/desactiva el microfono para voz
        private async Task ToggleListenAsync()
        {
            if (IsListening)
            {
                _listenCts?.Cancel();
                IsListening = false;
                StatusText = "Microfono cancelado";
                return;
            }

            bool isGranted = true;
#if !WINDOWS
            isGranted = await Permissions.CheckStatusAsync<Permissions.Microphone>() == PermissionStatus.Granted;
            if (!isGranted)
            {
                if (await Permissions.RequestAsync<Permissions.Microphone>() != PermissionStatus.Granted)
                {
                    StatusText = "Permiso denegado";
                    return;
                }
                isGranted = true;
            }
#endif
            if (!isGranted) return;

            IsListening = true;
            _listenCts = new CancellationTokenSource();
            _lastMessageWasVoice = true;
            StatusText = "Escuchando...";

            try
            {
#if WINDOWS
                await Task.Run(async () =>
                {
                    try
                    {
                        if (_speechRecognizer != null)
                        {
                            try { _speechRecognizer.Dispose(); } catch { }
                            _speechRecognizer = null;
                        }

                        _speechRecognizer = new SpeechRecognizer();
                        await _speechRecognizer.CompileConstraintsAsync();
                        _speechRecognizer.HypothesisGenerated += (s, e) =>
                        {
                            if (e.Hypothesis != null && !string.IsNullOrWhiteSpace(e.Hypothesis.Text))
                                MainThread.BeginInvokeOnMainThread(() => UserMessage = e.Hypothesis.Text);
                        };

                        _listenCts.Token.Register(() =>
                        {
                            try { _speechRecognizer.StopRecognitionAsync().AsTask().Wait(); } catch { }
                        });

                        var result = await _speechRecognizer.RecognizeAsync();

                        MainThread.BeginInvokeOnMainThread(async () =>
                        {
                            if (IsListening) IsListening = false;

                            if (_listenCts.IsCancellationRequested) return;

                            if (result.Status == SpeechRecognitionResultStatus.Success && !string.IsNullOrWhiteSpace(result.Text))
                            {
                                UserMessage = result.Text;
                                if (_lastMessageWasVoice) await SendAsync();
                            }
                            else if (result.Status != SpeechRecognitionResultStatus.Success && result.Status != SpeechRecognitionResultStatus.UserCanceled)
                            {
                                StatusText = "Error al escuchar (" + result.Status + ")";
                            }

                            try { _speechRecognizer.Dispose(); _speechRecognizer = null; } catch { }
                        });
                    }
                    catch (Exception winEx)
                    {
                        MainThread.BeginInvokeOnMainThread(() =>
                        {
                            IsListening = false;
                            StatusText = "Error micro: " + winEx.Message;
                            try { _speechRecognizer?.Dispose(); _speechRecognizer = null; } catch { }
                        });
                    }
                });
#else
                CommunityToolkit.Maui.Media.SpeechToText.Default.RecognitionResultUpdated -= OnRecognitionUpdated;
                CommunityToolkit.Maui.Media.SpeechToText.Default.RecognitionResultUpdated += OnRecognitionUpdated;
                CommunityToolkit.Maui.Media.SpeechToText.Default.StateChanged -= OnStateChanged;
                CommunityToolkit.Maui.Media.SpeechToText.Default.StateChanged += OnStateChanged;
                await CommunityToolkit.Maui.Media.SpeechToText.Default.StartListenAsync(
                    new CommunityToolkit.Maui.Media.SpeechToTextOptions { Culture = System.Globalization.CultureInfo.CurrentUICulture, ShouldReportPartialResults = true },
                    _listenCts.Token);
#endif
            }
            catch (Exception ex)
            {
                IsListening = false;
                StatusText = "Error micro: " + ex.Message;
            }
        }

        // Actualiza el texto del input mientras se escucha (no-Windows)
        private void OnRecognitionUpdated(object? sender, CommunityToolkit.Maui.Media.SpeechToTextRecognitionResultUpdatedEventArgs e)
            => UserMessage = e.RecognitionResult;

        // Cuando el microfono para, envia el mensaje (no-Windows)
        private async void OnStateChanged(object? sender, CommunityToolkit.Maui.Media.SpeechToTextStateChangedEventArgs e)
        {
            if (e.State == CommunityToolkit.Maui.Media.SpeechToTextState.Stopped)
            {
                IsListening = false;
                if (!string.IsNullOrWhiteSpace(UserMessage) && _lastMessageWasVoice)
                    await SendAsync();
            }
        }

        // Envia el mensaje al LLM y ejecuta la tool resultante
        private async Task SendAsync()
        {
            if (string.IsNullOrWhiteSpace(UserMessage)) return;
            IsBusy = true;
            StatusText = "🧠";
            AddMessage(UserMessage, isUser: true);
            var input = UserMessage;
            UserMessage = "";
            bool wasVoice = _lastMessageWasVoice;
            _lastMessageWasVoice = false;

            try
            {
                var (toolCalls, textResponse) = await _llm.SendAsync(input);

                if (toolCalls.Count > 0)
                {
                    foreach (var tool in toolCalls)
                        await ExecuteToolHardware(tool);
                    if (!string.IsNullOrEmpty(textResponse))
                    {
                        AddMessage(textResponse, isUser: false);
                        if (wasVoice) await TextToSpeech.Default.SpeakAsync(textResponse);
                    }
                    StatusText = "OK";
                }
                else if (!string.IsNullOrEmpty(textResponse))
                {
                    AddMessage(textResponse, isUser: false);
                    StatusText = "OK";
                    if (wasVoice) await TextToSpeech.Default.SpeakAsync(textResponse);
                }
            }
            catch (HttpRequestException)
            {
                AddMessage("Error conexion", isSystem: true);
                StatusText = "❌";
            }
            catch (Exception ex)
            {
                AddMessage($"Error: {ex.Message}", isSystem: true);
                StatusText = "❌";
            }
            finally { IsBusy = false; }
        }

        // Traduce el nombre de tool al comando MQTT y lo envia
        private async Task ExecuteToolHardware(string tool)
        {
            string mqtt;
            switch (tool)
            {
                case "encender_luz_roja":          mqtt = "encender_rojo";            break;
                case "encender_luz_amarilla":      mqtt = "encender_amarillo";        break;
                case "encender_luz_verde":         mqtt = "encender_verde";           break;
                case "apagar_luz_roja":            mqtt = "apagar_rojo";              break;
                case "apagar_luz_amarilla":        mqtt = "apagar_amarillo";          break;
                case "apagar_luz_verde":           mqtt = "apagar_verde";             break;
                case "apagar_todas":               mqtt = "apagar_todas";             break;
                case "parpadear_luz_roja":         mqtt = "parpadear_rojo";           break;
                case "parpadear_luz_amarilla":     mqtt = "parpadear_amarillo";       break;
                case "parpadear_luz_verde":        mqtt = "parpadear_verde";          break;
                case "parpadear_todas":            mqtt = "parpadear_todas";          break;
                case "encender_rojo_amarillo":     mqtt = "encender_rojo_amarillo";   break;
                case "encender_rojo_verde":        mqtt = "encender_rojo_verde";      break;
                case "encender_amarillo_verde":    mqtt = "encender_amarillo_verde";  break;
                case "encender_todas":             mqtt = "encender_todas";           break;
                case "apagar_rojo_amarillo":       mqtt = "apagar_rojo_amarillo";     break;
                case "apagar_rojo_verde":          mqtt = "apagar_rojo_verde";        break;
                case "apagar_amarillo_verde":      mqtt = "apagar_amarillo_verde";    break;
                case "parpadear_rojo_amarillo":    mqtt = "parpadear_rojo_amarillo";  break;
                case "parpadear_rojo_verde":       mqtt = "parpadear_rojo_verde";     break;
                case "parpadear_amarillo_verde":   mqtt = "parpadear_amarillo_verde"; break;
                default: return;
            }

            await _nodeRed.SendCommandAsync(mqtt);
        }

        // Toggle de luz al pulsar el semaforo en la UI
        private async Task ToggleLightAsync(string color)
        {
            if (IsBusy) return;

            // Calcula el nuevo estado deseado toggling solo el color pulsado
            bool newR = color == "red"    ? !RedOn    : RedOn;
            bool newY = color == "yellow" ? !YellowOn : YellowOn;
            bool newG = color == "green"  ? !GreenOn  : GreenOn;

            var mqtt = BuildStaticCommand(newR, newY, newG);
            await _nodeRed.SendCommandAsync(mqtt);
        }

        // Devuelve el comando MQTT segun que luces deben estar encendidas
        private static string BuildStaticCommand(bool r, bool y, bool g)
        {
            int n = (r ? 1 : 0) + (y ? 1 : 0) + (g ? 1 : 0);
            if (n == 0) return "apagar_todas";
            if (n == 3) return "encender_todas";
            if (n == 1) return r ? "encender_rojo" : y ? "encender_amarillo" : "encender_verde";
            // n == 2
            if (r && y) return "encender_rojo_amarillo";
            if (r && g) return "encender_rojo_verde";
            return "encender_amarillo_verde";
        }

        // Conecta MQTT y se suscribe al topic de telemetria de la placa
        private async Task SetupMqtt()
        {
            try
            {
                var factory = new MqttFactory();
                var client = factory.CreateMqttClient();
                var options = new MqttClientOptionsBuilder()
                    .WithTcpServer("test.mosquitto.org", 1883)
                    .WithClientId("IoTChat_" + Guid.NewGuid())
                    .Build();

                client.ApplicationMessageReceivedAsync += e =>
                {
                    var msg = System.Text.Encoding.UTF8.GetString(e.ApplicationMessage.PayloadSegment);
                    MainThread.BeginInvokeOnMainThread(() => ProcessMqttMessage(msg));
                    return Task.CompletedTask;
                };

                await client.ConnectAsync(options);
                var subOpt = factory.CreateSubscribeOptionsBuilder()
                    .WithTopicFilter(f => f.WithTopic(AppConfig.TelemetryTopic))
                    .Build();
                await client.SubscribeAsync(subOpt);
            }
            catch { }
        }

        // Actualiza el semaforo de la UI con el estado real de la placa
        private void ProcessMqttMessage(string msg)
        {
            try
            {
                var parts = msg.Split(',');
                if (parts.Length == 3)
                {
                    RedOn    = parts[0] == "1";
                    YellowOn = parts[1] == "1";
                    GreenOn  = parts[2] == "1";
                }
            }
            catch { }
        }

        // Agrega un mensaje al chat
        private void AddMessage(string text, bool isUser = false, bool isSystem = false)
            => Messages.Add(new ChatMessage { Text = text, IsUser = isUser, IsSystem = isSystem });

        public event PropertyChangedEventHandler? PropertyChanged;
        private void Notify([CallerMemberName] string? n = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(n));
    }
}
