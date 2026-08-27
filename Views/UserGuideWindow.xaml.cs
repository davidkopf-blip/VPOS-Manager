using System.Collections.Generic;
using Microsoft.UI;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Windows.Graphics;
using Windows.UI;
using WinRT.Interop;

namespace DumpLoader_2._0.Views
{
    /// <summary>One feature's heading plus its description, already in a single language.</summary>
    public class FeatureItem
    {
        public string Title { get; set; } = string.Empty;
        public string Body { get; set; } = string.Empty;
    }

    /// <summary>One changelog version's heading plus its list of bullet entries, already in a
    /// single language.</summary>
    public class ChangelogEntry
    {
        public string Title { get; set; } = string.Empty;
        public List<string> Entries { get; set; } = new();
    }

    /// <summary>
    /// Read-only overview of the app's features plus its changelog, opened from Help &gt; User
    /// Guide. Content is hand-maintained here (and mirrored in README.md) rather than loaded from
    /// a file on disk, so it always works regardless of where the packaged app is installed.
    /// Available in English and German via the Language selector in the title bar; both language
    /// versions are fully self-contained (see BuildFeaturesEnglish/German and
    /// BuildChangelogEnglish/German) rather than translated at runtime.
    /// </summary>
    public sealed partial class UserGuideWindow : Window
    {
        public UserGuideWindow()
        {
            this.InitializeComponent();

            var hwnd = WindowNative.GetWindowHandle(this);
            var windowId = Win32Interop.GetWindowIdFromWindow(hwnd);
            var appWindow = AppWindow.GetFromWindowId(windowId);
            appWindow?.Resize(new SizeInt32(1500, 630));

            SetUpCustomTitleBar(appWindow);

            // Apply the default (English) content directly, then only wire up the event handler
            // afterwards. Setting ComboBoxItem.IsSelected="True" in XAML (the more obvious way to
            // default the selection) fires SelectionChanged during InitializeComponent() itself,
            // before elements declared later in the tree (FeaturesItemsControl,
            // ChangelogItemsControl, etc.) are connected - crashing the window's construction with
            // a NullReferenceException before it ever opens.
            LanguageComboBox.SelectedIndex = 0;
            ApplyLanguage(german: false);
            LanguageComboBox.SelectionChanged += LanguageComboBox_SelectionChanged;
        }

        private void SetUpCustomTitleBar(AppWindow? appWindow)
        {
            this.ExtendsContentIntoTitleBar = true;
            this.SetTitleBar(AppTitleBar);

            var titleBar = appWindow?.TitleBar;
            if (titleBar == null)
                return;

            titleBar.BackgroundColor = Colors.Transparent;
            titleBar.InactiveBackgroundColor = Colors.Transparent;
            titleBar.ButtonBackgroundColor = Colors.Transparent;
            titleBar.ButtonInactiveBackgroundColor = Colors.Transparent;
            titleBar.ButtonForegroundColor = Colors.White;
            titleBar.ButtonInactiveForegroundColor = Color.FromArgb(255, 154, 154, 154);
            titleBar.ButtonHoverBackgroundColor = Color.FromArgb(30, 255, 255, 255);
            titleBar.ButtonHoverForegroundColor = Colors.White;
            titleBar.ButtonPressedBackgroundColor = Color.FromArgb(50, 255, 255, 255);
            titleBar.ButtonPressedForegroundColor = Colors.White;
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void LanguageComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var isGerman = (LanguageComboBox.SelectedItem as ComboBoxItem)?.Content as string == "Deutsch";
            ApplyLanguage(isGerman);
        }

        private void ApplyLanguage(bool german)
        {
            TitleBarText.Text = german ? "Benutzerhandbuch" : "User Guide";
            this.Title = german ? "Benutzerhandbuch" : "User Guide";

            OverviewHeadingText.Text = german ? "VPOS Manager — Benutzerhandbuch" : "VPOS Manager — User Guide";
            OverviewBodyText.Text = german
                ? "Ein Support-Tool eines Drittanbieters zum Laden von Dumps in VPOS PC, zur Verwaltung von VPOS-Sitzungen und zur Übersicht über installierte VPOS-Versionen. Es automatisiert wiederkehrende Einrichtungsaufgaben wie das Deaktivieren des Drucks, von Lizenzprüfungen und das Anwenden gängiger Konfigurationsänderungen."
                : "A third-party support tool for loading dumps into VPOS PC, managing VPOS sessions, and keeping track of installed VPOS versions. It automates repetitive setup tasks such as disabling printing, license checks, and applying common configuration changes.";

            DisclaimerBoldRun.Text = german ? "Erforderliche Einrichtung: " : "Required setup: ";
            DisclaimerBodyRun.Text = german
                ? "Unter Settings müssen sowohl ein DumpEditor.exe-Pfad als auch ein VPP-Pfad konfiguriert sein, damit automatische Dump-Bearbeitung, VPP-Austausch und alles, was davon abhängt, funktionieren."
                : "a DumpEditor.exe path and a VPP Path must both be configured under Settings for automatic dump editing, VPP swapping, and everything that depends on them to work.";

            FeaturesLabelText.Text = german ? "FUNKTIONEN" : "FEATURES";
            ChangelogLabelText.Text = german ? "ÄNDERUNGSPROTOKOLL" : "CHANGELOG";
            CloseButton.Content = german ? "Schließen" : "Close";

            FeaturesItemsControl.ItemsSource = german ? BuildFeaturesGerman() : BuildFeaturesEnglish();
            ChangelogItemsControl.ItemsSource = german ? BuildChangelogGerman() : BuildChangelogEnglish();
        }

        /// <summary>Mirrors the "## Features" section of README.md.</summary>
        private static List<FeatureItem> BuildFeaturesEnglish()
        {
            return new List<FeatureItem>
            {
                new FeatureItem
                {
                    Title = "Version management",
                    Body = "Register any number of installed VPOS PC builds by pointing at their .exe once — the version number is read straight from the file's own metadata and remembered between sessions."
                },
                new FeatureItem
                {
                    Title = "Dump loading",
                    Body = "Pick a .vpd/.VPosDump file and launch it against any registered version via \"Load Dump and Start VPOS\", or start a version standalone with \"Start VPOS\". \"Delete DATA, load Dump & Start VPOS\" additionally wipes the selected version's DATA folder before loading the dump (automatic dump editing is still evaluated as usual). \"Launch into Startmenu\" starts the selected version straight into VPOS's start menu via the /StartMenu parameter, without loading a dump."
                },
                new FeatureItem
                {
                    Title = "VPOS task manager",
                    Body = "Every VPOS instance launched through the tool is tracked in the \"Running VPOS Instances\" panel — bring any of them to the foreground, stop them, and see stopped ones disappear automatically."
                },
                new FeatureItem
                {
                    Title = "Automatic dump editing",
                    Body = "When \"Automatic Dump Editing\" is switched on, VPOS Manager runs the dump through the third-party VPOS Dump Editor (DIG) before loading it, applying a chosen set of edits to a working copy — the original dump file is never touched. Available toggles: Disable print, Disable license check, Disable myVectron, Disable VectronConnect, Disable bonVito, Disable keyboard sound, Disable error sound. Before DumpEditor.exe runs, the app also swaps in the VPP program file matching the selected VPOS version (VPP-{Version}.VPP from the configured VPP Path, copied in as VPOSPROG.DLL), so the dump is always edited with the correct version's program logic."
                },
                new FeatureItem
                {
                    Title = "myVectron & VectronConnect",
                    Body = "Bake a specific myVectron username/password into the dump before launch, and flip the Prod/Test switch to point VectronConnect and myVectron at the Test or Prod server environment. Check \"Save credentials\" to remember them between sessions (stored in plain text in settings.json, with a one-time warning). Next to it, an independent \"VectronConnect\" section can enable VectronConnect outright with a Connect ID and VC Password, with its own matching \"Save credentials\" checkbox and warning - mutually exclusive with \"Disable VectronConnect\" above, since both configure the same underlying setting; automatic dump editing refuses to run if both are checked."
                },
                new FeatureItem
                {
                    Title = "Printer (TCP/IP) & PAX, Verifone and MobileApp",
                    Body = "Check \"Set interface 20 to TCP/IP\" to register a TCP/IP interface named PRINTER (fixed port 9100) at the IPv4 address you enter - required and validated before any load-dump action runs while this is checked. With it on, \"Set all printers to this interface\" points all 10 printer driver slots at interface 20 with a programmed driver enabled, using a driver number (1-20, 20 by default) also validated before use. Separately, \"Set interface 19 to TCP/IP\" registers interface 19 (named TERMINAL, fixed port 8085) for PAX/Verifone/MobileApp routing at its own IPv4 address, and \"Add Shift4 Terminal to Interface 18 (for printing)\" retypes interface 18 for that purpose - no IP/port needed for that one."
                },
                new FeatureItem
                {
                    Title = "Status log",
                    Body = "The Status panel on the right shows what's happening as a dump loads and VPOS starts — from \"Loading dump and starting VPOS...\" through the VPP/DumpEditor steps to \"VPOS started\" — alongside DumpEditor's raw output, all in one place. The DumpEditor.exe found/not-found indicator is shown in its header row."
                },
                new FeatureItem
                {
                    Title = "Settings",
                    Body = "Configure the path to DumpEditor.exe and the VPP Path (the folder containing the per-version VPP-{Version}.VPP files, defaulting to the standard network share) under Settings."
                },
                new FeatureItem
                {
                    Title = "Error handling",
                    Body = "Genuine failures (a locked VPOSPROG.DLL, an unreachable VPP network path, a dump that can't be loaded, a process that won't start, and so on) surface in an app-styled error window with a collapsible details section, instead of a plain dialog or a silent failure. An app-wide handler catches anything else that would otherwise crash the app, logs it, and shows the same error window instead."
                },
                new FeatureItem
                {
                    Title = "Persistence & diagnostics",
                    Body = "Registered versions, the last-used dump path, and every toggle above are saved between runs. Startup errors and key actions are logged to timestamped files for troubleshooting."
                },
            };
        }

        private static List<FeatureItem> BuildFeaturesGerman()
        {
            return new List<FeatureItem>
            {
                new FeatureItem
                {
                    Title = "Versionsverwaltung",
                    Body = "Registrieren Sie beliebig viele installierte VPOS-PC-Versionen, indem Sie einmalig auf deren .exe verweisen — die Versionsnummer wird direkt aus den Metadaten der Datei ausgelesen und zwischen den Sitzungen gespeichert."
                },
                new FeatureItem
                {
                    Title = "Dump laden",
                    Body = "Wählen Sie eine .vpd-/.VPosDump-Datei aus und starten Sie sie über \"Load Dump and Start VPOS\" mit einer beliebigen registrierten Version, oder starten Sie eine Version eigenständig mit \"Start VPOS\". \"Delete DATA, load Dump & Start VPOS\" löscht zusätzlich den DATA-Ordner der ausgewählten Version, bevor der Dump geladen wird (die automatische Dump-Bearbeitung wird dabei wie gewohnt berücksichtigt). \"Launch into Startmenu\" startet die ausgewählte Version direkt im Startmenü von VPOS über den Parameter /StartMenu, ohne einen Dump zu laden."
                },
                new FeatureItem
                {
                    Title = "VPOS-Taskmanager",
                    Body = "Jede über das Tool gestartete VPOS-Instanz wird im Bereich \"Running VPOS Instances\" angezeigt — holen Sie eine beliebige Instanz in den Vordergrund, beenden Sie sie, und beendete Instanzen verschwinden automatisch aus der Liste."
                },
                new FeatureItem
                {
                    Title = "Automatische Dump-Bearbeitung",
                    Body = "Wenn \"Automatic Dump Editing\" aktiviert ist, führt VPOS Manager den Dump vor dem Laden durch den Dump Editor (DIG) eines Drittanbieters und wendet eine gewählte Menge an Änderungen auf eine Arbeitskopie an — die Original-Dump-Datei wird dabei nie verändert. Verfügbare Schalter: Disable print, Disable license check, Disable myVectron, Disable VectronConnect, Disable bonVito, Disable keyboard sound, Disable error sound. Bevor DumpEditor.exe ausgeführt wird, tauscht die App zusätzlich die zur gewählten VPOS-Version passende VPP-Programmdatei ein (VPP-{Version}.VPP aus dem konfigurierten VPP-Pfad, kopiert als VPOSPROG.DLL), sodass der Dump immer mit der korrekten Programmlogik der jeweiligen Version bearbeitet wird."
                },
                new FeatureItem
                {
                    Title = "myVectron & VectronConnect",
                    Body = "Hinterlegen Sie vor dem Start einen bestimmten myVectron-Benutzernamen/-Passwort im Dump, und schalten Sie mit dem Prod/Test-Schalter um, ob VectronConnect und myVectron auf die Test- oder Produktivumgebung zeigen. Aktivieren Sie \"Save credentials\", um die Zugangsdaten zwischen den Sitzungen zu speichern (im Klartext in settings.json, mit einmaliger Warnung). Daneben kann ein eigenständiger Bereich \"VectronConnect\" VectronConnect direkt mit Connect ID und VC Password aktivieren, mit eigenem \"Save credentials\"-Kästchen und derselben Warnung — schließt sich mit \"Disable VectronConnect\" oben gegenseitig aus, da beide dieselbe zugrunde liegende Einstellung konfigurieren; die automatische Dump-Bearbeitung verweigert den Start, wenn beide aktiviert sind."
                },
                new FeatureItem
                {
                    Title = "Printer (TCP/IP) & PAX, Verifone and MobileApp",
                    Body = "Aktivieren Sie \"Set interface 20 to TCP/IP\", um eine TCP/IP-Schnittstelle namens PRINTER (fest auf Port 9100) unter der eingegebenen IPv4-Adresse anzulegen — erforderlich und wird geprüft, bevor bei aktiviertem Kästchen irgendeine Lade-Aktion ausgeführt wird. Ist dies aktiviert, weist \"Set all printers to this interface\" allen 10 Druckertreiber-Plätzen Interface 20 zu und aktiviert den programmierten Treiber, mit einer Treibernummer (1-20, standardmäßig 20), die ebenfalls vor der Verwendung geprüft wird. Getrennt davon registriert \"Set interface 19 to TCP/IP\" Interface 19 (Name TERMINAL, fest auf Port 8085) für die Weiterleitung von PAX/Verifone/MobileApp unter einer eigenen IPv4-Adresse, und \"Add Shift4 Terminal to Interface 18 (for printing)\" richtet Interface 18 dafür um — dafür werden keine IP/Port benötigt."
                },
                new FeatureItem
                {
                    Title = "Statusprotokoll",
                    Body = "Der Statusbereich rechts zeigt, was beim Laden eines Dumps und beim Starten von VPOS passiert — von \"Loading dump and starting VPOS...\" über die VPP-/DumpEditor-Schritte bis hin zu \"VPOS started\" — zusammen mit der rohen Ausgabe von DumpEditor, alles an einem Ort. Die Anzeige, ob DumpEditor.exe gefunden wurde, befindet sich in der Kopfzeile dieses Bereichs."
                },
                new FeatureItem
                {
                    Title = "Einstellungen",
                    Body = "Konfigurieren Sie unter Settings den Pfad zu DumpEditor.exe sowie den VPP-Pfad (den Ordner mit den versionsspezifischen VPP-{Version}.VPP-Dateien, standardmäßig die übliche Netzwerkfreigabe)."
                },
                new FeatureItem
                {
                    Title = "Fehlerbehandlung",
                    Body = "Echte Fehlschläge (eine gesperrte VPOSPROG.DLL, ein nicht erreichbarer VPP-Netzwerkpfad, ein Dump, der sich nicht laden lässt, ein Prozess, der nicht startet, und so weiter) werden in einem im App-Design gestalteten Fehlerfenster mit einklappbarem Detailbereich angezeigt, statt in einem einfachen Dialog oder als stiller Fehlschlag. Eine app-weite Behandlung fängt alles andere ab, was die App sonst zum Absturz bringen würde, protokolliert es und zeigt ebenfalls dieses Fehlerfenster an."
                },
                new FeatureItem
                {
                    Title = "Persistenz & Diagnose",
                    Body = "Registrierte Versionen, der zuletzt verwendete Dump-Pfad und alle oben genannten Schalter werden zwischen den Programmstarts gespeichert. Startfehler und wichtige Aktionen werden zur Fehlerdiagnose in Dateien mit Zeitstempel protokolliert."
                },
            };
        }

        /// <summary>Mirrors the "## Changelog" section of README.md, newest first.</summary>
        private static List<ChangelogEntry> BuildChangelogEnglish()
        {
            return new List<ChangelogEntry>
            {
                new ChangelogEntry
                {
                    Title = "1.0.0 — VectronConnect, PAX/Verifone/MobileApp routing & sound toggles",
                    Entries =
                    {
                        "Added \"Disable keyboard sound\" and \"Disable error sound\" to General Settings.",
                        "Added an independent \"VectronConnect\" section (split 50/50 next to myVectron): a checkbox plus Connect ID and VC Password fields that enable VectronConnect outright. Mutually exclusive with \"Disable VectronConnect\" - both write to the same underlying setting, so automatic dump editing now refuses to run if both are checked, with an explanatory error.",
                        "\"Interface 20 (TCP/IP)\" is now \"Printer (TCP/IP)\", split 50/50 with a new \"PAX, Verifone & MobileApp\" section: \"Set interface 19 to TCP/IP\" (fixed port 8085) plus its own IPv4 field, and \"Add Shift4 Terminal to Interface 18 (for printing)\" (no IP/port needed).",
                        "Interface 20's port field was removed - it's now fixed at 9100 and shown as read-only text beside the IP field, matching interface 19's fixed-port treatment.",
                        "The title bar now shows the copyright line (\"VPOS Manager © David Kopf · DIG © Volker Görgler\") and the app's version number, right-bound, read from the assembly version so it never needs to be hand-typed.",
                        "Renamed myVectron's \"Save Username & Password\" checkbox to \"Save credentials\", and added a matching \"Save credentials\" checkbox (with the same one-time cleartext-storage warning) to the new VectronConnect section, gating its Connect ID/password persistence the same way.",
                        "Interface 20's registered name (441/1/20/1) changed from VPOSMANAGER to PRINTER.",
                    }
                },
                new ChangelogEntry
                {
                    Title = "0.9.1 — Interface 20 (TCP/IP) & printer driver editing",
                    Entries =
                    {
                        "Added \"Set interface 20 to TCP/IP\": registers a new TCP/IP interface named VPOSMANAGER at a chosen IPv4 address and port. Both fields are required and validated (valid IPv4, port 1-65535) before any load-dump action can run while automatic dump editing and this checkbox are on.",
                        "Added \"Set all printers to this interface\" (only available while the interface checkbox above is on): points all 10 printers at interface 20 with a programmed driver enabled, using a driver number field (1-20, defaults to 20, also validated before use).",
                    }
                },
                new ChangelogEntry
                {
                    Title = "0.9.0 — App-wide error handling & crash safety",
                    Entries =
                    {
                        "Added a new app-styled error window, replacing the default unstyled dialogs for real failures. It shows a friendly message plus a collapsible \"Show details\" section with the full exception (and a \"Copy details\" button), in the same dark card design as the rest of the app.",
                        "Added a single app-wide error-reporting service that logs every error to errors.log and shows the error window from anywhere, including background threads and global exception handlers.",
                        "Every operation that can genuinely fail and block a core function - adding a version, loading a dump, automatic dump editing (including VPP-path and network-drive failures), deleting the DATA folder, starting or stopping VPOS, bringing a VPOS window to the front, picking a file or folder in Settings - now routes through the error window instead of a plain dialog or a silent failure. Minor, self-resolving notices still only appear in the Status log, not as a popup.",
                        "The app-wide unhandled-exception handler now shows the error window instead of silently swallowing the exception, while still preventing the crash itself.",
                        "Fixed a latent bug where the plain validation dialogs (e.g. \"please select a version first\") had no XamlRoot set, which could make them silently fail to appear.",
                    }
                },
                new ChangelogEntry
                {
                    Title = "0.8.5 — Fixed the User Guide window not opening",
                    Entries =
                    {
                        "Fixed a crash that prevented the User Guide window from opening at all: defaulting the Language combobox's selection via IsSelected=\"True\" in XAML fired the language-switch handler during the window's own construction, before later UI elements existed yet. The default language is now applied directly, with the switch handler wired up only afterwards.",
                    }
                },
                new ChangelogEntry
                {
                    Title = "0.8.4 — User Guide language selector & German translation",
                    Entries =
                    {
                        "The User Guide window is now 1500×630 and has a \"Language\" selector in its title bar (English / Deutsch, English by default).",
                        "Added a full German translation of the User Guide's features and changelog, switched instantly via the Language selector - nothing is machine-translated at runtime, both language versions are maintained in full.",
                    }
                },
                new ChangelogEntry
                {
                    Title = "0.8.3 — User Guide setup disclaimer & thicker accent separator",
                    Entries =
                    {
                        "Added a red disclaimer box at the top of the User Guide stating that both a DumpEditor.exe path and a VPP Path must be configured under Settings for automatic dump editing, VPP swapping, and everything that depends on them to work.",
                        "The green separator under \"Automatic Dump Editing\" is now 6px thick (up from 3px), so its fully-rounded pill ends actually read as rounded instead of being too thin to notice.",
                    }
                },
                new ChangelogEntry
                {
                    Title = "0.8.2 — Automatic recovery from a locked VPOSPROG.DLL",
                    Entries =
                    {
                        "If VPOSPROG.DLL is still locked after clearing its read-only attribute (typically a leftover DumpEditor.exe instance from an earlier, aborted run), the app now closes any running instance of the configured DumpEditor.exe and retries automatically, with a status notification, before giving up with an actionable error.",
                    }
                },
                new ChangelogEntry
                {
                    Title = "0.8.1 — VPP swap & network drive reliability",
                    Entries =
                    {
                        "Fixed \"Access to VPOSPROG.DLL is denied\" during automatic dump editing: a read-only attribute carried over from the network share could block deletion even for administrators. That attribute is now cleared before every delete/copy.",
                        "Errors for a missing/unreachable VPP path, and for the Settings window's folder picker, now explain the most common cause - a mapped network drive (e.g. K:) invisible to an elevated process - and suggest running unelevated or using a full UNC path.",
                    }
                },
                new ChangelogEntry
                {
                    Title = "0.8.0 — DATA reset & Start Menu launch",
                    Entries =
                    {
                        "Added \"Delete DATA, load Dump & Start VPOS\": deletes the selected version's DATA folder, then loads the dump and starts VPOS (automatic dump editing is still evaluated as usual).",
                        "Added \"Launch into Startmenu\": starts the selected version with the /StartMenu parameter instead of loading a dump.",
                        "Version section's action buttons rearranged into a 2×2 grid to fit the two new buttons.",
                    }
                },
                new ChangelogEntry
                {
                    Title = "0.7.0 — Per-version VPP swapping & status log",
                    Entries =
                    {
                        "Added automatic VPP swapping: before DumpEditor.exe runs, the VPP file matching the selected VPOS version is copied in as VPOSPROG.DLL, with a status notification for each step.",
                        "Added a \"VPP Path\" setting (defaults to the standard network share), alongside the existing DumpEditor.exe path setting.",
                        "The terminal panel is now labeled \"Status\" and shows readable status notifications (\"Starting VPOS...\", \"Loading dump and starting VPOS...\", \"VPOS started (PID ...)\"), not just raw DumpEditor.exe output.",
                        "The DumpEditor.exe found/not-found indicator moved from the Versions card into the Status panel's header row.",
                        "Settings window resized to fit the new VPP Path field.",
                    }
                },
                new ChangelogEntry
                {
                    Title = "0.6.0 — Live DumpEditor output panel",
                    Entries =
                    {
                        "Added a terminal-style panel showing DumpEditor.exe's stdout/stderr live as it runs, in the same accent green on a dedicated dark \"screen\" nested inside the panel's card.",
                        "Main window layout rebalanced to a 60/40 column split; the right column now splits 65/35 between the Running VPOS Instances panel and the new terminal panel.",
                    }
                },
                new ChangelogEntry
                {
                    Title = "0.5.0 — Visual refresh",
                    Entries =
                    {
                        "Custom title bar: the native white Windows caption area is gone — the OS min/max/close buttons now render directly on the app's own dark menu bar.",
                        "Buttons, fields, and toggles restyled for a more consistent look; the accent green is now reserved for the one primary action per screen instead of scattered across secondary buttons.",
                        "Running VPOS Instances panel redesigned with a live count badge and a glowing status indicator per process.",
                    }
                },
                new ChangelogEntry
                {
                    Title = "0.4.0 — Configurable DumpEditor location & reliability fixes",
                    Entries =
                    {
                        "Added a Settings window for pointing the app at any DumpEditor.exe install, replacing the previous hardcoded path.",
                        "dig.ini, support.exml, and support.xml are now generated by the app itself rather than relying on pre-existing files, and are written to a VPOSManager folder next to DumpEditor.exe.",
                        "Added a menu bar with Settings and Help entries.",
                        "Added an opt-in \"Save Username & Password\" toggle for myVectron credentials, with a one-time cleartext-storage disclaimer.",
                        "Fixed a startup crash, a freeze-on-close deadlock, and hardened settings loading to recover from a corrupted settings.json instead of failing to start.",
                    }
                },
                new ChangelogEntry
                {
                    Title = "0.3.0 — myVectron credentials & server selection",
                    Entries =
                    {
                        "Added optional myVectron username/password overrides, baked into the dump on load.",
                        "Added a Prod/Test switch controlling which server environment VectronConnect and myVectron point at.",
                        "Settings screen reorganized into \"General Settings\" and \"myVectron\" sections for clarity.",
                    }
                },
                new ChangelogEntry
                {
                    Title = "0.2.0 — Automatic dump editing",
                    Entries =
                    {
                        "Integrated the third-party VPOS Dump Editor (DIG) into the load pipeline: dumps can now be edited automatically before VPOS starts, always working on a disposable copy.",
                        "Added toggles to disable print, license checks, myVectron, VectronConnect, and bonVito.",
                    }
                },
                new ChangelogEntry
                {
                    Title = "0.1.0 — VPOS task manager",
                    Entries =
                    {
                        "Added a live panel tracking every VPOS instance started from the tool, with the ability to bring any of them to the front or stop them directly.",
                    }
                },
                new ChangelogEntry
                {
                    Title = "0.0.0 — Basic dump loading",
                    Entries =
                    {
                        "Initial release: register VPOS PC versions, select a dump file, and launch a version against it.",
                    }
                },
            };
        }

        private static List<ChangelogEntry> BuildChangelogGerman()
        {
            return new List<ChangelogEntry>
            {
                new ChangelogEntry
                {
                    Title = "1.0.0 — VectronConnect, PAX/Verifone/MobileApp-Weiterleitung & Ton-Schalter",
                    Entries =
                    {
                        "\"Disable keyboard sound\" und \"Disable error sound\" wurden zu General Settings hinzugefügt.",
                        "Ein eigenständiger Bereich \"VectronConnect\" wurde hinzugefügt (50/50 neben myVectron aufgeteilt): ein Kästchen plus die Felder Connect ID und VC Password, die VectronConnect direkt aktivieren. Schließt sich mit \"Disable VectronConnect\" gegenseitig aus — beide schreiben in dieselbe zugrunde liegende Einstellung, sodass die automatische Dump-Bearbeitung jetzt den Start verweigert, wenn beide aktiviert sind, mit einer erklärenden Fehlermeldung.",
                        "\"Interface 20 (TCP/IP)\" heißt jetzt \"Printer (TCP/IP)\" und ist 50/50 mit einem neuen Bereich \"PAX, Verifone & MobileApp\" aufgeteilt: \"Set interface 19 to TCP/IP\" (fest auf Port 8085) mit eigenem IPv4-Feld, und \"Add Shift4 Terminal to Interface 18 (for printing)\" (ohne IP/Port).",
                        "Das Port-Feld von Interface 20 wurde entfernt — der Port ist jetzt fest auf 9100 gesetzt und wird als schreibgeschützter Text neben dem IP-Feld angezeigt, analog zur Behandlung des festen Ports bei Interface 19.",
                        "Die Titelleiste zeigt jetzt rechtsbündig die Copyright-Zeile (\"VPOS Manager © David Kopf · DIG © Volker Görgler\") und die Versionsnummer der App, gelesen aus der Assembly-Version, sodass sie nie von Hand eingetragen werden muss.",
                        "Das Kästchen \"Save Username & Password\" von myVectron wurde in \"Save credentials\" umbenannt, und ein passendes \"Save credentials\"-Kästchen (mit derselben einmaligen Klartext-Speicher-Warnung) wurde dem neuen VectronConnect-Bereich hinzugefügt, das die Persistenz von Connect ID/Passwort auf dieselbe Weise steuert.",
                        "Der registrierte Name von Interface 20 (441/1/20/1) wurde von VPOSMANAGER zu PRINTER geändert.",
                    }
                },
                new ChangelogEntry
                {
                    Title = "0.9.1 — Interface 20 (TCP/IP) & Druckertreiber-Bearbeitung",
                    Entries =
                    {
                        "Neuer Schalter \"Set interface 20 to TCP/IP\": legt eine neue TCP/IP-Schnittstelle namens VPOSMANAGER unter einer gewählten IPv4-Adresse und einem Port an. Beide Felder sind erforderlich und werden geprüft (gültige IPv4, Port 1-65535), bevor bei aktivierter automatischer Dump-Bearbeitung und diesem Kästchen irgendeine Lade-Aktion ausgeführt werden kann.",
                        "Neuer Schalter \"Set all printers to this interface\" (nur verfügbar, solange das Schnittstellen-Kästchen oben aktiviert ist): weist allen 10 Druckern Interface 20 zu und aktiviert den programmierten Treiber, mit einem Feld für die Treibernummer (1-20, standardmäßig 20), die ebenfalls vor der Verwendung geprüft wird.",
                    }
                },
                new ChangelogEntry
                {
                    Title = "0.9.0 — Fehlerbehandlung & Absturzsicherheit für die gesamte App",
                    Entries =
                    {
                        "Ein neues, im App-Design gestaltetes Fehlerfenster wurde hinzugefügt, das die bisherigen unstylischen Dialoge bei echten Fehlern ersetzt. Es zeigt eine verständliche Meldung plus einen einklappbaren Bereich \"Show details\" mit der vollständigen Ausnahme (und einem Button \"Copy details\"), im selben dunklen Kartendesign wie der Rest der App.",
                        "Ein zentraler, app-weiter Fehlermeldedienst wurde hinzugefügt, der jeden Fehler in errors.log protokolliert und das Fehlerfenster von überall aus anzeigen kann, auch aus Hintergrund-Threads und globalen Ausnahmebehandlungen.",
                        "Jede Aktion, die tatsächlich fehlschlagen und eine Kernfunktion blockieren kann - eine Version hinzufügen, einen Dump laden, automatische Dump-Bearbeitung (einschließlich VPP-Pfad- und Netzlaufwerksfehlern), den DATA-Ordner löschen, VPOS starten oder beenden, ein VPOS-Fenster in den Vordergrund holen, eine Datei oder einen Ordner in Settings auswählen - läuft jetzt über das Fehlerfenster statt über einen einfachen Dialog oder einen stillen Fehlschlag. Kleinere, sich selbst lösende Hinweise erscheinen weiterhin nur im Statusprotokoll, nicht als Popup.",
                        "Die app-weite Behandlung nicht abgefangener Ausnahmen zeigt jetzt das Fehlerfenster an, statt die Ausnahme stillschweigend zu verschlucken, verhindert den Absturz selbst aber weiterhin.",
                        "Ein latenter Fehler wurde behoben, bei dem die einfachen Hinweisdialoge (z. B. \"Bitte wählen Sie zuerst eine Version aus\") keinen XamlRoot gesetzt hatten, wodurch sie möglicherweise stillschweigend nicht erschienen.",
                    }
                },
                new ChangelogEntry
                {
                    Title = "0.8.5 — Fehler behoben: Benutzerhandbuch ließ sich nicht mehr öffnen",
                    Entries =
                    {
                        "Ein Absturz behoben, der das Öffnen des Benutzerhandbuch-Fensters komplett verhinderte: Die Vorauswahl der Sprachauswahl über IsSelected=\"True\" in XAML löste den Sprachumschalt-Handler bereits während des Aufbaus des Fensters selbst aus, bevor später deklarierte UI-Elemente überhaupt existierten. Die Standardsprache wird jetzt direkt gesetzt, der Umschalt-Handler erst danach verbunden.",
                    }
                },
                new ChangelogEntry
                {
                    Title = "0.8.4 — Sprachauswahl & deutsche Übersetzung des Benutzerhandbuchs",
                    Entries =
                    {
                        "Das Fenster des Benutzerhandbuchs ist jetzt 1500×630 groß und hat eine \"Language\"-Auswahl in der Titelleiste (English / Deutsch, standardmäßig English).",
                        "Eine vollständige deutsche Übersetzung der Funktionen und des Änderungsprotokolls wurde hinzugefügt, die sofort über die Sprachauswahl umgeschaltet wird - nichts wird zur Laufzeit maschinell übersetzt, beide Sprachversionen werden vollständig gepflegt.",
                    }
                },
                new ChangelogEntry
                {
                    Title = "0.8.3 — Hinweisfeld im Benutzerhandbuch & dickerer Trenner",
                    Entries =
                    {
                        "Am Anfang des Benutzerhandbuchs wurde ein rotes Hinweisfeld hinzugefügt, das darauf hinweist, dass unter Settings sowohl ein DumpEditor.exe-Pfad als auch ein VPP-Pfad konfiguriert sein müssen, damit automatische Dump-Bearbeitung, VPP-Austausch und alles, was davon abhängt, funktionieren.",
                        "Der grüne Trenner unter \"Automatic Dump Editing\" ist jetzt 6px dick (statt 3px), damit die vollständig abgerundeten Enden auch wirklich als abgerundet erkennbar sind.",
                    }
                },
                new ChangelogEntry
                {
                    Title = "0.8.2 — Automatische Wiederherstellung bei gesperrter VPOSPROG.DLL",
                    Entries =
                    {
                        "Ist VPOSPROG.DLL nach dem Entfernen des Schreibschutz-Attributs immer noch gesperrt (meist eine übrig gebliebene DumpEditor.exe-Instanz aus einem früheren, abgebrochenen Lauf), schließt die App jetzt automatisch alle laufenden Instanzen des konfigurierten DumpEditor.exe, informiert im Statusbereich darüber und versucht es erneut, bevor sie mit einer aussagekräftigen Fehlermeldung abbricht.",
                    }
                },
                new ChangelogEntry
                {
                    Title = "0.8.1 — Zuverlässigkeit von VPP-Austausch & Netzlaufwerken",
                    Entries =
                    {
                        "Der Fehler \"Access to VPOSPROG.DLL is denied\" bei der automatischen Dump-Bearbeitung wurde behoben: Die zuvor installierte VPP-Datei konnte ein von der Netzwerkfreigabe übernommenes Schreibschutz-Attribut mitbringen, das das Löschen selbst für Administratoren blockiert. Dieses Attribut wird jetzt vor jedem Löschen/Kopieren entfernt.",
                        "Fehlermeldungen bei einem fehlenden/nicht erreichbaren VPP-Pfad sowie beim Ordnerauswahldialog im Settings-Fenster erklären jetzt die häufigste Ursache — ein zugeordnetes Netzlaufwerk (z. B. K:), das für einen elevierten (als Administrator ausgeführten) Prozess nicht sichtbar ist — und schlagen vor, das Programm ohne erhöhte Rechte zu starten oder stattdessen einen vollständigen UNC-Pfad zu verwenden.",
                    }
                },
                new ChangelogEntry
                {
                    Title = "0.8.0 — DATA-Reset & Start im Startmenü",
                    Entries =
                    {
                        "Neuer Button \"Delete DATA, load Dump & Start VPOS\": löscht den DATA-Ordner der ausgewählten Version, lädt anschließend den Dump und startet VPOS (die automatische Dump-Bearbeitung wird dabei wie gewohnt berücksichtigt).",
                        "Neuer Button \"Launch into Startmenu\": startet die ausgewählte Version mit dem Parameter /StartMenu, anstatt einen Dump zu laden.",
                        "Die Aktionsschaltflächen im Bereich \"Versions\" wurden zu einem 2×2-Raster neu angeordnet, um die beiden neuen Buttons unterzubringen.",
                    }
                },
                new ChangelogEntry
                {
                    Title = "0.7.0 — Versionsspezifischer VPP-Austausch & Statusprotokoll",
                    Entries =
                    {
                        "Automatischer VPP-Austausch hinzugefügt: Bevor DumpEditor.exe ausgeführt wird, wird die zur gewählten VPOS-Version passende VPP-Datei als VPOSPROG.DLL eingesetzt, mit einer Statusmeldung für jeden Schritt.",
                        "Neue Einstellung \"VPP Path\" hinzugefügt (standardmäßig die übliche Netzwerkfreigabe), zusätzlich zum bestehenden DumpEditor.exe-Pfad.",
                        "Der Terminalbereich heißt jetzt \"Status\" und zeigt lesbare Statusmeldungen (\"Starting VPOS...\", \"Loading dump and starting VPOS...\", \"VPOS started (PID ...)\") statt nur der rohen DumpEditor.exe-Ausgabe.",
                        "Die Anzeige, ob DumpEditor.exe gefunden wurde, wurde von der Versions-Karte in die Kopfzeile des Statusbereichs verschoben.",
                        "Die Größe des Settings-Fensters wurde angepasst, um das neue Feld für den VPP-Pfad unterzubringen.",
                    }
                },
                new ChangelogEntry
                {
                    Title = "0.6.0 — Live-Ausgabebereich für DumpEditor",
                    Entries =
                    {
                        "Ein terminalartiger Bereich wurde hinzugefügt, der die stdout-/stderr-Ausgabe von DumpEditor.exe live während der Ausführung anzeigt, im gleichen Akzentgrün auf einem eigenen dunklen \"Bildschirm\" innerhalb der Karte.",
                        "Das Layout des Hauptfensters wurde auf eine 60/40-Spaltenaufteilung umgestellt; die rechte Spalte teilt sich jetzt 65/35 zwischen dem Bereich \"Running VPOS Instances\" und dem neuen Terminalbereich.",
                    }
                },
                new ChangelogEntry
                {
                    Title = "0.5.0 — Visuelle Überarbeitung",
                    Entries =
                    {
                        "Eigene Titelleiste: Die native weiße Windows-Titelleiste ist verschwunden — die Minimieren-/Maximieren-/Schließen-Schaltflächen des Betriebssystems werden jetzt direkt auf der eigenen dunklen Menüleiste der App dargestellt.",
                        "Buttons, Eingabefelder und Schalter wurden für ein einheitlicheres Erscheinungsbild neu gestaltet; das Akzentgrün ist jetzt der einen primären Aktion pro Bildschirm vorbehalten, statt über mehrere sekundäre Buttons verteilt zu sein.",
                        "Der Bereich \"Running VPOS Instances\" wurde mit einem Live-Zähler-Badge und einem leuchtenden Statusindikator pro Prozess neu gestaltet.",
                    }
                },
                new ChangelogEntry
                {
                    Title = "0.4.0 — Konfigurierbarer DumpEditor-Pfad & Zuverlässigkeitsverbesserungen",
                    Entries =
                    {
                        "Ein Settings-Fenster wurde hinzugefügt, mit dem die App auf eine beliebige DumpEditor.exe-Installation verweisen kann, statt auf den bisher fest eingetragenen Pfad.",
                        "dig.ini, support.exml und support.xml werden jetzt von der App selbst erzeugt, statt bereits vorhandene Dateien vorauszusetzen, und in einen Ordner VPOSManager neben DumpEditor.exe geschrieben.",
                        "Eine Menüleiste mit den Einträgen Settings und Help wurde hinzugefügt.",
                        "Ein optionaler Schalter \"Save Username & Password\" für myVectron-Zugangsdaten wurde hinzugefügt, inklusive einmaligem Hinweis auf die Klartextspeicherung.",
                        "Ein Absturz beim Start und ein Deadlock beim Schließen wurden behoben; das Laden der Einstellungen wurde gehärtet, sodass sich die App bei einer beschädigten settings.json wiederherstellt, statt den Start zu verweigern.",
                    }
                },
                new ChangelogEntry
                {
                    Title = "0.3.0 — myVectron-Zugangsdaten & Serverauswahl",
                    Entries =
                    {
                        "Optionale Überschreibung von myVectron-Benutzername/-Passwort hinzugefügt, die beim Laden in den Dump eingetragen wird.",
                        "Ein Prod/Test-Schalter wurde hinzugefügt, der festlegt, auf welche Serverumgebung VectronConnect und myVectron zeigen.",
                        "Der Settings-Bildschirm wurde zur besseren Übersicht in die Bereiche \"General Settings\" und \"myVectron\" gegliedert.",
                    }
                },
                new ChangelogEntry
                {
                    Title = "0.2.0 — Automatische Dump-Bearbeitung",
                    Entries =
                    {
                        "Der Dump Editor (DIG) eines Drittanbieters wurde in den Ladevorgang integriert: Dumps können jetzt automatisch bearbeitet werden, bevor VPOS startet, wobei immer nur eine Wegwerfkopie verwendet wird.",
                        "Schalter zum Deaktivieren von Druck, Lizenzprüfungen, myVectron, VectronConnect und bonVito wurden hinzugefügt.",
                    }
                },
                new ChangelogEntry
                {
                    Title = "0.1.0 — VPOS-Taskmanager",
                    Entries =
                    {
                        "Ein Live-Bereich wurde hinzugefügt, der jede über das Tool gestartete VPOS-Instanz anzeigt und es ermöglicht, jede davon direkt in den Vordergrund zu holen oder zu beenden.",
                    }
                },
                new ChangelogEntry
                {
                    Title = "0.0.0 — Grundlegendes Laden von Dumps",
                    Entries =
                    {
                        "Erstveröffentlichung: VPOS-PC-Versionen registrieren, eine Dump-Datei auswählen und eine Version damit starten.",
                    }
                },
            };
        }
    }
}
