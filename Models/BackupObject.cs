namespace BackupUtility.Models
{
    public class BackupObject
    {
        public Guid Id { get; set; }
        public string Source { get; set; }
        public string Destination { get; set; }

        public BackupObject()
        {
            Id = Guid.NewGuid();
            Source = string.Empty;
            Destination = string.Empty;
        }

        public BackupObject(string source, string destination) : this()
        {
            Source = source;
            Destination = destination;
        }

        public BackupObject(Guid id, string source, string destination)
        {
            Id = id;
            Source = source;
            Destination = destination;
        }

        public override bool Equals(object? obj)
        {
            if (obj == null || GetType() != obj.GetType()) return false;

            BackupObject other = (BackupObject)obj;

            return Id.Equals(other.Id);
        }

        public override int GetHashCode() => Id.GetHashCode();
    }
}
