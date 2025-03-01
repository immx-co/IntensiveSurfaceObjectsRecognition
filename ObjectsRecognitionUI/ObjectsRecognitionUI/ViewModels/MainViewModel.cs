using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Reactive;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ObjectsRecognitionUI.ViewModels;

public class MainViewModel : ReactiveObject, IRoutableViewModel
{
    #region View Model Settings
    public IScreen HostScreen { get; }
    public string UrlPathSegment { get; } = Guid.NewGuid().ToString().Substring(0, 5);

    public CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();
    #endregion

    #region Commands
    public ReactiveCommand<Unit, Unit> ImageBackCommand { get; }

    public ReactiveCommand<Unit, Unit> ImageForwardCommand { get; }

    public ReactiveCommand<Unit, Unit> SendImageCommand { get; }

    public ReactiveCommand<Unit, Unit> SendFolderCommand { get; }

    public ReactiveCommand<Unit, Unit> SendVideoCommand { get; }

    public ReactiveCommand<Unit, Unit> ConnectCommand { get; }
    #endregion

    #region Constructors
    public MainViewModel(IScreen screen)
    {
        HostScreen = screen;
    }
    #endregion
}
