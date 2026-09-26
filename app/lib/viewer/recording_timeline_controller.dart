import 'dart:async';

import 'package:flutter/foundation.dart';

import '../core/media/recording_player.dart';
import '../core/media/recording_relay.dart';
import '../core/network/api_client.dart';
import '../core/storage/credential_store.dart';

typedef RecordingTimelineFactory = RecordingTimelineController Function(
  String cameraId,
);

/// A recorded segment placed on the phone's own clock: [start] and [end] are wall-clock times
/// stored as UTC values, so a day runs from its midnight to the next one.
class TimelineSegment {
  const TimelineSegment({
    required this.start,
    required this.end,
    required this.url,
    required this.piece,
  });

  final DateTime start;
  final DateTime end;
  final String url;

  /// Segments of the same piece are back to back.
  final int piece;
}

/// A motion event placed on the phone's clock, like [TimelineSegment].
class MotionMark {
  const MotionMark({required this.start, required this.end});

  final DateTime start;
  final DateTime end;
}

/// A camera's recordings, day by day, in the phone's time zone, playback that starts where the
/// person taps and moves on to the next segment by itself, and the motion the server found.
class RecordingTimelineController extends ChangeNotifier {
  RecordingTimelineController({
    required this._api,
    required this._session,
    required this.cameraId,
    required this.player,
    required this._segments,
    Duration Function(DateTime utc)? utcOffsetOf,
  }) : _utcOffsetOf = utcOffsetOf ?? ((utc) => utc.toLocal().timeZoneOffset) {
    player.onFinished = () => unawaited(_playNext());
  }

  final ApiClient _api;
  final PairedSession _session;
  final String cameraId;
  final RecordingPlayer player;
  final SegmentSource _segments;
  final Duration Function(DateTime utc) _utcOffsetOf;

  /// Motion plays from a little before the event, so the start of it shows.
  static const motionLead = Duration(seconds: 5);

  List<DateTime> _days = const [];
  DateTime? _selectedDay;
  List<TimelineSegment> _timeline = const [];
  bool _loading = true;
  bool _failed = false;
  int? _playing;
  DateTime? _playingFrom;
  List<MotionMark> _motion = const [];
  MotionSensitivity? _sensitivity;
  bool _onlyMotion = false;
  bool _sensitivityFailed = false;
  RecordingQuota? _quota;

  /// Days with recordings, newest first, as dates on the phone's calendar.
  List<DateTime> get days => _days;

  /// The space all recordings share on the server; null until it loads or when it fails.
  RecordingQuota? get quota => _quota;

  DateTime? get selectedDay => _selectedDay;

  /// The selected day's segments, in order.
  List<TimelineSegment> get timeline => _timeline;

  bool get loading => _loading;

  bool get failed => _failed;

  TimelineSegment? get playing =>
      _playing == null ? null : _timeline[_playing!];

  /// Wall-clock time the playback started from.
  DateTime? get playingFrom => _playingFrom;

  /// Motion events of the selected day, in order.
  List<MotionMark> get motion => _motion;

  MotionSensitivity? get sensitivity => _sensitivity;

  bool get sensitivityFailed => _sensitivityFailed;

  /// The bar shows only motion, and a segment that ends goes on to the next motion.
  bool get onlyMotion => _onlyMotion;

  set onlyMotion(bool value) {
    _onlyMotion = value;
    notifyListeners();
  }

  Future<void> load() async {
    _setLoading();
    unawaited(_loadQuota());
    final result = await _api.recordingDays(
      _session.serverUrl,
      _session.credential,
      cameraId,
    );
    switch (result) {
      case ApiSuccess(:final value):
        _days = _localDays(value);
        _loading = false;
        notifyListeners();
        await _openNewestRecordedDay();
      case ApiFailure():
        _loading = false;
        _failed = true;
        notifyListeners();
    }
  }

  // A UTC day may reach a local day only by its edge, with nothing recorded in it (in São Paulo,
  // everything recorded after 21:00 also creates an empty "today"). Opens the newest day that has
  // something, and drops the empty ones from the list.
  Future<void> _openNewestRecordedDay() async {
    while (_days.isNotEmpty) {
      await selectDay(_days.first);
      if (_failed || _timeline.isNotEmpty) return;
      _days = _days.sublist(1);
    }
    _selectedDay = null;
    notifyListeners();
  }

  /// Loads one day on the phone's calendar, which may span two UTC days on the server.
  Future<void> _loadQuota() async {
    final result = await _api.recordingQuota(
      _session.serverUrl,
      _session.credential,
    );
    if (result case ApiSuccess(:final value)) {
      _quota = value;
      notifyListeners();
    }
  }

  Future<void> selectDay(DateTime day) async {
    _selectedDay = day;
    _playing = null;
    _playingFrom = null;
    _setLoading();
    final (windowStart, windowEnd, utcDays) = _window(day);
    final segments = <TimelineSegment>[];
    var piece = 0;
    for (final utcDay in utcDays) {
      final result = await _api.recordings(
        _session.serverUrl,
        _session.credential,
        cameraId,
        utcDay,
      );
      if (result is! ApiSuccess<List<RecordingPieceInfo>>) {
        _loading = false;
        _failed = true;
        notifyListeners();
        return;
      }
      for (final recorded in result.value) {
        for (final segment in recorded.segments) {
          if (segment.start.isBefore(windowStart) ||
              !segment.start.isBefore(windowEnd)) {
            continue;
          }
          segments.add(
            TimelineSegment(
              start: _wall(segment.start),
              end: _wall(segment.end),
              url: segment.url,
              piece: piece,
            ),
          );
        }
        piece++;
      }
    }
    final motion = await _loadMotion(day);
    if (_selectedDay != day) return;
    if (motion == null) {
      _loading = false;
      _failed = true;
      notifyListeners();
      return;
    }
    _timeline = segments;
    _motion = motion;
    _loading = false;
    notifyListeners();
  }

  /// Saves the camera's sensitivity and finds the day's motion again with it.
  Future<void> setSensitivity(MotionSensitivity sensitivity) async {
    _sensitivityFailed = false;
    final failure = await _api.setMotionSensitivity(
      _session.serverUrl,
      _session.credential,
      cameraId,
      sensitivity,
    );
    if (failure != null) {
      _sensitivityFailed = true;
      notifyListeners();
      return;
    }
    _sensitivity = sensitivity;
    final day = _selectedDay;
    if (day != null) {
      final motion = await _loadMotion(day);
      if (_selectedDay == day && motion != null) _motion = motion;
    }
    notifyListeners();
  }

  /// Plays the first motion after the one playing, or the day's first.
  Future<void> playNextMotion() async {
    final from = _playingFrom;
    final next = from == null
        ? _motion.firstOrNull
        : _motion
              .where(
                (mark) => mark.start
                    .subtract(motionLead)
                    .isAfter(from.add(const Duration(seconds: 1))),
              )
              .firstOrNull;
    if (next != null) await playAt(next.start.subtract(motionLead));
  }

  /// Plays the motion before the one playing, or the day's last.
  Future<void> playPreviousMotion() async {
    final from = _playingFrom;
    final previous = from == null
        ? _motion.lastOrNull
        : _motion
              .where(
                (mark) => mark.start
                    .subtract(motionLead)
                    .isBefore(from.subtract(const Duration(seconds: 1))),
              )
              .lastOrNull;
    if (previous != null) await playAt(previous.start.subtract(motionLead));
  }

  /// Plays from a wall-clock time on the selected day. In a gap, starts at the next recording.
  Future<void> playAt(DateTime wallTime) async {
    final index = _timeline.indexWhere(
      (segment) => segment.end.isAfter(wallTime),
    );
    if (index < 0) return;
    final segment = _timeline[index];
    final from = wallTime.isAfter(segment.start)
        ? wallTime.difference(segment.start)
        : Duration.zero;
    await _play(index, from);
  }

  Future<void> _play(int index, Duration from) async {
    _playing = index;
    _playingFrom = _timeline[index].start.add(from);
    notifyListeners();
    await player.play(await _segments.urlFor(_timeline[index].url), from: from);
  }

  Future<void> _playNext() async {
    final current = _playing;
    if (current == null || current + 1 >= _timeline.length) return;
    if (!_onlyMotion) {
      await _play(current + 1, Duration.zero);
      return;
    }
    // Motion that goes on past this segment continues in the next one; otherwise, skips ahead.
    final ended = _timeline[current].end;
    final next = _motion.where((mark) => mark.end.isAfter(ended)).firstOrNull;
    if (next == null) return;
    final lead = next.start.subtract(motionLead);
    await playAt(lead.isAfter(ended) ? lead : ended);
  }

  // Null when the server could not answer.
  Future<List<MotionMark>?> _loadMotion(DateTime day) async {
    final (windowStart, windowEnd, utcDays) = _window(day);
    final marks = <MotionMark>[];
    for (final utcDay in utcDays) {
      final result = await _api.motion(
        _session.serverUrl,
        _session.credential,
        cameraId,
        utcDay,
      );
      if (result is! ApiSuccess<MotionInfo>) return null;
      _sensitivity = result.value.sensitivity;
      for (final found in result.value.events) {
        if (found.start.isBefore(windowStart) ||
            !found.start.isBefore(windowEnd)) {
          continue;
        }
        marks.add(MotionMark(start: _wall(found.start), end: _wall(found.end)));
      }
    }
    return marks;
  }

  // The UTC instants a day on the phone's calendar spans, and the UTC days that hold them.
  (DateTime, DateTime, List<DateTime>) _window(DateTime day) {
    final start = day.subtract(_utcOffsetOf(day));
    final end = start.add(const Duration(days: 1));
    final utcDays = {
      _dateOf(start),
      _dateOf(end.subtract(const Duration(microseconds: 1))),
    }.toList()..sort();
    return (start, end, utcDays);
  }

  // A UTC day covers parts of one or two days on the phone's calendar.
  List<DateTime> _localDays(List<DateTime> utcDays) {
    final days = <DateTime>{};
    for (final utcDay in utcDays) {
      days.add(_dateOf(_wall(utcDay)));
      days.add(
        _dateOf(
          _wall(utcDay.add(const Duration(days: 1)))
              .subtract(const Duration(microseconds: 1)),
        ),
      );
    }
    return days.toList()..sort((a, b) => b.compareTo(a));
  }

  DateTime _wall(DateTime utc) => utc.add(_utcOffsetOf(utc));

  static DateTime _dateOf(DateTime value) =>
      DateTime.utc(value.year, value.month, value.day);

  void _setLoading() {
    _loading = true;
    _failed = false;
    notifyListeners();
  }

  @override
  void dispose() {
    unawaited(player.dispose());
    unawaited(_segments.close());
    super.dispose();
  }
}
