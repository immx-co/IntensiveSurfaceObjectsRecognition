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

namespace ObjectsRecognitionUI
{
    public partial class App : Application
    {
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

                IServiceCollection servicesCollection = new ServiceCollection();

                servicesCollection.AddSingleton<IScreen, IScreenRealization>();

                servicesCollection.AddSingleton<NavigationViewModel>();
                servicesCollection.AddSingleton<MainViewModel>();
                servicesCollection.AddSingleton<ConfigurationWindowViewModel>();

                servicesCollection.AddTransient(x => new FilesService(desktop.MainWindow));

                ServiceProvider servicesProvider = servicesCollection.BuildServiceProvider();
                desktop.MainWindow = new NavigationWindow
                {
                    DataContext = servicesProvider.GetService<NavigationViewModel>()
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