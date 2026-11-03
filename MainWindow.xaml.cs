using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using Windows.Media;

namespace SpotifyTaskbarVisualizer;

public partial class MainWindow : Window
{
    private readonly DispatcherTimer _timer = new() { Interval = TimeSpan.FromMilliseconds(500) };
    private MediaService? _media;

    public MainWindow()
    {
        InitializeComponent();
        _timer.Tick += (_, _) => RenderPosition();
        Loaded += async (_, _) =>
        {
            _media = await MediaService.CreateAsync();
            _media.Changed += Render;
            Render();
        };
    }

    private void Render()
    {
        var media = _media!;
        Player.Visibility = media.HasSession ? Visibility.Visible : Visibility.Collapsed;
        EmptyText.Visibility = media.HasSession ? Visibility.Collapsed : Visibility.Visible;

        CoverImage.Source = media.Cover;
        TitleText.Text = media.Title;
        ArtistText.Text = media.Artist;
        PlayPauseButton.Content = media.IsPlaying ? "" : "";

        ShuffleButton.Visibility = media.CanShuffle ? Visibility.Visible : Visibility.Collapsed;
        ShuffleButton.Opacity = media.IsShuffle ? 1 : 0.5;
        RepeatButton.Visibility = media.CanRepeat ? Visibility.Visible : Visibility.Collapsed;
        RepeatButton.Content = media.RepeatMode == MediaPlaybackAutoRepeatMode.Track ? "" : "";
        RepeatButton.Opacity = media.RepeatMode == MediaPlaybackAutoRepeatMode.None ? 0.5 : 1;

        DurationText.Text = Format(media.Duration);
        SeekArea.Cursor = media.CanSeek ? Cursors.Hand : null;
        _timer.IsEnabled = media.IsPlaying;
        RenderPosition();
    }

    private void RenderPosition()
    {
        var media = _media!;
        var position = media.GetPosition();
        PositionText.Text = Format(position);
        Progress.Value = media.Duration > TimeSpan.Zero ? position / media.Duration : 0;
    }

    private static string Format(TimeSpan time) => $"{(int)time.TotalMinutes}:{time.Seconds:00}";

    private void PlayPause_Click(object sender, RoutedEventArgs e) => _media?.TogglePlayPause();
    private void Next_Click(object sender, RoutedEventArgs e) => _media?.Next();
    private void Previous_Click(object sender, RoutedEventArgs e) => _media?.Previous();
    private void Shuffle_Click(object sender, RoutedEventArgs e) => _media?.ToggleShuffle();
    private void Repeat_Click(object sender, RoutedEventArgs e) => _media?.CycleRepeat();

    private void SeekArea_MouseLeftButtonDown(object sender, MouseButtonEventArgs e) =>
        _media?.Seek(e.GetPosition(SeekArea).X / SeekArea.ActualWidth);
}
