using IoTChat.ViewModels;

namespace IoTChat.Views
{
    public partial class MainPage : ContentPage
    {
        private readonly MainViewModel vm;

        public MainPage(MainViewModel viewModel)
        {
            InitializeComponent();
            BindingContext = viewModel;
            vm = viewModel;

#if WINDOWS
            MensajesCollection.HandlerChanged += (s, e) =>
            {
                if (MensajesCollection.Handler?.PlatformView is Microsoft.UI.Xaml.Controls.ListView lista)
                    lista.ItemContainerTransitions = new Microsoft.UI.Xaml.Media.Animation.TransitionCollection();
            };
#endif

            vm.Messages.CollectionChanged += (s, e) =>
            {
                if (e.Action == System.Collections.Specialized.NotifyCollectionChangedAction.Add && vm.Messages.Count > 0)
                {
                    ScrollAlFinal(50);
                    ScrollAlFinal(200);
                    ScrollAlFinal(500);
                }
            };

            vm.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(vm.SemaphoreLeft))
                {
                    ActualizarLayoutSemaforo(vm.SemaphoreLeft);
                }
            };
            ActualizarLayoutSemaforo(vm.SemaphoreLeft);
        }

        private void ActualizarLayoutSemaforo(bool semaphoreLeft)
        {
            if (semaphoreLeft)
            {
                MainGrid.ColumnDefinitions[0].Width = new GridLength(120);
                MainGrid.ColumnDefinitions[2].Width = GridLength.Star;
                Grid.SetColumn(SemaforoContainer, 0);
                Grid.SetColumn(MensajesCollection, 2);
                if (EmptyChatContainer != null) Grid.SetColumn(EmptyChatContainer, 2);
                MensajesCollection.FlowDirection = FlowDirection.LeftToRight;
            }
            else
            {
                MainGrid.ColumnDefinitions[0].Width = GridLength.Star;
                MainGrid.ColumnDefinitions[2].Width = new GridLength(120);
                Grid.SetColumn(MensajesCollection, 0);
                if (EmptyChatContainer != null) Grid.SetColumn(EmptyChatContainer, 0);
                Grid.SetColumn(SemaforoContainer, 2);
                MensajesCollection.FlowDirection = FlowDirection.RightToLeft;
            }
        }

        private void ScrollAlFinal(int retardoMs)
        {
            MainThread.BeginInvokeOnMainThread(async () =>
            {
                if (retardoMs > 0) await Task.Delay(retardoMs);
                try
                {
                    int total = vm.Messages.Count;
                    if (total == 0) return;

#if WINDOWS
                    if (MensajesCollection.Handler?.PlatformView is Microsoft.UI.Xaml.Controls.ListView lista)
                    {
                        var sv = BuscarScrollViewer(lista);
                        if (sv != null)
                        {
                            sv.UpdateLayout();
                            sv.ChangeView(null, sv.ScrollableHeight, null, true);
                        }
                        else MensajesCollection.ScrollTo(total - 1, position: ScrollToPosition.End, animate: false);
                    }
                    else MensajesCollection.ScrollTo(total - 1, position: ScrollToPosition.End, animate: false);
#else
                    MensajesCollection.ScrollTo(total - 1, position: ScrollToPosition.End, animate: false);
#endif
                }
                catch { }
            });
        }

#if WINDOWS
        private static Microsoft.UI.Xaml.Controls.ScrollViewer? BuscarScrollViewer(Microsoft.UI.Xaml.DependencyObject padre)
        {
            int hijos = Microsoft.UI.Xaml.Media.VisualTreeHelper.GetChildrenCount(padre);
            for (int i = 0; i < hijos; i++)
            {
                var hijo = Microsoft.UI.Xaml.Media.VisualTreeHelper.GetChild(padre, i);
                if (hijo is Microsoft.UI.Xaml.Controls.ScrollViewer sv) return sv;
                var resultado = BuscarScrollViewer(hijo);
                if (resultado != null) return resultado;
            }
            return null;
        }
#endif
    }
}
