using BackupUtility.Models;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace BackupUtility.ViewModels
{
    public class BackupObjectFormViewModel : INotifyPropertyChanged
    {
        private BackupObject _currentBackupObject;
        private string _formattedSourcePath;
        private string _destinationPath;

        public event PropertyChangedEventHandler? PropertyChanged;

        public BackupObject CurrentBackupObject
        {
            get => _currentBackupObject;
            set
            {
                if (_currentBackupObject != value)
                {
                    _currentBackupObject = value;
                    OnPropertyChanged();
                    // Update UI bound properties when the underlying model changes
                    FormattedSourcePath = FormatPath(_currentBackupObject.Source);
                    DestinationPath = _currentBackupObject.Destination;
                }
            }
        }

        public string FormattedSourcePath
        {
            get => _formattedSourcePath;
            set
            {
                if (_formattedSourcePath != value)
                {
                    _formattedSourcePath = value;
                    OnPropertyChanged();
                }
            }
        }

        public string DestinationPath
        {
            get => _destinationPath;
            set
            {
                if (_destinationPath != value)
                {
                    _destinationPath = value;
                    _currentBackupObject.Destination = value; // Update model immediately on UI change
                    OnPropertyChanged();
                    (SaveCommand as RelayCommand)?.RaiseCanExecuteChanged(); // Re-evaluate Save button state
                }
            }
        }

        // Commands
        public ICommand SelectSourceCommand { get; private set; }
        public ICommand SaveCommand { get; private set; }
        public ICommand CloseCommand { get; private set; }

        // Events for the View to subscribe to for window actions
        public event Action? RequestClose;
        public event Action? RequestSaveAndClose;

        public BackupObjectFormViewModel()
        {
            _currentBackupObject = new BackupObject();
            _formattedSourcePath = "Source...";
            _destinationPath = string.Empty;

            SelectSourceCommand = new RelayCommand(ExecuteSelectSource);
            SaveCommand = new RelayCommand(ExecuteSave, CanExecuteSave);
            CloseCommand = new RelayCommand(ExecuteClose);
        }

        public BackupObjectFormViewModel(BackupObject backupObject)
        {
            _currentBackupObject = backupObject ?? new BackupObject();
            _formattedSourcePath = FormatPath(_currentBackupObject.Source);
            _destinationPath = _currentBackupObject.Destination;

            SelectSourceCommand = new RelayCommand(ExecuteSelectSource);
            SaveCommand = new RelayCommand(ExecuteSave, CanExecuteSave);
            CloseCommand = new RelayCommand(ExecuteClose);
        }

        private void ExecuteSelectSource(object? parameter)
        {
            // TODO: Stop using OpenFileDialog to better align with WPF

            OpenFileDialog openFileDialog = new()
            {
                ValidateNames = false,
                CheckFileExists = false,
                CheckPathExists = true,
                FileName = "Folder Selection.", // Trick to make it select folders
                Filter = "Folders|*.fake", // Provide a dummy filter
                Title = "Select Source Folder"
            };

            bool? result = openFileDialog.ShowDialog();

            if (result == true)
            {
                string selectedPath = Path.GetDirectoryName(openFileDialog.FileName)!;
                if (!string.IsNullOrEmpty(selectedPath))
                {
                    _currentBackupObject.Source = selectedPath;
                    FormattedSourcePath = FormatPath(selectedPath);
                    (SaveCommand as RelayCommand)?.RaiseCanExecuteChanged();
                }
            }
        }

        private void ExecuteSave(object? parameter) =>
            // _currentBackupObject.Destination is already updated by the TwoWay binding on DestinationPath
            // Now, signal to the View to close and indicate success.
            RequestSaveAndClose?.Invoke();

        private bool CanExecuteSave(object? parameter) =>
            // Ensure both source and destination are provided
            !string.IsNullOrEmpty(_currentBackupObject.Source) && !string.IsNullOrEmpty(DestinationPath);

        private void ExecuteClose(object? parameter) => RequestClose?.Invoke();

        private static string FormatPath(string path)
        {
            if (string.IsNullOrEmpty(path)) return "Source...";

            string[] parts = path.Split(Path.DirectorySeparatorChar, StringSplitOptions.RemoveEmptyEntries);

            if (parts.Length <= 3)
                return path;

            string firstPart = parts[0];
            string lastPart = parts[^1];

            return $"/{firstPart}/.../{lastPart}/";
        }

        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
