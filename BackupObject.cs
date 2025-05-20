using BackupUtility.ViewModels;
using System.IO;
using System.Text.Json;
using System.Windows;

namespace BackupUtility
{
    public class BackupObject
    {
        public string Source { get; set; }
        public string Destination { get; set; }

        public BackupObject()
        {
            Source = string.Empty;
            Destination = string.Empty;
        }

        public BackupObject(string source, string destination)
        {
            Source = source;
            Destination = destination;
        }

        public async void PerformBackup(MainWindowViewModel sender, CancellationToken cancellationToken)
        {
            try
            {
                await Task.Run(() =>
                {
                    if (Directory.Exists(Source))
                    {

                    }
                }, cancellationToken);
            }
            catch (Exception ex)
            {
                sender.StatusMessage = $"[ERR] Backup failed: {ex.Message}";
                sender.BackupProgress = 0;
            }
        }
    }

    public static class BackupObjectSerializer
    {
        public static async Task SerializeListToFileAsync(List<BackupObject> backupObjects, string filePath)
        {
            try
            {
                var options = new JsonSerializerOptions { WriteIndented = true };
                string jsonString = JsonSerializer.Serialize(backupObjects, options);
                await File.WriteAllTextAsync(filePath, jsonString);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error serializing backup objects: {ex.Message}", "Serialization Error", MessageBoxButton.OK, MessageBoxImage.Error);
                // Consider logging the error as well
            }
        }

        public static async Task<List<BackupObject>> DeserializeListFromFileAsync(string filePath)
        {
            try
            {
                if (File.Exists(filePath))
                {
                    string jsonString = await File.ReadAllTextAsync(filePath);
                    var backupObjects = JsonSerializer.Deserialize<List<BackupObject>>(jsonString);
                    return backupObjects ?? [];
                }
                return [];
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error deserializing backup objects: {ex.Message}", "Deserialization Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return [];
            }
        }
    }
}
