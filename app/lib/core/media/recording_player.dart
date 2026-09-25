import 'dart:async';

import 'package:flutter/widgets.dart';
import 'package:video_player/video_player.dart';

/// Plays recorded segments one at a time.
abstract interface class RecordingPlayer {
  /// Starts [url] at [from]. Replaces whatever was playing.
  Future<void> play(Uri url, {Duration from = Duration.zero});

  /// Called once when the current segment reaches its end.
  set onFinished(void Function() callback);

  Widget buildVideo();

  Future<void> dispose();
}

class VideoPlayerRecordingPlayer implements RecordingPlayer {
  final _current = ValueNotifier<VideoPlayerController?>(null);
  void Function()? _onFinished;
  bool _finishReported = false;

  @override
  set onFinished(void Function() callback) => _onFinished = callback;

  @override
  Future<void> play(Uri url, {Duration from = Duration.zero}) async {
    final previous = _current.value;
    final controller = VideoPlayerController.networkUrl(url);
    _finishReported = false;
    controller.addListener(() => _watchEnd(controller));
    await controller.initialize();
    if (from > Duration.zero) await controller.seekTo(from);
    _current.value = controller;
    await previous?.dispose();
    await controller.play();
  }

  void _watchEnd(VideoPlayerController controller) {
    final value = controller.value;
    if (_finishReported ||
        controller != _current.value ||
        !value.isInitialized ||
        value.duration == Duration.zero ||
        value.position < value.duration) {
      return;
    }
    _finishReported = true;
    _onFinished?.call();
  }

  @override
  Widget buildVideo() => ValueListenableBuilder(
    valueListenable: _current,
    builder: (context, controller, _) => controller == null
        ? const SizedBox.expand()
        : AspectRatio(
            aspectRatio: controller.value.aspectRatio,
            child: VideoPlayer(controller),
          ),
  );

  @override
  Future<void> dispose() async {
    final controller = _current.value;
    _current.value = null;
    await controller?.dispose();
    _current.dispose();
  }
}
