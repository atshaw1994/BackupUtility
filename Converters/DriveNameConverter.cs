using System;
using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Data;

namespace BackupUtility.Converters
{
    public class DriveNameConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values == null || values.Length < 3 || values[0] is not string volumeLabel || values[1] is not DriveType driveType || values[2] is not string driveName)
                return DependencyProperty.UnsetValue; // Or string.Empty

            string driveLetterOnly = driveName.Length >= 2 ? driveName[..2] : string.Empty;
            string displayName;

            if (string.IsNullOrEmpty(volumeLabel))
                displayName = $"{driveType} Disk ({driveLetterOnly})";
            else
                displayName = $"{volumeLabel} ({driveLetterOnly})";

            return displayName.Replace("Fixed", "Local");
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException(); // Not needed for display purposes
        }
    }
}
