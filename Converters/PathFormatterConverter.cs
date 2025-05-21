using System.Globalization;
using System.Windows.Data;

namespace BackupUtility.Converters
{
    public class PathFormatterConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string path)
                return FormatPath(path) ?? path; // Fallback to original path if FormatPath is not accessible
            return value?.ToString()!; // Fallback to default string representation
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // ConvertBack is not needed for display-only formatting
            throw new NotImplementedException();
        }

        public static string FormatPath(string path)
        {
            if (string.IsNullOrEmpty(path)) return "";

            string[] parts = path.Split(System.IO.Path.DirectorySeparatorChar, StringSplitOptions.RemoveEmptyEntries);

            if (parts.Length <= 3)
                return path;

            string firstPart = parts[0];
            string lastPart = parts[^1];

            return $"/{firstPart}/.../{lastPart}/";
        }
    }
}