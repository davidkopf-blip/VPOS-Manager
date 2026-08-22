using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using DumpLoader_2._0.Models;

namespace DumpLoader_2._0.Services
{
    /// <summary>
    /// Drives the third-party VPOS Dump Editor (DIG, C:\Vectron\Dump-Editor\DumpEditor.exe) to
    /// apply the "support mode" DirectAccess edits to a dump before it gets loaded into VPOS.
    ///
    /// Pipeline (see C:\Vectron\Dump-Editor\DM\support.exml / support.xml):
    ///  1. Rewrite support.xml: always contains the fixed "Local speichern" block, plus one
    ///     block per enabled DumpModificationOptions checkbox.
    ///  2. Rewrite support.exml's &lt;LoadDump DumpFileName="..."&gt; to point at the dump the
    ///     user actually selected.
    ///  3. Run `DumpEditor.exe support.exml -nogui` and wait for it to finish.
    ///  4. DumpEditor writes the edited copy to support.vpd (the original dump is never
    ///     touched) - that path is returned for VPOS to load instead.
    /// </summary>
    public class DumpEditorService
    {
        private const string DumpEditorRoot = @"C:\Vectron\Dump-Editor";
        private const string DumpEditorExePath = DumpEditorRoot + @"\DumpEditor.exe";
        private const string DmFolder = DumpEditorRoot + @"\DM";
        private const string SupportXmlPath = DmFolder + @"\support.xml";
        private const string SupportExmlPath = DmFolder + @"\support.exml";

        public const string SupportDumpOutputPath = DmFolder + @"\support.vpd";

        public async Task<string> CreateEditedDumpAsync(string sourceDumpPath, DumpModificationOptions options)
        {
            if (string.IsNullOrEmpty(sourceDumpPath))
                throw new ArgumentException("sourceDumpPath must not be empty.", nameof(sourceDumpPath));

            if (!File.Exists(DumpEditorExePath))
                throw new FileNotFoundException("DumpEditor.exe wurde nicht gefunden.", DumpEditorExePath);

            if (!File.Exists(SupportExmlPath))
                throw new FileNotFoundException("support.exml wurde nicht gefunden.", SupportExmlPath);

            WriteSupportXml(options);
            UpdateSupportExmlDumpPath(sourceDumpPath);

            if (File.Exists(SupportDumpOutputPath))
            {
                try { File.Delete(SupportDumpOutputPath); } catch { /* best effort */ }
            }

            var startInfo = new ProcessStartInfo(DumpEditorExePath)
            {
                Arguments = $"\"{SupportExmlPath}\" -nogui",
                UseShellExecute = true,
                WorkingDirectory = DumpEditorRoot,
            };

            using var process = Process.Start(startInfo);
            if (process == null)
                throw new InvalidOperationException("DumpEditor.exe konnte nicht gestartet werden.");

            await process.WaitForExitAsync();

            if (!File.Exists(SupportDumpOutputPath))
                throw new FileNotFoundException("DumpEditor hat keine bearbeitete Dump-Datei erzeugt.", SupportDumpOutputPath);

            return SupportDumpOutputPath;
        }

        private static void WriteSupportXml(DumpModificationOptions options)
        {
            var root = new XElement("DIG", new XAttribute("Version", "1.0"));

            // Always included, regardless of which checkboxes are set.
            root.Add(new XComment(" Local speichern - ACHTUNG bei einer POS-PC bitte die Einstellungen in der Datei \"HWPROF.INI\" prüfen! "));
            root.Add(DirectAccess("3", "1", "1", "77", "0"));
            root.Add(DirectAccess("3", "1", "1", "78", "0"));
            root.Add(DirectAccess("3", "1", "1", "83", "0"));
            root.Add(DirectAccess("3", "1", "1", "15", "0"));
            root.Add(DirectAccess("3", "1", "1", "84", "0"));

            if (options.DisablePrint)
            {
                root.Add(new XComment(" Disable Drucken "));
                root.Add(DirectAccess("33", "1", "350", "1", "1"));
            }

            if (options.DisableMyVectron)
            {
                root.Add(new XComment(" Disable myVectron "));
                root.Add(DirectAccess("1021", "1", "1", "3", "0"));
            }

            if (options.DisableVectronConnect)
            {
                root.Add(new XComment(" Disable VectronConnect "));
                root.Add(DirectAccess("780", "1", "1", "1", "Banane"));
                root.Add(DirectAccess("780", "1", "1", "2", "0"));
                root.Add(DirectAccess("780", "1", "1", "12", "0"));
            }

            if (options.DisableLicenseCheck)
            {
                root.Add(new XComment(" Disable Lizenzabruf "));
                root.Add(DirectAccess("695", "1", "1", "1", "0"));
            }

            var doc = new XDocument(new XDeclaration("1.0", "utf-8", null), root);
            using var writer = new StreamWriter(SupportXmlPath, false, new UTF8Encoding(false));
            doc.Save(writer);
        }

        private static XElement DirectAccess(string tableNo, string planeNo, string recPos, string fieldNo, string data)
        {
            return new XElement("DirectAccess",
                new XAttribute("TableNo", tableNo),
                new XAttribute("PlaneNo", planeNo),
                new XElement("PutData",
                    new XAttribute("RecPos", recPos),
                    new XAttribute("FieldNo", fieldNo),
                    new XAttribute("Data", data)));
        }

        private static void UpdateSupportExmlDumpPath(string sourceDumpPath)
        {
            var doc = XDocument.Load(SupportExmlPath);
            var loadDump = doc.Root?.Element("LoadDump");
            if (loadDump == null)
                throw new InvalidOperationException("support.exml enthält kein <LoadDump>-Element.");

            loadDump.SetAttributeValue("DumpFileName", sourceDumpPath);
            doc.Save(SupportExmlPath);
        }
    }
}
