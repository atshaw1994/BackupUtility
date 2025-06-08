using BackupUtility.Services.Interfaces;
using System.IO;

namespace BackupUtility.Services.Implementations
{
    public class Logger : ILogger
    {
        private string? _logFilePath;
        private static readonly Lock _fileLock = new(); // For thread-safe file access

        public void SetLogFile(string logFilePath)
        {
            _logFilePath = logFilePath;
            if (!File.Exists(logFilePath)) 
                File.Create(logFilePath).Close();
            Directory.CreateDirectory(Path.GetDirectoryName(_logFilePath)!);
        }

        private void WriteToFile(string level, string message, Exception? ex)
        {
            if (string.IsNullOrEmpty(_logFilePath))
            {
                // Fallback to Console/Debug output if log file not set (or throw an error)
                Console.WriteLine($"[{level}] {DateTime.Now:HH:mm:ss} {message} {(ex != null ? $" - Exception: {ex.Message}" : "")}");
                System.Diagnostics.Debug.WriteLine($"[{level}] {DateTime.Now:HH:mm:ss} {message} {(ex != null ? $" - Exception: {ex.Message}" : "")}");
                return;
            }

            string logEntry = $"[{level}] {DateTime.Now:HH:mm:ss} {message}";
            if (ex != null)
            {
                logEntry += $" - Exception: {ex.Message}";
                if (ex.InnerException != null)
                    logEntry += $" (Inner: {ex.InnerException.Message})";
                logEntry += $"\n{ex.StackTrace}"; // Include stack trace for errors
            }
            logEntry += "\n";

            // Use a lock to prevent multiple threads from writing to the file simultaneously
            lock (_fileLock)
            {
                File.AppendAllText(_logFilePath, logEntry);
            }
        }

        public void Log(string message) => WriteToFile("INFO", message, null);

        public void LogWarning(string message) => WriteToFile("WARN", message, null);

        public void LogError(string message, Exception? ex = null) => WriteToFile("ERROR", message, ex);
    }
}
