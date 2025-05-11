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
        // VVVVVV  IMPORTANT: REPLACE THIS WITH YOUR ACTUAL, VALID YOUTUBE API KEY VVVVVV
        private const string YouTubeApiKey = "API KEY HERE"; // <<< REPLACE THIS WITH YOUR NEW KEY
        // ^^^^^^  IMPORTANT: REPLACE THIS WITH YOUR ACTUAL, VALID YOUTUBE API KEY ^^^^^^
        private DispatcherTimer _timer;
        private bool _isLooping = false;
        private YoutubeClient _youtubeClient;

        public MainWindow()
        {
            InitializeComponent();
            MediaElementPlayer.MediaOpened += MediaElementPlayer_MediaOpened;
            MediaElementPlayer.MediaEnded += MediaElementPlayer_MediaEnded;
            MediaElementPlayer.MediaFailed += MediaElementPlayer_MediaFailed;

            _timer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(1)
            };
            _timer.Tick += Timer_Tick;
            _youtubeClient = new YoutubeClient();
        }

        private void MediaElementPlayer_MediaFailed(object? sender, ExceptionRoutedEventArgs e)
        {
            MessageBox.Show($"Errore durante la riproduzione del media:\n{e.ErrorException.Message}", "Errore MediaElement", MessageBoxButton.OK, MessageBoxImage.Error);
            // Clean up any "Caricamento..." messages
            var loadingMessages = ResultsListBox.Items.OfType<string>().Where(s => s.StartsWith("Caricamento:")).ToList();
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
            // LoopButton.Content = _isLooping ? "Loop ON" : "Loop OFF";
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
                ResultsListBox.Items.Add("Inserisci un termine di ricerca.");
                return;
            }

            ResultsListBox.Items.Clear();
            ResultsListBox.Items.Add("Ricerca in corso...");

            try
            {
                var youtubeResults = await SearchYouTubeAsync(searchText);
                ResultsListBox.Items.Clear();

                if (youtubeResults != null && youtubeResults.Any())
                {
                    foreach (var track in youtubeResults)
                    {
                        ResultsListBox.Items.Add(track);
                    }
                }
                else
                {
                    ResultsListBox.Items.Add("Nessun risultato trovato per la tua ricerca.");
                }
            }
            catch (HttpRequestException httpEx)
            {
                ResultsListBox.Items.Clear();
                ResultsListBox.Items.Add($"Errore di rete o API: {httpEx.Message}");
                ResultsListBox.Items.Add("Verifica la tua connessione internet e la validità della API Key di YouTube.");
            }
            catch (JsonException jsonEx)
            {
                ResultsListBox.Items.Clear();
                ResultsListBox.Items.Add($"Errore nel formato dei dati ricevuti: {jsonEx.Message}");
            }
            catch (Exception ex)
            {
                ResultsListBox.Items.Clear();
                ResultsListBox.Items.Add($"Errore imprevisto durante la ricerca: {ex.Message}");
            }
        }

        private async Task<List<TrackInfo>> SearchYouTubeAsync(string query)
        {
            using var httpClient = new HttpClient();
            string url = $"https://www.googleapis.com/youtube/v3/search?part=snippet&type=video&maxResults=10&key={YouTubeApiKey}&q={Uri.EscapeDataString(query)}";
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
                string loadingMessage = $"Caricamento: {selectedTrack.Title}...";
                var existingLoadingMessage = ResultsListBox.Items.OfType<string>().FirstOrDefault(s => s.StartsWith("Caricamento:"));
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
                        MessageBox.Show($"Impossibile trovare uno stream audio/video valido per: {selectedTrack.Title}", "Errore Stream", MessageBoxButton.OK, MessageBoxImage.Warning);
                    }
                }
                catch (YoutubeExplode.Exceptions.VideoUnavailableException videoEx)
                {
                    ResultsListBox.Items.Remove(loadingMessage);
                    MessageBox.Show($"Video non disponibile: {selectedTrack.Title}\n{videoEx.Message}", "Errore YoutubeExplode", MessageBoxButton.OK, MessageBoxImage.Error);
                }
                catch (Exception ex)
                {
                    ResultsListBox.Items.Remove(loadingMessage);
                    MessageBox.Show($"Impossibile avviare la riproduzione per: {selectedTrack.Title}\nErrore: {ex.Message}", "Errore di Riproduzione", MessageBoxButton.OK, MessageBoxImage.Error);
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