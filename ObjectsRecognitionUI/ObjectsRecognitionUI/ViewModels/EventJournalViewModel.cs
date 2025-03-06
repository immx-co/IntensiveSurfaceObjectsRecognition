using Avalonia.Collections;
using ReactiveUI;
using System;
using System.Threading;

namespace ObjectsRecognitionUI.ViewModels;

public class EventJournalViewModel : ReactiveObject, IRoutableViewModel
{
    #region Private Fields
    private AvaloniaList<string> _eventResults;
    #endregion

    #region View Model Settings
    public IScreen HostScreen { get; }
    public string UrlPathSegment { get; } = Guid.NewGuid().ToString().Substring(0, 5);

    public CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();
    #endregion

    #region Properties
    public AvaloniaList<string> EventResults
    {
        get => _eventResults;
        set => this.RaiseAndSetIfChanged(ref _eventResults, value);
    }
    #endregion

    #region Constructor
    public EventJournalViewModel(IScreen screen)
    {
        HostScreen = screen;
        _eventResults = new AvaloniaList<string>();
    }
    #endregion
}
