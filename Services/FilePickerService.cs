using System;
using System.IO;
using System.Threading.Tasks;
using System.Runtime.InteropServices.WindowsRuntime;
using Microsoft.UI.Xaml;
using Windows.Storage.Pickers;
using Windows.Storage;
using Windows.Storage.Streams;
using WinRT.Interop;

namespace DumpLoader_2._0.Services
{
    public class FilePickerService
    {
        public async Task<string?> PickExeAsync(Window window)
        {
            var picker = new FileOpenPicker();
            InitializeWithWindow.Initialize(picker, WindowNative.GetWindowHandle(window));
            picker.FileTypeFilter.Add(".exe");
            StorageFile? file = await picker.PickSingleFileAsync();
            if (file == null) return null;
            if (!string.IsNullOrEmpty(file.Path))
                return file.Path;

            // If StorageFile.Path is not available in the packaged context, copy the file to local app folder and return that path
            try
            {
                var destFolder = DumpLoader_2._0.Services.LocalPaths.GetDumpLoaderFolder();
                Directory.CreateDirectory(destFolder);
                var destPath = Path.Combine(destFolder, file.Name);
                // Open as IRandomAccessStream and convert to Stream
                using (var ras = await file.OpenReadAsync())
                using (var inStream = ras.GetInputStreamAt(0))
                using (var src = inStream.AsStreamForRead())
                using (var dst = File.Create(destPath))
                {
                    await src.CopyToAsync(dst);
                }
                return destPath;
            }
            catch
            {
                return null;
            }
        }

        public async Task<string?> PickFolderAsync(Window window)
        {
            try
            {
                var picker = new FolderPicker();
                InitializeWithWindow.Initialize(picker, WindowNative.GetWindowHandle(window));
                picker.FileTypeFilter.Add("*");
                StorageFolder? folder = await picker.PickSingleFolderAsync();
                return folder?.Path;
            }
            catch (Exception ex)
            {
                // The shell folder picker throws for various vague COM reasons when navigating to
                // an unreachable/slow network location - most commonly a mapped drive letter that
                // isn't visible to the current process (e.g. because it's running elevated, which
                // uses a different logon-session token than the one the mapping was created
                // under). Re-throw with an actionable hint instead of the raw COM error text.
                throw new InvalidOperationException(
                    "Der Ordner konnte nicht ausgewählt werden. Dies passiert häufig bei Netzlaufwerken " +
                    "(z. B. K:), die für elevierte (als Administrator ausgeführte) Prozesse nicht sichtbar " +
                    "sind - starten Sie VPOS Manager ohne Administratorrechte, oder geben Sie stattdessen " +
                    "den vollständigen UNC-Netzwerkpfad an (z. B. \\\\server\\freigabe\\...).", ex);
            }
        }

        public async Task<string?> PickDumpAsync(Window window)
        {
            var picker = new FileOpenPicker();
            InitializeWithWindow.Initialize(picker, WindowNative.GetWindowHandle(window));
            picker.FileTypeFilter.Add(".vpd");
            picker.FileTypeFilter.Add(".VPosDump");
            StorageFile? file = await picker.PickSingleFileAsync();
            if (file == null) return null;
            if (!string.IsNullOrEmpty(file.Path))
                return file.Path;

            // If StorageFile.Path is not available, copy the dump into the app folder so we have a usable filesystem path
            try
            {
                var destFolder = DumpLoader_2._0.Services.LocalPaths.GetDumpLoaderFolder();
                Directory.CreateDirectory(destFolder);
                var destPath = Path.Combine(destFolder, file.Name);
                using (var ras = await file.OpenReadAsync())
                using (var inStream = ras.GetInputStreamAt(0))
                using (var src = inStream.AsStreamForRead())
                using (var dst = File.Create(destPath))
                {
                    await src.CopyToAsync(dst);
                }
                return destPath;
            }
            catch
            {
                return null;
            }
        }
    }
}

