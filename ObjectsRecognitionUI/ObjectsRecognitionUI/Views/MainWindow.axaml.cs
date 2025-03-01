using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.ReactiveUI;
using ReactiveUI;
using ObjectsRecognitionUI.ViewModels;

namespace ObjectsRecognitionUI.Views
{
    public partial class MainWindow : ReactiveUserControl<MainWindowViewModel>
    {
        public MainWindow()
        {
            this.WhenActivated(disposables => { });
            AvaloniaXamlLoader.Load(this);
        }
    }
}