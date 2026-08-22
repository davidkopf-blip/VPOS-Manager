using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DumpLoader_2._0.Models;
using DumpLoader_2._0.Services;
using Microsoft.UI.Xaml;

namespace DumpLoader_2._0.ViewModels
{
    public class MainViewModel : ObservableObject
    {
        private readonly SettingsService _settingsService = new SettingsService();
        private readonly FilePickerService _filePicker = new FilePickerService();
        private readonly ProcessService _processService = new ProcessService();
        private readonly DumpEditorService _dumpEditorService = new DumpEditorService();

        public ObservableCollection<VersionEntry> Versions { get; } = new();

        private VersionEntry? _selectedVersion;
        public VersionEntry? SelectedVersion
        {
            get => _selectedVersion;
            set => SetProperty(ref _selectedVersion, value);
        }


        private string? _selectedDumpPath;
        public string? SelectedDumpPath
        {
            get => _selectedDumpPath;
            set => SetProperty(ref _selectedDumpPath, value);
        }

        private bool _isBusy;
        public bool IsBusy
        {
            get => _isBusy;
            set => SetProperty(ref _isBusy, value);
        }

        public DumpModificationOptions Options { get; } = new DumpModificationOptions();

        public IAsyncRelayCommand AddVersionCommand { get; }
        public IAsyncRelayCommand SelectDumpCommand { get; }
        public IAsyncRelayCommand StartVposCommand { get; }
        public IAsyncRelayCommand StartVposWithoutDumpCommand { get; }
        public ObservableCollection<TrackedProcessViewModel> TrackedProcesses { get; } = new();

        private readonly Window _window;

        public MainViewModel(Window window, string? initialDumpPath = null)
        {
            _window = window;
            AddVersionCommand = new AsyncRelayCommand(ExecuteAddVersionAsync);
            SelectDumpCommand = new AsyncRelayCommand(ExecuteSelectDumpAsync);
            StartVposCommand = new AsyncRelayCommand(() => ExecuteStartVposAsync(loadDump: true));
            StartVposWithoutDumpCommand = new AsyncRelayCommand(() => ExecuteStartVposAsync(loadDump: false));

            // start monitor timer
            _monitorTimer = new Microsoft.UI.Xaml.DispatcherTimer { Interval = TimeSpan.FromSeconds(5) };
            _monitorTimer.Tick += MonitorTimer_Tick;
            _monitorTimer.Start();

            _ = InitializeAsync(initialDumpPath);
        }

        private readonly Microsoft.UI.Xaml.DispatcherTimer _monitorTimer;

        private void MonitorTimer_Tick(object? sender, object? e)
        {
            // snapshot to avoid collection modification during iteration
            var copy = TrackedProcesses.ToArray();
            foreach (var t in copy)
            {
                try
                {
                    if (t.Process.HasExited)
                    {
                        RemoveTrackedProcess(t);
                    }
                }
                catch
                {
                    RemoveTrackedProcess(t);
                }
            }
        }

        private void RemoveTrackedProcess(TrackedProcessViewModel vm)
        {
            if (vm == null) return;
            _window.DispatcherQueue.TryEnqueue(() =>
            {
                try { vm.Dispose(); } catch { }
                TrackedProcesses.Remove(vm);
            });
        }

        private async Task InitializeAsync(string? initialDumpPath)
        {
            IsBusy = true;
            var settings = await _settingsService.LoadAsync();
            Versions.Clear();
            foreach (var v in settings.Versions)
                Versions.Add(v);

            Options.AutomaticDumpEditing = settings.Options.AutomaticDumpEditing;
            Options.DisablePrint = settings.Options.DisablePrint;
            Options.DisableLicenseCheck = settings.Options.DisableLicenseCheck;
            Options.DisableMyVectron = settings.Options.DisableMyVectron;
            Options.DisableVectronConnect = settings.Options.DisableVectronConnect;
            Options.Placeholder1 = settings.Options.Placeholder1;
            Options.Placeholder2 = settings.Options.Placeholder2;
            Options.Placeholder3 = settings.Options.Placeholder3;
            Options.Placeholder4 = settings.Options.Placeholder4;
            Options.Placeholder5 = settings.Options.Placeholder5;

            // Do not overwrite a dump path that was selected by the user before initialization finished
            if (string.IsNullOrEmpty(SelectedDumpPath))
                SelectedDumpPath = initialDumpPath ?? settings.LastOpenedPath;
            SelectedVersion = Versions.FirstOrDefault();
            IsBusy = false;
        }

        private async Task ExecuteAddVersionAsync()
        {
            LogAction("ExecuteAddVersionAsync:start");
            var path = await _filePicker.PickExeAsync(_window);
            if (string.IsNullOrEmpty(path))
            {
                LogAction("ExecuteAddVersionAsync:cancelled");
                return;
            }

            try
            {
                var verInfo = FileVersionInfo.GetVersionInfo(path);
                var version = verInfo.FileVersion ?? verInfo.ProductVersion ?? System.IO.Path.GetFileNameWithoutExtension(path);

                var existing = Versions.FirstOrDefault(v => v.Version == version);

                LogAction($"ExecuteAddVersionAsync: found existing={existing != null}");

                // Ensure collection and SelectedVersion are modified on the UI thread
                var tcs = new TaskCompletionSource<bool>();
                _window.DispatcherQueue.TryEnqueue(() =>
                {
                    try
                    {
                        if (existing != null)
                        {
                            existing.ExePath = path;
                            LogAction($"ExecuteAddVersionAsync: updated existing ExePath={existing.ExePath}");
                        }
                        else
                        {
                            var entry = new VersionEntry { Version = version, ExePath = path };
                            Versions.Add(entry);
                            // select newly added version (object binding)
                            SelectedVersion = entry;
                            LogAction($"ExecuteAddVersionAsync: added entry Version={entry.Version}");
                        }
                        // ObservableCollection and SetProperty will raise notifications; avoid manual PropertyChanged to prevent WinRT CCW issues
                        LogAction($"ExecuteAddVersionAsync: Versions.Count={Versions.Count}, SelectedVersion={(SelectedVersion?.Version ?? "<null>")}");
                    }
                    catch (Exception ex)
                    {
                        LogAction($"ExecuteAddVersionAsync: ui-thread-exception: {ex}");
                    }
                    tcs.SetResult(true);
                });

                // wait for UI update to complete
                await tcs.Task;

                LogAction("ExecuteAddVersionAsync: after-dispatch");
                await SaveSettingsAsync();
                LogAction("ExecuteAddVersionAsync: settings-saved");
            }
            catch (Exception ex)
            {
                LogAction($"ExecuteAddVersionAsync:error: {ex}");
                // show dialog
                await ShowMessageAsync($"Fehler beim Lesen der Datei: {ex.Message}");
            }
            finally
            {
                LogAction("ExecuteAddVersionAsync:end");
            }
        }

        private async Task ExecuteSelectDumpAsync()
        {
            var path = await _filePicker.PickDumpAsync(_window);
            if (string.IsNullOrEmpty(path))
                return;

            SelectedDumpPath = path;
            await SaveSettingsAsync();
        }

        private async Task ExecuteStartVposAsync(bool loadDump)
        {
            LogAction($"ExecuteStartVposAsync:start loadDump={loadDump}");
            if (SelectedVersion == null)
            {
                await ShowMessageAsync("Bitte wählen Sie zuerst eine Version aus.");
                return;
            }

            if (loadDump && string.IsNullOrEmpty(SelectedDumpPath))
            {
                await ShowMessageAsync("Bitte wählen Sie zuerst eine Dump-Datei aus.");
                return;
            }

            var selectedEntry = SelectedVersion;
            var exePath = selectedEntry?.ExePath;
            if (string.IsNullOrEmpty(exePath))
            {
                await ShowMessageAsync("Der Pfad zur ausführbaren Datei für die gewählte Version ist nicht gesetzt.");
                return;
            }

            try
            {
                var dumpPathToLoad = SelectedDumpPath;

                if (loadDump && Options.AutomaticDumpEditing)
                {
                    LogAction("ExecuteStartVposAsync: running DumpEditor for automatic dump editing");
                    try
                    {
                        dumpPathToLoad = await _dumpEditorService.CreateEditedDumpAsync(SelectedDumpPath!, Options);
                        LogAction($"ExecuteStartVposAsync: DumpEditor produced {dumpPathToLoad}");
                    }
                    catch (Exception ex)
                    {
                        LogAction($"ExecuteStartVposAsync: DumpEditor failed: {ex}");
                        await ShowMessageAsync($"Automatische Dump-Bearbeitung fehlgeschlagen: {ex.Message}");
                        return;
                    }
                }

                var args = loadDump ? $"/LoadDump:\"{dumpPathToLoad}\"" : null;
                LogAction($"Starting process: {exePath} {args}");
                var proc = _processService.StartProcess(exePath, args);
                if (proc == null)
                {
                    LogAction("StartVpos: process start returned null");
                    await ShowMessageAsync("VPOS konnte nicht gestartet werden.");
                    return;
                }

                // create tracked VM
                var tracked = new TrackedProcessViewModel(proc, SelectedVersion.Version, ShowMessageAsync);
                tracked.RequestRemove += (vm) => RemoveTrackedProcess(vm);
                TrackedProcesses.Add(tracked);
                LogAction($"StartVpos: started PID={proc.Id}");
            }
            catch (Exception ex)
            {
                LogAction($"ExecuteStartVposAsync:error: {ex}");
                await ShowMessageAsync($"Fehler beim Starten: {ex.Message}");
            }
            finally
            {
                LogAction("ExecuteStartVposAsync:end");
            }
        }

        private async Task SaveSettingsAsync()
        {
            var settings = new AppSettings();
            settings.Versions = Versions.ToList();
            settings.LastOpenedPath = SelectedDumpPath;
            settings.Options = Options;
            await _settingsService.SaveAsync(settings);
        }

        private Task ShowMessageAsync(string text)
        {
            var dialog = new Microsoft.UI.Xaml.Controls.ContentDialog
            {
                Title = "Information",
                Content = text,
                CloseButtonText = "OK"
            };

            // Fire-and-forget show on UI thread
            _window.DispatcherQueue.TryEnqueue(() => { _ = dialog.ShowAsync().AsTask(); });
            return Task.CompletedTask;
        }

        private void LogAction(string entry)
        {
            try
            {
                var path = DumpLoader_2._0.Services.SessionFiles.ActionsLogPath ?? System.IO.Path.Combine(DumpLoader_2._0.Services.LocalPaths.GetDumpLoaderFolder(), "actions.log");
                var text = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {entry}\r\n";
                System.IO.File.AppendAllText(path, text);
            }
            catch { }
        }
    }
}
