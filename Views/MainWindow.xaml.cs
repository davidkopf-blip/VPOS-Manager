using System;
using System.ComponentModel;
using System.IO;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Windows.UI;
using WinRT.Interop;
using DumpLoader_2._0.ViewModels;

namespace DumpLoader_2._0.Views
{
    public sealed partial class MainWindow : Window
    {
        private readonly MainViewModel _viewModel;

        private static readonly SolidColorBrush AccentBrush = new SolidColorBrush(Color.FromArgb(255, 0, 200, 83));
        private static readonly SolidColorBrush AccentHaloBrush = new SolidColorBrush(Color.FromArgb(46, 0, 200, 83));
        private static readonly SolidColorBrush DangerBrush = new SolidColorBrush(Color.FromArgb(255, 211, 47, 47));
        private static readonly SolidColorBrush DangerHaloBrush = new SolidColorBrush(Color.FromArgb(46, 211, 47, 47));

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

            // Covers the initial load (settings load asynchronously after the constructor
            // returns) and any change made from the Settings window, since it mutates this same
            // MainViewModel instance.
            _viewModel.PropertyChanged += ViewModel_PropertyChanged;
            UpdateDumpEditorStatus();

            _viewModel.TerminalLines.CollectionChanged += TerminalLines_CollectionChanged;

            this.Closed += MainWindow_Closed;
        }

        /// <summary>
        /// Keeps the terminal panel pinned to the newest output line as DumpEditor.exe writes
        /// more. UpdateLayout() first, so ScrollableHeight reflects the just-added line before we
        /// scroll to it.
        /// </summary>
        private void TerminalLines_CollectionChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            TerminalScrollViewer.UpdateLayout();
            TerminalScrollViewer.ChangeView(null, TerminalScrollViewer.ScrollableHeight, null, true);
        }

        private void ViewModel_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(MainViewModel.DumpEditorExePath))
                UpdateDumpEditorStatus();
        }

        /// <summary>
        /// Same red/green halo-dot status shown in Settings, mirrored here (top-right of the
        /// Versions row) so it's visible without opening Settings.
        /// </summary>
        private void UpdateDumpEditorStatus()
        {
            var path = _viewModel.DumpEditorExePath;
            var isValid = !string.IsNullOrEmpty(path) && File.Exists(path);

            var dotBrush = isValid ? AccentBrush : DangerBrush;
            var haloBrush = isValid ? AccentHaloBrush : DangerHaloBrush;

            MainStatusDotEllipse.Fill = dotBrush;
            MainStatusHaloEllipse.Fill = haloBrush;
            MainStatusText.Foreground = dotBrush;
            MainStatusText.Text = isValid ? "DumpEditor.exe found" : "No DumpEditor.exe selected";
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

        private void ScrollToBottomButton_Tapped(object sender, TappedRoutedEventArgs e)
        {
            LeftScrollViewer.ChangeView(null, LeftScrollViewer.ScrollableHeight, null);
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
            settingsWindow.Closed += (_, _) => UpdateDumpEditorStatus();
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
