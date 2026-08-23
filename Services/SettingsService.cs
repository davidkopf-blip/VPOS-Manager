using System;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using System.Threading.Tasks;
using DumpLoader_2._0.Models;

namespace DumpLoader_2._0.Services
{
    public class SettingsService
    {
        private readonly string _path;

        public SettingsService(string? fileName = null)
        {
            var name = fileName ?? "settings.json";
            var folder = LocalPaths.GetDumpLoaderFolder();
            Directory.CreateDirectory(folder);
            _path = Path.Combine(folder, name);
        }

        private static readonly JsonSerializerOptions _jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            WriteIndented = true,
            // Provide a runtime type info resolver so serialization works when reflection-based serialization is disabled
            // Keep minimal resolver to avoid trim warnings when possible
            TypeInfoResolver = new DefaultJsonTypeInfoResolver()
        };

        public async Task<AppSettings> LoadAsync()
        {
            if (!File.Exists(_path))
                return new AppSettings();

            try
            {
                await using var stream = File.OpenRead(_path);
                return await JsonSerializer.DeserializeAsync<AppSettings>(stream, _jsonOptions) ?? new AppSettings();
            }
            catch (JsonException)
            {
                // Corrupted/truncated settings.json (e.g. from a previous forced process kill
                // mid-write). Preserve the bad file for inspection and fall back to defaults
                // instead of leaving the app permanently unable to start.
                TryBackupCorruptFile();
                return new AppSettings();
            }
        }

        public async Task SaveAsync(AppSettings settings)
        {
            await using var stream = File.Create(_path);
            await JsonSerializer.SerializeAsync(stream, settings, _jsonOptions);
        }

        /// <summary>
        /// Synchronous save for shutdown paths (e.g. Window.Closed). Blocking on an async method
        /// that resumes via the UI-thread SynchronizationContext (as SaveAsync's continuations
        /// do) deadlocks if called with .GetAwaiter().GetResult() from the UI thread - this
        /// avoids that entirely by never awaiting in the first place.
        /// </summary>
        public void Save(AppSettings settings)
        {
            var json = JsonSerializer.Serialize(settings, _jsonOptions);
            File.WriteAllText(_path, json);
        }

        private void TryBackupCorruptFile()
        {
            try
            {
                var backupPath = _path + $".corrupt-{DateTime.Now:yyyyMMdd_HHmmss}";
                File.Copy(_path, backupPath, overwrite: true);
            }
            catch { /* best effort */ }
        }
    }
}
