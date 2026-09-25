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

/// A camera's recordings, day by day, in the phone's time zone, and playback that starts where
/// the person taps and moves on to the next segment by itself.
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

  List<DateTime> _days = const [];
  DateTime? _selectedDay;
  List<TimelineSegment> _timeline = const [];
  bool _loading = true;
  bool _failed = false;
  int? _playing;

  /// Days with recordings, newest first, as dates on the phone's calendar.
  List<DateTime> get days => _days;

  DateTime? get selectedDay => _selectedDay;

  /// The selected day's segments, in order.
  List<TimelineSegment> get timeline => _timeline;

  bool get loading => _loading;

  bool get failed => _failed;

  TimelineSegment? get playing =>
      _playing == null ? null : _timeline[_playing!];

  Future<void> load() async {
    _setLoading();
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
        if (_days.isNotEmpty) await selectDay(_days.first);
      case ApiFailure():
        _loading = false;
        _failed = true;
        notifyListeners();
    }
  }

  /// Loads one day on the phone's calendar, which may span two UTC days on the server.
  Future<void> selectDay(DateTime day) async {
    _selectedDay = day;
    _playing = null;
    _setLoading();
    final windowStart = day.subtract(_utcOffsetOf(day));
    final windowEnd = windowStart.add(const Duration(days: 1));
    final utcDays = {
      _dateOf(windowStart),
      _dateOf(windowEnd.subtract(const Duration(microseconds: 1))),
    };
    final segments = <TimelineSegment>[];
    var piece = 0;
    for (final utcDay in utcDays.toList()..sort()) {
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
    if (_selectedDay != day) return;
    _timeline = segments;
    _loading = false;
    notifyListeners();
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
    notifyListeners();
    await player.play(await _segments.urlFor(_timeline[index].url), from: from);
  }

  Future<void> _playNext() async {
    final current = _playing;
    if (current == null || current + 1 >= _timeline.length) return;
    await _play(current + 1, Duration.zero);
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
