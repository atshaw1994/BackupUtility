using BackupUtility.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace BackupUtility.Views
{
    /// <summary>
    /// Interaction logic for DateTimePicker.xaml
    /// </summary>
    public partial class DateTimePicker : UserControl
    {
        public static readonly DependencyProperty ScheduleProperty = 
            DependencyProperty.Register("Schedule",
                typeof(BackupSchedule),
                typeof(DateTimePicker),
                new PropertyMetadata(new BackupSchedule()));

        public BackupSchedule Schedule
        {
            get { return (BackupSchedule)GetValue(ScheduleProperty); }
            set { SetValue(ScheduleProperty, value); }
        }

        public DateTimePicker()
        {
            InitializeComponent();
        }
    }
}
