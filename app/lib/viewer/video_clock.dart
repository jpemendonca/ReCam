import 'dart:async';

import 'package:flutter/material.dart';
import 'package:intl/intl.dart';

/// The time in a corner of the video, drawn on this screen only: the recording and the camera
/// stay as they are.
class VideoClock extends StatelessWidget {
  const VideoClock({super.key, required this.time});

  final DateTime time;

  @override
  Widget build(BuildContext context) {
    final locale = Localizations.localeOf(context).toString();
    return Align(
      alignment: Alignment.topRight,
      child: Container(
        key: const Key('video-clock'),
        margin: const EdgeInsets.all(8),
        padding: const EdgeInsets.symmetric(horizontal: 6, vertical: 2),
        decoration: BoxDecoration(
          color: Colors.black54,
          borderRadius: BorderRadius.circular(4),
        ),
        child: Text(
          DateFormat.Hms(locale).format(time),
          style: const TextStyle(
            color: Colors.white,
            fontFeatures: [FontFeature.tabularFigures()],
          ),
        ),
      ),
    );
  }
}

/// The clock on the live video: the time now, every second.
class LiveClock extends StatefulWidget {
  const LiveClock({super.key, this.now = DateTime.now});

  final DateTime Function() now;

  @override
  State<LiveClock> createState() => _LiveClockState();
}

class _LiveClockState extends State<LiveClock> {
  late final Timer _timer;

  @override
  void initState() {
    super.initState();
    _timer = Timer.periodic(const Duration(seconds: 1), (_) => setState(() {}));
  }

  @override
  void dispose() {
    _timer.cancel();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) => VideoClock(time: widget.now());
}
