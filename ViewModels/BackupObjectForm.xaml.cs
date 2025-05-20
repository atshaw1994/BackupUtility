using Microsoft.Win32;
using System.Windows;

namespace BackupUtility.ViewModels
{
    /// <summary>
    /// Interaction logic for BackupObjectForm.xaml
    /// </summary>
    public partial class BackupObjectForm : Window
    {
        public BackupObject BackupObjectResult { get; private set; }

        public BackupObjectForm()
        {
            InitializeComponent();
            BackupObjectResult = new();
        }

        public BackupObjectForm(BackupObject backupObject)
        {
            InitializeComponent();
            BackupObjectResult = backupObject;
            SourceButton.Content = FormatPath(BackupObjectResult.Source);
            DestinationTextBox.Text = BackupObjectResult.Destination;
        }

        public BackupObjectForm(string source, string destination)
        {
            InitializeComponent();
            BackupObjectResult = new()
            {
                Source = source,
                Destination = destination
            };
        }

        private void SourceButton_Click(object sender, RoutedEventArgs e)
        {
            OpenFolderDialog ofd = new();
            if (ofd.ShowDialog() == true)
            {
                BackupObjectResult.Source = ofd.FolderName;
                SourceButton.Content = FormatPath(BackupObjectResult.Source);
            }
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            BackupObjectResult.Destination = DestinationTextBox.Text;
            DialogResult = true;
            Close();
        }

        private static string FormatPath(string path)
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
