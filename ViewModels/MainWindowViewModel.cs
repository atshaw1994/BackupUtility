using BackupUtility.Commands;
using BackupUtility.Models;
using BackupUtility.Services.Interfaces;
using BackupUtility.Views;
using System.Collections.ObjectModel;
using System.IO;
using System.Windows.Input;
using System.Windows.Threading;

namespace BackupUtility.ViewModels
{
    public class MainWindowViewModel : BaseViewModel
    {
        private ObservableCollection<BackupObject> _backupObjects;
        public ObservableCollection<BackupObject> BackupObjects
        {
            get => _backupObjects;
            set => SetProperty(ref _backupObjects, value);
        }

        private BackupSchedule _globalBackupSchedule;
        public BackupSchedule GlobalBackupSchedule
        {
            get => _globalBackupSchedule;
            set => SetProperty(ref _globalBackupSchedule, value);
        }

        private DriveInfo? _backupDrive;
        public DriveInfo? BackupDrive
        {
            get => _backupDrive;
            set
            {
                if (SetProperty(ref _backupDrive, value))
                {
                    _settingsService.BackupDriveLetter = _backupDrive?.Name[..2] ?? string.Empty;
                    if (!_isInitializing)
                    {
                        _settingsService.SaveSettings(); // Only save when user interacts
                        _logger.Log($"Backup drive changed by user. Saved: {_settingsService.BackupDriveLetter}");
                    }
                    else
                    {
                        _logger.Log($"Backup drive set programmatically during initialization. Value: {_settingsService.BackupDriveLetter}");
                    }
                }
            }
        }

        private TimeSpan _backupTime;
        public TimeSpan BackupTime
        {
            get => _backupTime;
            set
            {
                if (SetProperty(ref _backupTime, value))
                {
                    _settingsService.BackupTime = _backupTime;
                    if (!_isInitializing)
                    {
                        _settingsService.SaveSettings(); // Only save when user interacts
                        _logger.Log($"Backup time updated by user. Saved: {value:hh\\:mm}");
                    }
                    else
                    {
                        _logger.Log($"Backup time set programmatically during initialization. Value: {value:hh\\:mm}");
                    }
                }
            }
        }

        private ObservableCollection<DriveInfo> _drivesList;
        public ObservableCollection<DriveInfo> DrivesList
        {
            get => _drivesList;
            set => SetProperty(ref _drivesList, value);
        }

        private string _statusMessage;
        public string StatusMessage
        {
            get => _statusMessage;
            set => SetProperty(ref _statusMessage, value);
        }

        private bool _isBackupInProgress;
        public bool IsBackupInProgress
        {
            get => _isBackupInProgress;
            set
            {
                SetProperty(ref _isBackupInProgress, value);
                ((RelayCommand)StartBackupCommand).RaiseCanExecuteChanged();
                ((RelayCommand)CancelBackupCommand).RaiseCanExecuteChanged();
            }
        }

        private int _backupProgress;
        public int BackupProgress
        {
            get => _backupProgress;
            set => SetProperty(ref _backupProgress, value);
        }

        public bool IsIdle => !_isBackupInProgress;

        // --- Commands ---
        public ICommand StartBackupCommand { get; }
        public ICommand AddSourceCommand { get; }
        public ICommand EditSourceCommand { get; }
        public ICommand RemoveSourceCommand { get; }
        public ICommand CancelBackupCommand { get; }
        public ICommand EditBackupTimeCommand { get; }
        public ICommand PopulateDrivesCommand { get; }

        // --- Private fields for injected services ---
        private readonly IBackupService _backupService;
        private readonly ISettingsService _settingsService;
        private readonly IDriveService _driveService;
        private readonly IBackupObjectStorageService _backupObjectStorageService;
        private readonly IDialogService _dialogService;
        private readonly ILogger _logger;

        // --- Other private fields ---
        private CancellationTokenSource? _backupCancellationTokenSource;
        private readonly DispatcherTimer _backupScheduleTimer;

        private bool _isInitializing;

        public MainWindowViewModel(
            IBackupService backupService,
            ISettingsService settingsService,
            IDriveService driveService,
            IBackupObjectStorageService backupObjectStorageService,
            IDialogService dialogService,
            ILogger logger)
        {
            // Assign injected services to private fields
            _backupService = backupService ?? throw new ArgumentNullException(nameof(backupService));
            _settingsService = settingsService ?? throw new ArgumentNullException(nameof(settingsService));
            _driveService = driveService ?? throw new ArgumentNullException(nameof(driveService));
            _backupObjectStorageService = backupObjectStorageService ?? throw new ArgumentNullException(nameof(backupObjectStorageService));
            _dialogService = dialogService ?? throw new ArgumentNullException(nameof(dialogService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            // Initialize Commands
            StartBackupCommand = new RelayCommand(ExecuteStartBackupAsync, CanStartBackup);
            CancelBackupCommand = new RelayCommand(CancelBackup, (parameter) => IsBackupInProgress);
            AddSourceCommand = new RelayCommand(ExecuteAddBackupObjectAsync);
            EditSourceCommand = new RelayCommand(ExecuteEditBackupObjectAsync);
            RemoveSourceCommand = new RelayCommand(ExecuteRemoveBackupObjectAsync);
            EditBackupTimeCommand = new AsyncRelayCommand(ExecuteEditBackupTimeAsync);
            PopulateDrivesCommand = new RelayCommand(ExecutePopulateDrivesAsync);

            // Initialize Collections (always do this before using them)
            BackupObjects = [];
            _drivesList = [];

            // Initialize global schedule (load from persistence or default)
            GlobalBackupSchedule = new BackupSchedule();
            LoadGlobalSchedule();

            // Initialize other simple properties
            _statusMessage = "";
            IsBackupInProgress = false;
            _backupProgress = 0;

            // Initialize and Start the Scheduler Timer
            _backupScheduleTimer = new() { Interval = TimeSpan.FromSeconds(1) };
            _backupScheduleTimer.Tick += BackupScheduleTimer_Tick;
            _backupScheduleTimer.Start();

            // Perform initial async loading
            InitializeDataAsync();
        }

        private async void InitializeDataAsync()
        {
            _isInitializing = true;

            try
            {
                _settingsService.LoadSettings();

                string driveLetterForLogs = _settingsService.BackupDriveLetter;

                // Add a fallback if, for some reason, the drive letter is still empty (e.g., first run, no settings saved yet)
                if (string.IsNullOrEmpty(driveLetterForLogs))
                {
                    driveLetterForLogs = AppDomain.CurrentDomain.BaseDirectory;
                    _logger.LogWarning("Backup drive not set in settings. Using application base directory for logs.");
                }

                // Construct the full log file path.
                string logDirectory = Path.Combine(driveLetterForLogs, "Logs");
                Directory.CreateDirectory(logDirectory);
                string logFileName = $"Log_{DateTime.Now:yyyyMMdd_HHmmss}.txt";
                string fullLogFilePath = Path.Combine(logDirectory, logFileName);

                // Call your Logger's SetLogFile method with the now correct path
                _logger.SetLogFile(fullLogFilePath);
                _logger.Log($"Logger initialized. Log file: {fullLogFilePath}"); // Log a confirmation message

                await ExecutePopulateDrivesAsync(); // Populate drives first
                await LoadBackupObjectsAsync();     // Then load backup objects

                // Restore previously selected drive based on settings after drives are loaded
                string savedDriveLetter = _settingsService.BackupDriveLetter;
                if (!string.IsNullOrEmpty(savedDriveLetter))
                {
                    DriveInfo? previouslySelected = DrivesList.FirstOrDefault(
                        d => d.Name.StartsWith(savedDriveLetter, StringComparison.OrdinalIgnoreCase));

                    if (previouslySelected != null)
                        BackupDrive = previouslySelected;
                    else if (DrivesList.Any())
                        BackupDrive = DrivesList.First();
                }
                else if (DrivesList.Any())
                    BackupDrive = DrivesList.First();
            }
            finally
            {
                _isInitializing = false; // <--- ENSURE FLAG IS RESET, even if errors occur during initialization
                _logger.Log("MainWindowViewModel initialization complete. Saving enabled for user interactions.");
            }
        }

        private void BackupScheduleTimer_Tick(object? sender, EventArgs e)
        {
            DateTime now = DateTime.Now;

            foreach (BackupObject backupObject in BackupObjects)
            {
                if (backupObject.UsesCustomSchedule)
                {
                    bool isTimeReached = now.TimeOfDay >= backupObject.CustomSchedule.BackupTime;
                    bool isTodayEnabled = backupObject.CustomSchedule.IsTodayEnabled();

                    bool hasBackupBeenPerformedToday = backupObject.LastBackupTime.HasValue &&
                                                       backupObject.LastBackupTime.Value.Date == now.Date &&
                                                       backupObject.LastBackupTime.Value.TimeOfDay >= backupObject.CustomSchedule.BackupTime;

                    if (isTimeReached && isTodayEnabled && !hasBackupBeenPerformedToday && !IsBackupInProgress)
                    {
                        // Perform backup on BackupObject
                    }

                }
                else
                {
                    bool isTimeReached = now.TimeOfDay >= GlobalBackupSchedule.BackupTime;
                    bool isTodayEnabled = GlobalBackupSchedule.IsTodayEnabled();

                    // Check if backup has not been performed TODAY for this object
                    bool hasBackupBeenPerformedToday = backupObject.LastBackupTime.HasValue &&
                                                      backupObject.LastBackupTime.Value.Date == now.Date &&
                                                      backupObject.LastBackupTime.Value.TimeOfDay >= GlobalBackupSchedule.BackupTime;

                    if (isTimeReached && isTodayEnabled && !hasBackupBeenPerformedToday && !IsBackupInProgress)
                    {
                        // Perform backup on BackupObject
                    }
                }
            }
        }

        private async Task LoadBackupObjectsAsync()
        {
            var loadedBackupItems = await _backupObjectStorageService.LoadBackupObjectsAsync();
            // Ensure collection is updated on the UI thread if not already on it
            System.Windows.Application.Current.Dispatcher.Invoke(() =>
            {
                BackupObjects.Clear();
                foreach (BackupObject backupObject in loadedBackupItems)
                    BackupObjects.Add(backupObject);
                if (BackupObjects.Count > 0)
                    BackupObjects[0].IsFirst = true;
            });
        }

        private void LoadGlobalSchedule()
        {
            // TODO: Implement loading GlobalBackupSchedule from a specific settings file
            // Example:
            // GlobalBackupSchedule = _settingsService.LoadGlobalSchedule();
            // For testing:
            GlobalBackupSchedule.BackupTime = new(6,0,0);
            GlobalBackupSchedule.MondayEnabled = true;
            GlobalBackupSchedule.TuesdayEnabled = true;
            GlobalBackupSchedule.WednesdayEnabled = true;
            GlobalBackupSchedule.ThursdayEnabled = true;
            GlobalBackupSchedule.FridayEnabled = true;
            GlobalBackupSchedule.SaturdayEnabled = false;
            GlobalBackupSchedule.SundayEnabled = false;
        }

        private void CancelBackup(object? parameter = null!) => _backupCancellationTokenSource?.Cancel();

        private bool CanStartBackup(object? parameter) => !IsBackupInProgress && BackupDrive is not null;

        private async Task ExecuteEditBackupTimeAsync(object? parameter = null!)
        {
            var timeInputViewModel = new BackupTimeWindowViewModel(BackupTime); // Pass current time

            var timeInputWindow = new BackupTimeWindow { DataContext = timeInputViewModel };

            bool? dialogResult = timeInputWindow.ShowDialog();

            if (dialogResult == true)
            {
                // User clicked OK and a new time was selected
                // Update BackupTime with the new time component
                BackupTime = timeInputViewModel.SelectedTime;
            }
        }

        private async void ExecuteStartBackupAsync(object? parameter = null!)
        {
            // Use _logger directly or rely on BackupService's internal logging
            StatusMessage = "[START] Backup started...";
            _logger.Log("Backup process initiated.");
            IsBackupInProgress = true;
            BackupProgress = 0;

            _backupCancellationTokenSource = new CancellationTokenSource();
            CancellationToken cancellationToken = _backupCancellationTokenSource.Token;

            // Create IProgress instances for the service to report back to the ViewModel
            var progressHandler = new Progress<int>(p => BackupProgress = p);
            var statusHandler = new Progress<string>(s => StatusMessage = s);

            try
            {
                if (BackupDrive?.RootDirectory == null)
                {
                    StatusMessage = "[ERR] Backup drive not selected or invalid.";
                    _logger.LogError("Backup failed: No valid backup drive selected.");
                    return;
                }

                await _backupService.PerformBackupAsync(
                    BackupDrive.RootDirectory.FullName,
                    BackupObjects,
                    progressHandler, // Pass progress reporter
                    statusHandler,   // Pass status reporter
                    cancellationToken);
            }
            catch (OperationCanceledException)
            {
                StatusMessage = "[STOP] Backup cancelled.";
                _logger.LogWarning("Backup process cancelled by user.");
                BackupProgress = 0;
            }
            catch (Exception ex)
            {
                StatusMessage = $"[ERR] Backup failed: {ex.Message}";
                _logger.LogError($"Backup failed with an error: {ex.Message}", ex);
                BackupProgress = 0;
            }
            finally
            {
                IsBackupInProgress = false;
                _backupCancellationTokenSource?.Dispose();
                _backupCancellationTokenSource = null;
                StatusMessage = "[END] Backup process concluded.";
            }
        }

        private async void ExecuteAddBackupObjectAsync(object? parameter = null)
        {
            BackupObject? backupObjectResult = await _dialogService.ShowAddBackupObjectDialogAsync(BackupObjects);
            if (backupObjectResult != null)
            {
                BackupObjects.Add(backupObjectResult);
                await _backupObjectStorageService.SaveBackupObjectsAsync([.. BackupObjects]);
                if (BackupObjects.Count > 0) BackupObjects[0].IsFirst = true;
                _logger.Log($"Added new backup object: {backupObjectResult.Source}");
            }
        }

        private async void ExecuteEditBackupObjectAsync(object? parameter = null!)
        {
            if (parameter is BackupObject backupObjectToEdit)
            {
                BackupObject? updatedObject = await _dialogService.ShowEditBackupObjectDialogAsync(backupObjectToEdit, BackupObjects);
                if (updatedObject != null)
                {
                    // Find the original object and update its properties, or replace it if it's a new instance
                    int index = BackupObjects.IndexOf(backupObjectToEdit);
                    if (index != -1)
                    {
                        // Assuming BackupObject has properties to update or can be replaced
                        BackupObjects[index] = updatedObject; // Replace the object
                    }
                    await _backupObjectStorageService.SaveBackupObjectsAsync([.. BackupObjects]);
                    _logger.Log($"Edited backup object: {updatedObject.Source}");
                }
            }
        }

        private async void ExecuteRemoveBackupObjectAsync(object? parameter = null!)
        {
            if (parameter is BackupObject bo_parameter)
            {
                BackupObjects.Remove(bo_parameter);
                await _backupObjectStorageService.SaveBackupObjectsAsync([.. BackupObjects]);
                _logger.Log($"Removed backup object: {bo_parameter.Source}");
                if (BackupObjects.Count > 0) BackupObjects[0].IsFirst = true;
            }
        }

        private async Task ExecutePopulateDrivesAsync(object? parameter = null!)
        {
            var drives = await _driveService.GetLogicalDrivesAsync();
            System.Windows.Application.Current.Dispatcher.Invoke(() =>
            {
                DrivesList.Clear();
                foreach (var drive in drives)
                    DrivesList.Add(drive);
            });
        }

    }
}
