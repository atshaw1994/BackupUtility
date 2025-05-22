namespace BackupUtility.Models
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
    }
}
