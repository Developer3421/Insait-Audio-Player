using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Runtime.CompilerServices;
using Avalonia.Media;

namespace Insait_Audio_Player.Models;

public sealed class TrackItem : INotifyPropertyChanged
{
    private static readonly IBrush CurrentBadgeBackground = new SolidColorBrush(Color.Parse("#30FFC09F"));
    private static readonly IBrush DefaultBadgeBackground = new SolidColorBrush(Color.Parse("#FF2A2230"));
    private static readonly IBrush CurrentBadgeForeground = new SolidColorBrush(Color.Parse("#FFFFC09F"));
    private static readonly IBrush DefaultBadgeForeground = new SolidColorBrush(Color.Parse("#FF9E90B0"));

    private string _title = string.Empty;
    private string _artist = string.Empty;
    private string _album = string.Empty;
    private string _filePath = string.Empty;
    private TimeSpan _duration;
    private int _order;
    private bool _isCurrent;

    public event PropertyChangedEventHandler? PropertyChanged;

    public string Title
    {
        get => _title;
        set => SetField(ref _title, value);
    }

    public string Artist
    {
        get => _artist;
        set => SetField(ref _artist, value);
    }

    public string Album
    {
        get => _album;
        set => SetField(ref _album, value);
    }

    public string FilePath
    {
        get => _filePath;
        set => SetField(ref _filePath, value);
    }

    public TimeSpan Duration
    {
        get => _duration;
        set
        {
            if (SetField(ref _duration, value))
            {
                OnPropertyChanged(nameof(DurationText));
            }
        }
    }

    public int Order
    {
        get => _order;
        set
        {
            if (SetField(ref _order, value))
            {
                OnPropertyChanged(nameof(LeadingText));
            }
        }
    }

    public bool IsCurrent
    {
        get => _isCurrent;
        set
        {
            if (SetField(ref _isCurrent, value))
            {
                OnPropertyChanged(nameof(LeadingText));
                OnPropertyChanged(nameof(BadgeBackground));
                OnPropertyChanged(nameof(BadgeForeground));
            }
        }
    }

    public string DurationText => FormatDuration(Duration);

    public string LeadingText => IsCurrent ? "▶" : Order.ToString(CultureInfo.InvariantCulture);

    public IBrush BadgeBackground => IsCurrent ? CurrentBadgeBackground : DefaultBadgeBackground;

    public IBrush BadgeForeground => IsCurrent ? CurrentBadgeForeground : DefaultBadgeForeground;

    public static string FormatDuration(TimeSpan duration)
    {
        if (duration.TotalHours >= 1)
        {
            return duration.ToString(@"h\:mm\:ss", CultureInfo.InvariantCulture);
        }

        return duration.ToString(@"m\:ss", CultureInfo.InvariantCulture);
    }

    private bool SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
        {
            return false;
        }

        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}

