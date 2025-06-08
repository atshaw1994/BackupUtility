using BackupUtility.Models;
using BackupUtility.Services.Interfaces;
using System.IO;
using System.Text.Json;

namespace BackupUtility.Services.Implementations
{
    public class BackupObjectStorageService : IBackupObjectStorageService
    {
        private static readonly JsonSerializerOptions _jsonSerializerOptions = new()
        {
            WriteIndented = true,
            PropertyNameCaseInsensitive = true
        };

        private const string _filePath = "backup_objects.json";

        public async Task<List<BackupObject>> LoadBackupObjectsAsync()
        {
            string filePath = "backup_objects.json"; // Consider injecting this path or getting it from settings

            if (!File.Exists(filePath))
            {
                return []; // Return an empty list if file doesn't exist yet
            }

            string json = await File.ReadAllTextAsync(filePath);
            var loadedObjects = JsonSerializer.Deserialize<List<BackupObject>>(json, _jsonSerializerOptions);
            return loadedObjects ?? []; // Return empty list if deserialization results in null
        }

        public async Task SaveBackupObjectsAsync(List<BackupObject> backupObjects)
        {
            string filePath = "backup_objects.json"; // Consider injecting this path or getting it from settings
            string json = JsonSerializer.Serialize(backupObjects, _jsonSerializerOptions);
            await File.WriteAllTextAsync(filePath, json);
        }
    }
}
