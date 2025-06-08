using BackupUtility.Helpers;
using BackupUtility.Models;
using BackupUtility.Services.Interfaces;
using System.Collections.ObjectModel;
using System.IO;

namespace BackupUtility.Services.Implementations
{
    public class BackupService(ILogger logger) : IBackupService
    {
        private readonly ILogger _logger = logger;

        public async Task PerformBackupAsync(string backupDriveRoot, ObservableCollection<BackupObject> backupObjects,
                                             IProgress<int> progress, IProgress<string> status, CancellationToken cancellationToken)
        {
            await Task.Run(() =>
            {
                string logDirectory = Path.Combine(backupDriveRoot, "Logs");
                string logFilePath = Path.Combine(logDirectory, $"Log_{DateTime.Now:yyyyMMdd_HHmmss}.txt");
                _logger.SetLogFile(logFilePath);
                _logger.Log($"--- Backup Session Started: {DateTime.Now} ---");

                status.Report("[START] Backup started...");
                _logger.Log($"Backup started at {DateTime.Now}");

                string backupDestination = Path.Combine(backupDriveRoot, $"Backup_{DateTime.Now:yyyyMMdd}");
                Directory.CreateDirectory(backupDestination);

                long totalFiles = backupObjects.Sum(backupObject =>
                {
                    try
                    {
                        return Directory.GetFiles(backupObject.Source, "*.*", SearchOption.AllDirectories).LongLength;
                    }
                    catch (DirectoryNotFoundException)
                    {
                        status.Report($"[ERR] Source folder '{backupObject.Source}' not found. Skipping.");
                        _logger.LogError($"Source folder '{backupObject.Source}' not found. Skipping.");
                        return 0;
                    }
                    catch (Exception ex)
                    {
                        status.Report($"[ERR] Error counting files in '{backupObject.Source}': {ex.Message}");
                        _logger.LogError($"Error counting files in '{backupObject.Source}': {ex.Message}", ex); // Pass exception
                        return 0;
                    }
                });

                long processedFiles = 0;

                foreach (BackupObject backupObject in backupObjects)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    if (!Directory.Exists(backupObject.Source))
                    {
                        status.Report($"[ERR] Source folder '{backupObject.Source}' does not exist. Skipping.");
                        _logger.LogError($"Source folder '{backupObject.Source}' does not exist. Skipping.");
                        continue;
                    }

                    string finalDestPathForSource = Path.Combine(backupDestination, new DirectoryInfo(backupObject.Source).Name);

                    CopyChanges(backupObject.Source, finalDestPathForSource, status,
                                (currentFileInSourceDir) =>
                                {
                                    Interlocked.Increment(ref processedFiles);
                                    if (totalFiles > 0)
                                        progress.Report((int)((double)processedFiles / totalFiles * 100));
                                },
                                cancellationToken);
                }

                if (!cancellationToken.IsCancellationRequested)
                {
                    status.Report("[END] Backup completed successfully.");
                    _logger.Log($"Backup completed successfully at {DateTime.Now}");
                }
                else
                {
                    status.Report("[CANCELED] Backup cancelled.");
                    _logger.Log($"Backup cancelled at {DateTime.Now}");
                }


                // Write installed apps list
                var installedApps = GetInstalledAppsAsync().Result;
                string appsListFilePath = Path.Combine(backupDriveRoot, "AppsList.txt");
                try
                {
                    File.WriteAllText(appsListFilePath, $"Last Updated: {DateTime.Now:MM/dd/yyyy 'at' hh:mm tt}\n\n");
                    File.AppendAllLines(appsListFilePath, installedApps);
                    _logger.Log($"Installed applications list written to '{appsListFilePath}'");
                }
                catch (Exception ex)
                {
                    _logger.LogError($"Error writing installed applications list: {ex.Message}", ex);
                }

                // End logging session (now uses the _logger)
                _logger.Log($"--- Backup Session Ended: {DateTime.Now} ---");

            }, cancellationToken);
        }

        private void CopyChanges(string sourceDir, string destDir, IProgress<string> status, Action<long> reportProgress, CancellationToken cancellationToken)
        {
            Directory.CreateDirectory(destDir);

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
                        status.Report($"[COPY] Copying '{sourceInfo.Name}'...");
                        File.Copy(sourceFile, destFile, true);
                        _logger.Log($"Copied '{sourceInfo.Name}' to '{destFile}'");
                    }
                    catch (Exception ex)
                    {
                        string errorMessage = $"[ERR] Error copying '{sourceInfo.Name}': {ex.Message}";
                        status.Report(errorMessage);
                        _logger.LogError($"{errorMessage} - Full Details: {ex}");
                    }
                }
                else
                    status.Report($"[INFO] File '{sourceInfo.Name}' already up to date. Continuing...");
                reportProgress(1); // Report 1 file processed
            }

            // Logic to delete extra files in destination (check for cancellation)
            foreach (string destEntry in Directory.GetFileSystemEntries(destDir, "*", SearchOption.AllDirectories))
            {
                cancellationToken.ThrowIfCancellationRequested();
                string relativePath = destEntry[(destDir.Length + 1)..];
                string sourceEntry = Path.Combine(sourceDir, relativePath);

                // Only delete if the corresponding source doesn't exist (i.e., it was deleted from source)
                if (!File.Exists(sourceEntry) && !Directory.Exists(sourceEntry))
                {
                    try
                    {
                        if (File.Exists(destEntry))
                        {
                            File.Delete(destEntry);
                            status.Report($"[DELETE] Deleted '{Path.GetFileName(destEntry)}'.");
                            _logger.Log($"Deleted file '{destEntry}'");
                        }
                        else if (Directory.Exists(destEntry))
                        {
                            Directory.Delete(destEntry, true);
                            status.Report($"[DELETE] Deleted directory '{Path.GetFileName(destEntry)}'.");
                            _logger.Log($"Deleted directory '{destEntry}'");
                        }
                    }
                    catch (Exception ex)
                    {
                        string errorMessage = $"[ERR] Error deleting '{destEntry}': {ex.Message}";
                        status.Report(errorMessage);
                        _logger.LogError($"{errorMessage} - Full Details: {ex}");
                    }
                }
            }
        }

        public Task<List<string>> GetInstalledAppsAsync() => Task.Run(InstalledAppsFromRegistry.GetInstalledApps);
    }
}
