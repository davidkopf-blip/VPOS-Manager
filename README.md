# VPOS Manager

A third-party support tool for loading dumps into VPOS PC, managing VPOS sessions, and keeping
track of installed VPOS versions. It automates repetitive setup tasks such as disabling printing,
license checks, and applying common configuration changes, saving time and streamlining dump
analysis.

## Features

**Version management**
Register any number of installed VPOS PC builds by pointing at their `.exe` once — the version
number is read straight from the file's own metadata and remembered between sessions.

**Dump loading**
Pick a `.vpd`/`.VPosDump` file and launch it against any registered version via `/LoadDump`, or
start a version standalone without a dump. "Delete DATA, load Dump & Start VPOS" additionally
wipes the selected version's `DATA` folder before loading the dump (evaluating automatic dump
editing the same way as the plain load button), and "Launch into Startmenu" starts the selected
version straight into VPOS's start menu via `/StartMenu`.

**VPOS task manager**
Every VPOS instance launched through the tool is tracked in a live side panel — bring any of them
to the foreground, stop them, and see stopped ones disappear automatically.

**Automatic dump editing**
Before loading a dump, VPOS Manager can run it through the third-party VPOS Dump Editor (DIG) and
apply a chosen set of edits to a working copy — the original dump file is never touched. Available
toggles:
- Disable print
- Disable license check
- Disable myVectron
- Disable VectronConnect
- Disable bonVito
- Disable keyboard sound
- Disable error sound

Before DumpEditor.exe runs, the app also swaps in the VPP program file matching the selected VPOS
version (`VPP-{Version}.VPP` from the configured VPP Path, copied in as `VPOSPROG.DLL`), so the
dump is always edited with the correct version's program logic.

**myVectron & VectronConnect**
Bake a specific myVectron username/password into the dump before launch, and flip a single switch
to point VectronConnect and myVectron at the Test or Prod server environment. Check "Save
credentials" to remember them between sessions (stored in plain text in `settings.json`, with a
one-time warning). Alongside it, an independent "VectronConnect" section can enable VectronConnect
outright with a Connect ID and password, with its own matching "Save credentials" checkbox and
warning — mutually exclusive with "Disable VectronConnect" above, since both configure the same
underlying setting; VPOS Manager refuses to run automatic dump editing if both are checked.

**Printer (TCP/IP) & PAX, Verifone and MobileApp**
"Set interface 20 to TCP/IP" registers a TCP/IP interface (named `PRINTER`, fixed port 9100)
at the IPv4 address you enter, required and validated before any load-dump action runs while this
is checked. With it on, "Set all printers to this interface" points all 10 printer driver slots
at interface 20 with a programmed driver enabled, using a driver number (1-20, 20 by default)
also validated before use. Separately, "Set interface 19 to TCP/IP" registers interface 19 (named
`TERMINAL`, fixed port 8085) for PAX/Verifone/MobileApp routing at its own IPv4 address, and "Add
Shift4 Terminal to Interface 18 (for printing)" retypes interface 18 for that purpose — no
IP/port needed for that one.

**Status log**
A live status panel shows what's happening as a dump loads and VPOS starts — from "Loading dump
and starting VPOS..." through the VPP/DumpEditor steps to "VPOS started" — alongside DumpEditor's
raw output, all in one place.

**Error handling**
Genuine failures (a locked VPOSPROG.DLL, an unreachable VPP network path, a dump that can't be
loaded, a process that won't start, and so on) surface in an app-styled error window with a
collapsible details section, instead of a plain dialog or a silent failure. An app-wide handler
catches anything else that would otherwise crash the app, logs it, and shows the same error
window instead.

**Persistence & diagnostics**
Registered versions, the last-used dump path, and every toggle above are saved between runs.
Startup errors and key actions are logged to timestamped files for troubleshooting.

## Changelog

### 1.0.0 — VectronConnect, PAX/Verifone/MobileApp routing & sound toggles
- Added "Disable keyboard sound" and "Disable error sound" to General Settings.
- Added an independent "VectronConnect" section (split 50/50 next to myVectron): a checkbox plus
  Connect ID and VC Password fields that enable VectronConnect outright. Mutually exclusive with
  "Disable VectronConnect" — both write to the same underlying setting, so automatic dump editing
  now refuses to run if both are checked, with an explanatory error.
- "Interface 20 (TCP/IP)" is now "Printer (TCP/IP)", split 50/50 with a new "PAX, Verifone &
  MobileApp" section: "Set interface 19 to TCP/IP" (fixed port 8085) plus its own IPv4 field, and
  "Add Shift4 Terminal to Interface 18 (for printing)" (no IP/port needed).
- Interface 20's port field was removed — it's now fixed at 9100 and shown as read-only text
  beside the IP field, matching interface 19's fixed-port treatment.
- The title bar now shows the copyright line ("VPOS Manager © David Kopf · DIG © Volker Görgler")
  and the app's version number, right-bound, read from the assembly version so it never needs to
  be hand-typed — only bumped once, in the `.csproj`'s `<Version>`.
- Renamed myVectron's "Save Username & Password" checkbox to "Save credentials", and added a
  matching "Save credentials" checkbox (with the same one-time cleartext-storage warning) to the
  new VectronConnect section, gating its Connect ID/password persistence the same way.
- Interface 20's registered name (`441/1/20/1`) changed from `VPOSMANAGER` to `PRINTER`.

### 0.9.1 — Interface 20 (TCP/IP) & printer driver editing
- Added "Set interface 20 to TCP/IP": registers a new TCP/IP interface named `VPOSMANAGER` at a
  chosen IPv4 address and port. Both fields are required and validated (valid IPv4, port 1-65535)
  before any load-dump action can run while automatic dump editing and this checkbox are on.
- Added "Set all printers to this interface" (only available while the interface checkbox above
  is on): points all 10 printers at interface 20 with a programmed driver enabled, using a driver
  number field (1-20, defaults to 20, also validated before use).

### 0.9.0 — App-wide error handling & crash safety
- Added a new app-styled error window (`ErrorWindow`), replacing the default unstyled
  ContentDialogs for real failures. It shows a friendly message plus a collapsible "Show
  details" section with the full exception (and a "Copy details" button), all in the same dark
  card design as the rest of the app.
- Added `ErrorReportingService`, a single app-wide entry point that logs every error to
  `errors.log` and shows the error window, callable from anywhere (including background threads
  and global exception handlers) since it opens a real Window rather than depending on a
  XamlRoot.
- Every operation that can genuinely fail and block a core function — adding a version, loading
  a dump, automatic dump editing (including VPP-path and network-drive failures), deleting the
  DATA folder, starting or stopping VPOS, bringing a VPOS window to the front, picking a file or
  folder in Settings — now routes through the error window instead of a plain dialog or a
  silent failure. Minor, self-resolving notices (e.g. "closing a leftover DumpEditor.exe
  instance") still only appear in the Status log, not as a popup.
- The app-wide unhandled-exception handler now shows the error window (instead of silently
  swallowing the exception) whenever it catches something that would otherwise crash the app,
  while still preventing the crash itself.
- Fixed a latent bug where the plain validation dialogs (e.g. "please select a version first")
  had no `XamlRoot` set, which could make them silently fail to appear.

### 0.8.5 — Fixed the User Guide window not opening
- Fixed a crash that prevented the User Guide window from opening at all: defaulting the
  Language combobox's selection via `IsSelected="True"` in XAML fired the language-switch
  handler during the window's own construction, before later UI elements existed yet. The
  default language is now applied directly, with the switch handler wired up only afterwards.

### 0.8.4 — User Guide language selector & German translation
- The User Guide window is now 1500×630 and has a "Language" selector in its title bar
  (English / Deutsch, English by default).
- Added a full German translation of the User Guide's features and changelog, switched
  instantly via the Language selector — nothing is machine-translated at runtime, both
  language versions are maintained in full.

### 0.8.3 — User Guide setup disclaimer & thicker accent separator
- Added a red disclaimer box at the top of the User Guide stating that both a DumpEditor.exe
  path and a VPP Path must be configured under Settings for automatic dump editing, VPP
  swapping, and everything that depends on them to work.
- The green separator under "Automatic Dump Editing" is now 6px thick (up from 3px), so its
  fully-rounded pill ends actually read as rounded instead of being too thin to notice.

### 0.8.2 — Automatic recovery from a locked VPOSPROG.DLL
- If VPOSPROG.DLL is still locked after clearing its read-only attribute (typically a leftover
  DumpEditor.exe instance from an earlier, aborted run), the app now closes any running instance
  of the configured DumpEditor.exe and retries automatically, with a status notification, before
  giving up with an actionable error.

### 0.8.1 — VPP swap & network drive reliability
- Fixed "Access to VPOSPROG.DLL is denied" during automatic dump editing: the VPP file
  previously installed could carry over a read-only attribute (inherited from the network
  share) that blocks deletion even for administrators. That attribute is now cleared before
  every delete/copy.
- Errors for a missing/unreachable VPP path, and for the Settings window's folder picker, now
  explain the most common cause — a mapped network drive (e.g. `K:`) that isn't visible to an
  elevated (Run as administrator) process — and suggest running unelevated or using a full UNC
  path instead.

### 0.8.0 — DATA reset & Start Menu launch
- Added "Delete DATA, load Dump & Start VPOS": deletes the selected version's `DATA` folder,
  then loads the dump and starts VPOS (automatic dump editing is still evaluated as usual).
- Added "Launch into Startmenu": starts the selected version with the `/StartMenu` parameter
  instead of loading a dump.
- Version section's action buttons rearranged into a 2×2 grid to fit the two new buttons.

### 0.7.0 — Per-version VPP swapping & status log
- Added automatic VPP swapping: before DumpEditor.exe runs, the VPP file matching the selected
  VPOS version is copied in as `VPOSPROG.DLL`, with a status notification for each step.
- Added a "VPP Path" setting (defaults to the standard network share), alongside the existing
  DumpEditor.exe path setting.
- The terminal panel is now labeled "Status" and shows readable status notifications ("Starting
  VPOS...", "Loading dump and starting VPOS...", "VPOS started (PID ...)"), not just raw
  DumpEditor.exe output.
- The DumpEditor.exe found/not-found indicator moved from the Versions card into the Status
  panel's header row.
- Settings window resized to fit the new VPP Path field.

### 0.6.0 — Live DumpEditor output panel
- Added a terminal-style panel showing DumpEditor.exe's stdout/stderr live as it runs, in the
  same accent green on a dedicated dark "screen" nested inside the panel's card.
- Main window layout rebalanced to a 60/40 column split; the right column now splits 65/35
  between the Running VPOS Instances panel and the new terminal panel.

### 0.5.0 — Visual refresh
- Custom title bar: the native white Windows caption area is gone — the OS min/max/close
  buttons now render directly on the app's own dark menu bar.
- Buttons, fields, and toggles restyled for a more consistent look; the accent green is now
  reserved for the one primary action per screen instead of scattered across secondary buttons.
- Running VPOS Instances panel redesigned with a live count badge and a glowing status
  indicator per process.

### 0.4.0 — Configurable DumpEditor location & reliability fixes
- Added a Settings window for pointing the app at any `DumpEditor.exe` install, replacing the
  previous hardcoded path.
- `dig.ini`, `support.exml`, and `support.xml` are now generated by the app itself rather than
  relying on pre-existing files, and are written to a `VPOSManager` folder next to
  `DumpEditor.exe`.
- Added a menu bar with Settings and Help entries.
- Added an opt-in "Save Username & Password" toggle for myVectron credentials, with a
  one-time cleartext-storage disclaimer.
- Fixed a startup crash, a freeze-on-close deadlock, and hardened settings loading to recover
  from a corrupted `settings.json` instead of failing to start.

### 0.3.0 — myVectron credentials & server selection
- Added optional myVectron username/password overrides, baked into the dump on load.
- Added a Prod/Test switch controlling which server environment VectronConnect and myVectron
  point at.
- Settings screen reorganized into "General Settings" and "myVectron" sections for clarity.

### 0.2.0 — Automatic dump editing
- Integrated the third-party VPOS Dump Editor (DIG) into the load pipeline: dumps can now be
  edited automatically before VPOS starts, always working on a disposable copy.
- Added toggles to disable print, license checks, myVectron, VectronConnect, and bonVito.

### 0.1.0 — VPOS task manager
- Added a live panel tracking every VPOS instance started from the tool, with the ability to
  bring any of them to the front or stop them directly.

### 0.0.0 — Basic dump loading
- Initial release: register VPOS PC versions, select a dump file, and launch a version against
  it.
