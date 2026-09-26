import 'dart:async';

import 'package:flutter/material.dart';
import 'package:intl/intl.dart';

import '../core/media/recording_player.dart';
import '../core/network/api_client.dart';
import '../l10n/generated/app_localizations.dart';
import 'brighten_controller.dart';
import 'brighten_panel.dart';
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
    super.key,
  });

  final String cameraName;

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
    this.title,
    this.header,
    super.key,
  });

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
                    : Center(
                        child: BrightenedVideo(
                          controller: _brighten,
                          child: _controller.player.buildVideo(),
                        ),
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
                _HourBar(controller: _controller, day: day),
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
    final sensitivity = controller.sensitivity;
    return Column(
      crossAxisAlignment: CrossAxisAlignment.stretch,
      children: [
        Text(
          motion.isEmpty ? l10n.motionNone : l10n.motionSummary(motion.length),
          textAlign: TextAlign.center,
        ),
        const SizedBox(height: 8),
        Row(
          children: [
            Expanded(
              child: OutlinedButton.icon(
                key: const Key('previous-motion'),
                onPressed: motion.isEmpty
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
                onPressed: motion.isEmpty
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

/// A stretch of the day (1 minute to 3 hours), recorded stretches filled in and motion marked
/// below them (only the motion with "Motion only"). Tap plays from that time; dragging moves
/// the stretch; pressing and holding shows the time under the finger and plays it on release.
class _HourBar extends StatefulWidget {
  const _HourBar({required this.controller, required this.day});

  final RecordingTimelineController controller;
  final DateTime day;

  @override
  State<_HourBar> createState() => _HourBarState();
}

class _HourBarState extends State<_HourBar> {
  static const _barHeight = 56.0;
  static const _trackTop = 12.0;
  static const _motionHeight = 10.0;
  static const _minimumMark = 3.0;
  static const _motionColor = Color(0xFFE08600);

  late TimelineWindow _window = _initialWindow(TimelineZoom.hour);

  /// The x of the finger while it is held down, to show the time under it.
  double? _pressX;

  // Opens on what is playing, or else on the day's latest recording.
  TimelineWindow _initialWindow(TimelineZoom zoom) {
    final controller = widget.controller;
    final focus =
        controller.playingFrom ??
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
              value: TimelineZoom.quarterHour,
              label: Text(l10n.timelineZoomQuarterHour),
            ),
            ButtonSegment(
              value: TimelineZoom.hour,
              label: Text(l10n.timelineZoomHour),
            ),
            ButtonSegment(
              value: TimelineZoom.threeHours,
              label: Text(l10n.timelineZoomThreeHours),
            ),
          ],
          selected: {_window.zoom},
          onSelectionChanged: (zoom) =>
              setState(() => _window = _window.zoomTo(zoom.single)),
        ),
        const SizedBox(height: 8),
        Row(
          children: [
            IconButton(
              key: const Key('timeline-earlier'),
              tooltip: l10n.timelineEarlier,
              onPressed: _window.atDayStart
                  ? null
                  : () => setState(() => _window = _window.step(-1)),
              icon: const Icon(Icons.chevron_left),
            ),
            Expanded(
              child: Text(
                l10n.timelineWindow(clock(_window.start), clock(_window.end)),
                key: const Key('timeline-window'),
                textAlign: TextAlign.center,
              ),
            ),
            IconButton(
              key: const Key('timeline-later'),
              tooltip: l10n.timelineLater,
              onPressed: _window.atDayEnd
                  ? null
                  : () => setState(() => _window = _window.step(1)),
              icon: const Icon(Icons.chevron_right),
            ),
          ],
        ),
        LayoutBuilder(
          builder: (context, constraints) {
            final width = constraints.maxWidth;
            final pressX = _pressX;
            return GestureDetector(
              key: const Key('timeline-hours'),
              onTapUp: (details) => _playAt(details.localPosition.dx, width),
              onHorizontalDragUpdate: (details) => setState(
                () => _window = _window.panBy(details.delta.dx, width),
              ),
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
                              segments: controller.onlyMotion
                                  ? const []
                                  : controller.timeline,
                              playing: controller.playing,
                              track: colors.surfaceContainerHighest,
                              recorded: colors.primary,
                              current: colors.error,
                              ticks: colors.outline,
                            ),
                          ),
                          for (final mark in controller.motion)
                            if (mark.end.isAfter(_window.start) &&
                                mark.start.isBefore(_window.end))
                              Positioned(
                                left: _window.xOf(mark.start, width),
                                width:
                                    (_window.xOf(mark.end, width) -
                                            _window.xOf(mark.start, width))
                                        .clamp(_minimumMark, 1e9),
                                top: controller.onlyMotion
                                    ? _trackTop
                                    : _barHeight - _trackTop - _motionHeight,
                                height: controller.onlyMotion
                                    ? _barHeight - 2 * _trackTop
                                    : _motionHeight,
                                child: const ColoredBox(
                                  key: Key('motion-mark'),
                                  color: _motionColor,
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
    required this.playing,
    required this.track,
    required this.recorded,
    required this.current,
    required this.ticks,
  });

  final TimelineWindow window;
  final List<TimelineSegment> segments;
  final TimelineSegment? playing;
  final Color track;
  final Color recorded;
  final Color current;
  final Color ticks;

  @override
  void paint(Canvas canvas, Size size) {
    double x(DateTime time) => window.xOf(time, size.width);
    final bar = Rect.fromLTWH(0, 12, size.width, size.height - 24);
    canvas.drawRect(bar, Paint()..color = track);
    for (final segment in segments) {
      if (!segment.end.isAfter(window.start) ||
          !segment.start.isBefore(window.end)) {
        continue;
      }
      final paint = Paint()..color = segment == playing ? current : recorded;
      final left = x(segment.start).clamp(0.0, size.width);
      canvas.drawRect(
        Rect.fromLTRB(
          left,
          bar.top,
          // At least a pixel, so a short stretch is still visible.
          x(segment.end).clamp(left + 1, size.width),
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
      oldDelegate.playing != playing ||
      oldDelegate.window != window;
}
