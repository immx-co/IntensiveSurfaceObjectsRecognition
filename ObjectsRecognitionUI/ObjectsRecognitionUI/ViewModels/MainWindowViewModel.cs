using ReactiveUI;
using System.Threading;
using System;

namespace ObjectsRecognitionUI.ViewModels
{
    public class MainWindowViewModel : ReactiveObject, IRoutableViewModel
    {
        public IScreen HostScreen { get; }

        public string UrlPathSegment { get; } = Guid.NewGuid().ToString().Substring(0, 5);

        public CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();
        public string Greeting { get; } = "Welcome to Main Window!";

        public MainWindowViewModel(IScreen screen)
        {
            HostScreen = screen;
        }
    }
}
