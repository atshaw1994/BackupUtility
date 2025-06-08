namespace BackupUtility.Services.Interfaces
{
    public interface ILogger
    {
        void Log(string message);
        void LogWarning(string message);
        void LogError(string message, Exception? ex = null);
    }
}
