using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using Microsoft.UI.Xaml.Shapes;
using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.ApplicationModel;
using Windows.ApplicationModel.Activation;
using Windows.Foundation;
using Windows.Foundation.Collections;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

using DumpLoader_2._0.Views;

namespace DumpLoader_2._0
{
    /// <summary>
    /// Provides application-specific behavior to supplement the default Application class.
    /// </summary>
    public partial class App : Application
    {
        private Window? _window;

        /// <summary>
        /// Initializes the singleton application object.  This is the first line of authored code
        /// executed, and as such is the logical equivalent of main() or WinMain().
        /// </summary>
        public App()
        {
            try
            {
                InitializeComponent();
            }
            catch (Exception ex)
            {
                try
                {
                    LogStartupException(ex, "InitializeComponent");
                }
                catch { }
                throw;
            }

            // global handlers to capture startup/runtime issues in packaged environments
            this.UnhandledException += App_UnhandledException;
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
            System.Threading.Tasks.TaskScheduler.UnobservedTaskException += TaskScheduler_UnobservedTaskException;

            // ensure session files/folder exist (timestamped log files)
            DumpLoader_2._0.Services.SessionFiles.InitializeSessionFiles();

            // Lets ErrorReportingService.ShowError marshal onto the UI thread from anywhere,
            // including these very exception handlers. The App constructor always runs on the UI
            // thread, which already has a DispatcherQueue by this point in WinUI 3.
            var dispatcherQueue = Microsoft.UI.Dispatching.DispatcherQueue.GetForCurrentThread();
            if (dispatcherQueue != null)
                DumpLoader_2._0.Services.ErrorReportingService.Initialize(dispatcherQueue);
        }

        /// <summary>
        /// Invoked when the application is launched.
        /// </summary>
        /// <param name="args">Details about the launch request and process.</param>
        protected override void OnLaunched(Microsoft.UI.Xaml.LaunchActivatedEventArgs args)
        {
            try
            {
                // If app is launched with a file path argument, pass it to the main window
                var initialArg = string.IsNullOrWhiteSpace(args.Arguments) ? null : args.Arguments.Trim('"');
                _window = new Views.MainWindow(initialArg);
                _window.Activate();
            }
            catch (Exception ex)
            {
                try
                {
                    LogStartupException(ex, "OnLaunched");
                }
                catch { }
                throw;
            }
        }

        private void App_UnhandledException(object? sender, Microsoft.UI.Xaml.UnhandledExceptionEventArgs e)
        {
            try
            {
                LogStartupException(e.Exception, "Application.UnhandledException");
            }
            catch { }

            // Without this, any unhandled exception on the UI thread (even from a single bad
            // event handler) takes down the entire process. We log it above, surface it to the
            // user via the app-styled error window, and mark it handled so a non-fatal bug
            // doesn't kill the whole app.
            try
            {
                DumpLoader_2._0.Services.ErrorReportingService.ShowError(
                    "Ein unerwarteter Fehler ist aufgetreten. VPOS Manager konnte weiterlaufen; Details wurden protokolliert.",
                    e.Exception,
                    "Unerwarteter Fehler");
            }
            catch { }

            e.Handled = true;
        }

        private void CurrentDomain_UnhandledException(object? sender, System.UnhandledExceptionEventArgs e)
        {
            try
            {
                if (e.ExceptionObject is Exception ex)
                {
                    LogStartupException(ex, "CurrentDomain.UnhandledException");

                    // This handler fires for exceptions that escaped a non-UI thread entirely -
                    // by the time we get here the process is almost always already terminating
                    // (e.IsTerminating), so this is best-effort only: logging above is the part
                    // that reliably survives.
                    DumpLoader_2._0.Services.ErrorReportingService.ShowError(
                        "Ein schwerwiegender Fehler ist auf einem Hintergrundthread aufgetreten.",
                        ex,
                        "Kritischer Fehler");
                }
            }
            catch { }
        }

        private void TaskScheduler_UnobservedTaskException(object? sender, System.Threading.Tasks.UnobservedTaskExceptionEventArgs e)
        {
            try
            {
                LogStartupException(e.Exception, "TaskScheduler.UnobservedTaskException");
                // Mark observed so the finalizer thread doesn't also crash the process over this.
                e.SetObserved();
            }
            catch { }
        }

        private void LogStartupException(Exception ex, string context)
        {
            try
            {
                var path = DumpLoader_2._0.Services.SessionFiles.StartupLogPath ?? System.IO.Path.Combine(DumpLoader_2._0.Services.LocalPaths.GetDumpLoaderFolder(), "startup-error.log");
                var text = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] Context: {context}\r\n{ex}\r\n\r\n";
                File.AppendAllText(path, text);
            }
            catch { }
        }
    }
}
