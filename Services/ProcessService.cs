using System;
using System.Diagnostics;

namespace DumpLoader_2._0.Services
{
    public class ProcessService
    {
        public Process? StartProcess(string filePath, string? args = null)
        {
            try
            {
                var start = new ProcessStartInfo(filePath)
                {
                    Arguments = args ?? string.Empty,
                    UseShellExecute = true
                };

                var p = Process.Start(start);
                if (p != null)
                {
                    try { p.EnableRaisingEvents = true; } catch { }
                }
                return p;
            }
            catch
            {
                return null;
            }
        }
    }
}

