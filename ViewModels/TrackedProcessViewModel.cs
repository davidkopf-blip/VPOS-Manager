using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace DumpLoader_2._0.ViewModels
{
    public class TrackedProcessViewModel : ObservableObject, IDisposable
    {
        private readonly Process _process;
        private readonly Func<string, Task> _showMessageAsync;

        public string Version { get; }
        public int ProcessId => _process.Id;
        public DateTime StartTime { get; }

        public string Status => !_process.HasExited ? "Running" : "Exited";

        public Process Process => _process;

        public IAsyncRelayCommand BringToFrontCommand { get; }
        public IAsyncRelayCommand StopProcessCommand { get; }

        public event Action<TrackedProcessViewModel>? RequestRemove;

        public TrackedProcessViewModel(Process process, string version, Func<string, Task> showMessageAsync)
        {
            _process = process ?? throw new ArgumentNullException(nameof(process));
            Version = version ?? string.Empty;
            StartTime = DateTime.Now;
            _showMessageAsync = showMessageAsync ?? throw new ArgumentNullException(nameof(showMessageAsync));

            BringToFrontCommand = new AsyncRelayCommand(BringToFrontAsync);
            StopProcessCommand = new AsyncRelayCommand(StopProcessAsync);

            try
            {
                _process.EnableRaisingEvents = true;
                _process.Exited += (_, _) => RequestRemove?.Invoke(this);
            }
            catch
            {
                // ignore
            }
        }

        private Task BringToFrontAsync()
        {
            try
            {
                var h = _process.MainWindowHandle;
                if (h == IntPtr.Zero)
                {
                    return _showMessageAsync("Kein Fensterhandle für den Prozess gefunden.");
                }

                if (IsIconic(h))
                {
                    ShowWindow(h, ShowWindowCommands.Restore);
                }

                SetForegroundWindow(h);
                return Task.CompletedTask;
            }
            catch (Exception ex)
            {
                return _showMessageAsync($"Fehler beim Aktivieren des Fensters: {ex.Message}");
            }
        }

        private async Task StopProcessAsync()
        {
            try
            {
                if (!_process.HasExited)
                {
                    try
                    {
                        _process.CloseMainWindow();
                    }
                    catch { }

                    await Task.Delay(500);

                    if (!_process.HasExited)
                    {
                        try
                        {
                            _process.Kill(entireProcessTree: true);
                        }
                        catch { }
                    }
                }
            }
            catch (Exception ex)
            {
                await _showMessageAsync($"Fehler beim Beenden des Prozesses: {ex.Message}");
            }
            finally
            {
                RequestRemove?.Invoke(this);
            }
        }

        public void Dispose()
        {
            try
            {
                _process.Exited -= (_, _) => RequestRemove?.Invoke(this);
            }
            catch { }
        }

        #region Win32
        [DllImport("user32.dll")]
        private static extern bool SetForegroundWindow(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern bool IsIconic(IntPtr hWnd);

        private enum ShowWindowCommands : int
        {
            Hide = 0,
            Normal = 1,
            ShowMinimized = 2,
            Maximize = 3,
            ShowNoActivate = 4,
            Show = 5,
            Minimize = 6,
            ShowMinNoActive = 7,
            ShowNA = 8,
            Restore = 9,
            ShowDefault = 10,
            ForceMinimize = 11
        }

        [DllImport("user32.dll")]
        private static extern bool ShowWindow(IntPtr hWnd, ShowWindowCommands nCmdShow);
        #endregion
    }
}
