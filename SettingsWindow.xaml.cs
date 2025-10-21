using System;
using System.Diagnostics;
using System.Windows;

namespace TiclyMusic
{
    public partial class SettingsWindow : Window
    {
        private AppConfig _config;

        public SettingsWindow(AppConfig config)
        {
            InitializeComponent();
            _config = config;
            LoadSettings();
        }

        private void LoadSettings()
        {
            ApiKeyTextBox.Text = _config.YouTubeApiKey;
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            _config.YouTubeApiKey = ApiKeyTextBox.Text.Trim();
            _config.Save();
            
            MessageBox.Show("Settings saved successfully!", "Settings", MessageBoxButton.OK, MessageBoxImage.Information);
            this.DialogResult = true;
            this.Close();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            this.Close();
        }

        private void ApiKeyGuide_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = "https://developers.google.com/youtube/v3/getting-started",
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Unable to open browser: {ex.Message}\n\nPlease visit: https://developers.google.com/youtube/v3/getting-started", 
                    "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }
    }
}