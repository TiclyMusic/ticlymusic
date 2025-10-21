using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using YoutubeExplode;
using YoutubeExplode.Videos.Streams; 

namespace TiclyMusic
{
    public partial class MainWindow : Window
    {
        private DispatcherTimer _timer;
        private bool _isLooping = false;
        private YoutubeClient _youtubeClient;
        private AppConfig _config;

        public MainWindow()
        {
            InitializeComponent();
            
            _config = AppConfig.Load();
            LoadWindowSettings();
            
            MediaElementPlayer.MediaOpened += MediaElementPlayer_MediaOpened;
            MediaElementPlayer.MediaEnded += MediaElementPlayer_MediaEnded;
            MediaElementPlayer.MediaFailed += MediaElementPlayer_MediaFailed;

            _timer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(1)
            };
            _timer.Tick += Timer_Tick;
            _youtubeClient = new YoutubeClient();
            
            // Set initial volume
            VolumeSlider.Value = _config.Volume;
            VolumePercentageText.Text = $"{(int)(_config.Volume * 100)}%";
            
            // Check if API key is configured
            if (string.IsNullOrEmpty(_config.YouTubeApiKey))
            {
                StatusIndicator.Fill = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.Orange);
                StatusText.Text = "Ready - Configure API key in Settings";
                MessageBox.Show("YouTube API key is not configured. Please go to Settings to set up your API key.", 
                    "Configuration Required", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            else
            {
                StatusIndicator.Fill = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.Green);
                StatusText.Text = "Ready - API key configured";
            }
        }

        private void LoadWindowSettings()
        {
            try
            {
                this.Width = Math.Max(400, _config.WindowWidth);
                this.Height = Math.Max(400, _config.WindowHeight);
                this.Left = Math.Max(0, _config.WindowLeft);
                this.Top = Math.Max(0, _config.WindowTop);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading window settings: {ex.Message}");
                // Use default values if loading fails
                this.Width = 450;
                this.Height = 550;
                this.Left = 100;
                this.Top = 100;
            }
        }

        private void SaveWindowSettings()
        {
            _config.WindowWidth = this.Width;
            _config.WindowHeight = this.Height;
            _config.WindowLeft = this.Left;
            _config.WindowTop = this.Top;
            _config.Volume = VolumeSlider.Value;
            _config.Save();
        }

        private void MediaElementPlayer_MediaFailed(object? sender, ExceptionRoutedEventArgs e)
        {
            MessageBox.Show($"Error during media playback:\n{e.ErrorException.Message}", "MediaElement Error", MessageBoxButton.OK, MessageBoxImage.Error);
            // Clean up any "Loading..." messages
            var loadingMessages = ResultsListBox.Items.OfType<string>().Where(s => s.StartsWith("Loading:")).ToList();
            foreach (var msg in loadingMessages) { ResultsListBox.Items.Remove(msg); }
        }

        private void Timer_Tick(object? sender, EventArgs e)
        {
            if (MediaElementPlayer.NaturalDuration.HasTimeSpan && MediaElementPlayer.Source != null)
            {
                ProgressBar.Value = MediaElementPlayer.Position.TotalSeconds;
                ElapsedTimeText.Text = MediaElementPlayer.Position.ToString(@"mm\:ss");
                var remainingTime = MediaElementPlayer.NaturalDuration.TimeSpan - MediaElementPlayer.Position;
                RemainingTimeText.Text = remainingTime.ToString(@"mm\:ss");
            }
        }

        private void MediaElementPlayer_MediaOpened(object sender, RoutedEventArgs e)
        {
            if (MediaElementPlayer.NaturalDuration.HasTimeSpan)
            {
                ProgressBar.Maximum = MediaElementPlayer.NaturalDuration.TimeSpan.TotalSeconds;
                _timer.Start();
            }
        }

        private void MediaElementPlayer_MediaEnded(object sender, RoutedEventArgs e)
        {
            if (_isLooping)
            {
                MediaElementPlayer.Position = TimeSpan.Zero;
                MediaElementPlayer.Play();
            }
            else
            {
                ProgressBar.Value = 0;
                ElapsedTimeText.Text = "00:00";
                if (MediaElementPlayer.NaturalDuration.HasTimeSpan)
                {
                    RemainingTimeText.Text = MediaElementPlayer.NaturalDuration.TimeSpan.ToString(@"mm\:ss");
                }
                else
                {
                    RemainingTimeText.Text = "00:00";
                }
                _timer.Stop();
            }
        }

        private void LoopButton_Click(object sender, RoutedEventArgs e)
        {
            _isLooping = !_isLooping;
            LoopButton.Content = _isLooping ? "🔁 Loop ON" : "🔁 Loop OFF";
            LoopButton.Background = _isLooping ? new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(76, 175, 80)) : new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(233, 233, 233));
            LoopButton.Foreground = _isLooping ? new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.White) : new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(86, 86, 86));
        }

        private void PlayButton_Click(object sender, RoutedEventArgs e)
        {
            if (MediaElementPlayer.Source != null)
            {
                MediaElementPlayer.Play();
                _timer.Start();
            }
        }

        private void PauseButton_Click(object sender, RoutedEventArgs e)
        {
            if (MediaElementPlayer.CanPause)
            {
                MediaElementPlayer.Pause();
                _timer.Stop();
            }
        }

        private void StopButton_Click(object sender, RoutedEventArgs e)
        {
            MediaElementPlayer.Stop();
            ProgressBar.Value = 0;
            ElapsedTimeText.Text = "00:00";
            RemainingTimeText.Text = "00:00";
            _timer.Stop();
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            SaveWindowSettings();
            Application.Current.Shutdown();
        }

        private void MinimizeButton_Click(object sender, RoutedEventArgs e)
        {
            this.WindowState = WindowState.Minimized;
        }

        private void FullScreenButton_Click(object sender, RoutedEventArgs e)
        {
            this.WindowState = this.WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;
        }

        private async void SearchButton_Click(object sender, RoutedEventArgs e)
        {
            string searchText = SearchTextBox.Text;

            if (string.IsNullOrWhiteSpace(searchText))
            {
                ResultsListBox.Items.Clear();
                ResultsListBox.Items.Add("Please enter a search term.");
                return;
            }

            ResultsListBox.Items.Clear();
            ResultsListBox.Items.Add("🔍 Searching...");
            StatusIndicator.Fill = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.Blue);
            StatusText.Text = "Searching YouTube...";

            try
            {
                var youtubeResults = await SearchYouTubeAsync(searchText);
                ResultsListBox.Items.Clear();
                
                // Reset status
                StatusIndicator.Fill = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.Green);
                StatusText.Text = "Ready - API key configured";

                if (youtubeResults != null && youtubeResults.Any())
                {
                    foreach (var track in youtubeResults)
                    {
                        ResultsListBox.Items.Add(track);
                    }
                }
                else
                {
                    ResultsListBox.Items.Add("No results found for your search.");
                }
            }
            catch (HttpRequestException httpEx)
            {
                ResultsListBox.Items.Clear();
                StatusIndicator.Fill = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.Red);
                StatusText.Text = "Error - Check API key";
                
                if (httpEx.Message.Contains("403") || httpEx.Message.Contains("forbidden"))
                {
                    ResultsListBox.Items.Add("Invalid or expired YouTube API key.");
                    ResultsListBox.Items.Add("Please check your API key in Settings.");
                }
                else
                {
                    ResultsListBox.Items.Add($"Network or API error: {httpEx.Message}");
                    ResultsListBox.Items.Add("Please check your internet connection and YouTube API key.");
                }
            }
            catch (InvalidOperationException configEx)
            {
                ResultsListBox.Items.Clear();
                StatusIndicator.Fill = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.Orange);
                StatusText.Text = "Ready - Configure API key in Settings";
                ResultsListBox.Items.Add(configEx.Message);
                ResultsListBox.Items.Add("Click the Settings button (⚙) to configure your API key.");
            }
            catch (JsonException jsonEx)
            {
                ResultsListBox.Items.Clear();
                StatusIndicator.Fill = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.Red);
                StatusText.Text = "Error - Data format issue";
                ResultsListBox.Items.Add($"Data format error: {jsonEx.Message}");
            }
            catch (Exception ex)
            {
                ResultsListBox.Items.Clear();
                StatusIndicator.Fill = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.Red);
                StatusText.Text = "Error occurred";
                ResultsListBox.Items.Add($"Unexpected error during search: {ex.Message}");
            }
        }

        private async Task<List<TrackInfo>> SearchYouTubeAsync(string query)
        {
            if (string.IsNullOrEmpty(_config.YouTubeApiKey))
            {
                throw new InvalidOperationException("YouTube API key is not configured. Please set it in Settings.");
            }

            using var httpClient = new HttpClient();
            string url = $"https://www.googleapis.com/youtube/v3/search?part=snippet&type=video&maxResults=10&key={_config.YouTubeApiKey}&q={Uri.EscapeDataString(query)}";
            var response = await httpClient.GetStringAsync(url);
            var json = JsonDocument.Parse(response);
            var items = json.RootElement.GetProperty("items");
            var tracks = new List<TrackInfo>();

            foreach (var item in items.EnumerateArray())
            {
                string? title = item.GetProperty("snippet").GetProperty("title").GetString();
                string? videoId = item.GetProperty("id").GetProperty("videoId").GetString();

                if (!string.IsNullOrEmpty(title) && !string.IsNullOrEmpty(videoId))
                {
                    tracks.Add(new TrackInfo
                    {
                        Source = "YouTube",
                        Title = title,
                        Url = $"https://www.youtube.com/watch?v={videoId}",
                        VideoId = videoId
                    });
                }
            }
            return tracks;
        }

        private async void ResultsListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ResultsListBox.SelectedItem is TrackInfo selectedTrack && !string.IsNullOrEmpty(selectedTrack.VideoId))
            {
                string loadingMessage = $"Loading: {selectedTrack.Title}...";
                var existingLoadingMessage = ResultsListBox.Items.OfType<string>().FirstOrDefault(s => s.StartsWith("Loading:"));
                if (existingLoadingMessage != null) ResultsListBox.Items.Remove(existingLoadingMessage);
                ResultsListBox.Items.Add(loadingMessage);

                try
                {
                    var streamManifest = await _youtubeClient.Videos.Streams.GetManifestAsync(selectedTrack.VideoId);

                    // Try to get an audio-only stream first
                    // The types AudioOnlyStreamInfo and MuxedStreamInfo should be concrete classes in YoutubeExplode.Videos.Streams
                    var audioStreamInfo = streamManifest.GetAudioOnlyStreams()
                                                        .OrderByDescending(s => s.Bitrate)
                                                        .FirstOrDefault();

                    string? streamUrl = audioStreamInfo?.Url;

                    // If no audio-only stream, try a muxed stream (audio+video)
                    if (string.IsNullOrEmpty(streamUrl))
                    {
                        var muxedStreamInfo = streamManifest.GetMuxedStreams()
                                                            .OrderByDescending(s => s.VideoQuality)
                                                            .ThenByDescending(s => s.Bitrate) // Fallback sort by bitrate
                                                            .FirstOrDefault();
                        streamUrl = muxedStreamInfo?.Url;
                    }

                    ResultsListBox.Items.Remove(loadingMessage);

                    if (!string.IsNullOrEmpty(streamUrl))
                    {
                        MediaElementPlayer.Stop();
                        MediaElementPlayer.Source = new Uri(streamUrl);
                        MediaElementPlayer.Play();
                    }
                    else
                    {
                        MessageBox.Show($"Unable to find a valid audio/video stream for: {selectedTrack.Title}", "Stream Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                    }
                }
                catch (YoutubeExplode.Exceptions.VideoUnavailableException videoEx)
                {
                    ResultsListBox.Items.Remove(loadingMessage);
                    MessageBox.Show($"Video unavailable: {selectedTrack.Title}\n{videoEx.Message}", "YoutubeExplode Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
                catch (Exception ex)
                {
                    ResultsListBox.Items.Remove(loadingMessage);
                    MessageBox.Show($"Unable to start playback for: {selectedTrack.Title}\nError: {ex.Message}", "Playback Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void ProgressBar_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (MediaElementPlayer.NaturalDuration.HasTimeSpan && MediaElementPlayer.Source != null)
            {
                TimeSpan newPosition = TimeSpan.FromSeconds(e.NewValue);
                if (Math.Abs(newPosition.TotalSeconds - MediaElementPlayer.Position.TotalSeconds) > 1.1 && ProgressBar.IsMouseOver)
                {
                    MediaElementPlayer.Position = newPosition;
                }
            }
        }

        private void VolumeSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (MediaElementPlayer != null)
            {
                MediaElementPlayer.Volume = VolumeSlider.Value;
            }
            if (VolumePercentageText != null)
            {
                VolumePercentageText.Text = $"{(int)(VolumeSlider.Value * 100)}%";
            }
        }

        // Add new methods for improved UX
        private void SearchTextBox_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == System.Windows.Input.Key.Enter)
            {
                SearchButton_Click(sender, new RoutedEventArgs());
            }
        }

        private void TitleBar_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (e.ButtonState == System.Windows.Input.MouseButtonState.Pressed)
            {
                this.DragMove();
            }
        }

        private void SettingsButton_Click(object sender, RoutedEventArgs e)
        {
            var settingsWindow = new SettingsWindow(_config);
            settingsWindow.Owner = this;
            if (settingsWindow.ShowDialog() == true)
            {
                // Settings were saved, reload config
                _config = AppConfig.Load();
                
                // Update status indicator
                if (string.IsNullOrEmpty(_config.YouTubeApiKey))
                {
                    StatusIndicator.Fill = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.Orange);
                    StatusText.Text = "Ready - Configure API key in Settings";
                }
                else
                {
                    StatusIndicator.Fill = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.Green);
                    StatusText.Text = "Ready - API key configured";
                }
            }
        }

        private void Window_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            switch (e.Key)
            {
                case System.Windows.Input.Key.Space:
                    if (MediaElementPlayer.Source != null)
                    {
                        if (MediaElementPlayer.CanPause && MediaElementPlayer.Position != MediaElementPlayer.NaturalDuration.TimeSpan)
                        {
                            PauseButton_Click(sender, new RoutedEventArgs());
                        }
                        else
                        {
                            PlayButton_Click(sender, new RoutedEventArgs());
                        }
                    }
                    e.Handled = true;
                    break;
                case System.Windows.Input.Key.S when e.KeyboardDevice.Modifiers == System.Windows.Input.ModifierKeys.Control:
                    StopButton_Click(sender, new RoutedEventArgs());
                    e.Handled = true;
                    break;
                case System.Windows.Input.Key.L when e.KeyboardDevice.Modifiers == System.Windows.Input.ModifierKeys.Control:
                    LoopButton_Click(sender, new RoutedEventArgs());
                    e.Handled = true;
                    break;
                case System.Windows.Input.Key.F when e.KeyboardDevice.Modifiers == System.Windows.Input.ModifierKeys.Control:
                    SearchTextBox.Focus();
                    e.Handled = true;
                    break;
                case System.Windows.Input.Key.OemComma when e.KeyboardDevice.Modifiers == System.Windows.Input.ModifierKeys.Control:
                    SettingsButton_Click(sender, new RoutedEventArgs());
                    e.Handled = true;
                    break;
            }
        }

        private void ClearSearchButton_Click(object sender, RoutedEventArgs e)
        {
            SearchTextBox.Clear();
            ResultsListBox.Items.Clear();
            SearchTextBox.Focus();
        }
    }

    public class TrackInfo
    {
        public required string Source { get; set; }
        public required string Title { get; set; }
        public required string Url { get; set; }
        public string? VideoId { get; set; }
        public override string ToString() => $"{Source}: {Title}";
    }
}