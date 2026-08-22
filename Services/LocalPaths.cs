using System;
using System.IO;

namespace DumpLoader_2._0.Services
{
    public static class LocalPaths
    {
        // Returns C:\Users\{User}\AppData\Local\DumpLoader2
        public static string GetDumpLoaderFolder()
        {
            // Use LocalApplicationData to reliably get C:\Users\{User}\AppData\Local
            try
            {
                var local = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                if (!string.IsNullOrEmpty(local))
                    return Path.Combine(local, "DumpLoader2");
            }
            catch { }

            // Fallback to environment variable
            var env = Environment.GetEnvironmentVariable("LOCALAPPDATA");
            if (!string.IsNullOrEmpty(env))
                return Path.Combine(env, "DumpLoader2");

            // Last resort
            return Path.Combine(Path.GetTempPath(), "DumpLoader2");
        }
    }
}
