using System;
using System.IO;

namespace DumpLoader_2._0.Services
{
    public static class SessionFiles
    {
        public static string? StartupLogPath { get; private set; }
        public static string? ActionsLogPath { get; private set; }

        // Call once at app startup
        public static void InitializeSessionFiles()
        {
            try
            {
                var folder = LocalPaths.GetDumpLoaderFolder();
                Directory.CreateDirectory(folder);

                var ts = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                StartupLogPath = Path.Combine(folder, $"startup-error-{ts}.log");
                ActionsLogPath = Path.Combine(folder, $"actions-{ts}.log");

                // Create empty files if they don't exist
                if (!File.Exists(StartupLogPath))
                    File.WriteAllText(StartupLogPath, "");
                if (!File.Exists(ActionsLogPath))
                    File.WriteAllText(ActionsLogPath, "");
            }
            catch
            {
                // ignore failures; callers will fallback to direct LocalPaths usage
            }
        }
    }
}
