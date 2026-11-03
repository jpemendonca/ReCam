import 'video_clock.dart';

import 'dart:async';

import 'package:flutter/material.dart';
import 'package:intl/intl.dart';

import '../core/media/recording_player.dart';
import '../core/network/api_client.dart';
import '../l10n/generated/app_localizations.dart';
import 'brighten_controller.dart';
import 'brighten_panel.dart';
import 'people_boxes.dart';
import 'recording_timeline_controller.dart';
import 'recordings_controller.dart';
import 'timeline_window.dart';

/// One camera's recordings on a screen of their own, opened from the camera.
class RecordingsTimelineScreen extends StatelessWidget {
  const RecordingsTimelineScreen({
    required this.cameraName,
    required this.create,
    required this.brighten,
    this.recordingCameras = 1,
    this.recording = false,
    this.onLive,
    super.key,
  });

  final String cameraName;

  /// The camera records now: its newest stretch grows to now on the bar.
  final bool recording;

  /// Opens the camera's live view; without it there is no "Live" button.
  final VoidCallback? onLive;

  /// How many cameras record now; they share the recording space.
  final int recordingCameras;

  /// Builds the controller once, in initState; the route builder may run again.
  final RecordingTimelineController Function() create;

  /// This camera's "Brighten" setting on this phone.
  final BrightenController Function() brighten;

  @override
  Widget build(BuildContext context) => RecordingsTimelinePane(
    create: create,
    brighten: brighten,
    recordingCameras: recordingCameras,
    recording: recording,
    onLive: onLive,
    title: AppLocalizations.of(context).timelineTitle(cameraName),
  );
}

/// One camera's recordings: the days, the 24 hours of the chosen day with the recorded stretches
/// and the motion marked, and the player. Tapping the hours plays from that moment. With a
/// [title] it is a screen of its own; the Recordings tab shows it under its camera picker
/// ([header]) instead.
class RecordingsTimelinePane extends StatefulWidget {
  const RecordingsTimelinePane({
    required this.create,
    required this.brighten,
    this.recordingCameras = 1,
    this.recording = false,
    this.onLive,
    this.title,
    this.header,
    super.key,
  });

  /// The camera records now: its newest stretch grows to now on the bar.
  final bool recording;

  /// Opens the camera's live view; without it there is no "Live" button.
  final VoidCallback? onLive;

  /// How many cameras record now; the hours the space holds are shared among them.
  final int recordingCameras;

  /// Builds the controller once, in initState.
  final RecordingTimelineController Function() create;

  /// This camera's "Brighten" setting on this phone.
  final BrightenController Function() brighten;

  /// The screen's title; with it, "Brighten" sits in the app bar.
  final String? title;

  /// Without a [title], shown on the left of the "Brighten" button, above the video.
  final Widget? header;

  @override
  State<RecordingsTimelinePane> createState() => _RecordingsTimelinePaneState();
}

class _RecordingsTimelinePaneState extends State<RecordingsTimelinePane> {
  late final RecordingTimelineController _controller = widget.create();
  late final BrightenController _brighten = widget.brighten();

  @override
  void initState() {
    super.initState();
    unawaited(_controller.load());
    unawaited(_brighten.load());
  }

  @override
  void dispose() {
    _controller.dispose();
    _brighten.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    final l10n = AppLocalizations.of(context);
    final locale = Localizations.localeOf(context).toString();
    final title = widget.title;
    final list = _list(context, l10n, locale);
    if (title == null) return list;
    return Scaffold(
      appBar: AppBar(
        title: Text(title),
        actions: [BrightenButton(controller: _brighten)],
      ),
      body: list,
    );
  }

  String _spaceText(
    AppLocalizations l10n,
    String locale,
    RecordingQuota quota,
  ) {
    final number = NumberFormat('0.#', locale);
    return l10n.timelineSpace(
      number.format(quota.usedBytes / RecordingQuota.bytesPerMegabyte / 1024),
      number.format(quota.megabytes / 1024),
      number.format(
        RecordingsController.hoursFor(quota.megabytes, widget.recordingCameras),
      ),
    );
  }

  Widget _list(BuildContext context, AppLocalizations l10n, String locale) {
    return ListenableBuilder(
      listenable: _controller,
      builder: (context, _) {
        final day = _controller.selectedDay;
        final playing = _controller.playing;
        return ListView(
          padding: const EdgeInsets.all(16),
          children: [
            if (widget.title == null) ...[
              Row(
                children: [
                  Expanded(child: widget.header ?? const SizedBox.shrink()),
                  BrightenButton(controller: _brighten),
                ],
              ),
              const SizedBox(height: 8),
            ],
            AspectRatio(
              aspectRatio: 16 / 9,
              child: ColoredBox(
                color: Colors.black,
                child: playing == null
                    ? Center(
                        child: Padding(
                          padding: const EdgeInsets.all(16),
                          child: Text(
                            l10n.timelineTapToPlay,
                            textAlign: TextAlign.center,
                            style: const TextStyle(color: Colors.white70),
                          ),
                        ),
                      )
                    : Stack(
                        fit: StackFit.expand,
                        children: [
                          Center(
                            child: BrightenedVideo(
                              controller: _brighten,
                              child: _controller.player.buildVideo(),
                            ),
                          ),
                          // Over the picture, outside "Brighten", which only changes the video.
                          if (_controller.playingPeople case final track?
                              when _controller.showPeople)
                            ValueListenableBuilder(
                              valueListenable: _controller.player.aspectRatio,
                              builder: (context, ratio, _) => ratio == null
                                  ? const SizedBox.shrink()
                                  : Center(
                                      child: AspectRatio(
                                        aspectRatio: ratio,
                                        child: PeopleBoxes(
                                          track: track,
                                          position: _controller.player.position,
                                        ),
                                      ),
                                    ),
                            ),
                          // The file's start plus where the player is: it follows every file.
                          ValueListenableBuilder(
                            valueListenable: _controller.player.position,
                            builder: (context, now, _) => VideoClock(
                              time: playing.start.add(now.position),
                            ),
                          ),
                        ],
                      ),
              ),
            ),
            ListenableBuilder(
              listenable: _brighten,
              builder: (context, _) => _brighten.open
                  ? BrightenPanel(controller: _brighten)
                  : const SizedBox.shrink(),
            ),
            if (playing != null) ...[
              _PlayerControls(player: _controller.player),
              if (_controller.playingPeople != null)
                SwitchListTile(
                  key: const Key('show-people'),
                  contentPadding: EdgeInsets.zero,
                  title: Text(l10n.peopleShow),
                  value: _controller.showPeople,
                  onChanged: (value) =>
                      unawaited(_controller.setShowPeople(value)),
                ),
              const SizedBox(height: 8),
              Text(
                l10n.timelinePlaying(
                  DateFormat.Hm(locale).format(playing.start),
                ),
                textAlign: TextAlign.center,
              ),
            ],
            const SizedBox(height: 16),
            if (_controller.failed)
              Text(l10n.timelineLoadFailed, textAlign: TextAlign.center)
            else if (!_controller.loading && _controller.days.isEmpty)
              Text(l10n.timelineEmpty, textAlign: TextAlign.center)
            else ...[
              SizedBox(
                height: 48,
                child: ListView(
                  scrollDirection: Axis.horizontal,
                  children: [
                    for (final option in _controller.days)
                      Padding(
                        padding: const EdgeInsets.only(right: 8),
                        child: ChoiceChip(
                          label: Text(DateFormat.MMMd(locale).format(option)),
                          selected: option == day,
                          onSelected: (_) =>
                              unawaited(_controller.selectDay(option)),
                        ),
                      ),
                  ],
                ),
              ),
              const SizedBox(height: 16),
              if (_controller.quota case final quota?) ...[
                Text(
                  _spaceText(l10n, locale, quota),
                  key: const Key('timeline-space'),
                  textAlign: TextAlign.center,
                  style: Theme.of(context).textTheme.bodySmall,
                ),
                const SizedBox(height: 8),
              ],
              if (_controller.loading)
                const Center(child: CircularProgressIndicator())
              else if (day != null) ...[
                // Keyed: the player appears above it when playback starts, and the zoom and
                // window must survive that.
                _HourBar(
                  key: const Key('timeline-bar'),
                  controller: _controller,
                  day: day,
                  recording: widget.recording,
                  onLive: widget.onLive,
                ),
                const SizedBox(height: 16),
                _MotionControls(controller: _controller),
              ],
            ],
          ],
        );
      },
    );
  }
}

/// Pause, 10 seconds back and ahead, and a bar to move within the file playing.
class _PlayerControls extends StatefulWidget {
  const _PlayerControls({required this.player});

  final RecordingPlayer player;

  @override
  State<_PlayerControls> createState() => _PlayerControlsState();
}

class _PlayerControlsState extends State<_PlayerControls> {
  static const _jump = Duration(seconds: 10);

  /// Where the person drags the bar to, before letting go.
  double? _dragging;

  String _clock(Duration time) {
    final minutes = time.inMinutes;
    final seconds = (time.inSeconds % 60).toString().padLeft(2, '0');
    return '$minutes:$seconds';
  }

  Future<void> _jumpBy(PlaybackPosition now, Duration by) {
    final to = now.position + by;
    return widget.player.seekTo(
      to < Duration.zero
          ? Duration.zero
          : to > now.duration
          ? now.duration
          : to,
    );
  }

  @override
  Widget build(BuildContext context) {
    final l10n = AppLocalizations.of(context);
    return ValueListenableBuilder(
      valueListenable: widget.player.position,
      builder: (context, now, _) {
        final length = now.duration.inMilliseconds.toDouble();
        final at = (_dragging ?? now.position.inMilliseconds.toDouble()).clamp(
          0.0,
          length <= 0 ? 0.0 : length,
        );
        return Row(
          children: [
            IconButton(
              key: const Key('player-back'),
              tooltip: l10n.playerBack,
              onPressed: () => unawaited(_jumpBy(now, -_jump)),
              icon: const Icon(Icons.replay_10),
            ),
            IconButton(
              key: const Key('player-toggle'),
              tooltip: now.playing ? l10n.playerPause : l10n.playerResume,
              onPressed: () => unawaited(
                now.playing ? widget.player.pause() : widget.player.resume(),
              ),
              icon: Icon(now.playing ? Icons.pause : Icons.play_arrow),
            ),
            IconButton(
              key: const Key('player-sound'),
              tooltip: widget.player.muted ? l10n.soundOn : l10n.soundOff,
              onPressed: () async {
                await widget.player.setMuted(!widget.player.muted);
                setState(() {});
              },
              icon: Icon(
                widget.player.muted ? Icons.volume_off : Icons.volume_up,
              ),
            ),
            IconButton(
              key: const Key('player-forward'),
              tooltip: l10n.playerForward,
              onPressed: () => unawaited(_jumpBy(now, _jump)),
              icon: const Icon(Icons.forward_10),
            ),
            Expanded(
              child: Slider(
                key: const Key('player-seek'),
                value: at,
                max: length <= 0 ? 1 : length,
                onChanged: length <= 0
                    ? null
                    : (value) => setState(() => _dragging = value),
                onChangeEnd: (value) {
                  setState(() => _dragging = null);
                  unawaited(
                    widget.player.seekTo(Duration(milliseconds: value.round())),
                  );
                },
              ),
            ),
            Text(
              '${_clock(Duration(milliseconds: at.round()))} / '
              '${_clock(now.duration)}',
            ),
          ],
        );
      },
    );
  }
}

/// Walks the day's motion, filters the bar to it and sets how much movement counts.
class _MotionControls extends StatelessWidget {
  const _MotionControls({required this.controller});

  final RecordingTimelineController controller;

  @override
  Widget build(BuildContext context) {
    final l10n = AppLocalizations.of(context);
    final motion = controller.motion;
    final shown = controller.shownMotion;
    final sensitivity = controller.sensitivity;
    return Column(
      crossAxisAlignment: CrossAxisAlignment.stretch,
      children: [
        Text(
          motion.isEmpty
              ? l10n.motionNone
              : controller.peopleAnalyzed
              ? l10n.motionSummaryPeople(
                  motion.length,
                  controller.people.length,
                )
              : l10n.motionSummary(motion.length),
          textAlign: TextAlign.center,
        ),
        const SizedBox(height: 8),
        Row(
          children: [
            Expanded(
              child: OutlinedButton.icon(
                key: const Key('previous-motion'),
                onPressed: shown.isEmpty
                    ? null
                    : () => unawaited(controller.playPreviousMotion()),
                icon: const Icon(Icons.skip_previous),
                label: Text(l10n.motionPrevious),
              ),
            ),
            const SizedBox(width: 8),
            Expanded(
              child: OutlinedButton.icon(
                key: const Key('next-motion'),
                onPressed: shown.isEmpty
                    ? null
                    : () => unawaited(controller.playNextMotion()),
                icon: const Icon(Icons.skip_next),
                label: Text(l10n.motionNext),
              ),
            ),
          ],
        ),
        SwitchListTile(
          key: const Key('only-motion'),
          contentPadding: EdgeInsets.zero,
          title: Text(l10n.motionOnly),
          value: controller.onlyMotion,
          onChanged: (value) => controller.onlyMotion = value,
        ),
        if (controller.peopleAnalyzed)
          SwitchListTile(
            key: const Key('only-people'),
            contentPadding: EdgeInsets.zero,
            title: Text(l10n.peopleOnly),
            value: controller.onlyPeople,
            onChanged: (value) => controller.onlyPeople = value,
          ),
        Text(l10n.motionSensitivity),
        const SizedBox(height: 8),
        if (sensitivity != null)
          SegmentedButton<MotionSensitivity>(
            segments: [
              ButtonSegment(
                value: MotionSensitivity.low,
                label: Text(l10n.motionLow),
              ),
              ButtonSegment(
                value: MotionSensitivity.medium,
                label: Text(l10n.motionMedium),
              ),
              ButtonSegment(
                value: MotionSensitivity.high,
                label: Text(l10n.motionHigh),
              ),
            ],
            selected: {sensitivity},
            onSelectionChanged: (chosen) =>
                unawaited(controller.setSensitivity(chosen.single)),
          ),
        if (controller.sensitivityFailed) ...[
          const SizedBox(height: 8),
          Text(
            l10n.motionSensitivityFailed,
            style: TextStyle(color: Theme.of(context).colorScheme.error),
          ),
        ],
      ],
    );
  }
}

/// The recordings around the moment under the fixed middle line (1 minute to a whole day), with
/// the day's events above them: yellow with a person when the detect service saw one, grey for
/// motion alone (bullet 13.6). Tapping an event plays it; tapping the bar plays from there;
/// dragging moves the moment and plays from it on release; pressing and holding shows the time
/// under the finger and plays it on release.
class _HourBar extends StatefulWidget {
  const _HourBar({
    super.key,
    required this.controller,
    required this.day,
    required this.recording,
    required this.onLive,
  });

  final RecordingTimelineController controller;
  final DateTime day;
  final bool recording;
  final VoidCallback? onLive;

  @override
  State<_HourBar> createState() => _HourBarState();
}

class _HourBarState extends State<_HourBar> {
  static const _barHeight = 56.0;

  /// An event shorter than this on screen still gets a pill this wide, so the icon always fits.
  static const _minimumEvent = 22.0;
  static const _personEventColor = Color(0xFFF2B705);
  static const _motionEventColor = Color(0xFF9AA0A6);

  /// A recording this close to now is still being written, so its stretch grows to now.
  static const _stillRecording = Duration(minutes: 2);

  late TimelineWindow _window = _initialWindow(TimelineZoom.halfHour);

  /// While the person drags, playback does not move the bar under their finger.
  bool _dragging = false;

  /// Redraws now and then while the camera records, so the newest stretch grows.
  Timer? _grow;

  /// The x of the finger while it is held down, to show the time under it.
  double? _pressX;

  @override
  void initState() {
    super.initState();
    widget.controller.player.position.addListener(_follow);
    if (widget.recording) {
      _grow = Timer.periodic(const Duration(seconds: 5), (_) {
        if (_growingUntil() != null) setState(() {});
      });
    }
  }

  @override
  void dispose() {
    _grow?.cancel();
    widget.controller.player.position.removeListener(_follow);
    super.dispose();
  }

  // The recordings run under the middle line as the video plays; not while the person drags.
  void _follow() {
    final controller = widget.controller;
    final head = controller.playing?.start.add(
      controller.player.position.value.position,
    );
    setState(() {
      if (head != null && !_dragging) {
        _window = TimelineWindow.around(widget.day, _window.zoom, head);
      }
    });
  }

  // While the camera records, the newest stretch grows to now.
  DateTime? _growingUntil() {
    final timeline = widget.controller.timeline;
    if (!widget.recording || timeline.isEmpty) return null;
    final now = widget.controller.wallNow;
    final last = timeline.last.end;
    return now.isAfter(last) && now.difference(last) < _stillRecording
        ? now
        : null;
  }

  // Opens on what is playing, or else on the day's latest recording.
  TimelineWindow _initialWindow(TimelineZoom zoom) {
    final controller = widget.controller;
    final focus =
        controller.playingFrom ??
        _growingUntil() ??
        (controller.timeline.isEmpty
            ? widget.day
            : controller.timeline.last.end);
    return TimelineWindow.around(widget.day, zoom, focus);
  }

  @override
  void didUpdateWidget(_HourBar oldWidget) {
    super.didUpdateWidget(oldWidget);
    if (oldWidget.day != widget.day) _window = _initialWindow(_window.zoom);
  }

  void _playAt(double x, double width) =>
      unawaited(widget.controller.playAt(_window.timeAt(x, width)));

  // An event as a pill over its stretch, never narrower than the icon.
  Widget _event(MotionMark mark, double width) {
    final from = _window.xOf(mark.start, width);
    final to = _window.xOf(mark.end, width);
    final wide = (to - from).clamp(_minimumEvent, double.infinity);
    final person = mark.person == true;
    return Positioned(
      left: (from + to) / 2 - wide / 2,
      width: wide,
      top: 2,
      height: 22,
      child: GestureDetector(
        key: Key(person ? 'event-person' : 'event-motion'),
        onTap: () => unawaited(widget.controller.playMark(mark)),
        child: DecoratedBox(
          decoration: BoxDecoration(
            color: person ? _personEventColor : _motionEventColor,
            borderRadius: BorderRadius.circular(11),
          ),
          child: person
              ? const Center(
                  child: Icon(
                    Icons.person,
                    key: Key('event-person-icon'),
                    size: 16,
                    color: Colors.black87,
                  ),
                )
              : const SizedBox.expand(),
        ),
      ),
    );
  }

  @override
  Widget build(BuildContext context) {
    final l10n = AppLocalizations.of(context);
    final locale = Localizations.localeOf(context).toString();
    final colors = Theme.of(context).colorScheme;
    final controller = widget.controller;
    final withSeconds = _window.zoom == TimelineZoom.minute;
    String clock(DateTime time) => withSeconds
        ? DateFormat.Hms(locale).format(time)
        : DateFormat.Hm(locale).format(time);
    final center = _window.center;
    final centerText =
        '${DateFormat.yMd(locale).format(center)} '
        '${_window.zoom == TimelineZoom.day ? DateFormat.Hm(locale).format(center) : DateFormat.Hms(locale).format(center)}';
    final growingUntil = _growingUntil();
    return Column(
      crossAxisAlignment: CrossAxisAlignment.stretch,
      children: [
        SegmentedButton<TimelineZoom>(
          key: const Key('timeline-zoom'),
          showSelectedIcon: false,
          segments: [
            ButtonSegment(
              value: TimelineZoom.minute,
              label: Text(l10n.timelineZoomMinute),
            ),
            ButtonSegment(
              value: TimelineZoom.fiveMinutes,
              label: Text(l10n.timelineZoomFiveMinutes),
            ),
            ButtonSegment(
              value: TimelineZoom.halfHour,
              label: Text(l10n.timelineZoomHalfHour),
            ),
            ButtonSegment(
              value: TimelineZoom.hour,
              label: Text(l10n.timelineZoomHour),
            ),
            ButtonSegment(
              value: TimelineZoom.day,
              label: Text(l10n.timelineZoomDay),
            ),
          ],
          selected: {_window.zoom},
          onSelectionChanged: (zoom) =>
              setState(() => _window = _window.zoomTo(zoom.single)),
        ),
        const SizedBox(height: 8),
        Row(
          children: [
            Expanded(
              child: Text(
                centerText,
                key: const Key('timeline-center'),
                textAlign: TextAlign.center,
                style: const TextStyle(
                  fontWeight: FontWeight.w600,
                  fontFeatures: [FontFeature.tabularFigures()],
                ),
              ),
            ),
            if (widget.onLive case final onLive?)
              OutlinedButton.icon(
                key: const Key('timeline-live'),
                onPressed: onLive,
                icon: const Icon(Icons.fiber_manual_record, size: 12),
                label: Text(l10n.timelineLive),
              ),
          ],
        ),
        const SizedBox(height: 4),
        LayoutBuilder(
          builder: (context, constraints) => SizedBox(
            height: 26,
            child: ClipRect(
              child: Stack(
                children: [
                  for (final mark in controller.shownMotion)
                    if (mark.end.isAfter(_window.start) &&
                        mark.start.isBefore(_window.end))
                      _event(mark, constraints.maxWidth),
                ],
              ),
            ),
          ),
        ),
        LayoutBuilder(
          builder: (context, constraints) {
            final width = constraints.maxWidth;
            final pressX = _pressX;
            return GestureDetector(
              key: const Key('timeline-hours'),
              onTapUp: (details) => _playAt(details.localPosition.dx, width),
              onHorizontalDragStart: (_) => _dragging = true,
              onHorizontalDragUpdate: (details) => setState(
                () => _window = _window.panBy(details.delta.dx, width),
              ),
              // Letting go plays from the moment under the middle line.
              onHorizontalDragEnd: (_) {
                _dragging = false;
                unawaited(widget.controller.playAt(_window.center));
              },
              onHorizontalDragCancel: () => _dragging = false,
              onLongPressStart: (details) =>
                  setState(() => _pressX = details.localPosition.dx),
              onLongPressMoveUpdate: (details) =>
                  setState(() => _pressX = details.localPosition.dx),
              onLongPressEnd: (details) {
                _playAt(details.localPosition.dx, width);
                setState(() => _pressX = null);
              },
              child: Stack(
                clipBehavior: Clip.none,
                children: [
                  ClipRect(
                    child: SizedBox(
                      width: width,
                      height: _barHeight,
                      child: Stack(
                        children: [
                          CustomPaint(
                            size: Size(width, _barHeight),
                            painter: _HourBarPainter(
                              window: _window,
                              segments: controller.timeline,
                              growUntil: growingUntil,
                              track: colors.surfaceContainerHighest,
                              recorded: colors.primary,
                              ticks: colors.outline,
                            ),
                          ),
                          // Fixed: the recordings run under it.
                          Positioned(
                            left: width / 2 - 1.5,
                            width: 3,
                            top: 0,
                            height: _barHeight,
                            child: ColoredBox(
                              key: const Key('timeline-playhead'),
                              color: colors.error,
                            ),
                          ),
                        ],
                      ),
                    ),
                  ),
                  if (pressX != null)
                    Positioned(
                      left: (pressX - 40).clamp(0, width - 80),
                      top: -30,
                      width: 80,
                      child: Container(
                        key: const Key('timeline-pointer-time'),
                        padding: const EdgeInsets.symmetric(vertical: 4),
                        decoration: BoxDecoration(
                          color: colors.inverseSurface,
                          borderRadius: BorderRadius.circular(8),
                        ),
                        child: Text(
                          clock(_window.timeAt(pressX, width)),
                          textAlign: TextAlign.center,
                          style: TextStyle(color: colors.onInverseSurface),
                        ),
                      ),
                    ),
                ],
              ),
            );
          },
        ),
        const SizedBox(height: 4),
        LayoutBuilder(
          builder: (context, constraints) => SizedBox(
            height: 18,
            child: Stack(
              clipBehavior: Clip.none,
              children: [
                for (final tick in _window.ticks)
                  Positioned(
                    left: (_window.xOf(tick, constraints.maxWidth) - 30).clamp(
                      0,
                      constraints.maxWidth - 60,
                    ),
                    width: 60,
                    child: Text(
                      clock(tick),
                      textAlign: TextAlign.center,
                      style: Theme.of(context).textTheme.bodySmall,
                    ),
                  ),
              ],
            ),
          ),
        ),
      ],
    );
  }
}

class _HourBarPainter extends CustomPainter {
  _HourBarPainter({
    required this.window,
    required this.segments,
    required this.growUntil,
    required this.track,
    required this.recorded,
    required this.ticks,
  });

  final TimelineWindow window;
  final List<TimelineSegment> segments;

  /// The newest stretch, still recording, is drawn up to here.
  final DateTime? growUntil;
  final Color track;
  final Color recorded;
  final Color ticks;

  @override
  void paint(Canvas canvas, Size size) {
    double x(DateTime time) => window.xOf(time, size.width);
    final bar = Rect.fromLTWH(0, 12, size.width, size.height - 24);
    canvas.drawRect(bar, Paint()..color = track);
    for (final segment in segments) {
      final end = segment == segments.last && growUntil != null
          ? growUntil!
          : segment.end;
      if (!end.isAfter(window.start) || !segment.start.isBefore(window.end)) {
        continue;
      }
      final paint = Paint()..color = recorded;
      final left = x(segment.start).clamp(0.0, size.width);
      canvas.drawRect(
        Rect.fromLTRB(
          left,
          bar.top,
          // At least a pixel, so a short stretch is still visible.
          x(end).clamp(left + 1, size.width),
          bar.bottom,
        ),
        paint,
      );
    }
    final tick = Paint()
      ..color = ticks
      ..strokeWidth = 1;
    for (final time in window.ticks) {
      final dx = x(time);
      canvas.drawLine(Offset(dx, bar.bottom), Offset(dx, bar.bottom + 6), tick);
    }
  }

  @override
  bool shouldRepaint(_HourBarPainter oldDelegate) =>
      oldDelegate.segments != segments ||
      oldDelegate.growUntil != growUntil ||
      oldDelegate.window != window;
}
