using System;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Text;
using System.Threading;

namespace Insait_Audio_Player.Services;

[SupportedOSPlatform("windows")]
public sealed class WinMmAudioPlayer : IDisposable
{
    private const string Alias = "insait_player";

    private enum PlaybackBackend
    {
        None,
        Mci,
        WindowsMediaPlayer
    }

    private bool _isPaused;
    private PlaybackBackend _backend;
    private object? _windowsMediaPlayer;
    private TimeSpan _positionSnapshot = TimeSpan.Zero;
    private DateTimeOffset? _playbackStartedAtUtc;

    public double PlaybackRate { get; private set; } = 1.0;

    public bool HasLoadedTrack { get; private set; }

    public string? LoadedFilePath { get; private set; }

    public bool Load(string filePath, out string? errorMessage)
    {
        errorMessage = null;
        CloseCurrentTrack();

        if (TryLoadWithMci(filePath, out errorMessage))
        {
            return true;
        }

        return TryLoadWithWindowsMediaPlayer(filePath, out errorMessage);
    }

    public bool Play(out string? errorMessage)
    {
        errorMessage = null;

        if (!HasLoadedTrack)
        {
            errorMessage = "Спочатку оберіть MP3 файл.";
            return false;
        }

        return _backend switch
        {
            PlaybackBackend.Mci => PlayWithMci(out errorMessage),
            PlaybackBackend.WindowsMediaPlayer => PlayWithWindowsMediaPlayer(out errorMessage),
            _ => false
        };
    }

    public bool Pause(out string? errorMessage)
    {
        errorMessage = null;

        if (!HasLoadedTrack)
        {
            return true;
        }

        return _backend switch
        {
            PlaybackBackend.Mci => PauseWithMci(out errorMessage),
            PlaybackBackend.WindowsMediaPlayer => PauseWithWindowsMediaPlayer(out errorMessage),
            _ => true
        };
    }

    public bool Stop(out string? errorMessage)
    {
        errorMessage = null;

        if (!HasLoadedTrack)
        {
            return true;
        }

        return _backend switch
        {
            PlaybackBackend.Mci => StopWithMci(out errorMessage),
            PlaybackBackend.WindowsMediaPlayer => StopWithWindowsMediaPlayer(out errorMessage),
            _ => true
        };
    }

    public bool Seek(TimeSpan position, out string? errorMessage)
    {
        errorMessage = null;

        if (!HasLoadedTrack)
        {
            return true;
        }

        return _backend switch
        {
            PlaybackBackend.Mci => SeekWithMci(position, out errorMessage),
            PlaybackBackend.WindowsMediaPlayer => SeekWithWindowsMediaPlayer(position, out errorMessage),
            _ => true
        };
    }

    public void SetVolume(double volumePercent)
    {
        if (!HasLoadedTrack)
        {
            return;
        }

        switch (_backend)
        {
            case PlaybackBackend.Mci:
            {
                var volume = (int)Math.Round(Math.Clamp(volumePercent, 0, 100) * 10, MidpointRounding.AwayFromZero);
                TryExecute($"setaudio {Alias} volume to {volume}", out _);
                break;
            }
            case PlaybackBackend.WindowsMediaPlayer:
            {
                try
                {
                    dynamic player = GetOrCreateWindowsMediaPlayer();
                    player.settings.volume = (int)Math.Round(Math.Clamp(volumePercent, 0, 100), MidpointRounding.AwayFromZero);
                }
                catch
                {
                    // ignored
                }
                break;
            }
        }
    }

    public bool SetPlaybackRate(double playbackRate, out string? errorMessage)
    {
        errorMessage = null;
        var normalizedRate = Math.Clamp(playbackRate, 0.5, 2.0);

        if (!HasLoadedTrack)
        {
            PlaybackRate = normalizedRate;
            return true;
        }

        switch (_backend)
        {
            case PlaybackBackend.Mci:
            {
                if (TryExecute($"set {Alias} speed to {ToMciSpeed(normalizedRate)}", out errorMessage))
                {
                    PlaybackRate = normalizedRate;
                    return true;
                }

                if (TrySwitchLoadedTrackToWindowsMediaPlayer(out var fallbackError) &&
                    TrySetWindowsMediaPlayerRate(normalizedRate, out errorMessage))
                {
                    PlaybackRate = normalizedRate;
                    return true;
                }

                errorMessage ??= fallbackError;
                return false;
            }
            case PlaybackBackend.WindowsMediaPlayer:
            {
                if (!TrySetWindowsMediaPlayerRate(normalizedRate, out errorMessage))
                {
                    return false;
                }

                PlaybackRate = normalizedRate;
                return true;
            }
            default:
                PlaybackRate = normalizedRate;
                return true;
        }
    }

    public TimeSpan GetPosition()
    {
        var reportedPosition = _backend switch
        {
            PlaybackBackend.Mci => TimeSpan.FromMilliseconds(QueryInteger($"status {Alias} position")),
            PlaybackBackend.WindowsMediaPlayer => TimeSpan.FromSeconds(GetWindowsMediaPlayerDouble(player => player.controls.currentPosition)),
            _ => TimeSpan.Zero
        };

        if (reportedPosition > TimeSpan.Zero)
        {
            _positionSnapshot = reportedPosition;
            if (_playbackStartedAtUtc.HasValue)
            {
                _playbackStartedAtUtc = DateTimeOffset.UtcNow;
            }

            return ClampToDuration(reportedPosition);
        }

        return ClampToDuration(GetEstimatedPosition());
    }

    public TimeSpan GetDuration()
    {
        return _backend switch
        {
            PlaybackBackend.Mci => TimeSpan.FromMilliseconds(QueryInteger($"status {Alias} length")),
            PlaybackBackend.WindowsMediaPlayer => TimeSpan.FromSeconds(GetWindowsMediaPlayerDouble(player => player.currentMedia?.duration)),
            _ => TimeSpan.Zero
        };
    }

    public string GetMode()
    {
        return _backend switch
        {
            PlaybackBackend.Mci => QueryString($"status {Alias} mode"),
            PlaybackBackend.WindowsMediaPlayer => GetWindowsMediaPlayerMode(),
            _ => string.Empty
        };
    }

    public bool TryGetDuration(string filePath, out TimeSpan duration)
    {
        duration = TimeSpan.Zero;
        var alias = $"probe_{Guid.NewGuid():N}";

        if (TryExecute($"open \"{EscapePath(filePath)}\" type mpegvideo alias {alias}", out _))
        {
            try
            {
                TryExecute($"set {alias} time format milliseconds", out _);
                duration = TimeSpan.FromMilliseconds(QueryInteger($"status {alias} length"));
                return duration >= TimeSpan.Zero;
            }
            finally
            {
                TryExecute($"close {alias}", out _);
            }
        }

        try
        {
            dynamic player = GetOrCreateWindowsMediaPlayer();
            dynamic media = player.newMedia(filePath);
            duration = TimeSpan.FromSeconds(ToDouble(media?.duration));
            return duration > TimeSpan.Zero;
        }
        catch
        {
            return false;
        }
    }

    public void Dispose()
    {
        CloseCurrentTrack();

        if (_windowsMediaPlayer is not null && Marshal.IsComObject(_windowsMediaPlayer))
        {
            Marshal.FinalReleaseComObject(_windowsMediaPlayer);
        }

        _windowsMediaPlayer = null;
    }

    private bool TryLoadWithMci(string filePath, out string? errorMessage)
    {
        errorMessage = null;

        if (!TryExecute($"open \"{EscapePath(filePath)}\" type mpegvideo alias {Alias}", out errorMessage) &&
            !TryExecute($"open \"{EscapePath(filePath)}\" alias {Alias}", out errorMessage))
        {
            return false;
        }

        TryExecute($"set {Alias} time format milliseconds", out _);
        TryExecute($"set {Alias} speed to {ToMciSpeed(PlaybackRate)}", out _);
        HasLoadedTrack = true;
        LoadedFilePath = filePath;
        _isPaused = false;
        _backend = PlaybackBackend.Mci;
        _positionSnapshot = TimeSpan.Zero;
        _playbackStartedAtUtc = null;
        return true;
    }

    private bool TryLoadWithWindowsMediaPlayer(string filePath, out string? errorMessage)
    {
        errorMessage = null;

        try
        {
            dynamic player = GetOrCreateWindowsMediaPlayer();
            player.settings.autoStart = false;
            player.URL = filePath;
            WaitForWindowsMediaPlayerMedia(player);
            player.controls.stop();
            player.settings.rate = PlaybackRate;

            HasLoadedTrack = true;
            LoadedFilePath = filePath;
            _isPaused = false;
            _backend = PlaybackBackend.WindowsMediaPlayer;
            _positionSnapshot = TimeSpan.Zero;
            _playbackStartedAtUtc = null;
            return true;
        }
        catch (Exception exception)
        {
            errorMessage = exception.Message;
            return false;
        }
    }

    private bool PlayWithMci(out string? errorMessage)
    {
        errorMessage = null;

        var position = GetPosition();
        var duration = GetDuration();
        if (duration > TimeSpan.Zero && position >= duration.Subtract(TimeSpan.FromMilliseconds(250)))
        {
            Seek(TimeSpan.Zero, out _);
        }

        if (_isPaused)
        {
            if (!TryExecute($"resume {Alias}", out errorMessage))
            {
                return false;
            }
        }
        else
        {
            var milliseconds = (int)Math.Max(0, position.TotalMilliseconds);
            var command = milliseconds > 0 ? $"play {Alias} from {milliseconds}" : $"play {Alias}";
            if (!TryExecute(command, out errorMessage))
            {
                return false;
            }
        }

        _isPaused = false;
        StartPositionTracking(position);
        return true;
    }

    private bool PlayWithWindowsMediaPlayer(out string? errorMessage)
    {
        errorMessage = null;

        try
        {
            dynamic player = GetOrCreateWindowsMediaPlayer();
            player.settings.rate = PlaybackRate;
            player.controls.play();
            _isPaused = false;
            StartPositionTracking(_positionSnapshot);
            return true;
        }
        catch (Exception exception)
        {
            errorMessage = exception.Message;
            return false;
        }
    }

    private bool PauseWithMci(out string? errorMessage)
    {
        errorMessage = null;
        if (!TryExecute($"pause {Alias}", out errorMessage))
        {
            return false;
        }

        _isPaused = true;
        FreezePositionTracking();
        return true;
    }

    private bool PauseWithWindowsMediaPlayer(out string? errorMessage)
    {
        errorMessage = null;

        try
        {
            dynamic player = GetOrCreateWindowsMediaPlayer();
            player.controls.pause();
            _isPaused = true;
            FreezePositionTracking();
            return true;
        }
        catch (Exception exception)
        {
            errorMessage = exception.Message;
            return false;
        }
    }

    private bool StopWithMci(out string? errorMessage)
    {
        errorMessage = null;
        if (!TryExecute($"stop {Alias}", out errorMessage))
        {
            return false;
        }

        TryExecute($"seek {Alias} to start", out _);
        _isPaused = false;
        _positionSnapshot = TimeSpan.Zero;
        _playbackStartedAtUtc = null;
        return true;
    }

    private bool StopWithWindowsMediaPlayer(out string? errorMessage)
    {
        errorMessage = null;

        try
        {
            dynamic player = GetOrCreateWindowsMediaPlayer();
            player.controls.stop();
            player.controls.currentPosition = 0;
            _isPaused = false;
            _positionSnapshot = TimeSpan.Zero;
            _playbackStartedAtUtc = null;
            return true;
        }
        catch (Exception exception)
        {
            errorMessage = exception.Message;
            return false;
        }
    }

    private bool SeekWithMci(TimeSpan position, out string? errorMessage)
    {
        errorMessage = null;
        var milliseconds = (int)Math.Max(0, Math.Min(GetDuration().TotalMilliseconds, position.TotalMilliseconds));
        var mode = GetMode();

        if (string.Equals(mode, "playing", StringComparison.OrdinalIgnoreCase))
        {
            if (!TryExecute($"play {Alias} from {milliseconds}", out errorMessage))
            {
                return false;
            }

            _isPaused = false;
            StartPositionTracking(TimeSpan.FromMilliseconds(milliseconds));
            return true;
        }

        if (!TryExecute($"seek {Alias} to {milliseconds}", out errorMessage))
        {
            return false;
        }

        _positionSnapshot = TimeSpan.FromMilliseconds(milliseconds);
        return true;
    }

    private bool SeekWithWindowsMediaPlayer(TimeSpan position, out string? errorMessage)
    {
        errorMessage = null;

        try
        {
            dynamic player = GetOrCreateWindowsMediaPlayer();
            var durationSeconds = GetDuration().TotalSeconds;
            var seconds = Math.Max(0, position.TotalSeconds);
            if (durationSeconds > 0)
            {
                seconds = Math.Min(durationSeconds, seconds);
            }

            player.controls.currentPosition = seconds;
            _positionSnapshot = TimeSpan.FromSeconds(seconds);
            if (!_isPaused)
            {
                StartPositionTracking(_positionSnapshot);
            }
            return true;
        }
        catch (Exception exception)
        {
            errorMessage = exception.Message;
            return false;
        }
    }

    private bool TrySwitchLoadedTrackToWindowsMediaPlayer(out string? errorMessage)
    {
        errorMessage = null;
        if (string.IsNullOrWhiteSpace(LoadedFilePath))
        {
            errorMessage = "Не вдалося переключити аудіо backend.";
            return false;
        }

        var currentPosition = GetPosition();
        var wasPlaying = string.Equals(GetMode(), "playing", StringComparison.OrdinalIgnoreCase);
        var currentFilePath = LoadedFilePath;

        CloseCurrentTrack();
        if (!TryLoadWithWindowsMediaPlayer(currentFilePath, out errorMessage))
        {
            return false;
        }

        SeekWithWindowsMediaPlayer(currentPosition, out _);
        if (wasPlaying)
        {
            PlayWithWindowsMediaPlayer(out _);
        }

        return true;
    }

    private bool TrySetWindowsMediaPlayerRate(double playbackRate, out string? errorMessage)
    {
        errorMessage = null;

        try
        {
            dynamic player = GetOrCreateWindowsMediaPlayer();
            player.settings.rate = playbackRate;
            return true;
        }
        catch (Exception exception)
        {
            errorMessage = exception.Message;
            return false;
        }
    }

    private string GetWindowsMediaPlayerMode()
    {
        try
        {
            dynamic player = GetOrCreateWindowsMediaPlayer();
            var playState = (int)player.playState;
            return playState switch
            {
                2 => "paused",
                3 => "playing",
                8 => "stopped",
                10 => "ready",
                _ => "stopped"
            };
        }
        catch
        {
            return string.Empty;
        }
    }

    private double GetWindowsMediaPlayerDouble(Func<dynamic, object?> accessor)
    {
        try
        {
            dynamic player = GetOrCreateWindowsMediaPlayer();
            return ToDouble(accessor(player));
        }
        catch
        {
            return 0;
        }
    }

    private dynamic GetOrCreateWindowsMediaPlayer()
    {
        if (_windowsMediaPlayer is not null)
        {
            return _windowsMediaPlayer;
        }

        var playerType = Type.GetTypeFromProgID("WMPlayer.OCX", throwOnError: false)
                         ?? throw new InvalidOperationException("Windows Media Player COM не знайдено в системі.");

        _windowsMediaPlayer = Activator.CreateInstance(playerType)
                              ?? throw new InvalidOperationException("Не вдалося створити Windows Media Player COM об'єкт.");

        dynamic player = _windowsMediaPlayer;
        player.settings.autoStart = false;
        return player;
    }

    private static void WaitForWindowsMediaPlayerMedia(dynamic player)
    {
        for (var attempt = 0; attempt < 20; attempt++)
        {
            try
            {
                if (player.currentMedia is not null)
                {
                    return;
                }
            }
            catch
            {
                // ignored
            }

            Thread.Sleep(50);
        }
    }

    private void CloseCurrentTrack()
    {
        if (_backend == PlaybackBackend.Mci)
        {
            TryExecute($"close {Alias}", out _);
        }
        else if (_backend == PlaybackBackend.WindowsMediaPlayer)
        {
            try
            {
                dynamic player = GetOrCreateWindowsMediaPlayer();
                player.controls.stop();
                player.URL = string.Empty;
            }
            catch
            {
                // ignored
            }
        }

        HasLoadedTrack = false;
        LoadedFilePath = null;
        _isPaused = false;
        _backend = PlaybackBackend.None;
        _positionSnapshot = TimeSpan.Zero;
        _playbackStartedAtUtc = null;
    }

    private TimeSpan GetEstimatedPosition()
    {
        if (!_playbackStartedAtUtc.HasValue)
        {
            return _positionSnapshot;
        }

        var elapsed = DateTimeOffset.UtcNow - _playbackStartedAtUtc.Value;
        if (elapsed < TimeSpan.Zero)
        {
            elapsed = TimeSpan.Zero;
        }

        return _positionSnapshot + TimeSpan.FromMilliseconds(elapsed.TotalMilliseconds * PlaybackRate);
    }

    private void StartPositionTracking(TimeSpan startPosition)
    {
        _positionSnapshot = startPosition;
        _playbackStartedAtUtc = DateTimeOffset.UtcNow;
    }

    private void FreezePositionTracking()
    {
        _positionSnapshot = GetEstimatedPosition();
        _playbackStartedAtUtc = null;
    }

    private TimeSpan ClampToDuration(TimeSpan position)
    {
        var duration = _backend switch
        {
            PlaybackBackend.Mci => TimeSpan.FromMilliseconds(QueryInteger($"status {Alias} length")),
            PlaybackBackend.WindowsMediaPlayer => TimeSpan.FromSeconds(GetWindowsMediaPlayerDouble(player => player.currentMedia?.duration)),
            _ => TimeSpan.Zero
        };

        if (duration <= TimeSpan.Zero)
        {
            return position < TimeSpan.Zero ? TimeSpan.Zero : position;
        }

        if (position < TimeSpan.Zero)
        {
            return TimeSpan.Zero;
        }

        return position > duration ? duration : position;
    }

    private static string EscapePath(string filePath)
    {
        return filePath.Replace("\"", "\"\"");
    }

    private static int ToMciSpeed(double playbackRate)
    {
        return (int)Math.Round(Math.Clamp(playbackRate, 0.5, 2.0) * 1000, MidpointRounding.AwayFromZero);
    }

    private static double ToDouble(object? value)
    {
        return value switch
        {
            null => 0,
            double doubleValue => doubleValue,
            float floatValue => floatValue,
            int intValue => intValue,
            long longValue => longValue,
            decimal decimalValue => (double)decimalValue,
            string stringValue when double.TryParse(stringValue, NumberStyles.Any, CultureInfo.InvariantCulture, out var parsed) => parsed,
            _ => 0
        };
    }

    private static bool TryExecute(string command, out string? errorMessage)
    {
        var result = mciSendString(command, null, 0, IntPtr.Zero);
        if (result == 0)
        {
            errorMessage = null;
            return true;
        }

        var builder = new StringBuilder(256);
        mciGetErrorString(result, builder, builder.Capacity);
        errorMessage = builder.Length > 0 ? builder.ToString() : $"MCI error {result}";
        return false;
    }

    private static int QueryInteger(string command)
    {
        if (!TryQuery(command, out var value))
        {
            return 0;
        }

        return int.TryParse(value, out var parsed) ? parsed : 0;
    }

    private static string QueryString(string command)
    {
        return TryQuery(command, out var value) ? value : string.Empty;
    }

    private static bool TryQuery(string command, out string value)
    {
        var builder = new StringBuilder(256);
        var result = mciSendString(command, builder, builder.Capacity, IntPtr.Zero);
        value = result == 0 ? builder.ToString() : string.Empty;
        return result == 0;
    }

    [DllImport("winmm.dll", CharSet = CharSet.Unicode)]
    private static extern int mciSendString(string command, StringBuilder? returnValue, int returnLength, IntPtr winHandle);

    [DllImport("winmm.dll", CharSet = CharSet.Unicode)]
    private static extern bool mciGetErrorString(int errorCode, StringBuilder errorText, int errorTextSize);
}
