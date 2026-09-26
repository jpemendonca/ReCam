import 'dart:async';

import 'package:flutter/foundation.dart';
import 'package:flutter/widgets.dart';
import 'package:video_player/video_player.dart';

/// Where the current segment is: the player's controls draw from it.
@immutable
class PlaybackPosition {
  const PlaybackPosition({
    this.position = Duration.zero,
    this.duration = Duration.zero,
    this.playing = false,
  });

  final Duration position;
  final Duration duration;
  final bool playing;

  @override
  bool operator ==(Object other) =>
      other is PlaybackPosition &&
      other.position == position &&
      other.duration == duration &&
      other.playing == playing;

  @override
  int get hashCode => Object.hash(position, duration, playing);
}

/// Plays recorded segments one at a time.
abstract interface class RecordingPlayer {
  /// Starts [url] at [from]. Replaces whatever was playing.
  Future<void> play(Uri url, {Duration from = Duration.zero});

  /// The current segment's position, length and whether it plays.
  ValueListenable<PlaybackPosition> get position;

  Future<void> pause();

  Future<void> resume();

  /// Moves within the current segment.
  Future<void> seekTo(Duration position);

  /// Called once when the current segment reaches its end.
  set onFinished(void Function() callback);

  Widget buildVideo();

  Future<void> dispose();
}

class VideoPlayerRecordingPlayer implements RecordingPlayer {
  final _current = ValueNotifier<VideoPlayerController?>(null);
  final _position = ValueNotifier(const PlaybackPosition());

  @override
  ValueListenable<PlaybackPosition> get position => _position;
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
    if (controller == _current.value && value.isInitialized) {
      _position.value = PlaybackPosition(
        position: value.position,
        duration: value.duration,
        playing: value.isPlaying,
      );
    }
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
  Future<void> pause() async => _current.value?.pause();

  @override
  Future<void> resume() async => _current.value?.play();

  @override
  Future<void> seekTo(Duration position) async =>
      _current.value?.seekTo(position);

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
    _position.dispose();
  }
}
