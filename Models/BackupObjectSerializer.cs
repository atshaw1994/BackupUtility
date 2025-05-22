using System.IO;
using System.Text.Json;
using System.Windows;

namespace BackupUtility.Models
{
    public static class BackupObjectSerializer
    {
        // Cache the JsonSerializerOptions instance
        private static readonly JsonSerializerOptions _jsonSerializerOptions = new() { WriteIndented = true };

        public static async Task SerializeListToFileAsync(List<BackupObject> backupObjects, string filePath)
        {
            try
            {
                string jsonString = JsonSerializer.Serialize(backupObjects, _jsonSerializerOptions);
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
                    // No options needed for deserialization unless you have specific converters or properties to ignore
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
