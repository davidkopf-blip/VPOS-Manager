using System;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Windows.UI;
using WinRT.Interop;
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

            SetUpCustomTitleBar();

            this.Closed += MainWindow_Closed;
        }

        /// <summary>
        /// Replaces the native white title bar/caption area with AppTitleBar (our own dark
        /// menu-bar-styled Grid) while keeping the OS-drawn min/max/close buttons, recolored to
        /// render directly on top of it instead of on their own separate light strip.
        /// </summary>
        private void SetUpCustomTitleBar()
        {
            this.ExtendsContentIntoTitleBar = true;
            this.SetTitleBar(AppTitleBar);

            var hwnd = WindowNative.GetWindowHandle(this);
            var windowId = Win32Interop.GetWindowIdFromWindow(hwnd);
            var appWindow = Microsoft.UI.Windowing.AppWindow.GetFromWindowId(windowId);
            var titleBar = appWindow?.TitleBar;
            if (titleBar == null)
                return;

            titleBar.BackgroundColor = Colors.Transparent;
            titleBar.InactiveBackgroundColor = Colors.Transparent;
            titleBar.ButtonBackgroundColor = Colors.Transparent;
            titleBar.ButtonInactiveBackgroundColor = Colors.Transparent;
            titleBar.ButtonForegroundColor = Colors.White;
            titleBar.ButtonInactiveForegroundColor = Color.FromArgb(255, 154, 154, 154);
            titleBar.ButtonHoverBackgroundColor = Color.FromArgb(30, 255, 255, 255);
            titleBar.ButtonHoverForegroundColor = Colors.White;
            titleBar.ButtonPressedBackgroundColor = Color.FromArgb(50, 255, 255, 255);
            titleBar.ButtonPressedForegroundColor = Colors.White;
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
