using BackupUtility.Commands;
using System.Collections.ObjectModel;
using System.Windows.Input;

namespace BackupUtility.ViewModels
{
    public partial class BackupTimeWindowViewModel : BaseViewModel
    {
        public ObservableCollection<int> Hours = new(Enumerable.Range(0, 24));
        public ObservableCollection<int> Minutes = new(Enumerable.Range(0, 60));
        public ObservableCollection<string> Meridiems = ["AM", "PM"];

        private int _selectedHour;
        public int SelectedHour
        {
            get => _selectedHour;
            set
            {
                // Only update if value truly changes to avoid unnecessary updates/loops
                if (SetProperty(ref _selectedHour, value))
                {
                    UpdateSelectedTime(); // Update the TimeSpan when hour changes
                }
            }
        }

        private int _selectedMinute;
        public int SelectedMinute
        {
            get => _selectedMinute;
            set
            {
                // Only update if value truly changes
                if (SetProperty(ref _selectedMinute, value))
                {
                    UpdateSelectedTime(); // Update the TimeSpan when minute changes
                }
            }
        }

        private string _selectedMeridiem;
        public string SelectedMeridiem
        {
            get => _selectedMeridiem;
            set
            {
                // Only update if value truly changes
                if (SetProperty(ref _selectedMeridiem, value))
                {
                    UpdateSelectedTime(); // Update the TimeSpan when meridiem changes
                }
            }
        }

        private TimeSpan _selectedTime;
        public TimeSpan SelectedTime
        {
            get => _selectedTime;
            set
            {
                if (SetProperty(ref _selectedTime, value))
                {
                    _selectedHour = value.Hours;
                    _selectedMinute = value.Minutes;
                    // Notify properties changed so UI updates if these are bound elsewhere (e.g., initial load)
                    OnPropertyChanged(nameof(SelectedHour));
                    OnPropertyChanged(nameof(SelectedMinute));
                }
            }
        }

        public ICommand OkCommand { get; }
        public ICommand CancelCommand { get; }

        public BackupTimeWindowViewModel()
        {
            // Initialize SelectedTime. This will also set SelectedHour and SelectedMinute via its setter.
            SelectedTime = new(6,0,0);

            // Initialize commands
            OkCommand = new RelayCommand(_ => ExecuteOk());
            CancelCommand = new RelayCommand(_ => ExecuteCancel());
        }

        public BackupTimeWindowViewModel(TimeSpan backupTime)
        {
            // Initialize SelectedTime. This will also set SelectedHour and SelectedMinute via its setter.
            SelectedTime = backupTime;

            // Initialize commands
            OkCommand = new RelayCommand(_ => ExecuteOk());
            CancelCommand = new RelayCommand(_ => ExecuteCancel());
        }

        /// <summary>
        /// Updates the SelectedTime TimeSpan whenever SelectedHour, SelectedMinute, or SelectedMeridiem changes.
        /// </summary>
        private void UpdateSelectedTime()
        {
            // Create a new TimeSpan from the current hour and minute selections
            SelectedTime = new TimeSpan(SelectedHour, SelectedMinute, 0);
        }

        public event Action<bool>? CloseRequested;

        private void ExecuteOk() => CloseRequested?.Invoke(true);

        private void ExecuteCancel() => CloseRequested?.Invoke(false);
    }
}
