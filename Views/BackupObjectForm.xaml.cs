using BackupUtility.Models;
using BackupUtility.ViewModels;
using Microsoft.Win32;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Management;
using System.Runtime.InteropServices;
using System.Security.Principal;
using System.Windows;
using System.Windows.Interop;
using System.Linq;

namespace BackupUtility.Views
{
    /// <summary>
    /// Interaction logic for BackupObjectForm.xaml
    /// </summary>
    public partial class BackupObjectForm : Window
    {
        public BackupObject BackupObjectResult
        {
            get => (DataContext as BackupObjectFormViewModel)?.CurrentBackupObject!;
        }

        public BackupObjectForm(ObservableCollection<BackupObject> backupObjects)
        {
            InitializeComponent();
            var viewModel = new BackupObjectFormViewModel();
            DataContext = viewModel;
            SetupViewModelEvents(viewModel);
            foreach (var backupObject in backupObjects.Where(backupObject => !DestinationComboBox.Items.Contains(backupObject.Destination)))
                DestinationComboBox.Items.Add(backupObject.Destination);
        }

        public BackupObjectForm(BackupObject BackupObject, ObservableCollection<BackupObject> backupObjects)
        {
            InitializeComponent();
            var viewModel = new BackupObjectFormViewModel(BackupObject);
            DataContext = viewModel;
            SetupViewModelEvents(viewModel);
            foreach (var backupObject in backupObjects.Where(backupObject => !DestinationComboBox.Items.Contains(backupObject.Destination)))
                DestinationComboBox.Items.Add(backupObject.Destination);
        }

        private void SetupViewModelEvents(BackupObjectFormViewModel viewModel)
        {
            viewModel.RequestClose += () => Close();
            viewModel.RequestSaveAndClose += () =>
            {
                DialogResult = true;
                Close();
            };
        }

        #region Borderless Methods

        [LibraryImport("user32.dll")]
        private static partial nint MonitorFromWindow(IntPtr handle, uint flags);

        [LibraryImport("user32.dll", EntryPoint = "GetMonitorInfoW")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static partial bool GetMonitorInfo(IntPtr hMonitor, ref MONITORINFO lpmi);

        private const int WM_GETMINMAXINFO = 0x0024;
        private const uint MONITOR_DEFAULTTONEAREST = 0x00000002;

        [Serializable]
        [StructLayout(LayoutKind.Sequential)]
        public struct RECT(int left, int top, int right, int bottom)
        {
            public int Left = left;
            public int Top = top;
            public int Right = right;
            public int Bottom = bottom;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct MONITORINFO
        {
            public int cbSize;
            public RECT rcMonitor;
            public RECT rcWork;
            public uint dwFlags;
        }

        [Serializable]
        [StructLayout(LayoutKind.Sequential)]
        public struct POINT(int x, int y)
        {
            public int X = x;
            public int Y = y;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct MINMAXINFO
        {
            public POINT ptReserved;
            public POINT ptMaxSize;
            public POINT ptMaxPosition;
            public POINT ptMinTrackSize;
            public POINT ptMaxTrackSize;
        }

        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);
            ((HwndSource)PresentationSource.FromVisual(this)).AddHook(HookProc);
        }

        public static nint HookProc(nint hwnd, int msg, nint wParam, nint lParam, ref bool handled)
        {
            if (msg == WM_GETMINMAXINFO)
            {
                MINMAXINFO mmi = Marshal.PtrToStructure<MINMAXINFO>(lParam)!;
                nint monitor = MonitorFromWindow(hwnd, MONITOR_DEFAULTTONEAREST);

                if (monitor != nint.Zero)
                {
                    MONITORINFO monitorInfo = new() { cbSize = Marshal.SizeOf<MONITORINFO>() };
                    GetMonitorInfo(monitor, ref monitorInfo);
                    RECT rcWorkArea = monitorInfo.rcWork;
                    RECT rcMonitorArea = monitorInfo.rcMonitor;
                    mmi.ptMaxPosition.X = Math.Abs(rcWorkArea.Left - rcMonitorArea.Left);
                    mmi.ptMaxPosition.Y = Math.Abs(rcWorkArea.Top - rcMonitorArea.Top);
                    mmi.ptMaxSize.X = Math.Abs(rcWorkArea.Right - rcWorkArea.Left);
                    mmi.ptMaxSize.Y = Math.Abs(rcWorkArea.Bottom - rcWorkArea.Top);
                }
                Marshal.StructureToPtr(mmi, lParam, true);
            }
            return nint.Zero;
        }
        #endregion

        #region Theming
        private const string RegistryKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize";
        private const string RegistryValueName = "AppsUseLightTheme";

        private enum WindowsTheme { Light, Dark }

        public static void WatchTheme()
        {
            // You might want to move this to your App.xaml.cs or a separate ThemeManager class
            // if you intend for the entire application to react to theme changes.
            var currentUser = WindowsIdentity.GetCurrent();
            string query = string.Format(
                CultureInfo.InvariantCulture,
                @"SELECT * FROM RegistryValueChangeEvent WHERE Hive = 'HKEY_USERS' AND KeyPath = '{0}\\{1}' AND ValueName = '{2}'",
                currentUser.User!.Value,
                RegistryKeyPath.Replace(@"\", @"\\"),
                RegistryValueName);

            try
            {
                var watcher = new ManagementEventWatcher(query);
                watcher.EventArrived += (sender, args) =>
                {
                    WindowsTheme newWindowsTheme = GetWindowsTheme();
                    // You would typically apply the new theme to your application resources here
                    // For example: if (newWindowsTheme == WindowsTheme.Dark) Application.Current.Resources.MergedDictionaries.Add(new ResourceDictionary { Source = new Uri("Themes/DarkTheme.xaml", UriKind.Relative) });
                };
                watcher.Start();
            }
            catch (Exception)
            {
                // This can fail on Windows 7
            }
            // WindowsTheme initialTheme = GetWindowsTheme(); // Initial theme setting
        }

        private static WindowsTheme GetWindowsTheme()
        {
            using RegistryKey key = Registry.CurrentUser.OpenSubKey(RegistryKeyPath)!;
            object registryValueObject = key?.GetValue(RegistryValueName)!;
            if (registryValueObject == null) { return WindowsTheme.Light; }

            int registryValue = (int)registryValueObject;

            return registryValue > 0 ? WindowsTheme.Light : WindowsTheme.Dark;
        }
        #endregion

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {

        }
    }
}