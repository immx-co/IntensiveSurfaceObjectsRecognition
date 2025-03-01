using ReactiveUI;
using System;
using System.Threading;

namespace ObjectsRecognitionUI.ViewModels
{
    public class ConfigurationWindowViewModel : ReactiveObject, IRoutableViewModel
    {
        #region Private Fields
        private string _connectionString;

        private string _url;
        #endregion
        public IScreen HostScreen { get; }

        public string UrlPathSegment { get; } = Guid.NewGuid().ToString().Substring(0, 5);

        public CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();

        public string ConnectionString
        {
            get => _connectionString;
            set => this.RaiseAndSetIfChanged(ref _connectionString, value);
        }

        public string Url
        {
            get => _url;
            set => this.RaiseAndSetIfChanged(ref _url, value);
        }

        public ConfigurationWindowViewModel(IScreen screen)
        {
            HostScreen = screen;
        }
    }
}
