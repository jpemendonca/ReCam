using Recam.Web.Api;

namespace Recam.Web.Recordings;

/// <summary>
/// A camera's recordings, day by day, in this computer's time zone, and playback that starts
/// where the person clicks and moves on to the next file by itself, and the motion the server
/// found in them, with the people the optional detect service saw. Same behavior as the app.
/// </summary>
public sealed class TimelineController(IRecamApi api, IPeopleBoxesStore peopleBoxes)
{
    /// <summary>Motion plays from a little before the event, so the start of it shows.</summary>
    public static readonly TimeSpan MotionLead = TimeSpan.FromSeconds(5);

    private List<TimelineSegment> _timeline = [];
    private List<MotionMark> _motion = [];
    private int? _playing;
    private Guid _cameraId;

    /// <summary>The offset of this computer's time zone at a UTC instant. Replaced in tests.</summary>
    public Func<DateTimeOffset, TimeSpan> UtcOffsetOf { get; set; } = utc => TimeZoneInfo.Local.GetUtcOffset(utc);

    /// <summary>Days with recordings, newest first, on this computer's calendar.</summary>
    public IReadOnlyList<DateOnly> Days { get; private set; } = [];

    public DateOnly? SelectedDay { get; private set; }

    public IReadOnlyList<TimelineSegment> Timeline => _timeline;

    /// <summary>Motion events of the selected day, in order.</summary>
    public IReadOnlyList<MotionMark> Motion => _motion;

    /// <summary>The camera's motion sensitivity: "low", "medium" or "high".</summary>
    public string? Sensitivity { get; private set; }

    /// <summary>The bar shows only motion, and a file that ends goes on to the next motion.</summary>
    public bool OnlyMotion { get; set; }

    /// <summary>Like <see cref="OnlyMotion"/>, with only the motion that had a person in it.</summary>
    public bool OnlyPeople { get; set; }

    /// <summary>The detect service looked at some motion of the selected day. Without it, nothing about people shows.</summary>
    public bool PeopleAnalyzed => _motion.Exists(mark => mark.Person is not null);

    /// <summary>Motion of the selected day that had a person in it, in order.</summary>
    public IReadOnlyList<MotionMark> People => [.. _motion.Where(mark => mark.Person == true)];

    /// <summary>The motion the bar and the motion buttons use: all of it, or only people.</summary>
    public IReadOnlyList<MotionMark> ShownMotion => Marks;

    private List<MotionMark> Marks => OnlyPeople && PeopleAnalyzed ? [.. _motion.Where(mark => mark.Person == true)] : _motion;

    public bool SensitivityFailed { get; private set; }

    public bool Loading { get; private set; } = true;

    public bool Failed { get; private set; }

    public TimelineSegment? Playing => _playing is { } index ? _timeline[index] : null;

    /// <summary>Where the people are in the file playing; null when the detect service did not look at it.</summary>
    public PersonTrack? PlayingPeople { get; private set; }

    /// <summary>The boxes around people show over the player. Kept in this browser.</summary>
    public bool ShowPeople { get; private set; } = true;

    /// <summary>Wall-clock time the playback started from.</summary>
    public DateTime? PlayingFrom { get; private set; }

    /// <summary>What the &lt;video&gt; plays: the file, and where to start with a media fragment.</summary>
    public string? Source { get; private set; }

    /// <summary>The space all recordings share on the server; null until it loads or when it fails.</summary>
    public QuotaInfo? Quota { get; private set; }

    public event Action? Changed;

    public async Task LoadAsync(Guid cameraId)
    {
        _cameraId = cameraId;
        SetLoading();
        ShowPeople = await peopleBoxes.ReadAsync();
        await LoadQuotaAsync();
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

    // The space line is a detail: without it the recordings still show.
    private async Task LoadQuotaAsync()
    {
        try
        {
            Quota = await api.GetQuotaAsync(CancellationToken.None);
        }
        catch (HttpRequestException)
        {
            Quota = null;
        }
    }

    /// <summary>Loads one day on this computer's calendar, which may span two UTC days on the server.</summary>
    public async Task SelectDayAsync(DateOnly day)
    {
        SelectedDay = day;
        _playing = null;
        PlayingPeople = null;
        Source = null;
        PlayingFrom = null;
        SetLoading();
        var (windowStart, windowEnd, utcDays) = Window(day);
        var segments = new List<TimelineSegment>();
        var piece = 0;
        List<MotionMark> motion;
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

            motion = await LoadMotionAsync(day);
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
        _motion = motion;
        Loading = false;
        Changed?.Invoke();
    }

    /// <summary>Saves the camera's sensitivity and finds the day's motion again with it.</summary>
    public async Task SetSensitivityAsync(string sensitivity)
    {
        SensitivityFailed = false;
        try
        {
            await api.SetMotionSensitivityAsync(_cameraId, sensitivity, CancellationToken.None);
            Sensitivity = sensitivity;
            if (SelectedDay is { } day)
            {
                var motion = await LoadMotionAsync(day);
                if (SelectedDay == day)
                {
                    _motion = motion;
                }
            }
        }
        catch (HttpRequestException)
        {
            SensitivityFailed = true;
        }

        Changed?.Invoke();
    }

    /// <summary>Plays the first motion after the one playing, or the day's first.</summary>
    public void PlayNextMotion()
    {
        var next = PlayingFrom is { } from ? Marks.Find(mark => mark.Start - MotionLead > from.AddSeconds(1)) : Marks.FirstOrDefault();
        if (next is not null)
        {
            PlayAt(next.Start - MotionLead);
        }
    }

    /// <summary>Plays the motion before the one playing, or the day's last.</summary>
    public void PlayPreviousMotion()
    {
        var previous = PlayingFrom is { } from ? Marks.FindLast(mark => mark.Start - MotionLead < from.AddSeconds(-1)) : Marks.LastOrDefault();
        if (previous is not null)
        {
            PlayAt(previous.Start - MotionLead);
        }
    }

    /// <summary>Turns the boxes around people on or off, and remembers it in this browser.</summary>
    public async Task SetShowPeopleAsync(bool show)
    {
        ShowPeople = show;
        Changed?.Invoke();
        await peopleBoxes.SaveAsync(show);
    }

    /// <summary>Plays a motion event from a little before it, like the motion buttons.</summary>
    public void PlayMark(MotionMark mark) => PlayAt(mark.Start - MotionLead);

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
        if (_playing is not { } current || current + 1 >= _timeline.Count)
        {
            return;
        }

        if (!OnlyMotion && !(OnlyPeople && PeopleAnalyzed))
        {
            Play(current + 1, TimeSpan.Zero);
            return;
        }

        // Motion that goes on past this file continues in the next one; otherwise, skips ahead.
        var ended = _timeline[current].End;
        if (Marks.Find(mark => mark.End > ended) is { } next)
        {
            PlayAt(next.Start - MotionLead > ended ? next.Start - MotionLead : ended);
        }
    }

    private void Play(int index, TimeSpan from)
    {
        _playing = index;
        var segment = _timeline[index];
        PlayingFrom = segment.Start + from;
        Source = $"{segment.Url.TrimStart('/')}#t={(int)from.TotalSeconds}";
        PlayingPeople = null;
        Changed?.Invoke();
        _ = LoadPeopleAsync(segment);
    }

    // The boxes are a detail: without them the recording still plays.
    private async Task LoadPeopleAsync(TimelineSegment segment)
    {
        SegmentPeopleInfo? people;
        try
        {
            people = await api.GetSegmentPeopleAsync(segment.Url, CancellationToken.None);
        }
        catch (HttpRequestException)
        {
            return;
        }

        if (people is not null && Playing == segment)
        {
            PlayingPeople = new PersonTrack(people);
            Changed?.Invoke();
        }
    }

    private async Task<List<MotionMark>> LoadMotionAsync(DateOnly day)
    {
        var (windowStart, windowEnd, utcDays) = Window(day);
        var marks = new List<MotionMark>();
        foreach (var utcDay in utcDays)
        {
            var motion = await api.GetMotionAsync(_cameraId, utcDay, CancellationToken.None);
            Sensitivity = motion.Sensitivity;
            marks.AddRange(motion.Events
                .Where(found => found.Start >= windowStart && found.Start < windowEnd)
                .Select(found => new MotionMark(Wall(found.Start), Wall(found.End), found.Person)));
        }

        return marks;
    }

    // The UTC instants a day on this computer's calendar spans, and the UTC days that hold them.
    private (DateTimeOffset Start, DateTimeOffset End, SortedSet<DateOnly> UtcDays) Window(DateOnly day)
    {
        var start = new DateTimeOffset(day.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero);
        start -= UtcOffsetOf(start);
        var end = start.AddDays(1);
        return (start, end, [DateOnly.FromDateTime(start.UtcDateTime), DateOnly.FromDateTime(end.AddTicks(-1).UtcDateTime)]);
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
