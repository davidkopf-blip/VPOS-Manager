using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
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

            this.Closed += MainWindow_Closed;
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

        private void SettingsMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var settingsWindow = new SettingsWindow(_viewModel);
            settingsWindow.Activate();
        }

        private async void AboutMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new ContentDialog
            {
                XamlRoot = ((FrameworkElement)this.Content).XamlRoot,
                Title = "About VPOS Manager",
                Content = "VPOS Manager\n\nSupport-Tool zum Laden von VPOS-Dumps, Verwalten laufender VPOS-Instanzen und automatisierten Bearbeiten von Dumps über den VPOS Dump Editor (DIG).",
                CloseButtonText = "OK"
            };
            await dialog.ShowAsync().AsTask();
        }

        private async void SaveCredentialsCheckBox_Click(object sender, RoutedEventArgs e)
        {
            // Click (unlike Checked/Unchecked) only fires for genuine user interaction, never
            // for the programmatic IsChecked change that happens when a previously-saved "true"
            // is restored from settings.json at startup - at which point the window may not even
            // have a XamlRoot yet, which would crash ShowAsync().
            if (sender is not CheckBox { IsChecked: true })
                return;

            var dialog = new ContentDialog
            {
                XamlRoot = ((FrameworkElement)this.Content).XamlRoot,
                Title = "Warnung",
                Content = "Benutzername und Passwort werden im Klartext in settings.json auf diesem Rechner gespeichert. Jeder mit Zugriff auf diese Datei kann sie lesen.",
                CloseButtonText = "Verstanden"
            };
            await dialog.ShowAsync().AsTask();
        }

        private void MainWindow_Closed(object sender, WindowEventArgs args)
        {
            // TextBox/PasswordBox TwoWay bindings only push to the source on LostFocus by
            // default - if the user edits a credential field and closes the app without the
            // field losing focus first, the in-memory Options value would still be stale. Pull
            // the live control values directly before the final save so nothing gets dropped.
            if (_viewModel.Options.SaveMyVectronCredentials)
            {
                _viewModel.Options.MyVectronUsername = MyVectronUsernameTextBox.Text;
                _viewModel.Options.MyVectronPassword = MyVectronPasswordBox.Password;
            }

            // Synchronous on purpose: blocking on the async save here (e.g. via
            // .GetAwaiter().GetResult()) deadlocks, because its continuations resume on this
            // same UI thread's SynchronizationContext, which is blocked waiting for them.
            _viewModel.SaveSettingsSync();
        }
    }
}
