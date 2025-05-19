using Microsoft.Win32;
using System.Collections.ObjectModel;

namespace BackupUtility
{
    public static class InstalledAppsFromRegistry
    {
        public static ObservableCollection<string> GetInstalledApps()
        {
            var displayNames = new ObservableCollection<string>();

            // Get display names from HKLM
            GetDisplayNamesFromRegistry(RegistryHive.LocalMachine, @"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall", displayNames);
            GetDisplayNamesFromRegistry(RegistryHive.LocalMachine, @"SOFTWARE\Wow6432Node\Microsoft\Windows\CurrentVersion\Uninstall", displayNames);

            // Get display names installed per user from HKCU
            GetDisplayNamesFromRegistry(RegistryHive.CurrentUser, @"Software\Microsoft\Windows\CurrentVersion\Uninstall", displayNames);

            // Process and filter the display names
            return new ObservableCollection<string>(displayNames
                .Where(dn => !string.IsNullOrEmpty(dn?.Trim())) // Trim and filter out null or empty
                .Select(dn => dn!.Trim()) // Now it's safe to dereference with ! as we've filtered
                .Distinct(StringComparer.InvariantCultureIgnoreCase)
                .OrderBy(dn => dn));
        }

        private static void GetDisplayNamesFromRegistry(RegistryHive hive, string subKeyPath, ObservableCollection<string> displayNames)
        {
            using RegistryKey? baseKey = RegistryKey.OpenBaseKey(hive, RegistryView.Default);
            using RegistryKey? uninstallKey = baseKey.OpenSubKey(subKeyPath);
            if (uninstallKey != null)
            {
                foreach (string subkeyName in uninstallKey.GetSubKeyNames())
                {
                    using RegistryKey? appKey = uninstallKey.OpenSubKey(subkeyName);
                    if (appKey != null && appKey.GetValue("DisplayName") is string displayName)
                        displayNames.Add(displayName);
                }
            }
        }
    }
}
