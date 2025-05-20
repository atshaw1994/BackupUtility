using BackupUtility.ViewModels;
using System;
using System.Globalization;
using System.Windows.Data;

namespace BackupUtility.Converters
{
    public class PathFormatterConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string path)
            {
                // Assuming your FormatPath method exists in your ViewModel or a utility class
                MainWindowViewModel? viewModel = System.Windows.Application.Current.MainWindow?.DataContext as MainWindowViewModel;
                return viewModel?.FormatPath(path) ?? path; // Fallback to original path if FormatPath is not accessible
            }
            return value?.ToString(); // Fallback to default string representation
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // ConvertBack is not needed for display-only formatting
            throw new NotImplementedException();
        }

    }

}