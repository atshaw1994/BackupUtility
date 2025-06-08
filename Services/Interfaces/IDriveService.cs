using System.Collections.ObjectModel;
using System.IO;

namespace BackupUtility.Services.Interfaces
{
    public interface IDriveService
    {
        Task<ObservableCollection<DriveInfo>> GetLogicalDrivesAsync();
    }
}
