using BackupUtility.Services.Interfaces;
using System.Collections.ObjectModel;
using System.IO;
using System.Windows;

namespace BackupUtility.Services.Implementations
{
    public class DriveService : IDriveService
    {
        public Task<ObservableCollection<DriveInfo>> GetLogicalDrivesAsync()
        {
            return Task.Run(() =>
            {
                ObservableCollection<DriveInfo> drives = [];
                foreach (string driveletter in Directory.GetLogicalDrives())
                {
                    DriveInfo driveInfo = new(driveletter);
                    if (driveInfo.IsReady)
                    {
                        // Add to collection on the UI thread to avoid cross-thread issues
                        // when the ViewModel's DrivesList is updated.
                        Application.Current.Dispatcher.Invoke(() => drives.Add(driveInfo));
                    }
                }
                return drives;
            });
        }
    }
}
