using BackupUtility.ViewModels;
using System.Windows;

namespace BackupUtility.Views
{
    /// <summary>
    /// Interaction logic for BackupTimeWindow.xaml
    /// </summary>
    public partial class BackupTimeWindow : Window
    {
        public BackupTimeWindow()
        {
            InitializeComponent();
            Loaded += BackupTimeWindow_Loaded;
        }

        private void BackupTimeWindow_Loaded(object sender, RoutedEventArgs e)
        {
            if (DataContext is BackupTimeWindowViewModel viewModel)
                viewModel.CloseRequested += ViewModel_CloseRequested;
        }

        private void ViewModel_CloseRequested(bool result)
        {
            DialogResult = result;
            Close();
        }

        // Unsubscribe to prevent memory leaks if the window is not explicitly closed
        protected override void OnClosed(EventArgs e)
        {
            if (DataContext is BackupTimeWindowViewModel viewModel)
                viewModel.CloseRequested -= ViewModel_CloseRequested;
            base.OnClosed(e);
        }
    }
}
