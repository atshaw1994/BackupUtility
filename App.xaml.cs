using System.Configuration;
using System.Data;
using System.Windows;
using BackupUtility.Themes;

namespace BackupUtility
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        public App()
        {
            ThemeManager.ThemeChanged += OnThemeChanged!;
            ApplyCurrentTheme(); // Apply initial theme
        }

        private void OnThemeChanged(object sender, ThemeChangedEventArgs e)
        {
            // Apply the new theme based on e.NewTheme
            ApplyTheme(e.NewTheme);
        }

        private static void ApplyCurrentTheme()
        {
            // Apply ThemeManager.CurrentTheme
            ApplyTheme(ThemeManager.CurrentTheme);
        }

        private static void ApplyTheme(ThemeManager.WindowsTheme theme)
        {
            var app = Application.Current;
            if (app == null || app.Resources == null) return;

            ResourceDictionary themeDictionary = [];
            switch (theme)
            {
                case ThemeManager.WindowsTheme.Light:
                    themeDictionary.Source = new Uri("Themes/Light.xaml", UriKind.Relative);
                    break;
                case ThemeManager.WindowsTheme.Dark:
                    themeDictionary.Source = new Uri("Themes/Dark.xaml", UriKind.Relative);
                    break;
            }

            // Merge the theme dictionary
            var existingTheme = app.Resources.MergedDictionaries.FirstOrDefault(d =>
                d.Source?.OriginalString.StartsWith("Themes/", StringComparison.OrdinalIgnoreCase) == true);
            if (existingTheme != null)
            {
                app.Resources.MergedDictionaries.Remove(existingTheme);
            }
            app.Resources.MergedDictionaries.Add(themeDictionary);
        }

    }

}
