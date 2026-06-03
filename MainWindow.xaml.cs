using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;

namespace TiclyMusic
{
    public partial class MainWindow : Window
    {
        private DispatcherTimer _timer;
        private bool _isLooping = false;
        private AppConfig _config;
        private readonly HttpClient _httpClient = new HttpClient();
        private string? _spotifyAccessToken;
        private DateTimeOffset _spotifyAccessTokenExpiresAt;
        private LyricsDocument? _currentLyrics;
        private LyricLine? _currentLyricLine;
        private int _currentLyricHighlightCount;

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
                Interval = TimeSpan.FromMilliseconds(100)
            };
            _timer.Tick += Timer_Tick;
            
            // Set initial volume
            VolumeSlider.Value = _config.Volume;
            VolumePercentageText.Text = $"{(int)(_config.Volume * 100)}%";
            
            // Check if API key is configured
            if (string.IsNullOrEmpty(_config.SpotifyClientId) || string.IsNullOrEmpty(_config.SpotifyClientSecret))
            {
                StatusIndicator.Fill = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.Orange);
                StatusText.Text = "Ready - Configure Spotify credentials";
                MessageBox.Show("Spotify client credentials are not configured. Please go to Settings to set up Client ID and Secret.", 
                    "Configuration Required", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            else if (string.IsNullOrEmpty(_config.SpotiFlacBaseUrl))
            {
                StatusIndicator.Fill = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.Orange);
                StatusText.Text = "Ready - Configure SpotiFLAC backend";
                MessageBox.Show("SpotiFLAC backend URL is not configured. Please set it in Settings to enable streaming and lyrics.", 
                    "Configuration Required", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            else
            {
                StatusIndicator.Fill = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.Green);
                StatusText.Text = "Ready - SpotiFLAC configured";
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
            ResetLyricsDisplay("Lyrics unavailable");
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
            UpdateLyricsDisplay(MediaElementPlayer.Position);
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
                ResetLyricsDisplay("Lyrics will appear here");
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
            ResetLyricsDisplay("Lyrics will appear here");
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
            StatusText.Text = "Searching Spotify...";

            try
            {
                var spotifyResults = await SearchSpotifyAsync(searchText);
                ResultsListBox.Items.Clear();
                
                // Reset status
                StatusIndicator.Fill = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.Green);
                StatusText.Text = "Ready - SpotiFLAC configured";

                if (spotifyResults != null && spotifyResults.Any())
                {
                    foreach (var track in spotifyResults)
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
                StatusText.Text = "Error - Check credentials";
                ResultsListBox.Items.Add($"Network or API error: {httpEx.Message}");
                ResultsListBox.Items.Add("Please check your Spotify credentials and SpotiFLAC backend URL.");
            }
            catch (InvalidOperationException configEx)
            {
                ResultsListBox.Items.Clear();
                StatusIndicator.Fill = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.Orange);
                StatusText.Text = "Ready - Configure settings";
                ResultsListBox.Items.Add(configEx.Message);
                ResultsListBox.Items.Add("Click the Settings button (⚙) to configure Spotify and SpotiFLAC.");
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

        private async Task<List<TrackInfo>> SearchSpotifyAsync(string query)
        {
            if (string.IsNullOrEmpty(_config.SpotifyClientId) || string.IsNullOrEmpty(_config.SpotifyClientSecret))
            {
                throw new InvalidOperationException("Spotify client credentials are not configured. Please set them in Settings.");
            }

            var token = await GetSpotifyAccessTokenAsync();
            string url = $"https://api.spotify.com/v1/search?type=track&limit=10&q={Uri.EscapeDataString(query)}";
            using var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            var response = await _httpClient.SendAsync(request);
            response.EnsureSuccessStatusCode();
            var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
            var items = json.RootElement.GetProperty("tracks").GetProperty("items");
            var tracks = new List<TrackInfo>();

            foreach (var item in items.EnumerateArray())
            {
                string? title = item.GetProperty("name").GetString();
                string? trackId = item.GetProperty("id").GetString();
                string? previewUrl = item.TryGetProperty("preview_url", out var previewProp) ? previewProp.GetString() : null;
                int durationMs = item.TryGetProperty("duration_ms", out var durationProp) ? durationProp.GetInt32() : 0;
                var artistNames = item.GetProperty("artists")
                    .EnumerateArray()
                    .Select(a => a.GetProperty("name").GetString())
                    .Where(name => !string.IsNullOrWhiteSpace(name))
                    .ToArray();
                string artist = artistNames.Length > 0 ? string.Join(", ", artistNames) : "Unknown Artist";

                if (!string.IsNullOrEmpty(title) && !string.IsNullOrEmpty(trackId))
                {
                    tracks.Add(new TrackInfo
                    {
                        Source = "SpotiFLAC",
                        Title = title,
                        Artist = artist,
                        TrackId = trackId,
                        PreviewUrl = previewUrl,
                        DurationMs = durationMs
                    });
                }
            }
            return tracks;
        }

        private async Task<string> GetSpotifyAccessTokenAsync()
        {
            if (!string.IsNullOrEmpty(_spotifyAccessToken) && _spotifyAccessTokenExpiresAt > DateTimeOffset.UtcNow.AddMinutes(1))
            {
                return _spotifyAccessToken;
            }

            var authValue = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{_config.SpotifyClientId}:{_config.SpotifyClientSecret}"));
            using var request = new HttpRequestMessage(HttpMethod.Post, "https://accounts.spotify.com/api/token");
            request.Headers.Authorization = new AuthenticationHeaderValue("Basic", authValue);
            request.Content = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("grant_type", "client_credentials")
            });

            var response = await _httpClient.SendAsync(request);
            response.EnsureSuccessStatusCode();
            var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
            _spotifyAccessToken = json.RootElement.GetProperty("access_token").GetString();
            var expiresIn = json.RootElement.GetProperty("expires_in").GetInt32();
            _spotifyAccessTokenExpiresAt = DateTimeOffset.UtcNow.AddSeconds(expiresIn);

            if (string.IsNullOrEmpty(_spotifyAccessToken))
            {
                throw new InvalidOperationException("Spotify authentication failed. Access token missing.");
            }

            return _spotifyAccessToken;
        }

        private async Task<string?> GetSpotiFlacStreamUrlAsync(TrackInfo track)
        {
            if (string.IsNullOrWhiteSpace(_config.SpotiFlacBaseUrl))
            {
                throw new InvalidOperationException("SpotiFLAC backend URL is not configured. Please set it in Settings.");
            }

            var requestUrl = $"{_config.SpotiFlacBaseUrl.TrimEnd('/')}/stream?trackId={Uri.EscapeDataString(track.TrackId)}";
            using var request = new HttpRequestMessage(HttpMethod.Get, requestUrl);
            if (!string.IsNullOrWhiteSpace(_config.SpotiFlacApiKey))
            {
                request.Headers.Add("X-Api-Key", _config.SpotiFlacApiKey);
            }

            var response = await _httpClient.SendAsync(request);
            response.EnsureSuccessStatusCode();
            var content = await response.Content.ReadAsStringAsync();
            var streamUrl = ExtractStreamUrl(content);

            if (string.IsNullOrWhiteSpace(streamUrl) && !string.IsNullOrWhiteSpace(track.PreviewUrl))
            {
                StatusIndicator.Fill = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.Orange);
                StatusText.Text = "SpotiFLAC stream unavailable - playing Spotify preview";
                return track.PreviewUrl;
            }

            return streamUrl;
        }

        private static string? ExtractStreamUrl(string content)
        {
            if (string.IsNullOrWhiteSpace(content))
            {
                return null;
            }

            try
            {
                var json = JsonDocument.Parse(content);
                var root = json.RootElement;
                if (root.ValueKind == JsonValueKind.String)
                {
                    return root.GetString();
                }

                if (root.TryGetProperty("url", out var urlProp))
                {
                    return urlProp.GetString();
                }

                if (root.TryGetProperty("streamUrl", out var streamUrlProp))
                {
                    return streamUrlProp.GetString();
                }

                if (root.TryGetProperty("stream_url", out var streamUrlSnakeProp))
                {
                    return streamUrlSnakeProp.GetString();
                }
            }
            catch (JsonException)
            {
                return null;
            }

            return null;
        }

        private async Task LoadLyricsAsync(TrackInfo track)
        {
            ResetLyricsDisplay("Loading lyrics...");
            _currentLyrics = await GetSpotiFlacLyricsAsync(track.TrackId);
            _currentLyricLine = null;
            _currentLyricHighlightCount = 0;

            if (_currentLyrics == null || _currentLyrics.Lines.Count == 0)
            {
                ResetLyricsDisplay("Lyrics unavailable");
                return;
            }

            UpdateLyricsDisplay(MediaElementPlayer.Position);
        }

        private async Task<LyricsDocument?> GetSpotiFlacLyricsAsync(string trackId)
        {
            if (string.IsNullOrWhiteSpace(_config.SpotiFlacBaseUrl))
            {
                return null;
            }

            var requestUrl = $"{_config.SpotiFlacBaseUrl.TrimEnd('/')}/lyrics?trackId={Uri.EscapeDataString(trackId)}";
            using var request = new HttpRequestMessage(HttpMethod.Get, requestUrl);
            if (!string.IsNullOrWhiteSpace(_config.SpotiFlacApiKey))
            {
                request.Headers.Add("X-Api-Key", _config.SpotiFlacApiKey);
            }

            var response = await _httpClient.SendAsync(request);
            if (!response.IsSuccessStatusCode)
            {
                return null;
            }

            var content = await response.Content.ReadAsStringAsync();
            return LyricsDocument.Parse(content);
        }

        private void UpdateLyricsDisplay(TimeSpan position)
        {
            if (_currentLyrics == null || _currentLyrics.Lines.Count == 0)
            {
                return;
            }

            var line = _currentLyrics.GetLineForTime(position);
            if (line == null || string.IsNullOrEmpty(line.Text))
            {
                return;
            }

            int highlightCount = GetHighlightCount(line, position);
            if (line == _currentLyricLine && highlightCount == _currentLyricHighlightCount)
            {
                return;
            }

            _currentLyricLine = line;
            _currentLyricHighlightCount = highlightCount;
            RenderLyricsLine(line, highlightCount);
        }

        private int GetHighlightCount(LyricLine line, TimeSpan position)
        {
            if (line.Characters != null && line.Characters.Count > 0)
            {
                return line.Characters.Count(c => position >= c.Time);
            }

            if (line.End.HasValue && line.End.Value > line.Start && !string.IsNullOrEmpty(line.Text))
            {
                var totalMs = (line.End.Value - line.Start).TotalMilliseconds;
                var elapsedMs = (position - line.Start).TotalMilliseconds;
                var progress = Math.Clamp(elapsedMs / totalMs, 0, 1);
                return (int)Math.Round(progress * line.Text.Length, MidpointRounding.AwayFromZero);
            }

            return position >= line.Start && !string.IsNullOrEmpty(line.Text) ? line.Text.Length : 0;
        }

        private void RenderLyricsLine(LyricLine line, int highlightCount)
        {
            LyricsTextBlock.Inlines.Clear();
            var text = line.Text ?? string.Empty;
            highlightCount = Math.Clamp(highlightCount, 0, text.Length);
            string highlighted = text.Substring(0, highlightCount);
            string remaining = text.Substring(highlightCount);

            LyricsTextBlock.Inlines.Add(new System.Windows.Documents.Run(highlighted)
            {
                Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(76, 175, 80))
            });
            LyricsTextBlock.Inlines.Add(new System.Windows.Documents.Run(remaining)
            {
                Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(120, 120, 120))
            });
        }

        private void ResetLyricsDisplay(string message)
        {
            _currentLyrics = null;
            _currentLyricLine = null;
            _currentLyricHighlightCount = 0;
            LyricsTextBlock.Inlines.Clear();
            LyricsTextBlock.Text = message;
        }

        private async void ResultsListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ResultsListBox.SelectedItem is TrackInfo selectedTrack && !string.IsNullOrEmpty(selectedTrack.TrackId))
            {
                string loadingMessage = $"Loading: {selectedTrack.Title}...";
                var existingLoadingMessage = ResultsListBox.Items.OfType<string>().FirstOrDefault(s => s.StartsWith("Loading:"));
                if (existingLoadingMessage != null) ResultsListBox.Items.Remove(existingLoadingMessage);
                ResultsListBox.Items.Add(loadingMessage);

                try
                {
                    var streamUrl = await GetSpotiFlacStreamUrlAsync(selectedTrack);
                    ResultsListBox.Items.Remove(loadingMessage);

                    if (!string.IsNullOrEmpty(streamUrl))
                    {
                        MediaElementPlayer.Stop();
                        MediaElementPlayer.Source = new Uri(streamUrl);
                        MediaElementPlayer.Play();
                        await LoadLyricsAsync(selectedTrack);
                    }
                    else
                    {
                        MessageBox.Show($"Unable to find a valid stream for: {selectedTrack.Title}", "Stream Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                    }
                }
                catch (InvalidOperationException configEx)
                {
                    ResultsListBox.Items.Remove(loadingMessage);
                    MessageBox.Show(configEx.Message, "Configuration Required", MessageBoxButton.OK, MessageBoxImage.Information);
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
                if (string.IsNullOrEmpty(_config.SpotifyClientId) || string.IsNullOrEmpty(_config.SpotifyClientSecret))
                {
                    StatusIndicator.Fill = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.Orange);
                    StatusText.Text = "Ready - Configure Spotify credentials";
                }
                else if (string.IsNullOrEmpty(_config.SpotiFlacBaseUrl))
                {
                    StatusIndicator.Fill = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.Orange);
                    StatusText.Text = "Ready - Configure SpotiFLAC backend";
                }
                else
                {
                    StatusIndicator.Fill = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.Green);
                    StatusText.Text = "Ready - SpotiFLAC configured";
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
        public required string Artist { get; set; }
        public required string TrackId { get; set; }
        public string? PreviewUrl { get; set; }
        public int DurationMs { get; set; }
        public override string ToString() => $"{Source}: {Title} — {Artist}";
    }

    public class LyricsDocument
    {
        public List<LyricLine> Lines { get; }

        public LyricsDocument(List<LyricLine> lines)
        {
            Lines = lines;
            Lines.Sort((a, b) => a.Start.CompareTo(b.Start));
            NormalizeEndTimes();
        }

        public LyricLine? GetLineForTime(TimeSpan position)
        {
            return Lines.LastOrDefault(line => position >= line.Start && (!line.End.HasValue || position < line.End.Value));
        }

        public static LyricsDocument? Parse(string content)
        {
            if (string.IsNullOrWhiteSpace(content))
            {
                return null;
            }

            if (TryParseJson(content, out var jsonLyrics))
            {
                return jsonLyrics;
            }

            return ParseLrc(content);
        }

        private void NormalizeEndTimes()
        {
            for (int i = 0; i < Lines.Count - 1; i++)
            {
                Lines[i].End = Lines[i + 1].Start;
            }
        }

        private static bool TryParseJson(string content, out LyricsDocument? document)
        {
            document = null;
            try
            {
                var json = JsonDocument.Parse(content);
                var root = json.RootElement;

                if (root.TryGetProperty("lyrics", out var lyricsProp) && lyricsProp.ValueKind == JsonValueKind.String)
                {
                    document = ParseLrc(lyricsProp.GetString() ?? string.Empty);
                    return true;
                }

                if (root.TryGetProperty("lrc", out var lrcProp) && lrcProp.ValueKind == JsonValueKind.String)
                {
                    document = ParseLrc(lrcProp.GetString() ?? string.Empty);
                    return true;
                }

                if (root.TryGetProperty("lines", out var linesProp) && linesProp.ValueKind == JsonValueKind.Array)
                {
                    var lines = new List<LyricLine>();
                    foreach (var lineElement in linesProp.EnumerateArray())
                    {
                        var text = lineElement.TryGetProperty("text", out var textProp) ? textProp.GetString() : null;
                        var start = ReadMilliseconds(lineElement, "startMs", "startTimeMs", "start");
                        var end = ReadMilliseconds(lineElement, "endMs", "endTimeMs", "end");
                        var chars = ReadLetters(lineElement);
                        if (start.HasValue && !string.IsNullOrWhiteSpace(text))
                        {
                            lines.Add(new LyricLine
                            {
                                Start = TimeSpan.FromMilliseconds(start.Value),
                                End = end.HasValue ? TimeSpan.FromMilliseconds(end.Value) : null,
                                Text = text ?? string.Empty,
                                Characters = chars
                            });
                        }
                    }

                    if (lines.Count > 0)
                    {
                        document = new LyricsDocument(lines);
                        return true;
                    }
                }
            }
            catch (JsonException)
            {
                return false;
            }

            return false;
        }

        private static List<LyricChar>? ReadLetters(JsonElement lineElement)
        {
            if (!lineElement.TryGetProperty("letters", out var lettersProp) || lettersProp.ValueKind != JsonValueKind.Array)
            {
                return null;
            }

            var letters = new List<LyricChar>();
            foreach (var letterElement in lettersProp.EnumerateArray())
            {
                string? text = letterElement.TryGetProperty("char", out var charProp) ? charProp.GetString() :
                               letterElement.TryGetProperty("text", out var textProp) ? textProp.GetString() : null;
                var timeMs = ReadMilliseconds(letterElement, "timeMs", "startMs", "startTimeMs");
                if (!string.IsNullOrEmpty(text) && timeMs.HasValue)
                {
                    letters.Add(new LyricChar
                    {
                        Character = text[0],
                        Time = TimeSpan.FromMilliseconds(timeMs.Value)
                    });
                }
            }

            return letters.Count > 0 ? letters : null;
        }

        private static double? ReadMilliseconds(JsonElement element, params string[] names)
        {
            foreach (var name in names)
            {
                if (element.TryGetProperty(name, out var prop) && prop.ValueKind == JsonValueKind.Number && prop.TryGetDouble(out var value))
                {
                    return value;
                }
            }

            return null;
        }

        private static LyricsDocument? ParseLrc(string lrc)
        {
            var lines = new List<LyricLine>();
            var lineRegex = new Regex(@"\[(\d{1,2}):(\d{2})(?:\.(\d{1,3}))?\]");
            var inlineRegex = new Regex(@"<(\d{1,2}):(\d{2})(?:\.(\d{1,3}))?>");

            foreach (var rawLine in lrc.Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries))
            {
                var matches = lineRegex.Matches(rawLine);
                if (matches.Count == 0)
                {
                    continue;
                }

                var text = lineRegex.Replace(rawLine, "").Trim();
                foreach (Match match in matches)
                {
                    var start = ParseTimestamp(match.Groups[1].Value, match.Groups[2].Value, match.Groups[3].Value);
                    if (start == null)
                    {
                        continue;
                    }

                    var (cleanText, characters) = ParseInlineTags(text, inlineRegex);
                    lines.Add(new LyricLine
                    {
                        Start = start.Value,
                        Text = cleanText,
                        Characters = characters
                    });
                }
            }

            return lines.Count > 0 ? new LyricsDocument(lines) : null;
        }

        private static (string text, List<LyricChar>? characters) ParseInlineTags(string input, Regex inlineRegex)
        {
            var matches = inlineRegex.Matches(input);
            if (matches.Count == 0)
            {
                return (input, null);
            }

            var characters = new List<LyricChar>();
            TimeSpan? currentTime = null;
            int lastIndex = 0;

            foreach (Match match in matches)
            {
                var segment = input.Substring(lastIndex, match.Index - lastIndex);
                if (currentTime.HasValue)
                {
                    foreach (var ch in segment)
                    {
                        characters.Add(new LyricChar { Character = ch, Time = currentTime.Value });
                    }
                }

                currentTime = ParseTimestamp(match.Groups[1].Value, match.Groups[2].Value, match.Groups[3].Value);
                lastIndex = match.Index + match.Length;
            }

            var tail = input.Substring(lastIndex);
            if (currentTime.HasValue)
            {
                foreach (var ch in tail)
                {
                    characters.Add(new LyricChar { Character = ch, Time = currentTime.Value });
                }
            }

            var cleaned = inlineRegex.Replace(input, "").Trim();
            return (cleaned, characters.Count > 0 ? characters : null);
        }

        private static TimeSpan? ParseTimestamp(string minutesValue, string secondsValue, string millisecondsValue)
        {
            if (!int.TryParse(minutesValue, out var minutes) || !int.TryParse(secondsValue, out var seconds))
            {
                return null;
            }

            int milliseconds = 0;
            if (!string.IsNullOrEmpty(millisecondsValue))
            {
                milliseconds = int.TryParse(millisecondsValue.PadRight(3, '0'), out var ms) ? ms : 0;
            }

            return new TimeSpan(0, 0, minutes, seconds, milliseconds);
        }
    }

    public class LyricLine
    {
        public TimeSpan Start { get; set; }
        public TimeSpan? End { get; set; }
        public string Text { get; set; } = string.Empty;
        public List<LyricChar>? Characters { get; set; }
    }

    public class LyricChar
    {
        public char Character { get; set; }
        public TimeSpan Time { get; set; }
    }
}