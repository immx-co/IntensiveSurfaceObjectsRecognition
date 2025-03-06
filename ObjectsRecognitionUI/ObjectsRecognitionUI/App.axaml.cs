using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Data.Core.Plugins;
using Avalonia.Markup.Xaml;
using ObjectsRecognitionUI.ViewModels;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using ReactiveUI;
using ObjectsRecognitionUI.Views;
using ObjectsRecognitionUI.Services;
using Avalonia.Controls;
using Microsoft.Extensions.Configuration;
using System;
using ClassLibrary.Database;
using Microsoft.EntityFrameworkCore;
using System.IO;

namespace ObjectsRecognitionUI
{
    public partial class App : Application
    {
        public new static App? Current => Application.Current as App;

        public Window? CurrentWindow
        {
            get
            {
                if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
                {
                    return desktop.MainWindow;
                }
                else return null;
            }
        }
        public override void Initialize()
        {
            AvaloniaXamlLoader.Load(this);
        }

        public override void OnFrameworkInitializationCompleted()
        {
            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                // Avoid duplicate validations from both Avalonia and the CommunityToolkit. 
                // More info: https://docs.avaloniaui.net/docs/guides/development-guides/data-validation#manage-validationplugins
                DisableAvaloniaDataAnnotationValidation();

                IConfiguration configuration = new ConfigurationBuilder()
                    .SetBasePath(Path.Combine(AppContext.BaseDirectory, "..\\..\\..\\"))
                    .AddJsonFile("appsettings.json")
                    .Build();

                IServiceCollection servicesCollection = new ServiceCollection();

                servicesCollection.AddSingleton<IScreen, IScreenRealization>();

                servicesCollection.AddSingleton(configuration);
                servicesCollection.AddSingleton<NavigationViewModel>();
                servicesCollection.AddSingleton<MainViewModel>();
                servicesCollection.AddSingleton<EventJournalViewModel>();
                servicesCollection.AddSingleton<ConfigurationViewModel>();

                servicesCollection.AddTransient<FilesService>();
                servicesCollection.AddTransient<VideoService>();

                servicesCollection.AddDbContext<ApplicationContext>(options => options.UseNpgsql(configuration.GetConnectionString("dbStringConnection")), ServiceLifetime.Transient);

                ServiceProvider servicesProvider = servicesCollection.BuildServiceProvider();

                desktop.MainWindow = new NavigationWindow
                {
                    DataContext = servicesProvider.GetRequiredService<NavigationViewModel>()
                };
            }

            base.OnFrameworkInitializationCompleted();
        }

        private void DisableAvaloniaDataAnnotationValidation()
        {
            // Get an array of plugins to remove
            var dataValidationPluginsToRemove =
                BindingPlugins.DataValidators.OfType<DataAnnotationsValidationPlugin>().ToArray();

            // remove each entry found
            foreach (var plugin in dataValidationPluginsToRemove)
            {
                BindingPlugins.DataValidators.Remove(plugin);
            }
        }
    }
}