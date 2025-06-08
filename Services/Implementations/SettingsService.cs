using BackupUtility.Properties;
using BackupUtility.Services.Interfaces;
using System.Configuration;

namespace BackupUtility.Services.Implementations
{
    public class SettingsService : ISettingsService
    {
        private readonly ILogger _logger;

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
        public TimeSpan BackupTime
        {
            get => Settings.Default.BackupTime;
            set
            {
                if (Settings.Default.BackupTime != value) // Only save if value changed
                {
                    Settings.Default.BackupTime = value;
                    Settings.Default.Save();
                }
            }
        }
        // Implement other settings properties as needed

        public SettingsService(ILogger logger)
        {
            
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            try
            {
                // Get the exact path to the user.config file being used by this running application instance
                Configuration config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.PerUserRoamingAndLocal);
                _logger.Log($"Settings loaded from path: {config.FilePath}");

                // Check if settings need to be upgraded from a previous application version
                if (Settings.Default.IsUpgradeNeeded)
                {
                    Settings.Default.Upgrade(); // Migrates settings from the old version's config
                    Settings.Default.IsUpgradeNeeded = false; // Set flag to false so it doesn't run again
                    Settings.Default.Save(); // Save the updated flag immediately
                    _logger.Log("Settings upgraded from a previous version.");
                }
            }
            catch (Exception ex)
            {
                // Log any errors encountered during path retrieval or upgrade
                _logger.LogError($"Error during settings initialization: {ex.Message}", ex);
            }
            // --- END DIAGNOSTIC LOGGING & UPGRADE LOGIC ---

            Settings.Default.Reload();

            LoadSettings();
        }

        public bool LoadSettings()
        {
            try
            {
                // Load all settings from Settings.Default into the public properties
                Properties.Settings.Default.Reload();
                BackupTime = Settings.Default.BackupTime;

                if (BackupTime == TimeSpan.Zero)
                {
                    // If it's zero and hasn't been explicitly set (or loaded as zero), use default
                    BackupTime = new TimeSpan(6, 0, 0); // Set default to 6:00 AM
                }
                return true;
            }
            catch (Exception ex)
            {
                // Log error
                _logger.LogError($"Error loading application settings: {ex.Message}", ex);
                return false;
            }
        }

        public void SaveSettings()
        {
            try
            {
                // Save the current values of the public properties back to Settings.Default
                Settings.Default.BackupDriveLetter = BackupDriveLetter;
                Settings.Default.BackupTime = BackupTime;

                Settings.Default.Save(); // Save all settings to disk only once
            }
            catch (Exception ex)
            {
                // Log error
                _logger.LogError($"Error saving application settings: {ex.Message}", ex);
            }
        }
    }
}
