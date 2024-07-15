using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.IO;
using System.Threading.Tasks;
using YoutubeExplode;
using YoutubeExplode.Videos.Streams;
using System.Net.Http;
using System.Text.RegularExpressions;

namespace Vulcan
{
    public partial class MainWindow : Window
    {
        private readonly HttpClient httpClient;
        private readonly YoutubeClient youtubeClient;

        public MainWindow()
        {
            InitializeComponent();

            youtubeClient = new YoutubeClient();
            httpClient = new HttpClient();
            DownloadButton.Click += DownloadButton_Click; // Attach event handler to the DownloadButton
        }

        public async void DownloadButton_Click(object sender, RoutedEventArgs e)
        {
            string videoUrl = UrlTextBox.Text;

            try
            {
                var video = await youtubeClient.Videos.GetAsync(videoUrl);
                var title = video.Title;
                var author = video.Author.ChannelTitle;
                var duration = video.Duration;
                var description = video.Description; // Get the description of the video

                var streamManifest = await youtubeClient.Videos.Streams.GetManifestAsync(videoUrl);
                var streamInfo = streamManifest.GetMuxedStreams().GetWithHighestVideoQuality();

                if (streamInfo != null)
                {
                    var stream = await youtubeClient.Videos.Streams.GetAsync(streamInfo);

                    var videoId = GetYouTubeVideoId(videoUrl);
                    var thumbnailUrl = $"https://img.youtube.com/vi/{videoId}/maxresdefault.jpg";

                    // Download the thumbnail image
                    using (var response = await httpClient.GetAsync(thumbnailUrl))
                    {
                        response.EnsureSuccessStatusCode();
                        using (var streamImage = await response.Content.ReadAsStreamAsync())
                        {
                            BitmapImage thumbnailImage = new BitmapImage();
                            thumbnailImage.BeginInit();
                            thumbnailImage.CacheOption = BitmapCacheOption.OnLoad;
                            thumbnailImage.StreamSource = streamImage; // Use the downloaded stream directly
                            thumbnailImage.EndInit();

                            // Ensure the UI updates happen on the UI thread
                            Dispatcher.Invoke(() =>
                            {
                                ThumbnailImage.Source = thumbnailImage; // Set the Image's Source property

                                // Set the video name to the DownloadedVideoName textblock
                                DownloadedVideoName.Text = title;

                                // Set the video duration to the VideoDuration textblock
                                VideoDuration.Text = duration.ToString();

                                // Set the video description to the VideoDescription textblock
                                VideoDescription.Text = description;
                            });
                        }
                    }

                    // Open file save dialog to choose the save location
                    var saveFileDialog = new Microsoft.Win32.SaveFileDialog();
                    saveFileDialog.FileName = $"{video.Title}.{streamInfo.Container}";
                    if (saveFileDialog.ShowDialog() == true)
                    {
                        string savePath = saveFileDialog.FileName;
                        await using (var fileStream = File.OpenWrite(savePath))
                        {
                            await stream.CopyToAsync(fileStream);
                        }
                    }
                }
            }

            catch (Exception ex)
            {
                MessageBox.Show($"Error: {ex.Message}");
            }

            string GetYouTubeVideoId(string videoUrl)
            {
                // Extract video ID from the YouTube URL
                Regex regex =
                    new Regex(
                        @"(?:youtube\.com\/(?:[^\/\n\s]+\/\S+\/|(?:v|e(?:mbed)?)\/|\S*?[?&]v(?:i)?=))([^\/\n\s&]+)");
                Match match = regex.Match(videoUrl);
                if (match.Success)
                {
                    return match.Groups[1].Value;
                }
                else
                {
                    throw new ArgumentException("Invalid YouTube video URL");
                }
            }
        }
    }
}