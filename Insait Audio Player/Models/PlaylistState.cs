using System.Collections.Generic;

namespace Insait_Audio_Player.Models;

public sealed class PlaylistState
{
	public List<PlaylistTrackState> Tracks { get; set; } = new();

	public int CurrentIndex { get; set; } = -1;

	public double Volume { get; set; } = 75;

	public bool ShuffleEnabled { get; set; }

	public bool RepeatEnabled { get; set; } = true;

	public double PlaybackRate { get; set; } = 1.0;

	public long CurrentPositionMilliseconds { get; set; }
}

public sealed class PlaylistTrackState
{
	public string Title { get; set; } = string.Empty;

	public string Artist { get; set; } = string.Empty;

	public string Album { get; set; } = string.Empty;

	public string FilePath { get; set; } = string.Empty;

	public long DurationTicks { get; set; }
}

