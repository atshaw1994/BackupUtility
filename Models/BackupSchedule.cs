using System.Collections.ObjectModel;
using System.Globalization;

namespace BackupUtility.Models
{
    public class BackupSchedule : BaseObject
    {
        public ObservableCollection<int> Hours { get; } = new(Enumerable.Range(1, 12));
        public ObservableCollection<string> Minutes { get; } = new(Enumerable.Range(0, 60).Select(i => i.ToString("D2")));
        public ObservableCollection<string> Meridiems { get; } = ["AM", "PM"];

        private bool _mondayEnabled;
        public bool MondayEnabled
        {
            get => _mondayEnabled;
            set
            {
                if (SetProperty(ref _mondayEnabled, value))
                {
                    OnPropertyChanged(nameof(DebugDisplayString));
                }
            }
        }

        private bool _tuesdayEnabled;
        public bool TuesdayEnabled
        {
            get => _tuesdayEnabled;
            set
            {
                if (SetProperty(ref _tuesdayEnabled, value))
                {
                    OnPropertyChanged(nameof(DebugDisplayString));
                }
            }
        }

        private bool _wednesdayEnabled;
        public bool WednesdayEnabled
        {
            get => _wednesdayEnabled;
            set
            {
                if (SetProperty(ref _wednesdayEnabled, value))
                {
                    OnPropertyChanged(nameof(DebugDisplayString));
                }
            }
        }

        private bool _thursdayEnabled;
        public bool ThursdayEnabled
        {
            get => _thursdayEnabled;
            set
            {
                if (SetProperty(ref _thursdayEnabled, value))
                {
                    OnPropertyChanged(nameof(DebugDisplayString));
                }
            }
        }

        private bool _fridayEnabled;
        public bool FridayEnabled
        {
            get => _fridayEnabled;
            set
            {
                if (SetProperty(ref _fridayEnabled, value))
                {
                    OnPropertyChanged(nameof(DebugDisplayString));
                }
            }
        }

        private bool _saturdayEnabled;
        public bool SaturdayEnabled
        {
            get => _saturdayEnabled;
            set
            {
                if (SetProperty(ref _saturdayEnabled, value))
                {
                    OnPropertyChanged(nameof(DebugDisplayString));
                }
            }
        }

        private bool _sundayEnabled;
        public bool SundayEnabled
        {
            get => _sundayEnabled;
            set
            {
                if (SetProperty(ref _sundayEnabled, value)) OnPropertyChanged(nameof(DebugDisplayString));
            }
        }

        private int _selectedHour;
        public int SelectedHour
        {
            get => _selectedHour;
            set
            {
                if (SetProperty(ref _selectedHour, value))
                {
                    UpdateBackupTime();
                    OnPropertyChanged(nameof(DebugDisplayString));
                }
            }
        }

        private int _selectedMinute;
        public int SelectedMinute
        {
            get => _selectedMinute;
            set
            {
                if (SetProperty(ref _selectedMinute, value))
                {
                    UpdateBackupTime();
                    OnPropertyChanged(nameof(DebugDisplayString));
                    SelectedMinuteString = _selectedMinute.ToString("D2");
                }
            }
        }

        private string _selectedMinuteString;
        public string SelectedMinuteString
        {
            get => _selectedMinuteString;
            set
            {
                if (SetProperty(ref _selectedMinuteString, value) && int.TryParse(value, NumberStyles.None, CultureInfo.InvariantCulture, out int parsedMinute))
                    SelectedMinute = parsedMinute;
            }
        }

        private string _selectedMeridiem;
        public string SelectedMeridiem
        {
            get => _selectedMeridiem;
            set
            {
                if (SetProperty(ref _selectedMeridiem, value))
                {
                    UpdateBackupTime();
                    OnPropertyChanged(nameof(DebugDisplayString));
                }
            }
        }

        private TimeSpan _backupTime;
        public TimeSpan BackupTime
        {
            get => _backupTime;
            set => SetProperty(ref _backupTime, value);
        }

        public BackupSchedule()
        {
            MondayEnabled = false;
            TuesdayEnabled = false;
            WednesdayEnabled = false;
            ThursdayEnabled = false;
            FridayEnabled = false;
            SaturdayEnabled = false;
            SundayEnabled = false;

            BackupTime = new TimeSpan(6, 0, 0);
            _selectedMeridiem = "AM";

            UpdateSelectedTimeProperties();

            _selectedMinuteString = _selectedMinute.ToString("D2");

            OnPropertyChanged(nameof(DebugDisplayString));
        }

        public void SetDay(int day, bool enabled)
        {
            if (day == 0) SundayEnabled = enabled;
            else if (day == 1) MondayEnabled = enabled;
            else if (day == 2) TuesdayEnabled = enabled;
            else if (day == 3) WednesdayEnabled = enabled;
            else if (day == 4) ThursdayEnabled = enabled;
            else if (day == 5) FridayEnabled = enabled;
            else if (day == 6) SaturdayEnabled = enabled;
        }

        public List<DayOfWeek> GetSelectedDays()
        {
            var selectedDays = new List<DayOfWeek>();
            if (MondayEnabled) selectedDays.Add(DayOfWeek.Monday);
            if (TuesdayEnabled) selectedDays.Add(DayOfWeek.Tuesday);
            if (WednesdayEnabled) selectedDays.Add(DayOfWeek.Wednesday);
            if (ThursdayEnabled) selectedDays.Add(DayOfWeek.Thursday);
            if (FridayEnabled) selectedDays.Add(DayOfWeek.Friday);
            if (SaturdayEnabled) selectedDays.Add(DayOfWeek.Saturday);
            if (SundayEnabled) selectedDays.Add(DayOfWeek.Sunday);
            return selectedDays;
        }

        public bool IsTodayEnabled() =>
            // Implement logic to check if current day of week is enabled
            DateTime.Now.DayOfWeek switch
            {
                DayOfWeek.Sunday => SundayEnabled,
                DayOfWeek.Monday => MondayEnabled,
                DayOfWeek.Tuesday => TuesdayEnabled,
                DayOfWeek.Wednesday => WednesdayEnabled,
                DayOfWeek.Thursday => ThursdayEnabled,
                DayOfWeek.Friday => FridayEnabled,
                DayOfWeek.Saturday => SaturdayEnabled,
                _ => false,
            };

        private void UpdateBackupTime()
        {
            int hour24 = SelectedHour;

            // Convert 12-hour to 24-hour format
            if (SelectedMeridiem == "PM" && hour24 != 12)
                hour24 += 12;
            else if (SelectedMeridiem == "AM" && hour24 == 12)
                hour24 = 0;

            BackupTime = new TimeSpan(hour24, SelectedMinute, 0);
        }

        public void UpdateSelectedTimeProperties()
        {
            int hour24 = BackupTime.Hours;

            if (hour24 >= 12)
            {
                _selectedMeridiem = "PM";
                _selectedHour = (hour24 == 12) ? 12 : hour24 - 12;
            }
            else
            {
                _selectedMeridiem = "AM";
                _selectedHour = (hour24 == 0) ? 12 : hour24;
            }

            _selectedMinute = BackupTime.Minutes;

            // Manually notify properties that were directly set to avoid recursion from SetProperty
            OnPropertyChanged(nameof(SelectedHour));
            OnPropertyChanged(nameof(SelectedMinute));
            OnPropertyChanged(nameof(SelectedMeridiem));

            // Set the string representation for the ComboBox
            _selectedMinuteString = _selectedMinute.ToString("D2");
            OnPropertyChanged(nameof(SelectedMinuteString));

            OnPropertyChanged(nameof(DebugDisplayString));
        }

        public string DebugDisplayString
        {
            get
            {
                // Construct the time string using SelectedHour, SelectedMinute, and SelectedMeridiem
                // This bypasses the TimeSpan.ToString() limitation for AM/PM
                string timeString = $"{SelectedHour:D2}:{SelectedMinute:D2} {SelectedMeridiem}";

                return $"Su: {(SundayEnabled ? "(T)" : "(F)")} " +
                        $"M: {(MondayEnabled ? "(T)" : "(F)")} " +
                        $"Tu: {(ThursdayEnabled ? "(T)" : "(F)")} " +
                        $"W: {(WednesdayEnabled ? "(T)" : "(F)")} " +
                        $"Th: {(ThursdayEnabled ? "(T)" : "(F)")} " +
                        $"F: {(FridayEnabled ? "(T)" : "(F)")} " +
                        $"Sa: {(SaturdayEnabled ? "(T)" : "(F)")} " +
                        $"Time: {timeString}";
            }
        }

        public override string ToString() => DebugDisplayString;
    }
}
