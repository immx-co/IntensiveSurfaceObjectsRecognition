using Avalonia.Collections;
using Avalonia.Metadata;
using ClassLibrary.Database.Models;
using ObjectsRecognitionUI.Services;
using ReactiveUI;
using Serilog;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Reactive;
using System.Threading;

namespace ObjectsRecognitionUI.ViewModels;

public class EventJournalViewModel : ReactiveObject, IRoutableViewModel
{
    #region Private Fields
    private AvaloniaList<string> _eventResults;

    private AvaloniaList<RectItem> _rectItems;

    private Avalonia.Media.Imaging.Bitmap _currentImage;

    private Dictionary<string, Avalonia.Media.Imaging.Bitmap> _imagesDictionary;

    private RectItemService _rectItemService;

    private string _title;

    private string _selectedEventResult;

    private AvaloniaList<LegendItem> _legendItems;
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

    public string SelectedEventResult 
    { 
        get => _selectedEventResult; 
        set
        {
            _selectedEventResult = value;
            if (_selectedEventResult != null) Render();
            else Clear();
        }
    }

    public AvaloniaList<RectItem> RectItems
    {
        get => _rectItems;
        set => this.RaiseAndSetIfChanged(ref _rectItems, value);
    }

    public Avalonia.Media.Imaging.Bitmap CurrentImage
    {
        get => _currentImage;
        set => this.RaiseAndSetIfChanged(ref _currentImage, value);
    }

    public Dictionary<string, Avalonia.Media.Imaging.Bitmap> ImagesDictionary
    {
        get => _imagesDictionary;
        set => this.RaiseAndSetIfChanged(ref _imagesDictionary, value);
    }

    public string Title
    {
        get => _title;
        set => this.RaiseAndSetIfChanged(ref _title, value);
    }

    public AvaloniaList<LegendItem> LegendItems
    {
        get => _legendItems;
        set => this.RaiseAndSetIfChanged(ref _legendItems, value);
    }
    #endregion

    #region Constructor
    public EventJournalViewModel(IScreen screen, RectItemService rectItemService)
    {
        HostScreen = screen;
        _rectItemService = rectItemService;
        _eventResults = new AvaloniaList<string>();

        LegendItems = new AvaloniaList<LegendItem>
        {
            new LegendItem { ClassName = "human", Color = "Green" },
            new LegendItem { ClassName = "wind/sup-board", Color = "Red" },
            new LegendItem { ClassName = "bouy", Color = "Blue" },
            new LegendItem { ClassName = "sailboat", Color = "Yellow" },
            new LegendItem { ClassName = "kayak", Color = "Purple" }
        };
    }
    #endregion

    #region Private Methods
    private void Render()
    {
        Log.Information("Start render event journal image");
        Log.Debug("EventJournalViewModel.Render: Start");
        var result = ParseSelectedEventResult();
        InitRectItem(result);
        CurrentImage = ImagesDictionary[result.Name];
        Title = result.Name;
        Log.Information("End render event journal image");
        Log.Debug("EventJournalViewModel.Render: Done; Title: {@Title}; Event Result: {@EventResult}", Title, result);
    }

    private void InitRectItem(EventResult eventResult)
    {
        Log.Debug("EventJournalViewModel.InitRectItem: Start");
        RecognitionResult recognitionResult = new RecognitionResult()
        {
            ClassName = eventResult.Class,
            X = eventResult.X,
            Y = eventResult.Y,
            Width = eventResult.Width,
            Height = eventResult.Height
        };

        RectItems = [_rectItemService.InitRect(recognitionResult, ImagesDictionary[eventResult.Name])];
        Log.Debug("EventJournalViewModel.InitRectItem: Done; Recognition Result: {@RecognitionResult}", recognitionResult);
    }

    private EventResult ParseSelectedEventResult()
    {
        var values = SelectedEventResult.Split("; ");
        return new EventResult
        {
            Name = values[0].Split(": ")[1],
            Class = values[1].Split(": ")[1],
            X = Convert.ToInt32(values[2].Split(": ")[1]),
            Y = Convert.ToInt32(values[3].Split(": ")[1]),
            Width = Convert.ToInt32(values[4].Split(": ")[1]),
            Height = Convert.ToInt32(values[5].Split(": ")[1])
        };
    }

    private void Clear()
    {
        CurrentImage = null;
        Title = String.Empty;
        RectItems = null;
    }

    #endregion

    private class EventResult
    {
        public string Name { get; set; }

        public string Class { get; set; }

        public int X { get; set; }

        public int Y { get; set; }

        public int Width { get; set; }

        public int Height { get; set; }
    }
}
