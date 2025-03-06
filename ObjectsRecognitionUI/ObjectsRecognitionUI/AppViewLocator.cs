using ObjectsRecognitionUI.ViewModels;
using ObjectsRecognitionUI.Views;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ObjectsRecognitionUI
{
    public class AppViewLocator : IViewLocator
    {
        public IViewFor ResolveView<T>(T viewModel, string contract = null) => viewModel switch
        {
            MainViewModel context => new MainView { ViewModel = context },
            EventJournalViewModel context => new EventJournalView { ViewModel = context },
            ConfigurationViewModel context => new ConfigurationView { ViewModel = context },
            _ => throw new ArgumentOutOfRangeException(nameof(viewModel))
        };
    }
}
