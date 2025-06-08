using BackupUtility.Models;
using System.Collections.ObjectModel;

namespace BackupUtility.Services.Interfaces
{
    public interface IDialogService
    {
        Task<BackupObject?> ShowAddBackupObjectDialogAsync(ObservableCollection<BackupObject> existingObjects);
        Task<BackupObject?> ShowEditBackupObjectDialogAsync(BackupObject objectToEdit, ObservableCollection<BackupObject> existingObjects);
        Task<string?> ShowFolderPickerAsync(string initialPath = "");
        Task ShowMessageAsync(string message, string title, MessageBoxType type = MessageBoxType.Information);

        public enum MessageBoxType
        {
            Information,
            Warning,
            Error,
            Confirmation
        }
    }
}
