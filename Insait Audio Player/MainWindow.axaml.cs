using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using Insait_Audio_Player.Localization;
using Insait_Audio_Player.Models;
using Insait_Audio_Player.Services;
using Ellipse = Avalonia.Controls.Shapes.Ellipse;

namespace Insait_Audio_Player;

public partial class MainWindow : Window
{
    private static readonly double[] PlaybackRatePresets = [0.5, 0.75, 1.0, 1.25, 1.5, 1.75, 2.0];

    private readonly ObservableCollection<TrackItem> _tracks = new();
    private readonly Dictionary<string, Bitmap?> _coverCache = new(StringComparer.OrdinalIgnoreCase);
    private readonly Random _random = new();
    private readonly WinMmAudioPlayer _player = new();
    private readonly AppSettingsRepository _appSettingsRepository = new();
    private readonly WindowsDefaultAppsService _defaultAppsService = new();
    private readonly PlaylistRepository _playlistRepository = new(new AesTextProtector());
    private readonly Mp3CoverArtProvider _coverArtProvider = new();
    private readonly DispatcherTimer _progressTimer;

    private readonly Border _titleBar;
    private readonly Button _languageButton;
    private readonly Button _closeButton;
    private readonly Button _minimizeButton;
    private readonly Button _playButton;
    private readonly Button _prevButton;
    private readonly Button _nextButton;
    private readonly Button _seekBackwardButton;
    private readonly Button _seekForwardButton;
    private readonly Button _shuffleButton;
    private readonly Button _repeatButton;
    private readonly Button _playbackRateButton;
    private readonly Button _encryptedInfoButton;
    private readonly Button _openButton;
    private readonly Button _setDefaultMp3Button;
    private readonly Button _addFilesButton;
    private readonly Button _clearButton;
    private readonly Button _savePlaylistButton;
    private readonly Image _coverImage;
    private readonly ListBox _playlistBox;
    private readonly Slider _seekSlider;
    private readonly Slider _volumeSlider;
    private readonly Image _defaultCoverIcon;
    private readonly TextBlock _languageButtonText;
    private readonly TextBlock _playIcon;
    private readonly TextBlock _windowTitleText;
    private readonly TextBlock _playlistHeaderText;
    private readonly TextBlock _speedText;
    private readonly TextBlock _trackTitle;
    private readonly TextBlock _trackArtist;
    private readonly TextBlock _trackAlbum;
    private readonly TextBlock _currentTime;
    private readonly TextBlock _totalTime;
    private readonly TextBlock _playlistCountText;
    private readonly TextBlock _playlistSummaryText;
    private readonly TextBlock _setDefaultMp3ButtonText;
    private readonly TextBlock _audioBackendText;
    private readonly Ellipse   _audioStatusDot;

    private bool _isPlaybackActive;
    private bool _isSeekDragActive;
    private bool _isUpdatingSeekSlider;
    private bool _suppressSelectionHandling;
    private bool _shuffleEnabled;
    private bool _repeatEnabled = true;
    private int _currentIndex = -1;
    private int _coverRequestVersion;
    private UiLocalizer _localizer;
    private int _currentLanguageIndex;
    private readonly string[] _pendingLaunchFiles;
    private bool _launchFilesHandled;

    public MainWindow()
        : this(null)
    {
    }

    public MainWindow(IEnumerable<string>? launchFilePaths)
    {
        _pendingLaunchFiles = launchFilePaths?
            .Where(static path => !string.IsNullOrWhiteSpace(path))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray() ?? [];

        _localizer = UiLocalizer.Create(_appSettingsRepository.Load().LanguageCode);

        InitializeComponent();

        _titleBar = RequireControl<Border>("TitleBar");
        _languageButton = RequireControl<Button>("LanguageButton");
        _closeButton = RequireControl<Button>("CloseButton");
        _minimizeButton = RequireControl<Button>("MinimizeButton");
        _playButton = RequireControl<Button>("PlayButton");
        _prevButton = RequireControl<Button>("PrevButton");
        _nextButton = RequireControl<Button>("NextButton");
        _seekBackwardButton = RequireControl<Button>("SeekBackwardButton");
        _seekForwardButton = RequireControl<Button>("SeekForwardButton");
        _shuffleButton = RequireControl<Button>("ShuffleButton");
        _repeatButton = RequireControl<Button>("RepeatButton");
        _playbackRateButton = RequireControl<Button>("PlaybackRateButton");
        _encryptedInfoButton = RequireControl<Button>("EncryptedInfoButton");
        _openButton = RequireControl<Button>("OpenButton");
        _setDefaultMp3Button = RequireControl<Button>("SetDefaultMp3Button");
        _addFilesButton = RequireControl<Button>("AddFilesButton");
        _clearButton = RequireControl<Button>("ClearButton");
        _savePlaylistButton = RequireControl<Button>("SavePlaylistButton");
        _coverImage = RequireControl<Image>("CoverImage");
        _playlistBox = RequireControl<ListBox>("PlaylistBox");
        _seekSlider = RequireControl<Slider>("SeekSlider");
        _volumeSlider = RequireControl<Slider>("VolumeSlider");
        _defaultCoverIcon = RequireControl<Image>("DefaultCoverIcon");
        _languageButtonText = RequireControl<TextBlock>("LanguageButtonText");
        _playIcon = RequireControl<TextBlock>("PlayIcon");
        _windowTitleText = RequireControl<TextBlock>("WindowTitleText");
        _playlistHeaderText = RequireControl<TextBlock>("PlaylistHeaderText");
        _speedText = RequireControl<TextBlock>("SpeedText");
        _trackTitle = RequireControl<TextBlock>("TrackTitle");
        _trackArtist = RequireControl<TextBlock>("TrackArtist");
        _trackAlbum = RequireControl<TextBlock>("TrackAlbum");
        _currentTime = RequireControl<TextBlock>("CurrentTime");
        _totalTime = RequireControl<TextBlock>("TotalTime");
        _playlistCountText = RequireControl<TextBlock>("PlaylistCountText");
        _playlistSummaryText = RequireControl<TextBlock>("PlaylistSummaryText");
        _setDefaultMp3ButtonText = RequireControl<TextBlock>("SetDefaultMp3ButtonText");
        _audioBackendText = RequireControl<TextBlock>("AudioBackendText");
        _audioStatusDot   = RequireControl<Ellipse>("AudioStatusDot");

        _playlistBox.ItemsSource = _tracks;
        _currentLanguageIndex = Array.IndexOf(UiLocalizer.SupportedLanguageCodes, _localizer.LanguageCode);
        if (_currentLanguageIndex < 0)
        {
            _currentLanguageIndex = 0;
        }

        _titleBar.PointerPressed += (_, e) => BeginMoveDrag(e);
        _languageButton.Click += (_, _) => CycleLanguage();
        RequireControl<Button>("AboutButton").Click += async (_, _) => await ShowAboutAsync();
        _closeButton.Click += (_, _) => Close();
        _minimizeButton.Click += (_, _) => WindowState = WindowState.Minimized;
        _playButton.Click += async (_, _) => await TogglePlaybackAsync();
        _prevButton.Click += (_, _) => PlayPreviousTrack();
        _nextButton.Click += (_, _) => PlayNextTrack(manualRequest: true);
        _seekBackwardButton.Click += (_, _) => SeekRelative(TimeSpan.FromSeconds(-10));
        _seekForwardButton.Click += (_, _) => SeekRelative(TimeSpan.FromSeconds(10));
        _shuffleButton.Click += (_, _) => ToggleShuffle();
        _repeatButton.Click += (_, _) => ToggleRepeat();
        _playbackRateButton.Click += (_, _) => CyclePlaybackRate();
        _openButton.Click += async (_, _) => await PickFilesAsync(allowMultiple: false, autoplayFirstSelection: true);
        _setDefaultMp3Button.Click += (_, _) => SetDefaultMp3();
        _addFilesButton.Click += async (_, _) => await PickFilesAsync(allowMultiple: true, autoplayFirstSelection: _tracks.Count == 0);
        _clearButton.Click += (_, _) => ClearPlaylist();
        _savePlaylistButton.Click += (_, _) => PersistState();
        _playlistBox.SelectionChanged += OnPlaylistSelectionChanged;
        _seekSlider.PointerPressed += (_, _) => _isSeekDragActive = _player.HasLoadedTrack;
        _seekSlider.PropertyChanged += OnSeekSliderPropertyChanged;
        _seekSlider.PointerCaptureLost += OnSeekSliderPointerCaptureLost;
        _seekSlider.PointerReleased += OnSeekSliderPointerReleased;
        _volumeSlider.PropertyChanged += OnVolumeSliderPropertyChanged;
        Opened += OnWindowOpened;
        Closing += OnWindowClosing;

        _progressTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(250)
        };
        _progressTimer.Tick += OnProgressTimerTick;
        _progressTimer.Start();

        // Single-instance: receive files forwarded by a secondary instance.
        SingleInstanceService.FilesReceived += OnFilesReceivedFromSecondInstance;

        ApplyLocalization();
        LoadPersistedState();
        UpdateToggleButtons();
        UpdatePlaylistSummary();
        UpdatePlayerUi();
    }

    private async Task TogglePlaybackAsync()
    {
        if (_tracks.Count == 0)
        {
            await PickFilesAsync(allowMultiple: false, autoplayFirstSelection: true);
            return;
        }

        if (_currentIndex < 0)
        {
            PlayTrackAtIndex(0, autoplay: true);
            return;
        }

        var currentTrack = _tracks[_currentIndex];
        var trackIsLoaded = _player.HasLoadedTrack &&
                            string.Equals(_player.LoadedFilePath, currentTrack.FilePath, StringComparison.OrdinalIgnoreCase);

        if (!trackIsLoaded)
        {
            PlayTrackAtIndex(_currentIndex, autoplay: true);
            return;
        }

        if (_isPlaybackActive)
        {
            if (_player.Pause(out var pauseError))
            {
                _isPlaybackActive = false;
            }
            else
            {
                ShowStatusMessage(pauseError ?? T(UiLocalizer.Keys.PauseFailed));
            }
        }
        else
        {
            if (_player.Play(out var playError))
            {
                _isPlaybackActive = true;
            }
            else
            {
                ShowStatusMessage(playError ?? T(UiLocalizer.Keys.ResumeFailed));
            }
        }

        UpdatePlayerUi();
        PersistState();
    }

    private void PlayPreviousTrack()
    {
        if (_tracks.Count == 0)
        {
            return;
        }

        if (_player.HasLoadedTrack && _player.GetPosition() > TimeSpan.FromSeconds(3))
        {
            _player.Seek(TimeSpan.Zero, out _);
            UpdatePlaybackProgress();
            PersistState();
            return;
        }

        var previousIndex = GetPreviousIndex();
        if (previousIndex >= 0)
        {
            PlayTrackAtIndex(previousIndex, autoplay: true);
        }
    }

    private void PlayNextTrack(bool manualRequest)
    {
        if (_tracks.Count == 0)
        {
            return;
        }

        // For manual Next always advance past the current track (skipRepeat = true).
        // If the playlist is exhausted, wrap to the beginning only on a manual request.
        var nextIndex = GetNextIndex(skipRepeat: true);
        if (nextIndex < 0 && manualRequest)
        {
            nextIndex = 0; // wrap around
        }

        if (nextIndex >= 0)
        {
            PlayTrackAtIndex(nextIndex, autoplay: true);
            return;
        }

        _isPlaybackActive = false;
        _player.Stop(out _);
        UpdatePlayerUi();

        if (manualRequest)
        {
            PersistState();
        }
    }

    private void ToggleShuffle()
    {
        _shuffleEnabled = !_shuffleEnabled;
        UpdateToggleButtons();
        PersistState();
    }

    private void ToggleRepeat()
    {
        _repeatEnabled = !_repeatEnabled;
        UpdateToggleButtons();
        PersistState();
    }

    private void SeekRelative(TimeSpan delta)
    {
        if (!_player.HasLoadedTrack)
        {
            return;
        }

        var targetPosition = _player.GetPosition() + delta;
        if (_player.Seek(targetPosition, out var seekError))
        {
            UpdatePlaybackProgress();
            PersistState();
        }
        else
        {
            ShowStatusMessage(seekError ?? T(UiLocalizer.Keys.RewindFailed));
        }
    }

    private void CyclePlaybackRate()
    {
        var currentIndex = Array.FindIndex(PlaybackRatePresets, rate => Math.Abs(rate - _player.PlaybackRate) < 0.01);
        if (currentIndex < 0)
        {
            currentIndex = 0;
        }

        var nextRate = PlaybackRatePresets[(currentIndex + 1) % PlaybackRatePresets.Length];
        if (_player.SetPlaybackRate(nextRate, out var speedError))
        {
            UpdateSpeedUi();
            PersistState();
        }
        else
        {
            ShowStatusMessage(speedError ?? T(UiLocalizer.Keys.UnsupportedPlaybackRate));
        }
    }

    private void CycleLanguage()
    {
        _currentLanguageIndex = (_currentLanguageIndex + 1) % UiLocalizer.SupportedLanguageCodes.Length;
        _localizer = UiLocalizer.Create(UiLocalizer.SupportedLanguageCodes[_currentLanguageIndex]);
        _appSettingsRepository.SaveLanguage(_localizer.LanguageCode);
        RefreshLocalizedTrackFallbacks();
        ApplyLocalization();
        UpdatePlayerUi();
        UpdatePlaylistSummary();
    }

    private void SetDefaultMp3()
    {
        var result = _defaultAppsService.RegisterAndPromptForMp3();
        var message = result.Outcome switch
        {
            DefaultMp3AssociationOutcome.OpenedAdvancedUi => T(UiLocalizer.Keys.DefaultMp3DialogOpened),
            DefaultMp3AssociationOutcome.OpenedSettings => T(UiLocalizer.Keys.DefaultMp3SettingsOpened),
            DefaultMp3AssociationOutcome.RegisteredOnly => T(UiLocalizer.Keys.DefaultMp3RegisteredOnly),
            DefaultMp3AssociationOutcome.NotSupported => T(UiLocalizer.Keys.DefaultMp3NotSupported),
            _ => Tf(UiLocalizer.Keys.DefaultMp3Failed, result.ErrorMessage ?? string.Empty)
        };

        ShowStatusMessage(message);
    }

    private async Task PickFilesAsync(bool allowMultiple, bool autoplayFirstSelection)
    {
        var topLevel = TopLevel.GetTopLevel(this);
        if (topLevel?.StorageProvider is null)
        {
            return;
        }

        var files = await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            AllowMultiple = allowMultiple,
            Title = allowMultiple ? T(UiLocalizer.Keys.SelectMultipleMp3) : T(UiLocalizer.Keys.SelectSingleMp3),
            FileTypeFilter = new[]
            {
                new FilePickerFileType(T(UiLocalizer.Keys.Mp3FilesLabel))
                {
                    Patterns = new[] { "*.mp3" },
                    MimeTypes = new[] { "audio/mpeg" }
                }
            }
        });

        if (files.Count == 0)
        {
            return;
        }

        ApplyFilesToPlaylist(files.Select(file => file.TryGetLocalPath() ?? string.Empty), autoplayFirstSelection);
    }

    private void ClearPlaylist()
    {
        _player.Stop(out _);
        _player.Dispose();
        _tracks.Clear();
        _currentIndex = -1;
        _isPlaybackActive = false;
        _shuffleEnabled = false;
        _repeatEnabled = true;
        _seekSlider.Maximum = 100;
        _seekSlider.Value = 0;
        _currentTime.Text = "0:00";
        _totalTime.Text = "0:00";
        UpdateToggleButtons();
        UpdatePlaylistSummary();
        UpdatePlayerUi();
        PersistState();
    }

    private void OnPlaylistSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (_suppressSelectionHandling)
        {
            return;
        }

        if (_playlistBox.SelectedIndex < 0 || _playlistBox.SelectedIndex >= _tracks.Count)
        {
            return;
        }

        PlayTrackAtIndex(_playlistBox.SelectedIndex, autoplay: true);
    }

    private void OnSeekSliderPointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        FinishSeekDrag();
    }

    private void OnSeekSliderPointerCaptureLost(object? sender, PointerCaptureLostEventArgs e)
    {
        FinishSeekDrag();
    }

    private void OnSeekSliderPropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
    {
        if (e.Property != RangeBase.ValueProperty ||
            _isUpdatingSeekSlider ||
            !_player.HasLoadedTrack)
        {
            return;
        }

        _isSeekDragActive = true;
        if (_player.Seek(TimeSpan.FromMilliseconds(_seekSlider.Value), out var seekError))
        {
            _currentTime.Text = TrackItem.FormatDuration(TimeSpan.FromMilliseconds(_seekSlider.Value));
            PersistState();
        }
        else
        {
            ShowStatusMessage(seekError ?? T(UiLocalizer.Keys.SliderSeekFailed));
        }
    }

    private void OnVolumeSliderPropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
    {
        if (e.Property != RangeBase.ValueProperty)
        {
            return;
        }

        _player.SetVolume(_volumeSlider.Value);
        PersistState();
    }

    private void OnProgressTimerTick(object? sender, EventArgs e)
    {
        UpdatePlaybackProgress();

        if (!_isPlaybackActive || !_player.HasLoadedTrack)
        {
            return;
        }

        var mode = _player.GetMode();
        var duration = _player.GetDuration();
        var position = _player.GetPosition();
        if (duration <= TimeSpan.Zero)
        {
            return;
        }

        // Допуск масштабується: на швидкості 2x за 250мс трек проходить 500мс,
        // а дуже короткі треки коротші за фіксований поріг 350мс.
        var toleranceMs = Math.Clamp(duration.TotalMilliseconds * 0.05, 150, 600);
        var isStopped = string.Equals(mode, "stopped", StringComparison.OrdinalIgnoreCase);
        var reachedEnd = position >= duration.Subtract(TimeSpan.FromMilliseconds(toleranceMs));
        var pastEnd = position >= duration;
        if ((reachedEnd && isStopped) || pastEnd)
        {
            HandleTrackCompleted();
        }
    }

    private void OnWindowClosing(object? sender, WindowClosingEventArgs e)
    {
        SingleInstanceService.FilesReceived -= OnFilesReceivedFromSecondInstance;
        PersistState();
        _progressTimer.Stop();
        DisposeCoverCache();
        _player.Dispose();
    }

    private void OnWindowOpened(object? sender, EventArgs e)
    {
        if (_launchFilesHandled || _pendingLaunchFiles.Length == 0)
        {
            return;
        }

        _launchFilesHandled = true;
        ApplyFilesToPlaylist(_pendingLaunchFiles, autoplayFirstSelection: true);
    }

    private void OnFilesReceivedFromSecondInstance(IReadOnlyList<string> filePaths)
    {
        // Restore and bring the window to the front.
        if (WindowState == WindowState.Minimized)
        {
            WindowState = WindowState.Normal;
        }

        Activate();
        ApplyFilesToPlaylist(filePaths, autoplayFirstSelection: true);
    }

    private void HandleTrackCompleted()
    {
        var nextIndex = GetNextIndex();
        if (nextIndex >= 0)
        {
            // Повтор того ж треку: не перевідкриваємо файл (це давало ефект
            // "прискорення"), а просто перемотуємо на початок і граємо далі.
            if (nextIndex == _currentIndex && _player.HasLoadedTrack)
            {
                _player.Seek(TimeSpan.Zero, out _);
                if (_player.Play(out _))
                {
                    _isPlaybackActive = true;
                }
                UpdatePlayerUi();
                PersistState();
                return;
            }

            PlayTrackAtIndex(nextIndex, autoplay: true);
            return;
        }

        _isPlaybackActive = false;
        _player.Seek(TimeSpan.Zero, out _);
        UpdatePlayerUi();
        PersistState();
    }

    private void LoadPersistedState()
    {
        var state = _playlistRepository.Load();

        foreach (var savedTrack in state.Tracks)
        {
            if (string.IsNullOrWhiteSpace(savedTrack.FilePath) || !File.Exists(savedTrack.FilePath))
            {
                continue;
            }

            _tracks.Add(CreateTrackFromState(savedTrack));
        }

        _shuffleEnabled = state.ShuffleEnabled;
        _repeatEnabled = state.RepeatEnabled;
        _volumeSlider.Value = Math.Clamp(state.Volume, 0, 100);
        _player.SetPlaybackRate(state.PlaybackRate, out _);

        if (_tracks.Count == 0)
        {
            UpdateSpeedUi();
            return;
        }

        _currentIndex = Math.Clamp(state.CurrentIndex, 0, _tracks.Count - 1);
        RefreshTrackPresentation();
        PlayTrackAtIndex(
            _currentIndex,
            autoplay: false,
            initialPosition: TimeSpan.FromMilliseconds(Math.Max(0, state.CurrentPositionMilliseconds)),
            persistState: false);
    }

    private bool PlayTrackAtIndex(int index, bool autoplay, TimeSpan? initialPosition = null, bool persistState = true)
    {
        if (index < 0 || index >= _tracks.Count)
        {
            return false;
        }

        var track = _tracks[index];
        if (!File.Exists(track.FilePath))
        {
            ShowStatusMessage(T(UiLocalizer.Keys.TrackNotFound));
            return false;
        }

        // Reset the seek slider to zero immediately so the UI doesn't briefly show
        // the previous track's position while the new file is being loaded.
        _isUpdatingSeekSlider = true;
        _seekSlider.Value = 0;
        _isUpdatingSeekSlider = false;
        _currentTime.Text = TrackItem.FormatDuration(TimeSpan.Zero);

        if (!_player.Load(track.FilePath, out var loadError))
        {
            ShowStatusMessage(loadError ?? T(UiLocalizer.Keys.OpenMp3Failed));
            return false;
        }

        _player.SetVolume(_volumeSlider.Value);
        _currentIndex = index;

        var actualDuration = _player.GetDuration();
        if (actualDuration > TimeSpan.Zero)
        {
            track.Duration = actualDuration;
        }

        if (initialPosition.HasValue && initialPosition.Value > TimeSpan.Zero)
        {
            _player.Seek(initialPosition.Value, out _);
        }

        _isPlaybackActive = false;
        if (autoplay)
        {
            if (_player.Play(out var playError))
            {
                _isPlaybackActive = true;
            }
            else
            {
                ShowStatusMessage(playError ?? T(UiLocalizer.Keys.StartPlaybackFailed));
            }
        }

        RefreshTrackPresentation();
        UpdatePlaylistSummary();
        UpdatePlayerUi();

        if (persistState)
        {
            PersistState();
        }

        return true;
    }

    private void RefreshTrackPresentation()
    {
        for (var i = 0; i < _tracks.Count; i++)
        {
            _tracks[i].Order = i + 1;
            _tracks[i].IsCurrent = i == _currentIndex;
        }

        ApplySelectedIndex(_currentIndex);
    }

    private void ApplySelectedIndex(int index)
    {
        _suppressSelectionHandling = true;
        _playlistBox.SelectedIndex = index;
        _suppressSelectionHandling = false;
    }

    private void UpdatePlayerUi()
    {
        UpdateToggleButtons();
        UpdateSpeedUi();
        UpdatePlaybackProgress();

        if (_currentIndex >= 0 && _currentIndex < _tracks.Count)
        {
            var track = _tracks[_currentIndex];
            _trackTitle.Text = track.Title;
            _trackArtist.Text = string.IsNullOrWhiteSpace(track.Artist) ? T(UiLocalizer.Keys.LocalMp3) : track.Artist;
            _trackAlbum.Text = string.IsNullOrWhiteSpace(track.Album)
                ? T(UiLocalizer.Keys.PlaylistEncryptedNoPassword)
                : track.Album;
        }
        else
        {
            _trackTitle.Text = T(UiLocalizer.Keys.AddMp3Placeholder);
            _trackArtist.Text = T(UiLocalizer.Keys.PlaybackBackendDescription);
            _trackAlbum.Text = T(UiLocalizer.Keys.PlaylistAutoSaved);
        }

        _playIcon.Text = _isPlaybackActive ? "⏸" : "▶";
        UpdateAudioBadge();
        _ = UpdateCoverArtAsync();
    }

    private void UpdateAudioBadge()
    {
        var deviceCount = AudioDeviceService.GetOutputDeviceCount();

        if (deviceCount == 0)
        {
            // No audio output devices — show a warning
            _audioBackendText.Text = "NO AUDIO";
            _audioStatusDot.Fill  = new SolidColorBrush(Color.Parse("#FFFF6060"));
        }
        else
        {
            var backend = _player.HasLoadedTrack ? _player.ActiveBackendName : "WIN API";
            _audioBackendText.Text = $"{backend} · {deviceCount}▶";
            _audioStatusDot.Fill  = new SolidColorBrush(Color.Parse("#FFA6E3A1")); // green
        }
    }

    private async Task UpdateCoverArtAsync()
    {
        var requestVersion = ++_coverRequestVersion;

        if (_currentIndex < 0 || _currentIndex >= _tracks.Count)
        {
            ApplyCoverArt(null, requestVersion);
            return;
        }

        var filePath = _tracks[_currentIndex].FilePath;
        if (_coverCache.TryGetValue(filePath, out var cachedCover))
        {
            ApplyCoverArt(cachedCover, requestVersion);
            return;
        }

        ApplyCoverArt(null, requestVersion);
        var cover = await Task.Run(() => _coverArtProvider.TryLoadCover(filePath));
        if (requestVersion != _coverRequestVersion)
        {
            cover?.Dispose();
            return;
        }

        _coverCache[filePath] = cover;
        ApplyCoverArt(cover, requestVersion);
    }

    private void ApplyCoverArt(Bitmap? cover, int requestVersion)
    {
        if (requestVersion != _coverRequestVersion)
        {
            return;
        }

        _coverImage.Source = cover;
        _coverImage.IsVisible = cover is not null;
        _defaultCoverIcon.IsVisible = cover is null;
    }

    private void UpdatePlaybackProgress()
    {
        var duration = _player.HasLoadedTrack
            ? _player.GetDuration()
            : (_currentIndex >= 0 && _currentIndex < _tracks.Count ? _tracks[_currentIndex].Duration : TimeSpan.Zero);

        var position = _player.HasLoadedTrack ? _player.GetPosition() : TimeSpan.Zero;

        if (_currentIndex >= 0 && _currentIndex < _tracks.Count && duration > TimeSpan.Zero)
        {
            _tracks[_currentIndex].Duration = duration;
        }

        _seekSlider.Maximum = Math.Max(1, duration.TotalMilliseconds);
        if (!_isSeekDragActive)
        {
            _isUpdatingSeekSlider = true;
            _seekSlider.Value = Math.Min(_seekSlider.Maximum, position.TotalMilliseconds);
            _isUpdatingSeekSlider = false;
        }

        _currentTime.Text = TrackItem.FormatDuration(position);
        _totalTime.Text = TrackItem.FormatDuration(duration);
    }

    private void UpdatePlaylistSummary()
    {
        _playlistCountText.Text = _tracks.Count.ToString(CultureInfo.InvariantCulture);
        var totalDuration = TimeSpan.FromTicks(_tracks.Sum(track => track.Duration.Ticks));
        _playlistSummaryText.Text = Tf(UiLocalizer.Keys.PlaylistSummaryFormat, _tracks.Count, TrackItem.FormatDuration(totalDuration));
    }

    private void UpdateToggleButtons()
    {
        SetActiveClass(_shuffleButton, _shuffleEnabled);
        SetActiveClass(_repeatButton, _repeatEnabled);
    }

    private bool ApplyFilesToPlaylist(IEnumerable<string> filePaths, bool autoplayFirstSelection)
    {
        var firstNewTrackIndex = _tracks.Count;

        foreach (var filePath in filePaths)
        {
            if (string.IsNullOrWhiteSpace(filePath))
            {
                continue;
            }

            var localPath = Path.GetFullPath(filePath);
            if (!File.Exists(localPath) ||
                !string.Equals(Path.GetExtension(localPath), ".mp3", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            _tracks.Add(CreateTrackFromFile(localPath));
        }

        if (firstNewTrackIndex == _tracks.Count)
        {
            return false;
        }

        if (_currentIndex < 0)
        {
            _currentIndex = 0;
        }

        RefreshTrackPresentation();
        UpdatePlaylistSummary();

        if (autoplayFirstSelection)
        {
            PlayTrackAtIndex(firstNewTrackIndex, autoplay: true, persistState: false);
        }
        else
        {
            ApplySelectedIndex(_currentIndex);
            UpdatePlayerUi();
        }

        PersistState();
        return true;
    }

    private void UpdateSpeedUi()
    {
        _speedText.Text = $"{_player.PlaybackRate:0.##}x";
        SetActiveClass(_playbackRateButton, Math.Abs(_player.PlaybackRate - 1.0) > 0.01);
    }

    private void ApplyLocalization()
    {
        Title = T(UiLocalizer.Keys.AppTitle);
        _languageButtonText.Text = _localizer.LanguageCode.ToUpperInvariant();
        _windowTitleText.Text = T(UiLocalizer.Keys.AppTitle);
        _playlistHeaderText.Text = T(UiLocalizer.Keys.PlaylistTitle);

        ToolTip.SetTip(_languageButton, T(UiLocalizer.Keys.ChangeLanguage));
        ToolTip.SetTip(RequireControl<Button>("AboutButton"), T(UiLocalizer.Keys.AboutInfo));
        ToolTip.SetTip(_minimizeButton, T(UiLocalizer.Keys.Minimize));
        ToolTip.SetTip(_closeButton, T(UiLocalizer.Keys.Close));
        ToolTip.SetTip(_prevButton, T(UiLocalizer.Keys.PreviousTrack));
        ToolTip.SetTip(_playButton, T(UiLocalizer.Keys.PlayPause));
        ToolTip.SetTip(_nextButton, T(UiLocalizer.Keys.NextTrack));
        ToolTip.SetTip(_seekBackwardButton, T(UiLocalizer.Keys.SeekBackward));
        ToolTip.SetTip(_seekForwardButton, T(UiLocalizer.Keys.SeekForward));
        ToolTip.SetTip(_shuffleButton, T(UiLocalizer.Keys.Shuffle));
        ToolTip.SetTip(_repeatButton, T(UiLocalizer.Keys.Repeat));
        ToolTip.SetTip(_playbackRateButton, T(UiLocalizer.Keys.PlaybackRate));
        ToolTip.SetTip(_openButton, T(UiLocalizer.Keys.OpenMp3File));
        ToolTip.SetTip(_setDefaultMp3Button, T(UiLocalizer.Keys.SetDefaultMp3));
        ToolTip.SetTip(_addFilesButton, T(UiLocalizer.Keys.AddMp3Files));
        ToolTip.SetTip(_savePlaylistButton, T(UiLocalizer.Keys.SavePlaylistNow));
        ToolTip.SetTip(_clearButton, T(UiLocalizer.Keys.ClearPlaylist));
        ToolTip.SetTip(_encryptedInfoButton, T(UiLocalizer.Keys.PlaylistEncryptedInfo));
        _setDefaultMp3ButtonText.Text = T(UiLocalizer.Keys.SetDefaultMp3Short);
    }

    private async Task ShowAboutAsync()
    {
        var dialog = new AboutWindow();
        await dialog.ShowDialog(this);
    }

    private void PersistState()
    {
        var state = new PlaylistState
        {
            CurrentIndex = _currentIndex,
            Volume = _volumeSlider.Value,
            ShuffleEnabled = _shuffleEnabled,
            RepeatEnabled = _repeatEnabled,
            PlaybackRate = _player.PlaybackRate,
            CurrentPositionMilliseconds = _player.HasLoadedTrack
                ? (long)_player.GetPosition().TotalMilliseconds
                : 0,
            Tracks = _tracks.Select(track => new PlaylistTrackState
            {
                Title = track.Title,
                Artist = NormalizeArtistForStorage(track),
                Album = NormalizeAlbumForStorage(track),
                FilePath = track.FilePath,
                DurationTicks = track.Duration.Ticks
            }).ToList()
        };

        _playlistRepository.Save(state);
    }

    private int GetNextIndex(bool skipRepeat = false)
    {
        if (_tracks.Count == 0)
        {
            return -1;
        }

        // Shuffle має пріоритет над повтором: коли обидва увімкнені,
        // автоперехід обирає випадковий трек, а не повторює поточний.
        // Повтор без shuffle перезапускає поточний трек (крім ручного Next).
        if (_shuffleEnabled && _tracks.Count > 1)
        {
            var nextIndex = _currentIndex;
            while (nextIndex == _currentIndex)
            {
                nextIndex = _random.Next(0, _tracks.Count);
            }

            return nextIndex;
        }

        // When repeat is enabled and this is an automatic completion (not a manual Next press),
        // replay the current track.
        if (_repeatEnabled && !skipRepeat)
        {
            return _currentIndex;
        }

        var candidate = _currentIndex + 1;
        return candidate < _tracks.Count ? candidate : -1;
    }

    private int GetPreviousIndex()
    {
        if (_tracks.Count == 0)
        {
            return -1;
        }

        if (_shuffleEnabled && _tracks.Count > 1)
        {
            var previousIndex = _currentIndex;
            while (previousIndex == _currentIndex)
            {
                previousIndex = _random.Next(0, _tracks.Count);
            }

            return previousIndex;
        }

        var candidate = _currentIndex - 1;
        if (candidate >= 0)
        {
            return candidate;
        }

        return _repeatEnabled ? _tracks.Count - 1 : -1;
    }

    private TrackItem CreateTrackFromFile(string filePath)
    {
        var duration = _player.TryGetDuration(filePath, out var detectedDuration)
            ? detectedDuration
            : TimeSpan.Zero;

        return new TrackItem
        {
            Title = Path.GetFileNameWithoutExtension(filePath),
            Artist = T(UiLocalizer.Keys.LocalMp3),
            Album = BuildAlbumDescription(filePath),
            FilePath = filePath,
            Duration = duration
        };
    }

    private TrackItem CreateTrackFromState(PlaylistTrackState savedTrack)
    {
        return new TrackItem
        {
            Title = string.IsNullOrWhiteSpace(savedTrack.Title)
                ? Path.GetFileNameWithoutExtension(savedTrack.FilePath)
                : savedTrack.Title,
            Artist = string.IsNullOrWhiteSpace(savedTrack.Artist) ? T(UiLocalizer.Keys.LocalMp3) : savedTrack.Artist,
            Album = string.IsNullOrWhiteSpace(savedTrack.Album)
                ? BuildAlbumDescription(savedTrack.FilePath)
                : savedTrack.Album,
            FilePath = savedTrack.FilePath,
            Duration = savedTrack.DurationTicks > 0 ? TimeSpan.FromTicks(savedTrack.DurationTicks) : TimeSpan.Zero
        };
    }

    private string BuildAlbumDescription(string filePath)
    {
        var directoryName = Path.GetFileName(Path.GetDirectoryName(filePath));
        return string.IsNullOrWhiteSpace(directoryName)
            ? T(UiLocalizer.Keys.PlaylistEncryptedNoPassword)
            : Tf(UiLocalizer.Keys.FolderFormat, directoryName);
    }

    private void ShowStatusMessage(string message)
    {
        _trackAlbum.Text = message;
    }

    private void RefreshLocalizedTrackFallbacks()
    {
        foreach (var track in _tracks)
        {
            if (IsFallbackArtist(track.Artist))
            {
                track.Artist = T(UiLocalizer.Keys.LocalMp3);
            }

            if (IsFallbackAlbum(track.Album, track.FilePath))
            {
                track.Album = BuildAlbumDescription(track.FilePath);
            }
        }
    }

    private string NormalizeArtistForStorage(TrackItem track)
    {
        return IsFallbackArtist(track.Artist) ? string.Empty : track.Artist;
    }

    private string NormalizeAlbumForStorage(TrackItem track)
    {
        return IsFallbackAlbum(track.Album, track.FilePath) ? string.Empty : track.Album;
    }

    private static bool IsFallbackArtist(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return true;
        }

        foreach (var languageCode in UiLocalizer.SupportedLanguageCodes)
        {
            if (string.Equals(value, UiLocalizer.Create(languageCode)[UiLocalizer.Keys.LocalMp3], StringComparison.Ordinal))
            {
                return true;
            }
        }

        return false;
    }

    private static bool IsFallbackAlbum(string? value, string filePath)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return true;
        }

        var directoryName = Path.GetFileName(Path.GetDirectoryName(filePath)) ?? string.Empty;
        foreach (var languageCode in UiLocalizer.SupportedLanguageCodes)
        {
            var localizer = UiLocalizer.Create(languageCode);
            if (string.Equals(value, localizer[UiLocalizer.Keys.PlaylistEncryptedNoPassword], StringComparison.Ordinal) ||
                string.Equals(value, localizer.Format(UiLocalizer.Keys.FolderFormat, directoryName), StringComparison.Ordinal))
            {
                return true;
            }
        }

        return false;
    }

    private string T(string key)
    {
        return _localizer[key];
    }

    private string Tf(string key, params object[] args)
    {
        return _localizer.Format(key, args);
    }

    private void DisposeCoverCache()
    {
        foreach (var cover in _coverCache.Values)
        {
            cover?.Dispose();
        }

        _coverCache.Clear();
        _coverImage.Source = null;
        _coverImage.IsVisible = false;
        _defaultCoverIcon.IsVisible = true;
    }

    private void FinishSeekDrag()
    {
        if (!_isSeekDragActive)
        {
            return;
        }

        _isSeekDragActive = false;
        UpdatePlaybackProgress();
        PersistState();
    }

    private static void SetActiveClass(Button button, bool isActive)
    {
        if (isActive)
        {
            if (!button.Classes.Contains("active"))
            {
                button.Classes.Add("active");
            }
        }
        else
        {
            button.Classes.Remove("active");
        }
    }

    private T RequireControl<T>(string name) where T : Control
    {
        return this.FindControl<T>(name)
               ?? throw new InvalidOperationException($"Не знайдено елемент керування '{name}'.");
    }
}

