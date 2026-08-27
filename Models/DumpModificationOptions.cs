using CommunityToolkit.Mvvm.ComponentModel;

namespace DumpLoader_2._0.Models
{
    public class DumpModificationOptions : ObservableObject
    {
        private bool _automaticDumpEditing;
        public bool AutomaticDumpEditing
        {
            get => _automaticDumpEditing;
            set => SetProperty(ref _automaticDumpEditing, value);
        }

        private bool _disablePrint;
        public bool DisablePrint
        {
            get => _disablePrint;
            set => SetProperty(ref _disablePrint, value);
        }

        private bool _disableLicenseCheck;
        public bool DisableLicenseCheck
        {
            get => _disableLicenseCheck;
            set => SetProperty(ref _disableLicenseCheck, value);
        }

        private bool _disableMyVectron;
        public bool DisableMyVectron
        {
            get => _disableMyVectron;
            set => SetProperty(ref _disableMyVectron, value);
        }

        private bool _disableVectronConnect;
        public bool DisableVectronConnect
        {
            get => _disableVectronConnect;
            set => SetProperty(ref _disableVectronConnect, value);
        }

        private bool _disableBonVito;
        public bool DisableBonVito
        {
            get => _disableBonVito;
            set => SetProperty(ref _disableBonVito, value);
        }

        private bool _myVectronUsernameEnabled;
        public bool MyVectronUsernameEnabled
        {
            get => _myVectronUsernameEnabled;
            set => SetProperty(ref _myVectronUsernameEnabled, value);
        }

        private string? _myVectronUsername;
        public string? MyVectronUsername
        {
            get => _myVectronUsername;
            set => SetProperty(ref _myVectronUsername, value);
        }

        private bool _myVectronPasswordEnabled;
        public bool MyVectronPasswordEnabled
        {
            get => _myVectronPasswordEnabled;
            set => SetProperty(ref _myVectronPasswordEnabled, value);
        }

        private string? _myVectronPassword;
        public string? MyVectronPassword
        {
            get => _myVectronPassword;
            set => SetProperty(ref _myVectronPassword, value);
        }

        /// <summary>
        /// When true, MyVectronUsername/MyVectronPassword are written to settings.json in
        /// cleartext and restored on next launch. When false, they are only ever kept in memory
        /// for the current session and never persisted.
        /// </summary>
        private bool _saveMyVectronCredentials;
        public bool SaveMyVectronCredentials
        {
            get => _saveMyVectronCredentials;
            set => SetProperty(ref _saveMyVectronCredentials, value);
        }

        /// <summary>
        /// Server environment for VectronConnect (33/1/524/1) and myVectron (33/1/589/1).
        /// false = Prod (both set to 0), true = Test (both set to 1). Always applied - not
        /// an opt-in checkbox like the others - whenever automatic dump editing runs.
        /// </summary>
        private bool _isTestServer;
        public bool IsTestServer
        {
            get => _isTestServer;
            set => SetProperty(ref _isTestServer, value);
        }

        /// <summary>
        /// "Set interface 20 to TCP/IP" - registers a new TCP/IP interface (number 20) under the
        /// name PRINTER, at the configured IP/port. Required before any of the printer edits
        /// below make sense, since they all point printers at interface 20.
        /// </summary>
        private bool _setTcpIpInterface20;
        public bool SetTcpIpInterface20
        {
            get => _setTcpIpInterface20;
            set
            {
                if (SetProperty(ref _setTcpIpInterface20, value))
                    OnPropertyChanged(nameof(PrinterDriverFieldEnabled));
            }
        }

        private string? _interface20IpAddress;
        /// <summary>Port is fixed at 9100 (not user-configurable) - see DumpEditorService.</summary>
        public string? Interface20IpAddress
        {
            get => _interface20IpAddress;
            set => SetProperty(ref _interface20IpAddress, value);
        }

        /// <summary>
        /// "Set all printers to this interface" - only meaningful (and only enabled in the UI)
        /// while SetTcpIpInterface20 is on. Points all 10 printer driver slots at interface 20
        /// with a programmed driver enabled.
        /// </summary>
        private bool _setAllPrintersToInterface;
        public bool SetAllPrintersToInterface
        {
            get => _setAllPrintersToInterface;
            set
            {
                if (SetProperty(ref _setAllPrintersToInterface, value))
                    OnPropertyChanged(nameof(PrinterDriverFieldEnabled));
            }
        }

        private string _printerDriverNumber = "20";
        /// <summary>Driver number (1-20) applied to all 10 printers when SetAllPrintersToInterface is on.</summary>
        public string PrinterDriverNumber
        {
            get => _printerDriverNumber;
            set => SetProperty(ref _printerDriverNumber, value);
        }

        /// <summary>
        /// Computed, not persisted: whether the printer driver-number field should be editable in
        /// the UI. WinUI has no MultiBinding, so this stands in for an AND of the two checkboxes
        /// above, re-raised whenever either one changes.
        /// </summary>
        public bool PrinterDriverFieldEnabled => SetTcpIpInterface20 && SetAllPrintersToInterface;

        private bool _disableKeyboardSound;
        public bool DisableKeyboardSound
        {
            get => _disableKeyboardSound;
            set => SetProperty(ref _disableKeyboardSound, value);
        }

        private bool _disableErrorSound;
        public bool DisableErrorSound
        {
            get => _disableErrorSound;
            set => SetProperty(ref _disableErrorSound, value);
        }

        /// <summary>
        /// "VectronConnect" (enable, with a Connect ID + password) - distinct from, and mutually
        /// exclusive with, DisableVectronConnect above: both write to the same DIG addresses
        /// (780/1/1/1, /2, /12) with opposite intent, so MainViewModel refuses to run automatic
        /// dump editing if both are checked at once.
        /// </summary>
        private bool _enableVectronConnect;
        public bool EnableVectronConnect
        {
            get => _enableVectronConnect;
            set => SetProperty(ref _enableVectronConnect, value);
        }

        private string? _vectronConnectId;
        public string? VectronConnectId
        {
            get => _vectronConnectId;
            set => SetProperty(ref _vectronConnectId, value);
        }

        private string? _vectronConnectPassword;
        public string? VectronConnectPassword
        {
            get => _vectronConnectPassword;
            set => SetProperty(ref _vectronConnectPassword, value);
        }

        /// <summary>
        /// When true, VectronConnectId/VectronConnectPassword are written to settings.json in
        /// cleartext and restored on next launch - same deal as SaveMyVectronCredentials above.
        /// When false, they only ever live in memory for the current session.
        /// </summary>
        private bool _saveVectronConnectCredentials;
        public bool SaveVectronConnectCredentials
        {
            get => _saveVectronConnectCredentials;
            set => SetProperty(ref _saveVectronConnectCredentials, value);
        }

        /// <summary>
        /// "Set interface 19 to TCP/IP" - registers interface 19 (named TERMINAL) for PAX,
        /// Verifone & MobileApp routing, at a fixed port (8085) and the configured IP.
        /// </summary>
        private bool _setTcpIpInterface19;
        public bool SetTcpIpInterface19
        {
            get => _setTcpIpInterface19;
            set => SetProperty(ref _setTcpIpInterface19, value);
        }

        private string? _interface19IpAddress;
        public string? Interface19IpAddress
        {
            get => _interface19IpAddress;
            set => SetProperty(ref _interface19IpAddress, value);
        }

        /// <summary>"Add Shift4 Terminal to Interface 18 (for printing)" - retypes the existing
        /// interface 18 (named TERMDRUCK), no IP/port involved.</summary>
        private bool _addShift4TerminalToInterface18;
        public bool AddShift4TerminalToInterface18
        {
            get => _addShift4TerminalToInterface18;
            set => SetProperty(ref _addShift4TerminalToInterface18, value);
        }
    }
}
