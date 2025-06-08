using BackupUtility.Models;

namespace BackupUtility.Services.Interfaces
{
    public interface IBackupObjectStorageService
    {
        Task<List<BackupObject>> LoadBackupObjectsAsync();
        Task SaveBackupObjectsAsync(List<BackupObject> backupObjects);
    }
}
