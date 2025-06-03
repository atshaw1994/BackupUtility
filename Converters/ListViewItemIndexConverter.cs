using BackupUtility.Models;
using System.Diagnostics;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

namespace BackupUtility.Converters
{
    public class ListViewItemIndexConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is not ListViewItem)
            {
                return ""; // Or DependencyProperty.UnsetValue; based on your preference
            }

            ListViewItem lvi = (ListViewItem)value;
            var fromContainer = ItemsControl.ItemsControlFromItemContainer(lvi).ItemContainerGenerator;

            var items = fromContainer.Items.Cast<ListViewItem>().Where(x => x.Visibility == Visibility.Visible).ToList();
            var count = items.Count;

            var index = items.IndexOf(lvi);
            if (index == 0)
                return "First";
            else if (count - 1 == index)
                return "Last";
            else
                return "";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return DependencyProperty.UnsetValue;
        }
    }
}
