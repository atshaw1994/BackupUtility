using Microsoft.Win32;
using System.Globalization;
using System.Management;
using System.Runtime.InteropServices;
using System.Security.Principal;
using System.Windows;
using System.Windows.Interop;

namespace BackupUtility
{
    /// <summary>
    /// Interaction logic for BackupObjectForm.xaml
    /// </summary>
    public partial class BackupObjectForm : Window
    {
        public BackupObject BackupObjectResult { get; private set; }

        public BackupObjectForm()
        {
            InitializeComponent();
            BackupObjectResult = new();
        }

        public BackupObjectForm(BackupObject backupObject)
        {
            InitializeComponent();
            BackupObjectResult = backupObject;
            SourceButton.Content = FormatPath(BackupObjectResult.Source);
            DestinationTextBox.Text = BackupObjectResult.Destination;
        }

        public BackupObjectForm(string source, string destination)
        {
            InitializeComponent();
            BackupObjectResult = new()
            {
                Source = source,
                Destination = destination
            };
        }

        #region Borderless Methods
        [DllImport("user32.dll")]
        private static extern nint MonitorFromWindow(IntPtr handle, uint flags);

        [DllImport("user32.dll")]
        private static extern bool GetMonitorInfo(IntPtr hMonitor, ref MONITORINFO lpmi);

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

        private void OnCloseButtonClick(object sender, RoutedEventArgs e) => Close();

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
                };

                watcher.Start();
            }
            catch (Exception)
            {
                // This can fail on Windows 7
            }

            WindowsTheme initialTheme = GetWindowsTheme();
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

        private void SourceButton_Click(object sender, RoutedEventArgs e)
        {
            OpenFolderDialog ofd = new();
            if (ofd.ShowDialog() == true)
            {
                BackupObjectResult.Source = ofd.FolderName;
                SourceButton.Content = FormatPath(BackupObjectResult.Source);
            }
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            BackupObjectResult.Destination = DestinationTextBox.Text;
            DialogResult = true;
            Close();
        }

        private string FormatPath(string path)
        {
            if (string.IsNullOrEmpty(path)) return "";

            string[] parts = path.Split(System.IO.Path.DirectorySeparatorChar, StringSplitOptions.RemoveEmptyEntries);

            if (parts.Length <= 3)
                return path;

            string firstPart = parts[0];
            string lastPart = parts[^1];

            return $"/{firstPart}/.../{lastPart}/";
        }
    }
}
