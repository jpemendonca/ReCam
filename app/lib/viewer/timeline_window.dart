/// How much of the day the timeline bar shows at once, and how far apart its ticks are.
enum TimelineZoom {
  minute(Duration(minutes: 1), Duration(seconds: 10)),
  quarterHour(Duration(minutes: 15), Duration(minutes: 3)),
  hour(Duration(hours: 1), Duration(minutes: 10)),
  threeHours(Duration(hours: 3), Duration(minutes: 30));

  const TimelineZoom(this.span, this.tick);

  final Duration span;
  final Duration tick;
}

/// The stretch of one day the timeline bar shows, and the conversions between a point on the
/// bar and a time. Pure: the bar keeps one and replaces it as the person zooms and drags.
class TimelineWindow {
  const TimelineWindow._(this.day, this.zoom, this.start);

  /// The window of [zoom] centered on [center], kept inside [day].
  factory TimelineWindow.around(
    DateTime day,
    TimelineZoom zoom,
    DateTime center,
  ) => TimelineWindow._(
    day,
    zoom,
    _clamp(day, zoom, center.subtract(zoom.span ~/ 2)),
  );

  /// Local midnight of the day shown.
  final DateTime day;
  final TimelineZoom zoom;
  final DateTime start;

  DateTime get end => start.add(zoom.span);

  DateTime get center => start.add(zoom.span ~/ 2);

  /// Where [time] falls on a bar [width] wide; outside the window it is off the bar.
  double xOf(DateTime time, double width) =>
      time.difference(start).inMilliseconds / zoom.span.inMilliseconds * width;

  /// The time under [x] on a bar [width] wide, within the window.
  DateTime timeAt(double x, double width) {
    final fraction = width <= 0 ? 0.0 : (x / width).clamp(0.0, 1.0);
    return start.add(
      Duration(milliseconds: (fraction * zoom.span.inMilliseconds).round()),
    );
  }

  /// The window after dragging [dx] on a bar [width] wide: dragging right shows earlier times.
  TimelineWindow panBy(double dx, double width) {
    if (width <= 0) return this;
    final shift = Duration(
      milliseconds: (-dx / width * zoom.span.inMilliseconds).round(),
    );
    return TimelineWindow._(day, zoom, _clamp(day, zoom, start.add(shift)));
  }

  /// One whole window earlier (negative) or later (positive).
  TimelineWindow step(int windows) => TimelineWindow._(
    day,
    zoom,
    _clamp(day, zoom, start.add(zoom.span * windows)),
  );

  /// The same center at another zoom.
  TimelineWindow zoomTo(TimelineZoom other) =>
      TimelineWindow.around(day, other, center);

  bool get atDayStart => !start.isAfter(day);

  bool get atDayEnd => !end.isBefore(_dayEnd(day));

  /// The tick times inside the window, on round multiples of the zoom's tick.
  List<DateTime> get ticks {
    final tick = zoom.tick.inMilliseconds;
    final fromMidnight = start.difference(day).inMilliseconds;
    var next = (fromMidnight / tick).ceil() * tick;
    final last = end.difference(day).inMilliseconds;
    final result = <DateTime>[];
    while (next <= last) {
      result.add(day.add(Duration(milliseconds: next)));
      next += tick;
    }
    return result;
  }

  // The timeline keeps wall-clock times in UTC DateTimes (see RecordingTimelineController),
  // so the next midnight keeps the day's kind.
  static DateTime _dayEnd(DateTime day) => day.isUtc
      ? DateTime.utc(day.year, day.month, day.day + 1)
      : DateTime(day.year, day.month, day.day + 1);

  static DateTime _clamp(DateTime day, TimelineZoom zoom, DateTime start) {
    final latest = _dayEnd(day).subtract(zoom.span);
    if (start.isBefore(day)) return day;
    if (start.isAfter(latest)) return latest;
    return start;
  }

  @override
  bool operator ==(Object other) =>
      other is TimelineWindow &&
      other.day == day &&
      other.zoom == zoom &&
      other.start == start;

  @override
  int get hashCode => Object.hash(day, zoom, start);
}
