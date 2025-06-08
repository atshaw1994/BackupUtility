namespace BackupUtility.Services.Interfaces
{
    public interface ISettingsService
    {
        string BackupDriveLetter { get; set; }
        TimeSpan BackupTime { get; set; }

        bool LoadSettings();
        void SaveSettings();
    }
}
