using System.Globalization;
using System.Management;
using System.Security.Principal;
using Microsoft.Win32;

namespace BackupUtility.Views.Themes
{
    public static class ThemeManager
    {
        private const string RegistryKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize";
        private const string RegistryValueName = "AppsUseLightTheme";
        public static WindowsTheme CurrentTheme => _currentTheme;
        public enum WindowsTheme { Light, Dark }
        public static event EventHandler<ThemeChangedEventArgs>? ThemeChanged;
        private static WindowsTheme _currentTheme;

        static ThemeManager()
        {
            _currentTheme = GetWindowsTheme();
            WatchTheme();
        }

        private static void WatchTheme()
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
                    if (newWindowsTheme != _currentTheme)
                    {
                        _currentTheme = newWindowsTheme;
                        OnThemeChanged(new ThemeChangedEventArgs(newWindowsTheme));
                    }
                };

                // Start listening for events
                watcher.Start();
            }
            catch (Exception)
            {
                // This can fail on Windows 7
            }
        }

        private static WindowsTheme GetWindowsTheme()
        {
            using RegistryKey key = Registry.CurrentUser.OpenSubKey(RegistryKeyPath)!;
            object registryValueObject = key?.GetValue(RegistryValueName)!;
            if (registryValueObject == null)
            {
                return WindowsTheme.Light;
            }

            int registryValue = (int)registryValueObject;

            return registryValue > 0 ? WindowsTheme.Light : WindowsTheme.Dark;
        }

        private static void OnThemeChanged(ThemeChangedEventArgs e)
        {
            ThemeChanged?.Invoke(null, e);
        }
    }

    public class ThemeChangedEventArgs(ThemeManager.WindowsTheme newTheme) : EventArgs
    {
        public ThemeManager.WindowsTheme NewTheme { get; } = newTheme;
    }

}
