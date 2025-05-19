using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

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
