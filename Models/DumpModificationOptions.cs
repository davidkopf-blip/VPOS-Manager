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

    }
}
