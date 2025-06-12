namespace BackupUtility.Models
{
    public class BackupObject : BaseObject
    {
        public Guid Id { get; set; }
        private string _source;
        public string Source
        {
            get => _source;
            set => SetProperty(ref _source, value);
        }
        private string _destination;
        public string Destination
        {
            get => _destination;
            set => SetProperty(ref _destination, value);
        }
        private bool _isFirst;
        public bool IsFirst
        {
            get => _isFirst;
            set
            {
                if (_isFirst != value)
                {
                    _isFirst = value;
                    OnPropertyChanged(nameof(IsFirst));
                }
            }
        }
        private bool _usesCustomSchedule;
        public bool UsesCustomSchedule
        {
            get => _usesCustomSchedule;
            set => SetProperty(ref _usesCustomSchedule, value);
        }
        private BackupSchedule _customSchedule;
        public BackupSchedule CustomSchedule
        {
            get => _customSchedule;
            set => SetProperty(ref _customSchedule, value);
        }

        public BackupObject()
        {
            Id = Guid.NewGuid();
            _source = string.Empty;
            _destination = string.Empty;
            _usesCustomSchedule = false;
            _customSchedule = new();
        }
        public BackupObject(string source, string destination) : this()
        {
            _source = source;
            _destination = destination;
        }
        public BackupObject(Guid id, string source, string destination) : this()
        {
            Id = id;
            _source = source;
            _destination = destination;
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
