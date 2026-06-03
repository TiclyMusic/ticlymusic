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
            SpotifyClientIdTextBox.Text = _config.SpotifyClientId;
            SpotifyClientSecretTextBox.Text = _config.SpotifyClientSecret;
            SpotiFlacBaseUrlTextBox.Text = _config.SpotiFlacBaseUrl;
            SpotiFlacApiKeyTextBox.Text = _config.SpotiFlacApiKey;
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            _config.SpotifyClientId = SpotifyClientIdTextBox.Text.Trim();
            _config.SpotifyClientSecret = SpotifyClientSecretTextBox.Text.Trim();
            _config.SpotiFlacBaseUrl = SpotiFlacBaseUrlTextBox.Text.Trim();
            _config.SpotiFlacApiKey = SpotiFlacApiKeyTextBox.Text.Trim();
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

        private void SpotifyGuide_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = "https://developer.spotify.com/documentation/web-api",
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Unable to open browser: {ex.Message}\n\nPlease visit: https://developer.spotify.com/documentation/web-api", 
                    "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }
    }
}