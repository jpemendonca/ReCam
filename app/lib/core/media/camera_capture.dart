import 'dart:async';

import 'package:flutter/widgets.dart';
import 'package:flutter_webrtc/flutter_webrtc.dart';

/// The open back camera. Opaque outside `core/media`: whoever holds it can only show it.
abstract interface class CameraFeed {
  /// A live thumbnail of what the camera films. Nothing is recorded or stored.
  Widget buildPreview();
}

/// Opens and releases the back camera. One feed at a time; publishing and the thumbnail
/// share it.
abstract interface class CameraCapture {
  /// Returns null when the camera cannot be opened (no permission, busy).
  Future<CameraFeed?> open();

  Future<void> close();
}

/// Opens the camera with `flutter_webrtc` at 1280x720 and 15 fps, with the microphone when it
/// is allowed (SPECS.md 2.3). Without it, the camera still sends video.
class PluginCameraCapture implements CameraCapture {
  static const frameRate = 15;

  PluginCameraFeed? _feed;

  @override
  Future<CameraFeed?> open() async {
    final current = _feed;
    if (current != null) return current;
    // Camera failures arrive as exceptions of several types; all mean "no camera".
    final stream =
        await _getUserMedia(withAudio: true) ??
        await _getUserMedia(withAudio: false);
    return stream == null ? null : _feed = PluginCameraFeed(stream);
  }

  /// What the camera asks the phone for: the back camera at 720p and, when asked, the
  /// microphone.
  static Map<String, Object> constraints({required bool withAudio}) => {
    'audio': withAudio,
    'video': {
      'facingMode': 'environment',
      'width': 1280,
      'height': 720,
      'frameRate': frameRate,
    },
  };

  Future<MediaStream?> _getUserMedia({required bool withAudio}) async {
    try {
      return await navigator.mediaDevices.getUserMedia(
        constraints(withAudio: withAudio),
      );
    } on Object {
      return null;
    }
  }

  @override
  Future<void> close() async {
    final feed = _feed;
    _feed = null;
    if (feed == null) return;
    for (final track in feed.stream.getTracks()) {
      await track.stop();
    }
    await feed.stream.dispose();
  }
}

class PluginCameraFeed implements CameraFeed {
  PluginCameraFeed(this.stream);

  final MediaStream stream;

  MediaStreamTrack? get videoTrack => stream.getVideoTracks().firstOrNull;

  /// Null when the microphone was not allowed.
  MediaStreamTrack? get audioTrack => stream.getAudioTracks().firstOrNull;

  @override
  Widget buildPreview() => _LocalPreview(stream: stream);
}

/// Owns the renderer for as long as the thumbnail is on screen.
class _LocalPreview extends StatefulWidget {
  const _LocalPreview({required this.stream});

  final MediaStream stream;

  @override
  State<_LocalPreview> createState() => _LocalPreviewState();
}

class _LocalPreviewState extends State<_LocalPreview> {
  final _renderer = RTCVideoRenderer();
  bool _ready = false;

  @override
  void initState() {
    super.initState();
    unawaited(_attach());
  }

  Future<void> _attach() async {
    await _renderer.initialize();
    _renderer.srcObject = widget.stream;
    if (mounted) setState(() => _ready = true);
  }

  @override
  void dispose() {
    unawaited(_renderer.dispose());
    super.dispose();
  }

  @override
  Widget build(BuildContext context) => _ready
      ? RTCVideoView(
          _renderer,
          objectFit: RTCVideoViewObjectFit.RTCVideoViewObjectFitCover,
        )
      : const SizedBox.expand();
}
