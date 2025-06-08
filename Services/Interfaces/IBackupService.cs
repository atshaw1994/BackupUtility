using BackupUtility.Models;
using System.Collections.ObjectModel;

namespace BackupUtility.Services.Interfaces
{
    public interface IBackupService
    {
        Task PerformBackupAsync(string backupDriveRoot, ObservableCollection<BackupObject> backupObjects,
                                IProgress<int> progress, IProgress<string> status, CancellationToken cancellationToken);
        Task<List<string>> GetInstalledAppsAsync();
    }
}
