using System.IO;
using System.Windows.Media.Imaging;
using Windows.Foundation;
using Windows.Media;
using Windows.Media.Control;
using Windows.Storage.Streams;
using Session = Windows.Media.Control.GlobalSystemMediaTransportControlsSession;

namespace SpotifyTaskbarVisualizer;

// Wraps the Windows media session API (SMTC). Every event is posted to the UI thread,
// so all state below is only ever touched from there.
public sealed class MediaService
{
    private readonly GlobalSystemMediaTransportControlsSessionManager _manager;
    private readonly SynchronizationContext _ui;
    private Session? _session;
    private TimeSpan _start;
    private TimeSpan _position;
    private DateTimeOffset _positionUpdated;
    private int _mediaVersion;
    private bool _seekEnabled;

    public event Action? Changed;

    public bool HasSession => _session != null;
    public string Title { get; private set; } = "";
    public string Artist { get; private set; } = "";
    public BitmapImage? Cover { get; private set; }
    public bool IsPlaying { get; private set; }
    public TimeSpan Duration { get; private set; }
    public bool CanSeek => _seekEnabled && Duration > TimeSpan.Zero;
    public bool CanShuffle { get; private set; }
    public bool IsShuffle { get; private set; }
    public bool CanRepeat { get; private set; }
    public MediaPlaybackAutoRepeatMode RepeatMode { get; private set; }

    private MediaService(GlobalSystemMediaTransportControlsSessionManager manager, SynchronizationContext ui)
    {
        _manager = manager;
        _ui = ui;
        _manager.SessionsChanged += (_, _) => Post(SelectSession);
        _manager.CurrentSessionChanged += (_, _) => Post(SelectSession);
    }

    // Must be called on the UI thread.
    public static async Task<MediaService> CreateAsync()
    {
        var ui = SynchronizationContext.Current ?? throw new InvalidOperationException("Call from the UI thread.");
        var service = new MediaService(await GlobalSystemMediaTransportControlsSessionManager.RequestAsync(), ui);
        service.SelectSession();
        return service;
    }

    // Spotify only reports the position on changes, so extrapolate while playing.
    public TimeSpan GetPosition()
    {
        var position = IsPlaying ? _position + (DateTimeOffset.Now - _positionUpdated) : _position;
        return TimeSpan.FromTicks(Math.Clamp(position.Ticks, 0, Duration.Ticks));
    }

    public void TogglePlayPause() => Send(s => s.TryTogglePlayPauseAsync());
    public void Next() => Send(s => s.TrySkipNextAsync());
    public void Previous() => Send(s => s.TrySkipPreviousAsync());
    public void ToggleShuffle() => Send(s => s.TryChangeShuffleActiveAsync(!IsShuffle));

    // fraction: 0..1 of the track length. The local position jumps immediately; the next timeline event resyncs.
    public void Seek(double fraction)
    {
        if (!CanSeek) return;
        var target = TimeSpan.FromTicks((long)(Duration.Ticks * Math.Clamp(fraction, 0, 1)));
        _position = target;
        _positionUpdated = DateTimeOffset.Now;
        Changed?.Invoke();
        Send(s => s.TryChangePlaybackPositionAsync((_start + target).Ticks));
    }

    public void CycleRepeat() => Send(s => s.TryChangeAutoRepeatModeAsync(RepeatMode switch
    {
        MediaPlaybackAutoRepeatMode.None => MediaPlaybackAutoRepeatMode.List,
        MediaPlaybackAutoRepeatMode.List => MediaPlaybackAutoRepeatMode.Track,
        _ => MediaPlaybackAutoRepeatMode.None,
    }));

    private void Post(Action action) => _ui.Post(_ => action(), null);

    private async void Send(Func<Session, IAsyncOperation<bool>> command)
    {
        if (_session == null) return;
        try { await command(_session); }
        catch (Exception) { } // Session vanished; the next event updates the state.
    }

    private void SelectSession()
    {
        Session? session;
        try
        {
            session = _manager.GetSessions().FirstOrDefault(s => s.SourceAppUserModelId.Contains("Spotify", StringComparison.OrdinalIgnoreCase))
                ?? _manager.GetCurrentSession();
        }
        catch (Exception) { return; }

        if (ReferenceEquals(session, _session)) return;

        if (_session != null)
        {
            try
            {
                _session.MediaPropertiesChanged -= OnMediaPropertiesChanged;
                _session.PlaybackInfoChanged -= OnPlaybackInfoChanged;
                _session.TimelinePropertiesChanged -= OnTimelinePropertiesChanged;
            }
            catch (Exception) { }
        }

        _session = session;
        if (session != null)
        {
            session.MediaPropertiesChanged += OnMediaPropertiesChanged;
            session.PlaybackInfoChanged += OnPlaybackInfoChanged;
            session.TimelinePropertiesChanged += OnTimelinePropertiesChanged;
        }

        UpdateTimeline();
        UpdatePlayback();
        Changed?.Invoke();
        _ = UpdateMediaAsync();
    }

    private void OnMediaPropertiesChanged(Session sender, MediaPropertiesChangedEventArgs args) =>
        Post(() => _ = UpdateMediaAsync());

    private void OnPlaybackInfoChanged(Session sender, PlaybackInfoChangedEventArgs args) =>
        Post(() => { UpdatePlayback(); Changed?.Invoke(); });

    private void OnTimelinePropertiesChanged(Session sender, TimelinePropertiesChangedEventArgs args) =>
        Post(() => { UpdateTimeline(); Changed?.Invoke(); });

    private void UpdatePlayback()
    {
        try
        {
            var info = _session?.GetPlaybackInfo();
            var position = GetPosition();
            var wasPlaying = IsPlaying;

            IsPlaying = info?.PlaybackStatus == GlobalSystemMediaTransportControlsSessionPlaybackStatus.Playing;
            _seekEnabled = info?.Controls.IsPlaybackPositionEnabled == true;
            CanShuffle = info?.Controls.IsShuffleEnabled == true;
            IsShuffle = info?.IsShuffleActive == true;
            CanRepeat = info?.Controls.IsRepeatEnabled == true;
            RepeatMode = info?.AutoRepeatMode ?? MediaPlaybackAutoRepeatMode.None;

            // Play/pause often arrives without a timeline update: rebase the extrapolation.
            if (IsPlaying != wasPlaying)
            {
                _position = position;
                _positionUpdated = DateTimeOffset.Now;
            }
        }
        catch (Exception) { }
    }

    private void UpdateTimeline()
    {
        try
        {
            var timeline = _session?.GetTimelineProperties();
            Duration = timeline == null ? TimeSpan.Zero : TimeSpan.FromTicks(Math.Max(0, (timeline.EndTime - timeline.StartTime).Ticks));
            _start = timeline?.StartTime ?? TimeSpan.Zero;
            _position = timeline == null ? TimeSpan.Zero : timeline.Position - timeline.StartTime;
            _positionUpdated = timeline?.LastUpdatedTime ?? DateTimeOffset.Now;
        }
        catch (Exception) { }
    }

    private async Task UpdateMediaAsync()
    {
        var version = ++_mediaVersion;
        try
        {
            var properties = _session == null ? null : await _session.TryGetMediaPropertiesAsync();
            var cover = properties?.Thumbnail == null ? null : await LoadCoverAsync(properties.Thumbnail);
            if (version != _mediaVersion) return; // A newer update is already running.

            Title = properties?.Title ?? "";
            Artist = properties?.Artist ?? "";
            Cover = cover;
            Changed?.Invoke();
        }
        catch (Exception) { }
    }

    private static async Task<BitmapImage?> LoadCoverAsync(IRandomAccessStreamReference thumbnail)
    {
        try
        {
            using var stream = await thumbnail.OpenReadAsync();
            using var memory = new MemoryStream();
            await stream.AsStreamForRead().CopyToAsync(memory);
            memory.Position = 0;

            var image = new BitmapImage();
            image.BeginInit();
            image.CacheOption = BitmapCacheOption.OnLoad;
            image.StreamSource = memory;
            image.EndInit();
            image.Freeze();
            return image;
        }
        catch (Exception) { return null; }
    }
}
