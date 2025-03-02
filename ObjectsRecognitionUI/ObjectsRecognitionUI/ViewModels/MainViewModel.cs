using Avalonia.Collections;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform.Storage;
using ClassLibrary.Database;
using ClassLibrary.Database.Models;
using DynamicData.Kernel;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MsBox.Avalonia;
using ObjectsRecognitionUI.Services;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Net.Http;
using System.Reactive;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace ObjectsRecognitionUI.ViewModels;

public class MainViewModel : ReactiveObject, IRoutableViewModel
{
    #region Private Fields
    private Bitmap? _currentImage;

    private List<Bitmap?> _imageFilesBitmap = new();

    private List<IStorageFile>? _imageFiles = new();

    private IStorageFile? _imageFile;

    private Bitmap? _imageFileBitmap;

    private FilesService _filesService;

    private readonly IConfiguration _configuration;

    private IServiceProvider _serviceProvider;

    private ISolidColorBrush _connectionStatus;

    private bool _isLoading;

    private AvaloniaList<RectItem> _rectItems;
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

    #region Properties
    public AvaloniaList<RectItem> RectItems
    {
        get => _rectItems;
        set => this.RaiseAndSetIfChanged(ref _rectItems, value);
    }

    private Bitmap? CurrentImage
    {
        get => _currentImage;
        set => this.RaiseAndSetIfChanged(ref _currentImage, value);
    }

    public ISolidColorBrush ConnectionStatus
    {
        get => _connectionStatus;
        private set => this.RaiseAndSetIfChanged(ref _connectionStatus, value);
    }

    public bool IsLoading
    {
        get => _isLoading;
        set => this.RaiseAndSetIfChanged(ref _isLoading, value);
    }
    #endregion
    #region Constructors
    public MainViewModel(IScreen screen, FilesService filesService, IConfiguration configuration, IServiceProvider serviceProvider)
    {
        HostScreen = screen;
        _filesService = filesService;
        _configuration = configuration;
        _serviceProvider = serviceProvider;

        ConnectionStatus = Brushes.Gray;

        ConnectCommand = ReactiveCommand.CreateFromTask(CheckHealth);
        SendImageCommand = ReactiveCommand.CreateFromTask(OpenImageFile);
        SendFolderCommand = ReactiveCommand.CreateFromTask(OpenFolder);
    }
    #endregion

    #region Public Methods
    private async Task OpenImageFile()
    {
        try
        {
            IsLoading = true;
            var file = await _filesService.OpenImageFileAsync();
            if (file != null)
            {
                _imageFile = file;
                _imageFileBitmap = new Bitmap(await file.OpenReadAsync());

                /// Тут мы посылаем изображение на нейросетевой сервис
                /// 
                /// ответ от сервиса.
                var recognitionResult1 = new RecognitionResult
                {
                    ClassName = "human",
                    X = 100,
                    Y = 100,
                    Width = 100,
                    Height = 100
                };
                var recognitionResult2 = new RecognitionResult
                {
                    ClassName = "bouy",
                    X = 960,
                    Y = 540,
                    Width = 100,
                    Height = 100
                };

                var recognitionResult3 = new RecognitionResult
                {
                    ClassName = "kayak",
                    X = 1870,
                    Y = 1030,
                    Width = 100,
                    Height = 100
                };
                await SaveRecognitionResultAsync(recognitionResult1);

                var items = new AvaloniaList<RectItem>
                {
                    InitRect(recognitionResult1),
                    InitRect(recognitionResult2),
                    InitRect(recognitionResult3)
                };

                RectItems = items;

                CurrentImage = _imageFileBitmap;
            };
        }
        finally
        {
            IsLoading = false;
        }
    }

    private RectItem InitRect(RecognitionResult recognitionResult)
    {
        double widthImage = _imageFileBitmap.Size.Width;
        double heightImage = _imageFileBitmap.Size.Height;

        double k1 = widthImage / 500;
        double k2 = heightImage / 300;

        if (k1 > k2)
        {
            widthImage /= k1;
            heightImage /= k1;
        }
        else
        {
            widthImage /= k2;
            heightImage /= k2;
        }

        double xCenter = widthImage * (recognitionResult.X / _imageFileBitmap.Size.Width) + (500 - widthImage) / 2;
        double yCenter = heightImage * (recognitionResult.Y / _imageFileBitmap.Size.Height) + (300 - heightImage) / 2;

        int width = (int)(widthImage * (recognitionResult.Width / _imageFileBitmap.Size.Width));
        int height = (int)(heightImage * (recognitionResult.Height / _imageFileBitmap.Size.Height));

        int x = (int)(xCenter - width / 2);
        int y = (int)(yCenter - height / 2);

        string color = recognitionResult.ClassName switch
        {
            "human" => "Green",
            "sup-board" => "Red",
            "bouy" => "Blue",
            "sailboat" => "Yellow",
            "kayak" => "Purple"
        };

        return new RectItem
        {
            X = x,
            Y = y,
            Width = width,
            Height = height,
            Color = color
        };
    }


    private async Task OpenFolder()
    {
        ;
    }

    private async Task CheckHealth()
    {
        string surfaceRecognitionServiceAddress = _configuration.GetConnectionString("srsStringConnection");
        using (var client = new HttpClient())
        {
            try
            {
                var response = await client.GetAsync($"{surfaceRecognitionServiceAddress}/health");

                if (response.IsSuccessStatusCode)
                {
                    var jsonResponse = await response.Content.ReadAsStringAsync();
                    var healthResponse = JsonSerializer.Deserialize<HealthCheckResponse>(jsonResponse);
                    if (healthResponse?.StatusCode == 200)
                    {
                        ConnectionStatus = Brushes.Green;
                        ShowMessageBox("Success", $"Сервис доступен. Статус: {healthResponse.StatusCode}");
                    }
                    else
                    {
                        ConnectionStatus = Brushes.Red;
                        ShowMessageBox("Failed", $"Сервис недоступен. Статус: {healthResponse?.StatusCode}");
                    }
                }
                else
                {
                    ConnectionStatus = Brushes.Red;
                    ShowMessageBox("Failed", $"Не удалось подключиться к сервису с адресом {surfaceRecognitionServiceAddress}");
                }
            }
            catch
            {
                ConnectionStatus = Brushes.Red;
                ShowMessageBox("Failed", $"Не удалось подключиться к сервису с адресом {surfaceRecognitionServiceAddress}");
            }
        }
    }

    /// <summary>
    /// Показывает всплывающее сообщение.
    /// </summary>
    /// <param name="caption">Заголовок сообщения.</param>
    /// <param name="message">Сообщение пользователю.</param>
    public void ShowMessageBox(string caption, string message)
    {
        var messageBoxStandardWindow = MessageBoxManager.GetMessageBoxStandard(caption, message);
        messageBoxStandardWindow.ShowAsync();
    }
    #endregion

    #region Private Methods
    private async Task SaveRecognitionResultAsync(RecognitionResult recognitionResult)
    { 
        using ApplicationContext db = _serviceProvider.GetRequiredService<ApplicationContext>();
        db.RecognitionResults.AddRange(recognitionResult);
        await db.SaveChangesAsync();
    }
    #endregion

    private class HealthCheckResponse
    {
        [JsonPropertyName("status_code")]
        public int StatusCode { get; set; }

        [JsonPropertyName("datetime")]
        public DateTime Datetime { get; set; }
    }
}
