using Avalonia.Collections;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform.Storage;
using ClassLibrary.Database;
using ClassLibrary.Database.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MsBox.Avalonia;
using ObjectsRecognitionUI.Services;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Reactive;
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

    private IStorageFile? _videoFile;

    private List<Bitmap?> _frames = new();

    private string _currentFileName;

    private FilesService _filesService;

    private VideoService _videoService;

    private readonly IConfiguration _configuration;

    private IServiceProvider _serviceProvider;

    private ISolidColorBrush _connectionStatus;

    private bool _canSwitchImages;

    private bool _isLoading;

    private bool _areButtonsEnabled;

    private bool _isVideoSelected = false;

    private AvaloniaList<string> _detectionResults;

    private AvaloniaList<RectItem> _rectItems;

    private AvaloniaList<AvaloniaList<RectItem>> _rectItemsLists;

    private int _currentNumberOfImage;

    private int _currentNumberOfFrame;
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
    public int ImageWidth { get; } = 800;

    public int ImageHeight { get; } = 400;

    public AvaloniaList<RectItem> RectItems
    {
        get => _rectItems;
        set => this.RaiseAndSetIfChanged(ref _rectItems, value);
    }

    public Bitmap? CurrentImage
    {
        get => _currentImage;
        set => this.RaiseAndSetIfChanged(ref _currentImage, value);
    }

    public string CurrentFileName
    {
        get => _currentFileName;
        set => this.RaiseAndSetIfChanged(ref _currentFileName, value);
    }

    public ISolidColorBrush ConnectionStatus
    {
        get => _connectionStatus;
        private set => this.RaiseAndSetIfChanged(ref _connectionStatus, value);
    }

    public bool CanSwitchImages
    {
        get => _canSwitchImages;
        set => this.RaiseAndSetIfChanged(ref _canSwitchImages, value);
    }

    public bool IsLoading
    {
        get => _isLoading;
        set => this.RaiseAndSetIfChanged(ref _isLoading, value);
    }
    public bool AreButtonsEnabled
    {
        get => _areButtonsEnabled;
        private set => this.RaiseAndSetIfChanged(ref _areButtonsEnabled, value);
    }

    public AvaloniaList<string> DetectionResults
    {
        get => _detectionResults;
        set => this.RaiseAndSetIfChanged(ref _detectionResults, value);
    }
    #endregion

    #region Constructors
    public MainViewModel(
        IScreen screen, 
        FilesService filesService, 
        IConfiguration configuration, 
        IServiceProvider serviceProvider,
        VideoService videoService)
    {
        HostScreen = screen;
        _filesService = filesService;
        _videoService = videoService;
        _configuration = configuration;
        _serviceProvider = serviceProvider;

        ConnectionStatus = Brushes.Gray;
        AreButtonsEnabled = false;
        _detectionResults = new AvaloniaList<string>();

        CanSwitchImages = false;

        ConnectCommand = ReactiveCommand.CreateFromTask(CheckHealthAsync);
        SendImageCommand = ReactiveCommand.CreateFromTask(OpenImageFileAsync);
        SendFolderCommand = ReactiveCommand.CreateFromTask(OpenFolderAsync);
        ImageBackCommand = ReactiveCommand.Create(Previous);
        ImageForwardCommand = ReactiveCommand.Create(Next);
        SendVideoCommand = ReactiveCommand.CreateFromTask(OpenVideoAsync);
    }
    #endregion

    #region Private Methods

    #region Command Methods
    private async Task OpenImageFileAsync()
    {
        try
        {
            IsLoading = true;
            var file = await _filesService.OpenImageFileAsync();
            if (file != null)
            {
                await InitImagesAsync(new List<IStorageFile> { file });
                CanSwitchImages = false;
                _isVideoSelected = false;
            }
        }
        finally
        {
            IsLoading = false;
        }
    }

    private async Task OpenFolderAsync()
    {
        try
        {
            var files = await _filesService.OpenImageFolderAsync();
            if (files != null)
            {
                await InitImagesAsync(files);
                CanSwitchImages = true;
                _isVideoSelected = false;
            }
        }
        catch
        {
            ShowMessageBox("Error", "В выбранной дирректории отсутвуют изображения или пристуствуют файлы с недопустимым расширением");
            return;
        }
    }

    private async Task OpenVideoAsync()
    {
        var file = await _filesService.OpenVideoFileAsync();
        if (file != null)
        {
            await InitFramesAsync(file);
            CanSwitchImages = true;
            _isVideoSelected = true;
        }
    }

    private void Next()
    {
        if (_isVideoSelected) NextFrame();
        else NextImage();
    }

    private void Previous()
    {
        if (_isVideoSelected) PreviousFrame();
        else PreviousImage();
    }
    #endregion

    #region Image Methods
    private async Task InitImagesAsync(List<IStorageFile> files)
    {
        var itemsLists = new AvaloniaList<AvaloniaList<RectItem>>();
        var filesBitmap = new List<Bitmap>();
        foreach (var file in files)
        {
            var fileBitmap = new Bitmap(await file.OpenReadAsync());
            filesBitmap.Add(fileBitmap);

            var results = await GetImageDetectionResultsAsync(file, fileBitmap);
            itemsLists.Add(results);
        }

        _imageFilesBitmap = filesBitmap;
        _rectItemsLists = itemsLists;
        _imageFiles = files;

        _currentNumberOfImage = 0;
        SetImage();
    }

    private async Task<AvaloniaList<RectItem>> GetImageDetectionResultsAsync(IStorageFile file, Bitmap fileBitmap)
    {
        List<RecognitionResult> detections = await GetImageRecognitionResultsAsync(file);
        var items = new AvaloniaList<RectItem>();
        var detectionResults = new AvaloniaList<string>();
        foreach (RecognitionResult det in detections)
        {
            try
            {
                items.Add(InitRect(det, fileBitmap));
                await SaveRecognitionResultAsync(det);
                detectionResults.Add($"Событие: Class: {det.ClassName}, X: {det.X}, Y: {det.Y}, Width: {det.Width}, Height: {det.Height}");
            }
            catch (Exception ex)
            {
                ShowMessageBox("Error", $"Ошибка при обработке детекции: {ex.Message}");
            }
        }
        DetectionResults = detectionResults;
        return items;
    }

    private async Task<List<RecognitionResult>> GetImageRecognitionResultsAsync(IStorageFile file)
    {
        string surfaceRecognitionServiceAddress = _configuration.GetConnectionString("srsStringConnection");
        using (var client = new HttpClient())
        {
            try
            {
                using (var imageStream = await file.OpenReadAsync())
                {
                    var content = new MultipartFormDataContent();
                    var imageContent = new StreamContent(imageStream);
                    imageContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("image/jpeg");
                    content.Add(imageContent, "image", file.Name);

                    var response = await client.PostAsync($"{surfaceRecognitionServiceAddress}/inference", content);

                    if (response.IsSuccessStatusCode)
                    {
                        var jsonResponse = await response.Content.ReadAsStringAsync();
                        var result = JsonSerializer.Deserialize<DetectedAndClassifiedObject>(jsonResponse);

                        if (result?.ObjectBbox != null)
                        {
                            return result.ObjectBbox.Select(bbox => new RecognitionResult
                            {
                                ClassName = bbox.ClassName,
                                X = bbox.X,
                                Y = bbox.Y,
                                Width = bbox.Width,
                                Height = bbox.Height
                            }).ToList();
                        }
                    }
                    else
                    {
                        ShowMessageBox("Error", $"Ошибка при отправке изображения: {response.StatusCode}");
                    }
                }
            }
            catch (Exception ex)
            {
                ShowMessageBox("Error", $"Ошибка при отправке изображения: {ex.Message}");
            }
        }

        return new List<RecognitionResult>();
    }

    private void NextImage()
    {
        if (_currentNumberOfImage < _imageFilesBitmap.Count - 1) _currentNumberOfImage++;
        else _currentNumberOfImage = 0;

        SetImage();
    }

    private void PreviousImage()
    {
        if (_currentNumberOfImage > 0) _currentNumberOfImage--;
        else _currentNumberOfImage = _imageFilesBitmap.Count - 1;

        SetImage();
    }

    private void SetImage()
    {
        CurrentFileName = _imageFiles[_currentNumberOfImage].Name;
        CurrentImage = _imageFilesBitmap[_currentNumberOfImage];
        RectItems = _rectItemsLists[_currentNumberOfImage];
    }
    #endregion

    #region Video Methods
    private async Task InitFramesAsync(IStorageFile file)
    {
        var itemsLists = new AvaloniaList<AvaloniaList<RectItem>>();
        var frames = await _videoService.GetFramesAsync(file);
        for (int i = 0; i < frames.Count; i++)
        {
            var results = await GetFrameDetectionResultsAsync(frames[i], i + 1);
            itemsLists.Add(results);
        }

        _videoFile = file;
        _rectItemsLists = itemsLists;
        _frames = frames;

        CurrentFileName = file.Name;
        _currentNumberOfFrame = 0;
        SetFrame();
    }

    private async Task<AvaloniaList<RectItem>> GetFrameDetectionResultsAsync(Bitmap frame, int numberOfFrame)
    {
        List<RecognitionResult> detections = await GetFrameRecognitionResultsAsync(frame, numberOfFrame);
        var items = new AvaloniaList<RectItem>();
        var detectionResults = new AvaloniaList<string>();
        foreach (RecognitionResult det in detections)
        {
            try
            {
                items.Add(InitRect(det, frame));
                await SaveRecognitionResultAsync(det);
                detectionResults.Add($"Событие: Class: {det.ClassName}, X: {det.X}, Y: {det.Y}, Width: {det.Width}, Height: {det.Height}");
            }
            catch (Exception ex)
            {
                ShowMessageBox("Error", $"Ошибка при обработке детекции: {ex.Message}");
            }
        }
        DetectionResults = detectionResults;
        return items;
    }

    private async Task<List<RecognitionResult>> GetFrameRecognitionResultsAsync(Bitmap frame, int numberOfFrame)
    {
        string surfaceRecognitionServiceAddress = _configuration.GetConnectionString("srsStringConnection");
        using (var client = new HttpClient())
        {
            try
            {
                using (MemoryStream imageStream = new())
                {
                    frame.Save(imageStream);
                    var content = new MultipartFormDataContent();
                    var imageContent = new StreamContent(imageStream);
                    imageContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("image/jpeg");
                    content.Add(imageContent, "image", $"frame{numberOfFrame}");

                    var response = await client.PostAsync($"{surfaceRecognitionServiceAddress}/inference", content);

                    if (response.IsSuccessStatusCode)
                    {
                        var jsonResponse = await response.Content.ReadAsStringAsync();
                        var result = JsonSerializer.Deserialize<DetectedAndClassifiedObject>(jsonResponse);

                        if (result?.ObjectBbox != null)
                        {
                            return result.ObjectBbox.Select(bbox => new RecognitionResult
                            {
                                ClassName = bbox.ClassName,
                                X = bbox.X,
                                Y = bbox.Y,
                                Width = bbox.Width,
                                Height = bbox.Height
                            }).ToList();
                        }
                    }
                    else
                    {
                        ShowMessageBox("Error", $"Ошибка при отправке видео: {response.StatusCode}");
                    }
                }
            }
            catch (Exception ex)
            {
                ShowMessageBox("Error", $"Ошибка при отправке видео: {ex.Message}");
            }
        }

        return new List<RecognitionResult>();
    }

    private void NextFrame()
    {
        if (_currentNumberOfFrame < _frames.Count - 1) _currentNumberOfFrame++;
        else _currentNumberOfFrame = 0;

        SetFrame();
    }

    private void PreviousFrame()
    {
        if (_currentNumberOfFrame > 0) _currentNumberOfFrame--;
        else _currentNumberOfFrame = _frames.Count - 1;

        SetFrame();
    }

    private void SetFrame()
    {
        CurrentImage = _frames[_currentNumberOfFrame];
        RectItems = _rectItemsLists[_currentNumberOfFrame];
    }
    #endregion

    #region Rect Drawing Methods
    private RectItem InitRect(RecognitionResult recognitionResult, Bitmap file)
    {
        double widthImage = file.Size.Width;
        double heightImage = file.Size.Height;

        double k1 = widthImage / ImageWidth;
        double k2 = heightImage / ImageHeight;

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

        double xCenter = widthImage * (recognitionResult.X / file.Size.Width) + (ImageWidth - widthImage) / 2;
        double yCenter = heightImage * (recognitionResult.Y / file.Size.Height) + (ImageHeight - heightImage) / 2;

        int width = (int)(widthImage * (recognitionResult.Width / file.Size.Width));
        int height = (int)(heightImage * (recognitionResult.Height / file.Size.Height));

        int x = (int)(xCenter - width / 2);
        int y = (int)(yCenter - height / 2);

        string color = recognitionResult.ClassName switch
        {
            "human" => "Green",
            "wind/sup-board" => "Red",
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
    #endregion

    #region Client Methods
    private async Task CheckHealthAsync()
    {
        AreButtonsEnabled = false;
        ConnectionStatus = Brushes.Red;
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
                        AreButtonsEnabled = true;
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
    #endregion

    #region Data Base Methods
    private async Task SaveRecognitionResultAsync(RecognitionResult recognitionResult)
    {
        using ApplicationContext db = _serviceProvider.GetRequiredService<ApplicationContext>();
        db.RecognitionResults.AddRange(recognitionResult);
        await db.SaveChangesAsync();
    }
    #endregion

    #endregion

    #region Public Methods
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

    #region Classes
    private class HealthCheckResponse
    {
        [JsonPropertyName("status_code")]
        public int StatusCode { get; set; }

        [JsonPropertyName("datetime")]
        public DateTime Datetime { get; set; }
    }

    public class InferenceResult
    {
        [JsonPropertyName("class_name")]
        public string ClassName { get; set; }

        [JsonPropertyName("x")]
        public int X { get; set; }

        [JsonPropertyName("y")]
        public int Y { get; set; }

        [JsonPropertyName("width")]
        public int Width { get; set; }

        [JsonPropertyName("height")]
        public int Height { get; set; }
    }

    public class DetectedAndClassifiedObject
    {
        [JsonPropertyName("object_bbox")]
        public List<InferenceResult> ObjectBbox { get; set; }
    }
    #endregion
}
