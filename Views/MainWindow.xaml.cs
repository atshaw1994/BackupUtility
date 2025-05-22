using BackupUtility.Models;
using BackupUtility.ViewModels;
using Microsoft.Win32;
using System.ComponentModel;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Management;
using System.Runtime.InteropServices;
using System.Security.Principal;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Threading;

namespace BackupUtility.Views
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private readonly DispatcherTimer BackupTimer;
        private MainWindowViewModel ViewModel { get; }
        private readonly List<DriveInfo> drives = [];

        public MainWindow()
        {
            InitializeComponent();
            ViewModel = (MainWindowViewModel)DataContext;
            _trayIcon = new NotifyIcon
            {
                Icon = new Icon("Backup.ico", 40, 40),
                Text = "BackupUtility",
                Visible = true,
                ContextMenuStrip = CreateContextMenu()
            };
            _trayIcon.DoubleClick += (s, e) => { Dispatcher.Invoke(() => ShowMainWindow()); };
            IsVisibleChanged += (s, e) => { if (!IsVisible) Hide(); };
            BackupTimer = new() { Interval = TimeSpan.FromSeconds(1) };
            BackupTimer.Tick += (sender, e) => {
                if (DataContext is MainWindowViewModel viewModel)
                {
                    if (DateTime.Now.TimeOfDay == new TimeSpan(6, 0, 0))
                    {
                        ICommand myCommand = viewModel.StartBackupCommand;
                        if (myCommand != null && myCommand.CanExecute(null))
                            myCommand.Execute(null);
                    }
                }
            };
            PopulateDrives();
        }

        #region TrayIcon
        private readonly NotifyIcon _trayIcon;
        private ContextMenuStrip CreateContextMenu()
        {
            ContextMenuStrip contextMenu = new();
            contextMenu.Items.Add("Open", null, (s, e) => Dispatcher.Invoke(() => ShowMainWindow()));
            contextMenu.Items.Add("Backup Now", null, (s, e) => Dispatcher.Invoke(() => 
            {
                if (DataContext is MainWindowViewModel viewModel)
                {
                    ICommand myCommand = viewModel.StartBackupCommand;

                    if (myCommand != null && myCommand.CanExecute(null))
                    {
                        myCommand.Execute(null);
                    }
                }
            }));
            contextMenu.Items.Add("Exit", null, (s, e) => Dispatcher.Invoke(() => System.Windows.Application.Current.Shutdown()));
            return contextMenu;
        }
        private void ShowMainWindow()
        {
            Show();
            WindowState = WindowState.Normal;
        }
        protected override void OnClosing(CancelEventArgs e)
        {
            // Minimize to tray instead of closing
            e.Cancel = true;
            Visibility = Visibility.Hidden;
            base.OnClosing(e);
        }
        #endregion

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

        private void OnMinimizeButtonClick(object sender, RoutedEventArgs e)
            => WindowState = WindowState.Minimized;

        private void OnMaximizeRestoreButtonClick(object sender, RoutedEventArgs e)
            => WindowState = WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;

        private void OnCloseButtonClick(object sender, RoutedEventArgs e)
            => Close();

        private void RefreshMaximizeRestoreButton()
        {
            if (WindowState == WindowState.Maximized)
            {
                maximizeButton.Visibility = Visibility.Collapsed;
                restoreButton.Visibility = Visibility.Visible;
            }
            else
            {
                maximizeButton.Visibility = Visibility.Visible;
                restoreButton.Visibility = Visibility.Collapsed;
            }
        }

        private void Window_StateChanged(object sender, EventArgs e)
            => RefreshMaximizeRestoreButton();

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

                // These parameters are indeed used here in the call to MonitorFromWindow
                nint monitor = MonitorFromWindow(hwnd, MONITOR_DEFAULTTONEAREST);

                if (monitor != nint.Zero)
                {
                    MONITORINFO monitorInfo = new() { cbSize = Marshal.SizeOf<MONITORINFO>() };
                    // This parameter is indeed used here in the call to GetMonitorInfo
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

        private enum WindowsTheme
        {
            Light,
            Dark
        }

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

        private void PopulateDrives()
        {
            foreach (string driveletter in Directory.GetLogicalDrives())
            {
                DriveInfo driveInfo = new(driveletter);
                if (driveInfo.IsReady)
                {
                    drives.Add(driveInfo);
                    string volumeLabel = driveInfo.VolumeLabel;
                    string driveLetterOnly = driveInfo.Name[..2];
                    string displayName;

                    if (string.IsNullOrEmpty(volumeLabel))
                        displayName = $"{driveInfo.DriveType} Disk ({driveLetterOnly})";
                    else if (!string.IsNullOrEmpty(volumeLabel))
                        displayName = $"{volumeLabel} ({driveLetterOnly})";
                    else
                        displayName = driveLetterOnly; // Or some other default if no label
                    displayName = displayName.Replace("Fixed", "Local");
                    long freeSpaceBytes = driveInfo.AvailableFreeSpace;
                    // Convert bytes to GB for display
                    double freeSpaceGB = Math.Round((double)freeSpaceBytes / (1024 * 1024 * 1024), 0);
                    double totalSizeGB = Math.Round((double)driveInfo.TotalSize / (1024 * 1024 * 1024), 0);

                    displayName += $" {freeSpaceGB} GB free of {totalSizeGB} GB";

                    BackupDriveSelection.Items.Add(displayName);
                }
            }
            foreach (string drive in BackupDriveSelection.Items)
                if (drive.Contains(Properties.Settings.Default.BackupDriveLetter))
                {
                    BackupDriveSelection.SelectedIndex = BackupDriveSelection.Items.IndexOf(drive);
                    ViewModel.BackupDrive = drives[BackupDriveSelection.SelectedIndex];
                }
        }

        private void BackupObjectsListView_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (BackupObjectsListView.SelectedItem is BackupObject selectedObject)
            {
                BackupObjectForm form = new(selectedObject);
                form.ShowDialog();
            }
        }

        private void BackupDriveSelection_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ViewModel.BackupDrive = drives[BackupDriveSelection.SelectedIndex];
            Properties.Settings.Default.BackupDriveLetter = ViewModel.BackupDrive.Name[..2];
            Properties.Settings.Default.Save();
        }
    }
}