import 'dart:async';

import 'package:flutter/widgets.dart';
import 'package:flutter_webrtc/flutter_webrtc.dart';

import '../storage/credential_store.dart';
import 'ice_gathering.dart';
import 'signaling_client.dart';

/// Plays one camera's live video.
abstract interface class WebRtcViewer {
  /// Connects to the stream. Returns false while the camera is not publishing yet or the
  /// server refuses.
  Future<bool> start();

  Future<void> stop();

  /// Plays the camera's sound or keeps it silent; kept for streams started later.
  Future<void> setMuted(bool muted);

  /// Called when a playing stream drops (camera went away, network lost).
  set onEnded(void Function() callback);

  Widget buildVideo();

  Future<void> dispose();
}

typedef WebRtcViewerFactory = WebRtcViewer Function(String cameraId);

/// Receives video with WHEP through the server proxy.
class WhepViewer implements WebRtcViewer {
  WhepViewer({
    required this._session,
    required this._cameraId,
    required this._signaling,
  });

  // Both must stay true: false would turn a receive-only line into a rejected one (port 0), and
  // MediaMTX would have nothing to send on it. A camera without a microphone rejects the audio.
  static const _receiveOnlyOffer = <String, Object>{
    'mandatory': {'OfferToReceiveAudio': true, 'OfferToReceiveVideo': true},
    'optional': <Object>[],
  };

  final PairedSession _session;
  final String _cameraId;
  final SignalingClient _signaling;
  final _renderer = RTCVideoRenderer();

  RTCPeerConnection? _connection;
  Uri? _resource;
  bool _rendererReady = false;
  bool _muted = false;
  void Function()? _onEnded;

  @override
  set onEnded(void Function() callback) => _onEnded = callback;

  @override
  Future<bool> start() async {
    if (_connection != null) return true;
    // Plugin calls fail with exceptions of several types; any of them means "not playing".
    try {
      final playing = await _play();
      if (!playing) await stop();
      return playing;
    } on Object {
      await stop();
      return false;
    }
  }

  Future<bool> _play() async {
    if (!_rendererReady) {
      await _renderer.initialize();
      _rendererReady = true;
    }
    final connection = await createPeerConnection({
      'sdpSemantics': 'unified-plan',
      'iceServers': <Map<String, Object>>[],
    });
    _connection = connection;
    connection.onTrack = (event) => unawaited(_show(event));
    connection.onConnectionState = (state) {
      if (state == RTCPeerConnectionState.RTCPeerConnectionStateFailed ||
          state == RTCPeerConnectionState.RTCPeerConnectionStateDisconnected) {
        _onEnded?.call();
      }
    };
    await connection.addTransceiver(
      kind: RTCRtpMediaType.RTCRtpMediaTypeVideo,
      init: RTCRtpTransceiverInit(direction: TransceiverDirection.RecvOnly),
    );
    await connection.addTransceiver(
      kind: RTCRtpMediaType.RTCRtpMediaTypeAudio,
      init: RTCRtpTransceiverInit(direction: TransceiverDirection.RecvOnly),
    );

    await connection.setLocalDescription(
      await connection.createOffer(_receiveOnlyOffer),
    );
    await waitForIceGathering(connection);
    final offer = await connection.getLocalDescription();
    if (offer?.sdp == null) return false;

    final answer = await _signaling.offer(
      _session.serverUrl.resolve('/whep/$_cameraId'),
      _session.credential,
      offer!.sdp!,
    );
    if (answer == null) return false;
    _resource = answer.resource;
    await connection.setRemoteDescription(
      RTCSessionDescription(answer.sdp, 'answer'),
    );
    return true;
  }

  Future<void> _show(RTCTrackEvent event) async {
    // Android plays remote audio by itself; muting is turning the track off.
    if (event.track.kind == 'audio') {
      event.track.enabled = !_muted;
      return;
    }
    if (event.track.kind != 'video') return;
    if (event.streams.isNotEmpty) {
      _renderer.srcObject = event.streams.first;
      return;
    }
    final stream = await createLocalMediaStream('recam-live');
    await stream.addTrack(event.track);
    _renderer.srcObject = stream;
  }

  @override
  Future<void> stop() async {
    final resource = _resource;
    _resource = null;
    if (resource != null) {
      await _signaling.end(resource, _session.credential);
    }
    final connection = _connection;
    _connection = null;
    connection?.onConnectionState = null;
    await connection?.close();
    if (_rendererReady) _renderer.srcObject = null;
  }

  @override
  Future<void> setMuted(bool muted) async {
    _muted = muted;
    final receivers = await _connection?.getReceivers() ?? const [];
    for (final receiver in receivers) {
      final track = receiver.track;
      if (track != null && track.kind == 'audio') track.enabled = !muted;
    }
  }

  @override
  Widget buildVideo() => RTCVideoView(
    _renderer,
    objectFit: RTCVideoViewObjectFit.RTCVideoViewObjectFitContain,
  );

  @override
  Future<void> dispose() async {
    await stop();
    if (_rendererReady) await _renderer.dispose();
  }
}
