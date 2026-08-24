using System;
using System.IO;
using Microsoft.UI.Dispatching;

namespace DumpLoader_2._0.Services
{
    /// <summary>
    /// App-wide entry point for surfacing errors that would otherwise crash the app or block its
    /// core function (an unhandled exception, DumpEditor failing, a VPP/network-drive problem, a
    /// process failing to start, etc.) - not for minor, self-resolving notices, which stay as
    /// Status panel log lines instead.
    ///
    /// Every call logs to errors.log first, then shows the app-styled <see cref="ErrorWindow"/>.
    /// Because ErrorWindow is a real Window rather than a ContentDialog, showing it never depends
    /// on a XamlRoot being available, so this is safe to call from anywhere - including global
    /// exception handlers, background threads, and code with no Window reference at all.
    /// </summary>
    public static class ErrorReportingService
    {
        private static DispatcherQueue? _dispatcherQueue;

        /// <summary>Call once at startup (from the UI thread) so ShowError can marshal onto it
        /// from any thread.</summary>
        public static void Initialize(DispatcherQueue dispatcherQueue)
        {
            _dispatcherQueue = dispatcherQueue;
        }

        public static void ShowError(string message, Exception? exception = null, string title = "An error occurred")
        {
            LogError(title, message, exception);

            void ShowWindow()
            {
                try
                {
                    var window = new Views.ErrorWindow(title, message, exception?.ToString());
                    window.Activate();
                }
                catch
                {
                    // If even the error window itself fails to show, there's nothing further we
                    // can safely do here - the failure is already on disk via LogError above.
                }
            }

            var dispatcher = _dispatcherQueue;
            if (dispatcher == null)
            {
                // Not initialized yet (e.g. a very early startup failure) - best effort, try
                // showing directly; if this isn't the UI thread it will simply fail silently,
                // which is fine since LogError already captured it.
                try { ShowWindow(); } catch { }
                return;
            }

            if (dispatcher.HasThreadAccess)
                ShowWindow();
            else
                dispatcher.TryEnqueue(ShowWindow);
        }

        private static void LogError(string title, string message, Exception? exception)
        {
            try
            {
                var path = Path.Combine(LocalPaths.GetDumpLoaderFolder(), "errors.log");
                var text = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {title}: {message}\r\n{exception}\r\n\r\n";
                File.AppendAllText(path, text);
            }
            catch
            {
                // Logging must never itself throw and mask the original error.
            }
        }
    }
}
