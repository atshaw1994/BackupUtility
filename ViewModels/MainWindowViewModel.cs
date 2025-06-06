using BackupUtility.Models;
using BackupUtility.Views;
using System.Collections.ObjectModel;
using System.IO;
using System.Windows.Input;
using System.Windows.Threading;

namespace BackupUtility.ViewModels
{
    public class MainWindowViewModel : BaseViewModel
    {
        public ObservableCollection<BackupObject> BackupObjects { get; set; }
        public DriveInfo _BackupDrive;
        public DriveInfo BackupDrive
        {
            get => _BackupDrive;
            set
            {
                if (SetProperty(ref _BackupDrive, value))
                {
                    if (_BackupDrive != null)
                    {
                        Properties.Settings.Default.BackupDriveLetter = _BackupDrive.Name[..2];
                        Properties.Settings.Default.Save();
                    }
                    else
                    {
                        Properties.Settings.Default.BackupDriveLetter = string.Empty;
                        Properties.Settings.Default.Save();
                    }
                }
            }
        }
        private ObservableCollection<DriveInfo> _DrivesList;
        public ObservableCollection<DriveInfo> DrivesList
        {
            get => _DrivesList;
            set => SetProperty(ref _DrivesList, value);
        }
        private string _statusMessage;
        public string StatusMessage
        {
            get => _statusMessage;
            set
            {
                SetProperty(ref _statusMessage, value);
                Logs.Add($"{DateTime.Now:yyyy-MM-dd HH:mm:ss}: {value}\n");
            }
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

        private readonly List<string> Logs = [];
        private CancellationTokenSource? _backupCancellationTokenSource;
        private readonly DispatcherTimer _backupSchedulerTimer;

        public ICommand StartBackupCommand { get; }
        public ICommand AddSourceCommand { get; }
        public ICommand EditSourceCommand { get; }
        public ICommand RemoveSourceCommand { get; }
        public ICommand CancelBackupCommand { get; }
        public ICommand PopulateDrivesCommand { get; }

        public MainWindowViewModel()
        {
            StartBackupCommand = new RelayCommand(PerformBackupAsync, CanStartBackup);
            CancelBackupCommand = new RelayCommand(CancelBackup, (parameter) => IsBackupInProgress);
            AddSourceCommand = new RelayCommand(AddBackupObjectAsync);
            EditSourceCommand = new RelayCommand(EditBackupObjectAsync);
            RemoveSourceCommand = new RelayCommand(RemoveBackupObjecAsync);
            PopulateDrivesCommand = new RelayCommand(PopulateDrivesAsync);
            _DrivesList = [];
            PopulateDrivesAsync();
            BackupObjects = [];
            Task.Run(LoadBackupObjectsAsync).Wait();
            _BackupDrive = new("C:");
            _statusMessage = "";
            IsBackupInProgress = false;
            _backupProgress = 0;
            _backupSchedulerTimer = new() { Interval = TimeSpan.FromSeconds(1) };
            _backupSchedulerTimer.Tick += BackupSchedulerTimer_Tick;
            _backupSchedulerTimer.Start();
        }

        private void BackupSchedulerTimer_Tick(object? sender, EventArgs e)
        {
            if (DateTime.Now.TimeOfDay == new TimeSpan(6, 0, 0))
                if (StartBackupCommand != null && StartBackupCommand.CanExecute(null))
                    StartBackupCommand.Execute(null);
        }

        private async void LoadBackupObjectsAsync()
        {
            string loadFilePath = "backup_objects.json";
            List<BackupObject> loadedBackupItems = await BackupObjectSerializer.DeserializeListFromFileAsync(loadFilePath);
            foreach (BackupObject backupObject in loadedBackupItems)
                BackupObjects.Add(backupObject);
            _backupProgress = 0;
            if (BackupObjects.Count > 0)
                BackupObjects[0].IsFirst = true;
        }

        private void CancelBackup(object? parameter = null!) => _backupCancellationTokenSource?.Cancel();

        private bool CanStartBackup(object? parameter) => !IsBackupInProgress && BackupDrive is not null;

        private async void PerformBackupAsync(object? parameter = null!)
        {
            if (!Directory.Exists($"{BackupDrive.RootDirectory}\\Logs\\"))
                Directory.CreateDirectory($"{BackupDrive.RootDirectory}\\Logs\\");
            using (File.Create($"{BackupDrive.RootDirectory}\\Logs\\Log_{DateTime.Now:yyyyMMdd}.txt")) { }
            using (File.Create($"{BackupDrive.RootDirectory}\\AppsList.txt")) { }
            StatusMessage = "[START] Backup started...";
            IsBackupInProgress = true;
            BackupProgress = 0;

            _backupCancellationTokenSource = new CancellationTokenSource();
            CancellationToken cancellationToken = _backupCancellationTokenSource.Token;

            try
            {
                await Task.Run(() =>
                {
                    string backupDestination = $"{BackupDrive.RootDirectory}\\Backup_{DateTime.Now:yyyyMMdd}";
                    Directory.CreateDirectory(backupDestination);
                    long totalFiles = BackupObjects.Sum(backupObject => Directory.GetFiles(backupObject.Source, "*.*", SearchOption.AllDirectories).Length);
                    long processedFiles = 0;

                    foreach (BackupObject backupObject in BackupObjects)
                    {
                        if (cancellationToken.IsCancellationRequested)
                        {
                            StatusMessage = "[STOP] Backup cancelled.";
                            return;
                        }

                        if (Directory.Exists(backupObject.Source))
                        {
                            // Calculate currentBackupObjectFiles once for this object
                            long currentBackupObjectFiles = Directory.GetFiles(backupObject.Source, "*.*", SearchOption.AllDirectories).Length;

                            string finalDestPathForSource = Path.Combine(backupDestination, backupObject.Destination, new DirectoryInfo(backupObject.Source).Name);

                            CopyChanges(backupObject.Source,
                                finalDestPathForSource,
                                new Progress<int>(p =>
                                {
                                    if (totalFiles > 0) BackupProgress = (int)((double)processedFiles / totalFiles * 100);
                                }),
                                ref processedFiles,
                                currentBackupObjectFiles,
                                cancellationToken: cancellationToken);
                        }
                        else
                        {
                            StatusMessage = $"[ERR] Source folder '{backupObject.Source}' does not exist.";
                        }
                    }

                    if (!cancellationToken.IsCancellationRequested)
                    {
                        StatusMessage = "[END] Backup completed successfully.";
                        BackupProgress = 100;
                    }
                }, cancellationToken);
            }
            catch (OperationCanceledException)
            {
                StatusMessage = "[STOP] Backup cancelled.";
                BackupProgress = 0;
            }
            catch (Exception ex)
            {
                StatusMessage = $"[ERR] Backup failed: {ex.Message}";
                BackupProgress = 0;
            }
            finally
            {
                IsBackupInProgress = false;
                _backupCancellationTokenSource?.Dispose();
                _backupCancellationTokenSource = null;

                string logs_str = "";
                foreach (string log in Logs)
                    logs_str += log + "\n";

                File.AppendAllText($"{BackupDrive.RootDirectory}\\Logs\\Log_{DateTime.Now:yyyyMMdd}.txt", logs_str);

                File.AppendAllText($"{BackupDrive.RootDirectory}\\AppsList.txt", $"Last Updated: {DateTime.Now:MM/dd/yyyy 'at' hh:mm tt}\n\n");

                foreach (string app in InstalledAppsFromRegistry.GetInstalledApps())
                    File.AppendAllText($"{BackupDrive.RootDirectory}\\AppsList.txt", $"{app}\n");
            }
        }

        private void CopyChanges(string sourceDir, string destDir, IProgress<int> progress, ref long processedFiles, long totalFilesInThisSourceDir, CancellationToken cancellationToken)
        {
            Directory.CreateDirectory(destDir);
            long currentFile = 0;

            foreach (string sourceFile in Directory.GetFiles(sourceDir, "*.*", SearchOption.AllDirectories))
            {
                cancellationToken.ThrowIfCancellationRequested();

                string relativePath = sourceFile[(sourceDir.Length + 1)..];
                string destFile = Path.Combine(destDir, relativePath);
                Directory.CreateDirectory(Path.GetDirectoryName(destFile)!);

                FileInfo sourceInfo = new(sourceFile);
                FileInfo destInfo = new(destFile);

                if (!File.Exists(destFile) || sourceInfo.LastWriteTime > destInfo.LastWriteTime || sourceInfo.Length != destInfo.Length)
                {
                    try
                    {
                        StatusMessage = $"[COPY] Copying '{sourceInfo.Name}'...";
                        File.Copy(sourceFile, destFile, true);
                    }
                    catch (Exception ex)
                    {
                        string errorMessage = $"[ERR] Error copying '{sourceInfo.Name}': {ex.Message}";
                        StatusMessage = errorMessage;
                        Logs.Add($"{errorMessage} - Full Details: {ex}"); // Add this line
                    }
                }
                else
                {
                    StatusMessage = $"[INFO] File '{sourceInfo.Name}' already up to date. Continuing...";
                }

                currentFile++; // Increment currentFileInThisSourceDir
                processedFiles++; // Increment the global counter

                if (totalFilesInThisSourceDir > 0)
                {
                    progress?.Report((int)((double)currentFile / totalFilesInThisSourceDir * 100));
                }
            }

            // Logic to delete extra files in destination (check for cancellation)
            foreach (string destEntry in Directory.GetFileSystemEntries(destDir, "*", SearchOption.AllDirectories))
            {
                cancellationToken.ThrowIfCancellationRequested();
                string relativePath = destEntry[(destDir.Length + 1)..];
                string sourceEntry = Path.Combine(sourceDir, relativePath);

                if (!File.Exists(sourceEntry) && !Directory.Exists(sourceEntry))
                {
                    try
                    {
                        if (File.Exists(destEntry))
                            File.Delete(destEntry);
                        else if (Directory.Exists(destEntry))
                            Directory.Delete(destEntry, true);
                    }
                    catch (Exception ex)
                    {
                        StatusMessage = $"[ERR] Error deleting '{destEntry}': {ex.Message}";
                        Logs.Add($"[ERR] Error deleting '{destEntry}': {ex.Message} - Full Details: {ex}"); // Add this line
                    }
                }
            }
        }

        private async void AddBackupObjectAsync(object? parameter = null)
        {
            BackupObjectForm backupForm = new(BackupObjects);
            if (backupForm.ShowDialog() == true)
            {
                BackupObjects.Add(backupForm.BackupObjectResult);
                List<BackupObject> backupObjectList = [.. BackupObjects];
                string saveFilePath = "backup_objects.json";
                await BackupObjectSerializer.SerializeListToFileAsync(backupObjectList, saveFilePath);
                BackupObjects[0].IsFirst = true;
            }
        }

        private async void EditBackupObjectAsync(object? parameter = null!)
        {
            if (parameter is BackupObject backupObject)
            {
                BackupObjectForm backupForm = new(backupObject, BackupObjects);
                if (backupForm.ShowDialog() == true)
                {
                    BackupObjects.Remove(backupObject);
                    BackupObjects.Add(backupForm.BackupObjectResult);
                    List<BackupObject> backupObjectList = [.. BackupObjects];
                    string saveFilePath = "backup_objects.json";
                    await BackupObjectSerializer.SerializeListToFileAsync(backupObjectList, saveFilePath);
                }
            }
        }

        private async void RemoveBackupObjecAsync(object? parameter = null!)
        {
            if (parameter is BackupObject bo_parameter)
                BackupObjects.Remove(bo_parameter);
            List<BackupObject> backupObjectList = [.. BackupObjects];
            string saveFilePath = "backup_objects.json";
            await BackupObjectSerializer.SerializeListToFileAsync(backupObjectList, saveFilePath);
        }

        private async void PopulateDrivesAsync(object? parameter = null!)
        {
            await Task.Run(() =>
            {
                foreach (string driveletter in Directory.GetLogicalDrives())
                {
                    DriveInfo driveInfo = new(driveletter);
                    if (driveInfo.IsReady)
                        DrivesList.Add(driveInfo);
                }
                string savedDriveLetter = Properties.Settings.Default.BackupDriveLetter;
                if (!string.IsNullOrEmpty(savedDriveLetter))
                {
                    DriveInfo previouslySelected = DrivesList.FirstOrDefault(
                        d => d.Name.StartsWith(savedDriveLetter, StringComparison.OrdinalIgnoreCase))!;

                    if (previouslySelected != null)
                        BackupDrive = previouslySelected;
                    else if (DrivesList.Any())
                        BackupDrive = DrivesList.First();
                }
                else if (DrivesList.Any())
                    BackupDrive = DrivesList.First();
            });
        }

        public static string FormatPath(string path)
        {
            if (string.IsNullOrEmpty(path)) return "";

            string[] parts = path.Split(System.IO.Path.DirectorySeparatorChar, StringSplitOptions.RemoveEmptyEntries);

            if (parts.Length <= 3)
                return path;

            string firstPart = parts[0];
            string lastPart = parts[^1];

            return $"/{firstPart}/.../{lastPart}/";
        }
    }
}
