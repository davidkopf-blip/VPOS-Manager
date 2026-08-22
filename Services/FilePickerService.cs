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

