using Avalonia;
using Avalonia.ReactiveUI;
using Avalonia.Markup.Xaml;
using ObjectsRecognitionUI.ViewModels;
using ReactiveUI;

namespace ObjectsRecognitionUI.Views;

public partial class MainView : ReactiveUserControl<MainViewModel>
{
    public MainView()
    {
        this.WhenActivated(disposables => { });
        AvaloniaXamlLoader.Load(this);
    }
}