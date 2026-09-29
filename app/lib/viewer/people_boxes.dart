import 'package:flutter/foundation.dart';
import 'package:flutter/material.dart';
import 'package:flutter/scheduler.dart';

import '../core/media/recording_player.dart';
import 'person_track.dart';

/// Green boxes around the people in the segment playing, drawn over the picture only (the
/// recording stays as it is). The player reports its position a few times a second; between
/// reports the time runs on by the clock, so the boxes move smoothly.
class PeopleBoxes extends StatefulWidget {
  const PeopleBoxes({required this.track, required this.position, super.key});

  static const color = Color(0xFF2E7D32);

  final PersonTrack track;
  final ValueListenable<PlaybackPosition> position;

  @override
  State<PeopleBoxes> createState() => _PeopleBoxesState();
}

class _PeopleBoxesState extends State<PeopleBoxes>
    with SingleTickerProviderStateMixin {
  late final Ticker _ticker = createTicker((_) => setState(() {}));
  late PlaybackPosition _reported = widget.position.value;
  final _sinceReport = Stopwatch()..start();

  @override
  void initState() {
    super.initState();
    widget.position.addListener(_onPosition);
    _ticker.start();
  }

  @override
  void didUpdateWidget(PeopleBoxes old) {
    super.didUpdateWidget(old);
    if (old.position != widget.position) {
      old.position.removeListener(_onPosition);
      widget.position.addListener(_onPosition);
    }
  }

  @override
  void dispose() {
    widget.position.removeListener(_onPosition);
    _ticker.dispose();
    super.dispose();
  }

  void _onPosition() {
    _reported = widget.position.value;
    _sinceReport
      ..reset()
      ..start();
  }

  double get _seconds {
    final reported = _reported.position.inMicroseconds / 1e6;
    return _reported.playing
        ? reported + _sinceReport.elapsedMicroseconds / 1e6
        : reported;
  }

  @override
  Widget build(BuildContext context) => CustomPaint(
    key: const Key('people-boxes'),
    painter: _BoxesPainter(widget.track, _seconds),
    size: Size.infinite,
  );
}

class _BoxesPainter extends CustomPainter {
  _BoxesPainter(this.track, this.seconds);

  final PersonTrack track;
  final double seconds;

  @override
  void paint(Canvas canvas, Size size) {
    final paint = Paint()
      ..color = PeopleBoxes.color
      ..style = PaintingStyle.stroke
      ..strokeWidth = 3;
    for (final box in track.at(seconds)) {
      canvas.drawRect(
        Rect.fromLTWH(
          box.x * size.width,
          box.y * size.height,
          box.width * size.width,
          box.height * size.height,
        ),
        paint,
      );
    }
  }

  @override
  bool shouldRepaint(_BoxesPainter old) =>
      old.seconds != seconds || old.track != track;
}
