using Avalonia.Markup.Xaml;
using Avalonia.ReactiveUI;
using ObjectsRecognitionUI.ViewModels;
using ReactiveUI;

namespace ObjectsRecognitionUI.Views;

public partial class VideoEventJournalView : ReactiveUserControl<VideoEventJournalViewModel>
{
    public VideoEventJournalView()
    {
        this.WhenActivated(disposables => { });
        AvaloniaXamlLoader.Load(this);
    }
}