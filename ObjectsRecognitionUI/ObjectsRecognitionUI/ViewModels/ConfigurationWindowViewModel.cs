using ReactiveUI;
using System;
using System.Threading;

namespace ObjectsRecognitionUI.ViewModels
{
    public class ConfigurationWindowViewModel : ReactiveObject, IRoutableViewModel
    {
        public IScreen HostScreen { get; }

        public string UrlPathSegment { get; } = Guid.NewGuid().ToString().Substring(0, 5);

        public CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();
        public string Greeting { get; } = "Welcome to Configuration!";
    }
}
