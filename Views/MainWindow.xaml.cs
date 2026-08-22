using Microsoft.UI.Xaml;
using DumpLoader_2._0.ViewModels;

namespace DumpLoader_2._0.Views
{
    public sealed partial class MainWindow : Window
    {
        private readonly MainViewModel _viewModel;

        public MainWindow()
            : this(null)
        {
        }

        public MainWindow(string? initialDumpPath)
        {
            this.InitializeComponent();

            _viewModel = new MainViewModel(this, initialDumpPath);

            if (this.Content is FrameworkElement root)
            {
                root.DataContext = _viewModel;
            }
        }

        private async void AddVersionButton_Click(object sender, RoutedEventArgs e)
        {
            await _viewModel.AddVersionCommand.ExecuteAsync(null);
        }

        private async void SelectDumpButton_Click(object sender, RoutedEventArgs e)
        {
            await _viewModel.SelectDumpCommand.ExecuteAsync(null);
        }

        private async void StartVposButton_Click(object sender, RoutedEventArgs e)
        {
            await _viewModel.StartVposCommand.ExecuteAsync(null);
        }

        private async void StartVposOnlyButton_Click(object sender, RoutedEventArgs e)
        {
            await _viewModel.StartVposWithoutDumpCommand.ExecuteAsync(null);
        }
    }
}
