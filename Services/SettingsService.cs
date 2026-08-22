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

            await using var stream = File.OpenRead(_path);
            return await JsonSerializer.DeserializeAsync<AppSettings>(stream, _jsonOptions) ?? new AppSettings();
        }

        public async Task SaveAsync(AppSettings settings)
        {
            await using var stream = File.Create(_path);
            await JsonSerializer.SerializeAsync(stream, settings, _jsonOptions);
        }
    }
}
