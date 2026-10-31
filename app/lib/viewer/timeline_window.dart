/// How much time the timeline bar shows at once, and how far apart its ticks are.
enum TimelineZoom {
  minute(Duration(minutes: 1), Duration(seconds: 10)),
  fiveMinutes(Duration(minutes: 5), Duration(minutes: 1)),
  halfHour(Duration(minutes: 30), Duration(minutes: 5)),
  hour(Duration(hours: 1), Duration(minutes: 10)),
  day(Duration(days: 1), Duration(hours: 3));

  const TimelineZoom(this.span, this.tick);

  final Duration span;
  final Duration tick;
}

/// What the timeline bar shows: the zoom's span around a center, the moment under the fixed line
/// in the middle of the bar (bullet 13.5). The center stays within the day; the window may reach
/// past midnight, where nothing is drawn. Pure: the bar keeps one and replaces it as the video
/// plays, the person drags and zooms.
class TimelineWindow {
  const TimelineWindow._(this.day, this.zoom, this.center);

  /// The window of [zoom] centered on [center], kept within [day].
  factory TimelineWindow.around(
    DateTime day,
    TimelineZoom zoom,
    DateTime center,
  ) => TimelineWindow._(day, zoom, _clamp(day, center));

  /// Local midnight of the day shown.
  final DateTime day;
  final TimelineZoom zoom;

  /// The moment under the line in the middle of the bar.
  final DateTime center;

  DateTime get start => center.subtract(zoom.span ~/ 2);

  DateTime get end => center.add(zoom.span ~/ 2);

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
    return TimelineWindow.around(day, zoom, center.add(shift));
  }

  /// The same center at another zoom.
  TimelineWindow zoomTo(TimelineZoom other) =>
      TimelineWindow.around(day, other, center);

  /// Whether [time] falls inside the window.
  bool contains(DateTime time) => !time.isBefore(start) && time.isBefore(end);

  /// The tick times inside the window and the day, on round multiples of the zoom's tick.
  List<DateTime> get ticks {
    final tick = zoom.tick.inMilliseconds;
    final dayEnd = _dayEnd(day);
    final from = start.isBefore(day) ? day : start;
    final to = end.isAfter(dayEnd) ? dayEnd : end;
    var next = (from.difference(day).inMilliseconds / tick).ceil() * tick;
    final last = to.difference(day).inMilliseconds;
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

  static DateTime _clamp(DateTime day, DateTime center) {
    final end = _dayEnd(day);
    if (center.isBefore(day)) return day;
    if (center.isAfter(end)) return end;
    return center;
  }

  @override
  bool operator ==(Object other) =>
      other is TimelineWindow &&
      other.day == day &&
      other.zoom == zoom &&
      other.center == center;

  @override
  int get hashCode => Object.hash(day, zoom, center);
}
