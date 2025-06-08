using BackupUtility.Properties;
using BackupUtility.Services.Interfaces;

namespace BackupUtility.Services.Implementations
{
    public class SettingsService : ISettingsService
    {
        public string BackupDriveLetter
        {
            get => Settings.Default.BackupDriveLetter;
            set
            {
                if (Settings.Default.BackupDriveLetter != value) // Only save if value changed
                {
                    Settings.Default.BackupDriveLetter = value;
                    Settings.Default.Save();
                }
            }
        }
        // Implement other settings properties as needed
    }
}
