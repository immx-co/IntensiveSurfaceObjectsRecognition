using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Reactive;
using System.Text;
using System.Threading.Tasks;

namespace ObjectsRecognitionUI.ViewModels;

public class MainViewModel
{
    #region Commands
    public ReactiveCommand<Unit, Unit> ImageBackCommand { get; }

    public ReactiveCommand<Unit, Unit> ImageForwardCommand { get; }

    public ReactiveCommand<Unit, Unit> SendImageCommand { get; }

    public ReactiveCommand<Unit, Unit> SendFolderCommand { get; }

    public ReactiveCommand<Unit, Unit> SendVideoCommand { get; }

    public ReactiveCommand<Unit, Unit> ConnectCommand { get; }
    #endregion

    #region Constructors
    public MainViewModel() { }
    #endregion
}
