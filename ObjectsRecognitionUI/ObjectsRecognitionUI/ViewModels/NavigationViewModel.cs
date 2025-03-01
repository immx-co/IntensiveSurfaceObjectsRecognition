using Avalonia.Media;
using System.Reactive.Linq;
using System.Linq;
using System.Threading.Tasks;
using ReactiveUI;
using System.Reactive;
using System;
using Microsoft.Extensions.DependencyInjection;

namespace ObjectsRecognitionUI.ViewModels
{
    public class NavigationViewModel : ReactiveObject
    {
        private readonly IServiceProvider _serviceProvider;
        public RoutingState Router { get; }

        public ReactiveCommand<Unit, Unit> GoMainWindow {  get; }

        public ReactiveCommand<Unit, Unit> GoConfiguration { get; }

        public NavigationViewModel(IScreen screenRealization, IServiceProvider serviceProvider)
        {
            Router = screenRealization.Router;
            _serviceProvider = serviceProvider;

            GoMainWindow = ReactiveCommand.Create(NavigateToMainWindow);
            GoConfiguration = ReactiveCommand.Create(NavigateToConfiguration);

            Router.Navigate.Execute(_serviceProvider.GetRequiredService<MainViewModel>());
        }

        private void NavigateToMainWindow()
        {
            CheckDisposedCancelletionToken();
            Router.Navigate.Execute(_serviceProvider.GetRequiredService<MainViewModel>());
        }

        private void NavigateToConfiguration()
        {
            CheckDisposedCancelletionToken();
            Router.Navigate.Execute(_serviceProvider.GetRequiredService<ConfigurationWindowViewModel>());
        }

        private void CheckDisposedCancelletionToken()
        {
            if (Router.NavigationStack.Count > 0)
            {
                var currentViewModel = Router.NavigationStack.Last();
                if (currentViewModel is IDisposable disposableViewModel)
                {
                    disposableViewModel.Dispose();
                }
            }
        }
    }
}
