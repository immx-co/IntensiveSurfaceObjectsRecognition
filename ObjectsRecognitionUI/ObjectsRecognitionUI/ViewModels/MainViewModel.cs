using Avalonia.Media.Imaging;
using Avalonia.Platform.Storage;
using ObjectsRecognitionUI.Services;
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
    #region Private Fields
    private Bitmap? _currentImage;

    private List<Bitmap?> _imageFilesBitmap = new();

    private List<IStorageFile>? _imageFiles = new();

    private FilesService _filesService;

    private Bitmap? CurrentImage
    {
        get => _currentImage;
        set => this.RaiseAndSetIfChanged(ref _currentImage, value);
    }
    #endregion

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
    public MainViewModel(IScreen screen, FilesService filesService)
    {
        HostScreen = screen;
        _filesService = filesService;

        SendImageCommand = ReactiveCommand.CreateFromTask(OpenImageFile);
    }
    #endregion

    #region Public Methods
    private async Task OpenImageFile()
    {
        var file = await _filesService.OpenImageFileAsync();
        if (file != null)
        {
            _imageFiles.Add(file);
            _imageFilesBitmap.Add(new Bitmap(await file.OpenReadAsync()));
        }
    }
    #endregion
}
