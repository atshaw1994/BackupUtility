using BackupUtility.ViewModels;
using System.IO;
using System.Text.Json;
using System.Threading;
using System.Windows;

namespace BackupUtility
{
    public class BackupObject : DependencyObject
    {
        public static readonly DependencyProperty SourceProperty = DependencyProperty.Register(
           "Source", typeof(string), typeof(BackupObject), new PropertyMetadata(string.Empty));
        public static readonly DependencyProperty DestinationProperty = DependencyProperty.Register(
           "Destination", typeof(string), typeof(BackupObject), new PropertyMetadata(string.Empty));

        public string Source
        { get => (string)GetValue(SourceProperty); set => SetValue(SourceProperty, value); }
        public string Destination
        { get => (string)GetValue(DestinationProperty); set => SetValue(DestinationProperty, value); }

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
                    return backupObjects ?? new List<BackupObject>();
                }
                return new List<BackupObject>();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error deserializing backup objects: {ex.Message}", "Deserialization Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return new List<BackupObject>();
            }
        }
    }
}
