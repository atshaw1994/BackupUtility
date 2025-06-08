using BackupUtility.Commands;
using BackupUtility.Models;
using Microsoft.Win32;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Windows.Forms;
using System.Windows.Input;

namespace BackupUtility.ViewModels
{
    public class BackupObjectFormViewModel : INotifyPropertyChanged
    {
        // Fields
        private BackupObject _currentBackupObject;
        private string _formattedSourcePath;
        private string _formattedDestinationPath;
        private string _destinationPath;

        // Properties
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
                    _currentBackupObject.Destination = value;
                    OnPropertyChanged();
                    (SaveCommand as RelayCommand)?.RaiseCanExecuteChanged();
                }
            }
        }
        public string FormattedDestinationPath
        {
            get => _formattedDestinationPath;
            set
            {
                if (_formattedDestinationPath != value)
                {
                    _formattedDestinationPath = value;
                    OnPropertyChanged();
                }
            }
        }
        public event PropertyChangedEventHandler? PropertyChanged;

        // Commands
        public ICommand SelectSourceCommand { get; private set; }
        public ICommand SelectDestinationCommand { get; private set; }
        public ICommand SaveCommand { get; private set; }
        public ICommand CloseCommand { get; private set; }

        // Events
        public event Action? RequestClose;
        public event Action? RequestSaveAndClose;

        public BackupObjectFormViewModel()
        {
            _currentBackupObject = new BackupObject();
            _formattedSourcePath = "Source...";
            _formattedDestinationPath = "Destination...";
            _destinationPath = string.Empty;

            SelectSourceCommand = new RelayCommand(ExecuteSelectSource);
            SelectDestinationCommand = new RelayCommand(ExecuteSelectDestination);
            SaveCommand = new RelayCommand(ExecuteSave, CanExecuteSave);
            CloseCommand = new RelayCommand(ExecuteClose);
        }

        public BackupObjectFormViewModel(BackupObject backupObject)
        {
            _currentBackupObject = backupObject ?? new BackupObject();
            _formattedSourcePath = FormatPath(_currentBackupObject.Source);
            _formattedDestinationPath = FormatPath(_currentBackupObject.Destination);
            _destinationPath = _currentBackupObject.Destination;

            SelectSourceCommand = new RelayCommand(ExecuteSelectSource);
            SelectDestinationCommand = new RelayCommand(ExecuteSelectDestination);
            SaveCommand = new RelayCommand(ExecuteSave, CanExecuteSave);
            CloseCommand = new RelayCommand(ExecuteClose);
        }

        private void ExecuteSelectSource(object? parameter)
        {
            // TODO: Stop using OpenFileDialog to better align with WPF

            OpenFolderDialog openFileDialog = new()
            {
                ValidateNames = false,
                Multiselect = false,
                Title = "Select Source Folder"
            };

            bool? result = openFileDialog.ShowDialog();

            if (result == true)
            {
                if (!string.IsNullOrEmpty(openFileDialog.FolderName))
                {
                    _currentBackupObject.Source = openFileDialog.FolderName;
                    FormattedSourcePath = FormatPath(openFileDialog.FolderName);
                    (SaveCommand as RelayCommand)?.RaiseCanExecuteChanged();
                }
            }
        }

        private void ExecuteSelectDestination(object? parameter)
        {
            OpenFolderDialog openFileDialog = new()
            {
                ValidateNames = false,
                Multiselect = false,
                Title = "Select Source Folder"
            };

            bool? result = openFileDialog.ShowDialog();

            if (result == true)
            {
                if (!string.IsNullOrEmpty(openFileDialog.FolderName))
                {
                    _currentBackupObject.Destination = openFileDialog.FolderName;
                    FormattedDestinationPath = FormatPath(openFileDialog.FolderName);
                    (SaveCommand as RelayCommand)?.RaiseCanExecuteChanged();
                }
            }
        }

        private void ExecuteSave(object? parameter) =>
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

            return $"{firstPart}\\...\\{lastPart}\\";
        }

        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null) 
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
