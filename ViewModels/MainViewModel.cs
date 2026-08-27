using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
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

        private string? _dumpEditorExePath;
        public string? DumpEditorExePath
        {
            get => _dumpEditorExePath;
            set => SetProperty(ref _dumpEditorExePath, value);
        }

        private string? _vppFolderPath;
        /// <summary>Folder containing the per-version "VPP-{Version}.VPP" files.</summary>
        public string? VppFolderPath
        {
            get => _vppFolderPath;
            set => SetProperty(ref _vppFolderPath, value);
        }

        public DumpModificationOptions Options { get; } = new DumpModificationOptions();

        public IAsyncRelayCommand AddVersionCommand { get; }
        public IAsyncRelayCommand SelectDumpCommand { get; }
        public IAsyncRelayCommand StartVposCommand { get; }
        public IAsyncRelayCommand StartVposWithoutDumpCommand { get; }
        public IAsyncRelayCommand DeleteDataLoadDumpAndStartVposCommand { get; }
        public IAsyncRelayCommand LaunchIntoStartMenuCommand { get; }
        public ObservableCollection<TrackedProcessViewModel> TrackedProcesses { get; } = new();

        /// <summary>Live stdout/stderr from the last DumpEditor.exe run, shown in the terminal panel.</summary>
        public ObservableCollection<string> TerminalLines { get; } = new();
        private const int MaxTerminalLines = 500;

        /// <summary>
        /// Appends one line to the status panel - both DumpEditor.exe's raw stdout/stderr and our
        /// own status notifications ("VPOS started", "Loading dump...") go through here, so the
        /// panel reads as a single status log rather than just a process console.
        /// </summary>
        private void AppendStatus(string line)
        {
            _window.DispatcherQueue.TryEnqueue(() =>
            {
                TerminalLines.Add(line);
                while (TerminalLines.Count > MaxTerminalLines)
                    TerminalLines.RemoveAt(0);
            });
        }

        private static bool IsValidIPv4(string? address)
        {
            return System.Net.IPAddress.TryParse(address, out var parsed) &&
                   parsed.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork;
        }

        private readonly Window _window;

        public MainViewModel(Window window, string? initialDumpPath = null)
        {
            _window = window;
            AddVersionCommand = new AsyncRelayCommand(ExecuteAddVersionAsync);
            SelectDumpCommand = new AsyncRelayCommand(ExecuteSelectDumpAsync);
            StartVposCommand = new AsyncRelayCommand(() => ExecuteStartVposAsync(loadDump: true));
            StartVposWithoutDumpCommand = new AsyncRelayCommand(() => ExecuteStartVposAsync(loadDump: false));
            DeleteDataLoadDumpAndStartVposCommand = new AsyncRelayCommand(() => ExecuteStartVposAsync(loadDump: true, deleteData: true));
            LaunchIntoStartMenuCommand = new AsyncRelayCommand(() => ExecuteStartVposAsync(loadDump: false, startMenu: true));

            _dumpEditorService.OutputReceived += line => AppendStatus(line);

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
            Options.DisableBonVito = settings.Options.DisableBonVito;
            Options.MyVectronUsernameEnabled = settings.Options.MyVectronUsernameEnabled;
            Options.MyVectronUsername = settings.Options.MyVectronUsername;
            Options.MyVectronPasswordEnabled = settings.Options.MyVectronPasswordEnabled;
            Options.MyVectronPassword = settings.Options.MyVectronPassword;
            Options.SaveMyVectronCredentials = settings.Options.SaveMyVectronCredentials;
            Options.IsTestServer = settings.Options.IsTestServer;
            Options.SetTcpIpInterface20 = settings.Options.SetTcpIpInterface20;
            Options.Interface20IpAddress = settings.Options.Interface20IpAddress;
            Options.SetAllPrintersToInterface = settings.Options.SetAllPrintersToInterface;
            Options.PrinterDriverNumber = settings.Options.PrinterDriverNumber ?? "20";
            Options.DisableKeyboardSound = settings.Options.DisableKeyboardSound;
            Options.DisableErrorSound = settings.Options.DisableErrorSound;
            Options.EnableVectronConnect = settings.Options.EnableVectronConnect;
            Options.VectronConnectId = settings.Options.VectronConnectId;
            Options.VectronConnectPassword = settings.Options.VectronConnectPassword;
            Options.SaveVectronConnectCredentials = settings.Options.SaveVectronConnectCredentials;
            Options.SetTcpIpInterface19 = settings.Options.SetTcpIpInterface19;
            Options.Interface19IpAddress = settings.Options.Interface19IpAddress;
            Options.AddShift4TerminalToInterface18 = settings.Options.AddShift4TerminalToInterface18;

            DumpEditorExePath = settings.DumpEditorExePath;
            VppFolderPath = settings.VppFolderPath;

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
                ErrorReportingService.ShowError($"Fehler beim Lesen der Datei: {ex.Message}", ex, "Version konnte nicht hinzugefügt werden");
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

        private async Task ExecuteStartVposAsync(bool loadDump, bool deleteData = false, bool startMenu = false)
        {
            LogAction($"ExecuteStartVposAsync:start loadDump={loadDump} deleteData={deleteData} startMenu={startMenu}");
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

                if (deleteData)
                {
                    AppendStatus(Options.AutomaticDumpEditing
                        ? "Deleting DATA folder, loading dump, and starting VPOS..."
                        : "Deleting DATA folder, loading dump (automatic dump editing is off), and starting VPOS...");

                    try
                    {
                        var exeDirectory = Path.GetDirectoryName(exePath);
                        var dataFolder = !string.IsNullOrEmpty(exeDirectory) ? Path.Combine(exeDirectory, "DATA") : null;
                        if (!string.IsNullOrEmpty(dataFolder) && Directory.Exists(dataFolder))
                        {
                            LogAction($"ExecuteStartVposAsync: deleting DATA folder {dataFolder}");
                            Directory.Delete(dataFolder, recursive: true);
                            AppendStatus("DATA folder deleted.");
                        }
                        else
                        {
                            AppendStatus("No DATA folder found - nothing to delete.");
                        }
                    }
                    catch (Exception ex)
                    {
                        LogAction($"ExecuteStartVposAsync: deleting DATA folder failed: {ex}");
                        AppendStatus($"Deleting DATA folder failed: {ex.Message}");
                        ErrorReportingService.ShowError($"Löschen des DATA-Ordners fehlgeschlagen: {ex.Message}", ex, "DATA-Ordner konnte nicht gelöscht werden");
                        return;
                    }
                }
                else if (loadDump)
                {
                    AppendStatus(Options.AutomaticDumpEditing
                        ? "Loading dump and starting VPOS..."
                        : "Loading dump (automatic dump editing is off) and starting VPOS...");
                }
                else if (startMenu)
                {
                    AppendStatus("Starting VPOS into Start Menu...");
                }
                else
                {
                    AppendStatus("Starting VPOS...");
                }

                if (loadDump && Options.AutomaticDumpEditing)
                {
                    if (string.IsNullOrEmpty(DumpEditorExePath))
                    {
                        AppendStatus("DumpEditor.exe path not configured - aborted.");
                        ErrorReportingService.ShowError("Der Pfad zu DumpEditor.exe ist nicht konfiguriert. Bitte unter Settings festlegen.", title: "DumpEditor.exe nicht konfiguriert");
                        return;
                    }

                    if (Options.DisableVectronConnect && Options.EnableVectronConnect)
                    {
                        AppendStatus("Disable VectronConnect and VectronConnect are both checked - aborted.");
                        ErrorReportingService.ShowError("\"Disable VectronConnect\" und \"VectronConnect\" können nicht gleichzeitig aktiviert sein.", title: "Widersprüchliche VectronConnect-Einstellung");
                        return;
                    }

                    if (Options.SetTcpIpInterface20 && !IsValidIPv4(Options.Interface20IpAddress))
                    {
                        AppendStatus("Invalid IPv4 address for Interface 20 - aborted.");
                        ErrorReportingService.ShowError("Bitte eine gültige IPv4-Adresse für Interface 20 angeben (z. B. 192.168.1.100).", title: "Ungültige IP-Adresse");
                        return;
                    }

                    if (Options.SetTcpIpInterface20 && Options.SetAllPrintersToInterface &&
                        (!int.TryParse(Options.PrinterDriverNumber, out var driverNo) || driverNo < 1 || driverNo > 20))
                    {
                        AppendStatus("Invalid printer driver number - aborted.");
                        ErrorReportingService.ShowError("Bitte eine gültige Treibernummer (1-20) für die Drucker angeben.", title: "Ungültige Treibernummer");
                        return;
                    }

                    if (Options.SetTcpIpInterface19 && !IsValidIPv4(Options.Interface19IpAddress))
                    {
                        AppendStatus("Invalid IPv4 address for Interface 19 - aborted.");
                        ErrorReportingService.ShowError("Bitte eine gültige IPv4-Adresse für Interface 19 angeben (z. B. 192.168.1.100).", title: "Ungültige IP-Adresse");
                        return;
                    }

                    if (Options.EnableVectronConnect &&
                        (string.IsNullOrWhiteSpace(Options.VectronConnectId) || string.IsNullOrWhiteSpace(Options.VectronConnectPassword)))
                    {
                        AppendStatus("VectronConnect Connect ID/password missing - aborted.");
                        ErrorReportingService.ShowError("Bitte Connect ID und VC Password für VectronConnect angeben.", title: "VectronConnect-Zugangsdaten fehlen");
                        return;
                    }

                    LogAction("ExecuteStartVposAsync: running DumpEditor for automatic dump editing");
                    AppendStatus("Running DumpEditor to apply dump modifications...");
                    try
                    {
                        dumpPathToLoad = await _dumpEditorService.CreateEditedDumpAsync(DumpEditorExePath, SelectedDumpPath!, Options, selectedEntry?.Version, VppFolderPath);
                        LogAction($"ExecuteStartVposAsync: DumpEditor produced {dumpPathToLoad}");
                        AppendStatus("DumpEditor finished successfully.");
                    }
                    catch (Exception ex)
                    {
                        LogAction($"ExecuteStartVposAsync: DumpEditor failed: {ex}");
                        AppendStatus($"DumpEditor failed: {ex.Message}");
                        ErrorReportingService.ShowError($"Automatische Dump-Bearbeitung fehlgeschlagen: {ex.Message}", ex, "Dump-Bearbeitung fehlgeschlagen");
                        return;
                    }
                }

                var args = loadDump ? $"/LoadDump:\"{dumpPathToLoad}\"" : (startMenu ? "/StartMenu" : null);
                LogAction($"Starting process: {exePath} {args}");
                var proc = _processService.StartProcess(exePath, args);
                if (proc == null)
                {
                    LogAction("StartVpos: process start returned null");
                    AppendStatus("VPOS failed to start.");
                    ErrorReportingService.ShowError("VPOS konnte nicht gestartet werden.", title: "VPOS konnte nicht gestartet werden");
                    return;
                }

                // create tracked VM
                var tracked = new TrackedProcessViewModel(proc, SelectedVersion.Version, ShowMessageAsync);
                tracked.RequestRemove += (vm) => RemoveTrackedProcess(vm);
                TrackedProcesses.Add(tracked);
                LogAction($"StartVpos: started PID={proc.Id}");
                AppendStatus($"VPOS started (PID {proc.Id}).");
            }
            catch (Exception ex)
            {
                LogAction($"ExecuteStartVposAsync:error: {ex}");
                AppendStatus($"Error starting VPOS: {ex.Message}");
                ErrorReportingService.ShowError($"Fehler beim Starten: {ex.Message}", ex, "Fehler beim Starten von VPOS");
            }
            finally
            {
                LogAction("ExecuteStartVposAsync:end");
            }
        }

        public async Task SaveSettingsAsync()
        {
            var settings = BuildSettingsForPersistence();
            await _settingsService.SaveAsync(settings);
        }

        /// <summary>
        /// Synchronous counterpart used on shutdown (Window.Closed) - see SettingsService.Save
        /// for why this must not go through the async path there.
        /// </summary>
        public void SaveSettingsSync()
        {
            var settings = BuildSettingsForPersistence();
            _settingsService.Save(settings);
        }

        private AppSettings BuildSettingsForPersistence()
        {
            var settings = new AppSettings();
            settings.Versions = Versions.ToList();
            settings.LastOpenedPath = SelectedDumpPath;
            settings.Options = BuildOptionsForPersistence();
            settings.DumpEditorExePath = DumpEditorExePath;
            settings.VppFolderPath = VppFolderPath;
            return settings;
        }

        /// <summary>
        /// Builds the DumpModificationOptions instance that actually gets written to
        /// settings.json. This is a snapshot copy, never the live Options object bound to the UI -
        /// mutating that directly here would wipe out whatever the user is currently typing.
        /// myVectron username/password are only included when SaveMyVectronCredentials is on, and
        /// VectronConnect's Connect ID/password only when SaveVectronConnectCredentials is on;
        /// otherwise they stay in-memory for this session only and are never persisted.
        /// </summary>
        private DumpModificationOptions BuildOptionsForPersistence()
        {
            return new DumpModificationOptions
            {
                AutomaticDumpEditing = Options.AutomaticDumpEditing,
                DisablePrint = Options.DisablePrint,
                DisableLicenseCheck = Options.DisableLicenseCheck,
                DisableMyVectron = Options.DisableMyVectron,
                DisableVectronConnect = Options.DisableVectronConnect,
                DisableBonVito = Options.DisableBonVito,
                MyVectronUsernameEnabled = Options.MyVectronUsernameEnabled,
                MyVectronPasswordEnabled = Options.MyVectronPasswordEnabled,
                SaveMyVectronCredentials = Options.SaveMyVectronCredentials,
                MyVectronUsername = Options.SaveMyVectronCredentials ? Options.MyVectronUsername : null,
                MyVectronPassword = Options.SaveMyVectronCredentials ? Options.MyVectronPassword : null,
                IsTestServer = Options.IsTestServer,
                SetTcpIpInterface20 = Options.SetTcpIpInterface20,
                Interface20IpAddress = Options.Interface20IpAddress,
                SetAllPrintersToInterface = Options.SetAllPrintersToInterface,
                PrinterDriverNumber = Options.PrinterDriverNumber,
                DisableKeyboardSound = Options.DisableKeyboardSound,
                DisableErrorSound = Options.DisableErrorSound,
                EnableVectronConnect = Options.EnableVectronConnect,
                SaveVectronConnectCredentials = Options.SaveVectronConnectCredentials,
                VectronConnectId = Options.SaveVectronConnectCredentials ? Options.VectronConnectId : null,
                VectronConnectPassword = Options.SaveVectronConnectCredentials ? Options.VectronConnectPassword : null,
                SetTcpIpInterface19 = Options.SetTcpIpInterface19,
                Interface19IpAddress = Options.Interface19IpAddress,
                AddShift4TerminalToInterface18 = Options.AddShift4TerminalToInterface18,
            };
        }

        /// <summary>
        /// Lightweight, non-error guidance only ("please select a version first") - actual
        /// failures go through ErrorReportingService.ShowError instead, which uses a real Window
        /// rather than a ContentDialog and so isn't affected by the XamlRoot issue below.
        /// </summary>
        private Task ShowMessageAsync(string text)
        {
            _window.DispatcherQueue.TryEnqueue(() =>
            {
                try
                {
                    var dialog = new Microsoft.UI.Xaml.Controls.ContentDialog
                    {
                        // Required in WinUI 3 Desktop - without it, ShowAsync() throws instead of
                        // displaying anything, which previously failed silently here because the
                        // call was fire-and-forget.
                        XamlRoot = ((Microsoft.UI.Xaml.FrameworkElement)_window.Content).XamlRoot,
                        Title = "Information",
                        Content = text,
                        CloseButtonText = "OK"
                    };
                    _ = dialog.ShowAsync().AsTask();
                }
                catch (Exception ex)
                {
                    LogAction($"ShowMessageAsync failed: {ex}");
                }
            });
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
