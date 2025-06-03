using System.ComponentModel;

namespace BackupUtility.Models
{
    public class BackupObject : INotifyPropertyChanged
    {
        public Guid Id { get; set; }
        public string Source { get; set; }
        public string Destination { get; set; }

        private bool _isFirst;
        public bool IsFirst
        {
            get { return _isFirst; }
            set
            {
                if (_isFirst != value)
                {
                    _isFirst = value;
                    OnPropertyChanged(nameof(IsFirst));
                }
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

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
