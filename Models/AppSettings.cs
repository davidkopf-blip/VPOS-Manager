using System.Collections.Generic;

namespace DumpLoader_2._0.Models
{
    public class AppSettings
    {
        public string? LastOpenedPath { get; set; }
        public List<VersionEntry> Versions { get; set; } = new();

        public DumpModificationOptions Options { get; set; } = new DumpModificationOptions();
    }
}
