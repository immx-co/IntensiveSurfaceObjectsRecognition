﻿using Microsoft.Extensions.Configuration;
using ReactiveUI;
using System;
using System.Reactive;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.IO;
using ClassLibrary;
using MsBox.Avalonia;

namespace ObjectsRecognitionUI.ViewModels
{
    public class ConfigurationViewModel : ReactiveObject, IRoutableViewModel
    {
        #region Private Fields
        private string _connectionString;

        private string _url;

        private int _neuralWatcherTimeout;

        private int _frameRate;

        private readonly IConfiguration _configuration;
        #endregion

        #region View Model Settings
        public IScreen HostScreen { get; }

        public string UrlPathSegment { get; } = Guid.NewGuid().ToString().Substring(0, 5);

        public CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();
        #endregion

        #region Commands
        public ReactiveCommand<Unit, Unit> SaveConfigCommand { get; }
        #endregion

        #region Properties
        public string ConnectionString
        {
            get => _connectionString;
            set => this.RaiseAndSetIfChanged(ref _connectionString, value);
        }

        public string Url
        {
            get => _url;
            set => this.RaiseAndSetIfChanged(ref _url, value);
        }

        public int FrameRate
        {
            get => _frameRate;
            set => this.RaiseAndSetIfChanged(ref _frameRate, value);
        }
        #endregion

        public int NeuralWatcherTimeout
        {
            get => _neuralWatcherTimeout;
            set => this.RaiseAndSetIfChanged(ref _neuralWatcherTimeout, value);
        }

        #region Constructors
        public ConfigurationViewModel(IScreen screen, IConfiguration configuration)
        {
            HostScreen = screen;
            _configuration = configuration;

            ConnectionString = _configuration.GetConnectionString("dbStringConnection");
            Url = _configuration.GetConnectionString("srsStringConnection");
            NeuralWatcherTimeout = _configuration.GetSection("NeuralWatcherTimeout").Get<int>();
            FrameRate = Convert.ToInt32(_configuration.GetSection("FrameRate:Value").Value);

            SaveConfigCommand = ReactiveCommand.CreateFromTask(SaveConfig);
        }
        #endregion

        #region Private Methods
        private async Task SaveConfig()
        {
            try
            {
                var appSettingsPath = Path.Combine(AppContext.BaseDirectory, "..\\..\\..\\", "appsettings.json");

                var json = await File.ReadAllTextAsync(appSettingsPath);

                var appSettings = JsonSerializer.Deserialize<AppSettings>(json);

                appSettings.ConnectionStrings.dbStringConnection = ConnectionString;
                appSettings.ConnectionStrings.srsStringConnection = Url;
                appSettings.NeuralWatcherTimeout = NeuralWatcherTimeout;
                appSettings.FrameRate.Value = FrameRate;

                var updatedJson = JsonSerializer.Serialize(appSettings, new JsonSerializerOptions { WriteIndented = true });

                await File.WriteAllTextAsync(appSettingsPath, updatedJson);

                ShowMessageBox("Success", $"Конфигурация успешно сохранена!");
            }
            catch (Exception ex)
            {
                ShowMessageBox("Failed", "Возникла ошибка при сохранении конфигурации.");
            }
        }
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
    }
}
