using System;
using System.Threading.Tasks;
using Microsoft.UI;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
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

        public SettingsWindow(MainViewModel mainViewModel)
        {
            _mainViewModel = mainViewModel;
            this.InitializeComponent();

            var hwnd = WindowNative.GetWindowHandle(this);
            var windowId = Win32Interop.GetWindowIdFromWindow(hwnd);
            var appWindow = AppWindow.GetFromWindowId(windowId);
            // Generous margin over the content's actual size (title bar + border padding +
            // stacked rows) so nothing gets clipped - a too-small window here previously left
            // the Browse/Close buttons unreachable outside the visible client area.
            appWindow?.Resize(new SizeInt32(560, 360));

            SetUpCustomTitleBar(appWindow);

            DumpEditorPathTextBox.Text = _mainViewModel.DumpEditorExePath ?? string.Empty;
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

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}
