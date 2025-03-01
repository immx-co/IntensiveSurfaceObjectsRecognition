using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.ReactiveUI;
using ObjectsRecognitionUI.ViewModels;
using ReactiveUI;

namespace ObjectsRecognitionUI.Views;

public partial class ConfigurationView : ReactiveUserControl<ConfigurationViewModel>
{
    public ConfigurationView()
    {
        this.WhenActivated(disposables => { });
        AvaloniaXamlLoader.Load(this);
    }
}