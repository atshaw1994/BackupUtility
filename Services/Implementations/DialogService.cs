using BackupUtility.Models;
using BackupUtility.Services.Interfaces;
using BackupUtility.Views;
using System.Collections.ObjectModel;
using System.IO;
using System.Windows;

namespace BackupUtility.Services.Implementations
{
    public class DialogService : IDialogService
    {
        public Task<BackupObject?> ShowAddBackupObjectDialogAsync(ObservableCollection<BackupObject> existingObjects)
        {
            return Task.Run(() =>
            {
                BackupObject? resultObject = null;
                // Ensure UI operations are on the UI thread
                Application.Current.Dispatcher.Invoke(() =>
                {
                    // Assuming BackupObjectForm is the name of your dialog window
                    BackupObjectForm backupForm = new(existingObjects); // Pass existing objects if needed for validation
                    if (backupForm.ShowDialog() == true)
                    {
                        resultObject = backupForm.BackupObjectResult;
                    }
                });
                return resultObject;
            });
        }

        public Task<BackupObject?> ShowEditBackupObjectDialogAsync(BackupObject objectToEdit, ObservableCollection<BackupObject> existingObjects)
        {
            return Task.Run(() =>
            {
                BackupObject? resultObject = null;
                // Ensure UI operations are on the UI thread
                Application.Current.Dispatcher.Invoke(() =>
                {
                    BackupObjectForm backupForm = new(objectToEdit, existingObjects); // Pass object to edit
                    if (backupForm.ShowDialog() == true)
                    {
                        resultObject = backupForm.BackupObjectResult;
                    }
                });
                return resultObject;
            });
        }

        public Task<string?> ShowFolderPickerAsync(string initialPath = "")
        {
            return Task.Run(() =>
            {
                string? selectedPath = null;
                Application.Current.Dispatcher.Invoke(() =>
                {
                    var dialog = new System.Windows.Forms.FolderBrowserDialog(); // Requires System.Windows.Forms
                    if (!string.IsNullOrEmpty(initialPath) && Directory.Exists(initialPath))
                    {
                        dialog.SelectedPath = initialPath;
                    }

                    if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                    {
                        selectedPath = dialog.SelectedPath;
                    }
                });
                return selectedPath;
            });
        }

        public Task ShowMessageAsync(string message, string title, IDialogService.MessageBoxType type = IDialogService.MessageBoxType.Information)
        {
            return Task.Run(() =>
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    MessageBoxImage icon = MessageBoxImage.None;
                    switch (type)
                    {
                        case IDialogService.MessageBoxType.Information: icon = MessageBoxImage.Information; break;
                        case IDialogService.MessageBoxType.Warning: icon = MessageBoxImage.Warning; break;
                        case IDialogService.MessageBoxType.Error: icon = MessageBoxImage.Error; break;
                        case IDialogService.MessageBoxType.Confirmation: icon = MessageBoxImage.Question; break;
                    }
                    MessageBox.Show(message, title, MessageBoxButton.OK, icon);
                });
            });
        }
    }
}
