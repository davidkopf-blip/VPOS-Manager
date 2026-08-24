using System.Collections.Generic;

namespace DumpLoader_2._0.Models
{
    public class AppSettings
    {
        public string? LastOpenedPath { get; set; }
        public List<VersionEntry> Versions { get; set; } = new();

        public DumpModificationOptions Options { get; set; } = new DumpModificationOptions();

        /// <summary>Path to DumpEditor.exe, configured via the Settings window.</summary>
        public string? DumpEditorExePath { get; set; }

        /// <summary>
        /// Folder containing the per-version VPP files (named "VPP-{Version}.VPP"), configured via
        /// the Settings window. Defaults to the standard network share so a fresh install works
        /// out of the box.
        /// </summary>
        public string? VppFolderPath { get; set; } = @"K:\Support\Support\Tools\VPPs";
    }
}
