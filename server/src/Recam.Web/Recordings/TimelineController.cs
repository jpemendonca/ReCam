using Recam.Web.Api;

namespace Recam.Web.Recordings;

/// <summary>
/// A camera's recordings, day by day, in this computer's time zone, and playback that starts
/// where the person clicks and moves on to the next file by itself. Same behavior as the app.
/// </summary>
public sealed class TimelineController(IRecamApi api)
{
    private List<TimelineSegment> _timeline = [];
    private int? _playing;
    private Guid _cameraId;

    /// <summary>The offset of this computer's time zone at a UTC instant. Replaced in tests.</summary>
    public Func<DateTimeOffset, TimeSpan> UtcOffsetOf { get; set; } = utc => TimeZoneInfo.Local.GetUtcOffset(utc);

    /// <summary>Days with recordings, newest first, on this computer's calendar.</summary>
    public IReadOnlyList<DateOnly> Days { get; private set; } = [];

    public DateOnly? SelectedDay { get; private set; }

    public IReadOnlyList<TimelineSegment> Timeline => _timeline;

    public bool Loading { get; private set; } = true;

    public bool Failed { get; private set; }

    public TimelineSegment? Playing => _playing is { } index ? _timeline[index] : null;

    /// <summary>Wall-clock time the playback started from.</summary>
    public DateTime? PlayingFrom { get; private set; }

    /// <summary>What the &lt;video&gt; plays: the file, and where to start with a media fragment.</summary>
    public string? Source { get; private set; }

    public event Action? Changed;

    public async Task LoadAsync(Guid cameraId)
    {
        _cameraId = cameraId;
        SetLoading();
        try
        {
            var days = LocalDays(await api.GetRecordingDaysAsync(cameraId, CancellationToken.None));
            Days = days;
            Loading = false;
            Changed?.Invoke();

            // A UTC day may reach a local day only by its edge, with nothing recorded in it. Opens
            // the newest day that has something, and drops the empty ones from the list.
            while (days.Count > 0)
            {
                await SelectDayAsync(days[0]);
                if (Failed || _timeline.Count > 0)
                {
                    break;
                }

                days.RemoveAt(0);
                Days = [.. days];
            }

            if (days.Count == 0)
            {
                SelectedDay = null;
                Changed?.Invoke();
            }
        }
        catch (HttpRequestException)
        {
            Loading = false;
            Failed = true;
            Changed?.Invoke();
        }
    }

    /// <summary>Loads one day on this computer's calendar, which may span two UTC days on the server.</summary>
    public async Task SelectDayAsync(DateOnly day)
    {
        SelectedDay = day;
        _playing = null;
        Source = null;
        PlayingFrom = null;
        SetLoading();
        var windowStart = new DateTimeOffset(day.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero);
        windowStart -= UtcOffsetOf(windowStart);
        var windowEnd = windowStart.AddDays(1);
        var utcDays = new SortedSet<DateOnly> { DateOnly.FromDateTime(windowStart.UtcDateTime), DateOnly.FromDateTime(windowEnd.AddTicks(-1).UtcDateTime) };
        var segments = new List<TimelineSegment>();
        var piece = 0;
        try
        {
            foreach (var utcDay in utcDays)
            {
                foreach (var recorded in await api.GetRecordingsAsync(_cameraId, utcDay, CancellationToken.None))
                {
                    segments.AddRange(recorded.Segments
                        .Where(segment => segment.Start >= windowStart && segment.Start < windowEnd)
                        .Select(segment => new TimelineSegment(Wall(segment.Start), Wall(segment.End), segment.Url, piece)));
                    piece++;
                }
            }
        }
        catch (HttpRequestException)
        {
            Loading = false;
            Failed = true;
            Changed?.Invoke();
            return;
        }

        if (SelectedDay != day)
        {
            return;
        }

        _timeline = segments;
        Loading = false;
        Changed?.Invoke();
    }

    /// <summary>Plays from a wall-clock time on the selected day. In a gap, starts at the next recording.</summary>
    public void PlayAt(DateTime wallTime)
    {
        var index = _timeline.FindIndex(segment => segment.End > wallTime);
        if (index < 0)
        {
            return;
        }

        var segment = _timeline[index];
        Play(index, wallTime > segment.Start ? wallTime - segment.Start : TimeSpan.Zero);
    }

    /// <summary>The &lt;video&gt; reached the end of a file: the next one follows.</summary>
    public void PlayNext()
    {
        if (_playing is { } current && current + 1 < _timeline.Count)
        {
            Play(current + 1, TimeSpan.Zero);
        }
    }

    private void Play(int index, TimeSpan from)
    {
        _playing = index;
        var segment = _timeline[index];
        PlayingFrom = segment.Start + from;
        Source = $"{segment.Url.TrimStart('/')}#t={(int)from.TotalSeconds}";
        Changed?.Invoke();
    }

    // A UTC day covers parts of one or two days on this computer's calendar.
    private List<DateOnly> LocalDays(IEnumerable<DateOnly> utcDays)
    {
        var days = new SortedSet<DateOnly>();
        foreach (var utcDay in utcDays)
        {
            var start = new DateTimeOffset(utcDay.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero);
            days.Add(DateOnly.FromDateTime(Wall(start)));
            days.Add(DateOnly.FromDateTime(Wall(start.AddDays(1).AddTicks(-1))));
        }

        return [.. days.Reverse()];
    }

    private DateTime Wall(DateTimeOffset utc) => (utc + UtcOffsetOf(utc)).UtcDateTime;

    private void SetLoading()
    {
        Loading = true;
        Failed = false;
        Changed?.Invoke();
    }
}
