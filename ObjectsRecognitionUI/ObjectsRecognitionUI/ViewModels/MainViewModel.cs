﻿using Avalonia.Collections;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform.Storage;
using ClassLibrary.Database;
using ClassLibrary.Database.Models;
using DynamicData;
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
using System.IO;
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

    private FilesService _filesService;

    private readonly IConfiguration _configuration;

    private IServiceProvider _serviceProvider;

    private ISolidColorBrush _connectionStatus;

    private bool _isLoading;

    private bool _areButtonsEnabled;

    private bool _areConnectButtonEnabled = true;

    private AvaloniaList<string> _detectionResults;

    private AvaloniaList<RectItem> _rectItems;

    private AvaloniaList<AvaloniaList<RectItem>> _rectItemsLists;

    private int _currentNumberOfImage;

    private AvaloniaList<LegendItem> _legendItems;
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

    public Bitmap? CurrentImage
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
    public bool AreButtonsEnabled
    {
        get => _areButtonsEnabled;
        private set => this.RaiseAndSetIfChanged(ref _areButtonsEnabled, value);
    }

    public bool AreConnectButtonEnabled
    {
        get => _areConnectButtonEnabled;
        private set => this.RaiseAndSetIfChanged(ref _areConnectButtonEnabled, value);
    }

    public AvaloniaList<string> DetectionResults
    {
        get => _detectionResults;
        set => this.RaiseAndSetIfChanged(ref _detectionResults, value);
    }

    public AvaloniaList<LegendItem> LegendItems
    {
        get => _legendItems;
        set => this.RaiseAndSetIfChanged(ref _legendItems, value);
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
        AreButtonsEnabled = false;
        _detectionResults = new AvaloniaList<string>();

        _legendItems = new AvaloniaList<LegendItem>
        {
            new LegendItem { ClassName = "human", Color = "Green" },
            new LegendItem { ClassName = "wind/sup-board", Color = "Red" },
            new LegendItem { ClassName = "bouy", Color = "Blue" },
            new LegendItem { ClassName = "sailboat", Color = "Yellow" },
            new LegendItem { ClassName = "kayak", Color = "Purple" }
        };

        ConnectCommand = ReactiveCommand.CreateFromTask(CheckHealthAsync);
        SendImageCommand = ReactiveCommand.CreateFromTask(OpenImageFileAsync);
        SendFolderCommand = ReactiveCommand.CreateFromTask(OpenFolderAsync);
        ImageBackCommand = ReactiveCommand.Create(PreviousImage);
        ImageForwardCommand = ReactiveCommand.Create(NextImage);
    }
    #endregion

    #region Private Methods
    private async Task OpenImageFileAsync()
    {
        try
        {
            IsLoading = true;
            var file = await _filesService.OpenImageFileAsync();
            if (file != null)
            {
                _imageFiles = [file];
                _imageFilesBitmap = [new Bitmap(await file.OpenReadAsync())];

                List<RecognitionResult> detections = await GetRecognitionResultsAsync(_imageFiles[0]);

                var items = await GetDetectionResultsAsync(_imageFiles[0], _imageFilesBitmap[0]);
                RectItems = items;
                CurrentImage = _imageFilesBitmap[0];
            };
        }
        finally
        {
            IsLoading = false;
        }
    }

    private async Task<AvaloniaList<RectItem>> GetDetectionResultsAsync(IStorageFile file, Bitmap fileBitmap)
    {
        List<RecognitionResult> detections = await GetRecognitionResultsAsync(file);
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

    private async Task<List<RecognitionResult>> GetRecognitionResultsAsync(IStorageFile file)
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

    private RectItem InitRect(RecognitionResult recognitionResult, Bitmap file)
    {
        if (file == null)
        {
            throw new InvalidOperationException("Изображение не загружено.");
        }

        if (recognitionResult.X < 0 || recognitionResult.Y < 0 || recognitionResult.Width <= 0 || recognitionResult.Height <= 0)
        {
            throw new ArgumentException("Некорректные координаты или размеры прямоугольника.");
        }

        double widthImage = file.Size.Width;
        double heightImage = file.Size.Height;

        if (widthImage <= 0 || heightImage <= 0)
        {
            throw new InvalidOperationException("Некорректные размеры изображения.");
        }

        double k1 = widthImage / 1250;
        double k2 = heightImage / 600;

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

        double xCenter = widthImage * (recognitionResult.X / file.Size.Width) + (1250 - widthImage) / 2;
        double yCenter = heightImage * (recognitionResult.Y / file.Size.Height) + (600 - heightImage) / 2;

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

    private async Task OpenFolderAsync()
    {
        try
        {
            var files = await _filesService.OpenImageFolderAsync();
            _imageFiles = files;
            if (files != null)
            {
                var itemsLists = new AvaloniaList<AvaloniaList<RectItem>>();
                var filesBitmap = new List<Bitmap>();
                foreach (var file in files)
                {
                    var fileBitmap = new Bitmap(await file.OpenReadAsync());
                    filesBitmap.Add(fileBitmap);

                    var results = await GetDetectionResultsAsync(file, fileBitmap);
                    itemsLists.Add(results);
                }
                _imageFilesBitmap = filesBitmap;
                _rectItemsLists = itemsLists;

                _currentNumberOfImage = 0;
                SetImage();
            }
        }
        catch
        {
            ShowMessageBox("Ошибка", "В выбранной дирректории отсутвуют изображения или пристуствуют файлы с недопустимым расширением");
            return;
        }
    }

    private void NextImage()
    {
        if (_currentNumberOfImage < _imageFilesBitmap.Count - 1) _currentNumberOfImage++;
        else _currentNumberOfImage = 0;

        SetImage();
    }

    private void PreviousImage()
    {
        if(_currentNumberOfImage > 0) _currentNumberOfImage--;
        else _currentNumberOfImage = _imageFilesBitmap.Count - 1;

        SetImage();
    }

    private void SetImage()
    {
        CurrentImage = _imageFilesBitmap[_currentNumberOfImage];
        RectItems = _rectItemsLists[_currentNumberOfImage];
    }

    private async Task CheckHealthAsync()
    {
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
                        AreConnectButtonEnabled = false;
                        ShowMessageBox("Success", $"Сервис доступен. Статус: {healthResponse.StatusCode}");
                        Task.Run(() => StartNeuralServiceWatcher(surfaceRecognitionServiceAddress));
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

    private async void StartNeuralServiceWatcher(string surfaceRecognitionServiceAddress)
    {
        int neuralWatcherTimeout = _configuration.GetSection("NeuralWatcherTimeout").Get<int>();
        while (true)
        {
            using (var client = new HttpClient())
            {
                try
                {
                    await Task.Delay(TimeSpan.FromMilliseconds(neuralWatcherTimeout));
                    var response = await client.GetAsync($"{surfaceRecognitionServiceAddress}/health");
                    if (response.IsSuccessStatusCode)
                    {
                        continue;
                    }
                    else
                    {
                        await Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() =>
                        {
                            AreConnectButtonEnabled = true;
                            AreButtonsEnabled = false;
                            ConnectionStatus = Brushes.Red;
                            ShowMessageBox("Failed", "Пропало соединение с нейросетевым сервисом, попробуйте подключиться еще раз.");
                        });
                        break;
                    }
                }
                catch (Exception)
                {
                    await Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() =>
                    {
                        AreConnectButtonEnabled = true;
                        AreButtonsEnabled = false;
                        ConnectionStatus = Brushes.Red;
                        ShowMessageBox("Failed", "Пропало соединение с нейросетевым сервисом, попробуйте подключиться еще раз.");
                    });
                    break;
                }
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

    public class LegendItem
    {
        public string ClassName { get; set; }
        public string Color { get; set; }
    }
}
