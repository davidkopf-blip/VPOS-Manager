using Microsoft.UI;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Windows.ApplicationModel.DataTransfer;
using Windows.Graphics;
using Windows.UI;
using WinRT.Interop;

namespace DumpLoader_2._0.Views
{
    /// <summary>
    /// App-styled replacement for the default (unstyled, XamlRoot-dependent) ContentDialog for
    /// surfacing errors. Used for anything that would otherwise crash the app or block its core
    /// function - not for minor, self-resolving notices (those stay as Status panel log lines).
    /// Being a real Window rather than a ContentDialog, it never depends on a XamlRoot being
    /// available, which makes it safe to show from anywhere, including global exception handlers.
    /// </summary>
    public sealed partial class ErrorWindow : Window
    {
        private readonly string? _details;

        public ErrorWindow(string title, string message, string? details)
        {
            this.InitializeComponent();

            var hwnd = WindowNative.GetWindowHandle(this);
            var windowId = Win32Interop.GetWindowIdFromWindow(hwnd);
            var appWindow = AppWindow.GetFromWindowId(windowId);
            appWindow?.Resize(new SizeInt32(560, 320));

            SetUpCustomTitleBar(appWindow);

            ErrorTitleText.Text = title;
            ErrorMessageText.Text = message;
            _details = details;

            if (string.IsNullOrWhiteSpace(details))
            {
                DetailsToggleButton.Visibility = Visibility.Collapsed;
                CopyDetailsButton.Visibility = Visibility.Collapsed;
            }
            else
            {
                DetailsTextBlock.Text = details;
            }
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

        private void DetailsToggleButton_Click(object sender, RoutedEventArgs e)
        {
            var expanding = DetailsBorder.Visibility != Visibility.Visible;
            DetailsBorder.Visibility = expanding ? Visibility.Visible : Visibility.Collapsed;
            DetailsToggleButton.Content = expanding ? "Hide details" : "Show details";

            var hwnd = WindowNative.GetWindowHandle(this);
            var windowId = Win32Interop.GetWindowIdFromWindow(hwnd);
            var appWindow = AppWindow.GetFromWindowId(windowId);
            var currentSize = appWindow?.Size ?? new SizeInt32(560, 320);
            appWindow?.Resize(new SizeInt32(currentSize.Width, expanding ? 520 : 320));
        }

        private void CopyDetailsButton_Click(object sender, RoutedEventArgs e)
        {
            var package = new DataPackage();
            package.SetText(_details ?? ErrorMessageText.Text);
            Clipboard.SetContent(package);
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}
