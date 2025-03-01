using Avalonia.ReactiveUI;
using ObjectsRecognitionUI.ViewModels;

namespace ObjectsRecognitionUI.Views
{
    public partial class MainWindow : ReactiveWindow<MainWindowViewModel>
    {
        public MainWindow()
        {
            InitializeComponent();
        }
    }
}