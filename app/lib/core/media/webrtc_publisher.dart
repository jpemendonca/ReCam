import 'dart:async';

import 'package:flutter_webrtc/flutter_webrtc.dart';

import '../storage/credential_store.dart';
import 'ice_gathering.dart';
import 'signaling_client.dart';

/// Sends this phone's camera to the server.
abstract interface class WebRtcPublisher {
  /// Opens the camera and publishes. Returns false when the camera or the server fails.
  Future<bool> start();

  /// Stops publishing and releases the camera.
  Future<void> stop();

  /// Switches the torch of the camera being published. Returns false when there is no
  /// publishing camera or it has no torch.
  Future<bool> setTorch(bool on);
}

/// Publishes with WHIP through the server proxy: 1280x720, 15 fps, no audio, H.264 first,
/// at most 700 kbps (SPECS.md 2.3). Frames go from the camera to the hardware encoder to the
/// network inside libwebrtc; none pass through Dart.
class WhipPublisher implements WebRtcPublisher {
  WhipPublisher({required this._session, required this._signaling});

  static const _maxBitrate = 700000;
  static const _frameRate = 15;
  static const _sendOnlyOffer = <String, Object>{
    'mandatory': {'OfferToReceiveAudio': false, 'OfferToReceiveVideo': false},
    'optional': <Object>[],
  };

  final PairedSession _session;
  final SignalingClient _signaling;

  RTCPeerConnection? _connection;
  MediaStream? _stream;
  Uri? _resource;

  MediaStreamTrack? get videoTrack => _stream?.getVideoTracks().firstOrNull;

  @override
  Future<bool> start() async {
    if (_connection != null) return true;
    // Camera and codec calls surface platform failures as exceptions of several types;
    // any of them means "could not publish", and the half-built session is released.
    try {
      final published = await _publish();
      if (!published) await stop();
      return published;
    } on Object {
      await stop();
      return false;
    }
  }

  Future<bool> _publish() async {
    final stream = await navigator.mediaDevices.getUserMedia({
      'audio': false,
      'video': {
        'facingMode': 'environment',
        'width': 1280,
        'height': 720,
        'frameRate': _frameRate,
      },
    });
    _stream = stream;

    final connection = await createPeerConnection({
      'sdpSemantics': 'unified-plan',
      'iceServers': <Map<String, Object>>[],
    });
    _connection = connection;

    final transceiver = await connection.addTransceiver(
      track: stream.getVideoTracks().first,
      kind: RTCRtpMediaType.RTCRtpMediaTypeVideo,
      init: RTCRtpTransceiverInit(
        direction: TransceiverDirection.SendOnly,
        streams: [stream],
        sendEncodings: [
          RTCRtpEncoding(maxBitrate: _maxBitrate, maxFramerate: _frameRate),
        ],
      ),
    );
    await _preferH264(transceiver);

    // Without these flags the Android plugin adds receive-only audio and video lines, and
    // MediaMTX refuses an offer with more than one video track.
    await connection.setLocalDescription(
      await connection.createOffer(_sendOnlyOffer),
    );
    await waitForIceGathering(connection);
    final offer = await connection.getLocalDescription();
    if (offer?.sdp == null) return false;

    final answer = await _signaling.offer(
      _session.serverUrl.resolve('/whip/${_session.deviceId}'),
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

  // H.264 is what old phones encode in hardware and what recording will store later. Other
  // codecs stay as fallback for phones without a hardware H.264 encoder.
  static Future<void> _preferH264(RTCRtpTransceiver transceiver) async {
    final capabilities = await getRtpSenderCapabilities('video');
    final codecs = capabilities.codecs ?? [];
    bool isH264(RTCRtpCodecCapability codec) =>
        codec.mimeType.toLowerCase() == 'video/h264';
    await transceiver.setCodecPreferences([
      ...codecs.where(isH264),
      ...codecs.where((codec) => !isH264(codec)),
    ]);
  }

  // The torch belongs to the capture session, so it only exists while publishing.
  @override
  Future<bool> setTorch(bool on) async {
    final track = videoTrack;
    if (track == null) return false;
    // The plugin reports a camera without a torch, or one busy elsewhere, as exceptions.
    try {
      if (!await track.hasTorch()) return false;
      await track.setTorch(on);
      return true;
    } on Object {
      return false;
    }
  }

  @override
  Future<void> stop() async {
    final resource = _resource;
    _resource = null;
    if (resource != null) {
      await _signaling.end(resource, _session.credential);
    }
    await _connection?.close();
    _connection = null;
    for (final track in _stream?.getTracks() ?? <MediaStreamTrack>[]) {
      await track.stop();
    }
    await _stream?.dispose();
    _stream = null;
  }
}
