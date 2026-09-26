import 'dart:async';

import 'package:flutter/material.dart';
import 'package:intl/intl.dart';

import '../core/network/api_client.dart';
import '../l10n/generated/app_localizations.dart';
import 'brighten_controller.dart';
import 'brighten_panel.dart';
import 'recording_timeline_controller.dart';

/// One camera's recordings: the days, the 24 hours of the chosen day with the recorded stretches
/// and the motion marked, and the player. Tapping the hours plays from that moment.
class RecordingsTimelineScreen extends StatefulWidget {
  const RecordingsTimelineScreen({
    required this.cameraName,
    required this.create,
    required this.brighten,
    super.key,
  });

  final String cameraName;

  /// Builds the controller once, in initState; the route builder may run again.
  final RecordingTimelineController Function() create;

  /// This camera's "Brighten" setting on this phone.
  final BrightenController Function() brighten;

  @override
  State<RecordingsTimelineScreen> createState() =>
      _RecordingsTimelineScreenState();
}

class _RecordingsTimelineScreenState extends State<RecordingsTimelineScreen> {
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
    return Scaffold(
      appBar: AppBar(
        title: Text(l10n.timelineTitle(widget.cameraName)),
        actions: [BrightenButton(controller: _brighten)],
      ),
      body: ListenableBuilder(
        listenable: _controller,
        builder: (context, _) {
          final day = _controller.selectedDay;
          final playing = _controller.playing;
          return ListView(
            padding: const EdgeInsets.all(16),
            children: [
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
      ),
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

/// The 24 hours of a day, recorded stretches filled in and motion marked below them (only the
/// motion with "Motion only"). A tap plays from that time.
class _HourBar extends StatelessWidget {
  const _HourBar({required this.controller, required this.day});

  final RecordingTimelineController controller;
  final DateTime day;

  static const _barHeight = 56.0;
  static const _trackTop = 12.0;
  static const _motionHeight = 10.0;
  static const _motionColor = Color(0xFFE08600);

  double _x(DateTime time, double width) =>
      time.difference(day).inMilliseconds / Duration.millisecondsPerDay * width;

  @override
  Widget build(BuildContext context) {
    final colors = Theme.of(context).colorScheme;
    return Column(
      crossAxisAlignment: CrossAxisAlignment.stretch,
      children: [
        LayoutBuilder(
          builder: (context, constraints) => GestureDetector(
            key: const Key('timeline-hours'),
            onTapUp: (details) {
              final fraction = (details.localPosition.dx / constraints.maxWidth)
                  .clamp(0.0, 1.0);
              final tapped = day.add(
                Duration(
                  milliseconds: (fraction * Duration.millisecondsPerDay)
                      .round(),
                ),
              );
              unawaited(controller.playAt(tapped));
            },
            child: Stack(
              children: [
                CustomPaint(
                  size: Size(constraints.maxWidth, _barHeight),
                  painter: _HourBarPainter(
                    day: day,
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
                  Positioned(
                    left: _x(mark.start, constraints.maxWidth),
                    width:
                        (_x(mark.end, constraints.maxWidth) -
                                _x(mark.start, constraints.maxWidth))
                            .clamp(2, constraints.maxWidth),
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
        const SizedBox(height: 4),
        Row(
          mainAxisAlignment: MainAxisAlignment.spaceBetween,
          children: [
            for (final hour in const ['0h', '6h', '12h', '18h', '24h'])
              Text(hour, style: Theme.of(context).textTheme.bodySmall),
          ],
        ),
      ],
    );
  }
}

class _HourBarPainter extends CustomPainter {
  _HourBarPainter({
    required this.day,
    required this.segments,
    required this.playing,
    required this.track,
    required this.recorded,
    required this.current,
    required this.ticks,
  });

  final DateTime day;
  final List<TimelineSegment> segments;
  final TimelineSegment? playing;
  final Color track;
  final Color recorded;
  final Color current;
  final Color ticks;

  @override
  void paint(Canvas canvas, Size size) {
    double x(DateTime time) =>
        time.difference(day).inMilliseconds /
        Duration.millisecondsPerDay *
        size.width;
    final bar = Rect.fromLTWH(0, 12, size.width, size.height - 24);
    canvas.drawRect(bar, Paint()..color = track);
    for (final segment in segments) {
      final paint = Paint()..color = segment == playing ? current : recorded;
      canvas.drawRect(
        Rect.fromLTRB(
          x(segment.start),
          bar.top,
          // At least a pixel, so a one-minute stretch is still visible.
          (x(segment.end)).clamp(x(segment.start) + 1, size.width),
          bar.bottom,
        ),
        paint,
      );
    }
    final tick = Paint()
      ..color = ticks
      ..strokeWidth = 1;
    for (var hour = 0; hour <= 24; hour += 3) {
      final dx = hour / 24 * size.width;
      canvas.drawLine(Offset(dx, bar.bottom), Offset(dx, bar.bottom + 6), tick);
    }
  }

  @override
  bool shouldRepaint(_HourBarPainter oldDelegate) =>
      oldDelegate.segments != segments ||
      oldDelegate.playing != playing ||
      oldDelegate.day != day;
}
