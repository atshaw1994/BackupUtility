using System;
using System.Globalization;
using System.Windows.Data;

namespace BackupUtility.Converters
{
    public class FreeSpaceConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is long availableFreeSpaceBytes)
            {
                double freeSpaceGB = Math.Round((double)availableFreeSpaceBytes / 1073741824, 0); // Convert bytes to GB
                return $"({freeSpaceGB} GB free)";
            }
            return string.Empty;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException(); // Not needed for display purposes
        }
    }
}
