using Avalonia;
using Avalonia.ReactiveUI;
using Avalonia.Markup.Xaml;
using ObjectsRecognitionUI.ViewModels;

namespace ObjectsRecognitionUI.Views;

public partial class MainView : ReactiveUserControl<MainViewModel>
{
    public MainView()
    {
        InitializeComponent();
    }
}