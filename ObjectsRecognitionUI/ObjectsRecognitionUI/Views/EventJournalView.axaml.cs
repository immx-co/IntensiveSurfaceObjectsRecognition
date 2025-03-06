using Avalonia.ReactiveUI;
using Avalonia.Markup.Xaml;
using ObjectsRecognitionUI.ViewModels;
using ReactiveUI;

namespace ObjectsRecognitionUI.Views;

public partial class EventJournalView : ReactiveUserControl<EventJournalViewModel>
{
    public EventJournalView()
    {
        this.WhenActivated(disposables => { });
        AvaloniaXamlLoader.Load(this);
    }
}