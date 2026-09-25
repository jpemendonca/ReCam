import 'dart:async';

import 'package:flutter/material.dart';
import 'package:intl/intl.dart';

import '../l10n/generated/app_localizations.dart';
import 'recording_timeline_controller.dart';

/// One camera's recordings: the days, the 24 hours of the chosen day with the recorded stretches
/// marked, and the player. Tapping the hours plays from that moment.
class RecordingsTimelineScreen extends StatefulWidget {
  const RecordingsTimelineScreen({
    required this.cameraName,
    required this.create,
    super.key,
  });

  final String cameraName;

  /// Builds the controller once, in initState; the route builder may run again.
  final RecordingTimelineController Function() create;

  @override
  State<RecordingsTimelineScreen> createState() =>
      _RecordingsTimelineScreenState();
}

class _RecordingsTimelineScreenState extends State<RecordingsTimelineScreen> {
  late final RecordingTimelineController _controller = widget.create();

  @override
  void initState() {
    super.initState();
    unawaited(_controller.load());
  }

  @override
  void dispose() {
    _controller.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    final l10n = AppLocalizations.of(context);
    final locale = Localizations.localeOf(context).toString();
    return Scaffold(
      appBar: AppBar(title: Text(l10n.timelineTitle(widget.cameraName))),
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
                      : Center(child: _controller.player.buildVideo()),
                ),
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
                else if (day != null)
                  _HourBar(controller: _controller, day: day),
              ],
            ],
          );
        },
      ),
    );
  }
}

/// The 24 hours of a day, recorded stretches filled in. A tap plays from that time.
class _HourBar extends StatelessWidget {
  const _HourBar({required this.controller, required this.day});

  final RecordingTimelineController controller;
  final DateTime day;

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
            child: CustomPaint(
              size: Size(constraints.maxWidth, 56),
              painter: _HourBarPainter(
                day: day,
                segments: controller.timeline,
                playing: controller.playing,
                track: colors.surfaceContainerHighest,
                recorded: colors.primary,
                current: colors.error,
                ticks: colors.outline,
              ),
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
