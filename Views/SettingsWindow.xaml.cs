using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.UI;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using Windows.Graphics;
using Windows.UI;
using WinRT.Interop;
using DumpLoader_2._0.Services;
using DumpLoader_2._0.ViewModels;

namespace DumpLoader_2._0.Views
{
    public sealed partial class SettingsWindow : Window
    {
        private readonly MainViewModel _mainViewModel;
        private readonly FilePickerService _filePicker = new FilePickerService();

        private static readonly SolidColorBrush AccentBrush = new SolidColorBrush(Color.FromArgb(255, 0, 200, 83));
        private static readonly SolidColorBrush AccentHaloBrush = new SolidColorBrush(Color.FromArgb(46, 0, 200, 83));
        private static readonly SolidColorBrush DangerBrush = new SolidColorBrush(Color.FromArgb(255, 211, 47, 47));
        private static readonly SolidColorBrush DangerHaloBrush = new SolidColorBrush(Color.FromArgb(46, 211, 47, 47));

        public SettingsWindow(MainViewModel mainViewModel)
        {
            _mainViewModel = mainViewModel;
            this.InitializeComponent();

            var hwnd = WindowNative.GetWindowHandle(this);
            var windowId = Win32Interop.GetWindowIdFromWindow(hwnd);
            var appWindow = AppWindow.GetFromWindowId(windowId);
            appWindow?.Resize(new SizeInt32(670, 325));

            SetUpCustomTitleBar(appWindow);

            DumpEditorPathTextBox.Text = _mainViewModel.DumpEditorExePath ?? string.Empty;
            UpdateDumpEditorStatus();
        }

        private void SetUpCustomTitleBar(AppWindow? appWindow)
        {
            this.ExtendsContentIntoTitleBar = true;
            this.SetTitleBar(AppTitleBar);

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

        private async void BrowseButton_Click(object sender, RoutedEventArgs e)
        {
            await PickDumpEditorPathAsync();
        }

        private async Task PickDumpEditorPathAsync()
        {
            try
            {
                var path = await _filePicker.PickExeAsync(this);
                if (string.IsNullOrEmpty(path))
                    return;

                _mainViewModel.DumpEditorExePath = path;
                DumpEditorPathTextBox.Text = path;
                UpdateDumpEditorStatus();
                await _mainViewModel.SaveSettingsAsync();
            }
            catch (Exception ex)
            {
                var dialog = new Microsoft.UI.Xaml.Controls.ContentDialog
                {
                    XamlRoot = ((FrameworkElement)this.Content).XamlRoot,
                    Title = "Fehler",
                    Content = $"Datei konnte nicht ausgewählt werden: {ex.Message}",
                    CloseButtonText = "OK"
                };
                await dialog.ShowAsync().AsTask();
            }
        }

        /// <summary>
        /// Colors/labels the status dot+text red ("no valid DumpEditor.exe") or green ("found"),
        /// matching the same halo-dot look used for process status in RunningProcessesPanel.
        /// </summary>
        private void UpdateDumpEditorStatus()
        {
            var path = _mainViewModel.DumpEditorExePath;
            var isValid = !string.IsNullOrEmpty(path) && File.Exists(path);

            var dotBrush = isValid ? AccentBrush : DangerBrush;
            var haloBrush = isValid ? AccentHaloBrush : DangerHaloBrush;

            StatusDotEllipse.Fill = dotBrush;
            StatusHaloEllipse.Fill = haloBrush;
            StatusText.Foreground = dotBrush;
            StatusText.Text = isValid ? "DumpEditor.exe found" : "No DumpEditor.exe selected";
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            // Re-check in case the configured path became invalid/valid since the window opened
            // (e.g. the file was moved) so the main window's next dump-editing run reflects it.
            UpdateDumpEditorStatus();
            this.Close();
        }
    }
}
