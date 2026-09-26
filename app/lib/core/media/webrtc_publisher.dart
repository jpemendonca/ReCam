import 'dart:async';

import 'package:flutter_webrtc/flutter_webrtc.dart';

import '../storage/credential_store.dart';
import 'camera_capture.dart';
import 'ice_gathering.dart';
import 'signaling_client.dart';
import 'video_quality.dart';

/// Sends this phone's camera to the server.
abstract interface class WebRtcPublisher {
  /// Publishes the open camera. Returns false when the server or the codec setup fails.
  Future<bool> start(CameraFeed feed);

  /// Stops publishing. The camera stays open; whoever opened it closes it.
  Future<void> stop();

  /// Switches the torch of the camera being published. Returns false when there is no
  /// publishing camera or it has no torch.
  Future<bool> setTorch(bool on);

  /// Changes what is sent without reopening the camera. Kept for the next [start] too.
  Future<void> setQuality(VideoQuality quality);

  /// Whether this phone can encode H.264, the only codec the server records (SPECS.md 11).
  Future<bool> canSendH264();
}

/// Publishes with WHIP through the server proxy: Opus audio when there is a microphone, H.264 first, at the current
/// [VideoQuality] (SPECS.md 2.3). Frames go from the camera to the hardware encoder to the network inside
/// libwebrtc; none pass through Dart.
class WhipPublisher implements WebRtcPublisher {
  WhipPublisher({required this._session, required this._signaling});

  static const _sendOnlyOffer = <String, Object>{
    'mandatory': {'OfferToReceiveAudio': false, 'OfferToReceiveVideo': false},
    'optional': <Object>[],
  };

  final PairedSession _session;
  final SignalingClient _signaling;

  RTCPeerConnection? _connection;
  RTCRtpSender? _sender;
  VideoQuality _quality = VideoQuality.full;
  PluginCameraFeed? _feed;
  Uri? _resource;

  @override
  Future<bool> start(CameraFeed feed) async {
    if (_connection != null) return true;
    if (feed is! PluginCameraFeed) return false;
    _feed = feed;
    // Codec and network calls surface platform failures as exceptions of several types;
    // any of them means "could not publish", and the half-built session is released.
    try {
      final published = await _publish(feed);
      if (!published) await stop();
      return published;
    } on Object {
      await stop();
      return false;
    }
  }

  Future<bool> _publish(PluginCameraFeed feed) async {
    final track = feed.videoTrack;
    if (track == null) return false;
    final connection = await createPeerConnection({
      'sdpSemantics': 'unified-plan',
      'iceServers': <Map<String, Object>>[],
    });
    _connection = connection;

    final transceiver = await connection.addTransceiver(
      track: track,
      kind: RTCRtpMediaType.RTCRtpMediaTypeVideo,
      init: RTCRtpTransceiverInit(
        direction: TransceiverDirection.SendOnly,
        streams: [feed.stream],
        sendEncodings: [
          RTCRtpEncoding(
            maxBitrate: _quality.maxBitrate,
            maxFramerate: _quality.maxFramerate,
            scaleResolutionDownBy: _quality.scaleDownBy,
          ),
        ],
      ),
    );
    _sender = transceiver.sender;
    await _preferH264(transceiver);
    final audio = feed.audioTrack;
    if (audio != null) {
      // Opus is libwebrtc's default for audio, and what the recordings keep.
      await connection.addTransceiver(
        track: audio,
        kind: RTCRtpMediaType.RTCRtpMediaTypeAudio,
        init: RTCRtpTransceiverInit(
          direction: TransceiverDirection.SendOnly,
          streams: [feed.stream],
        ),
      );
    }

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

  // libwebrtc lists H.264 only when the phone has an encoder for it.
  @override
  Future<bool> canSendH264() async {
    // A plugin failure means the answer is unknown; assume the common case, H.264.
    try {
      final capabilities = await getRtpSenderCapabilities('video');
      return (capabilities.codecs ?? []).any(_isH264);
    } on Object {
      return true;
    }
  }

  static bool _isH264(RTCRtpCodecCapability codec) =>
      codec.mimeType.toLowerCase() == 'video/h264';

  // H.264 is what old phones encode in hardware and what recording stores. Other codecs stay
  // as fallback for phones without a hardware H.264 encoder.
  static Future<void> _preferH264(RTCRtpTransceiver transceiver) async {
    final capabilities = await getRtpSenderCapabilities('video');
    final codecs = capabilities.codecs ?? [];
    await transceiver.setCodecPreferences([
      ...codecs.where(_isH264),
      ...codecs.where((codec) => !_isH264(codec)),
    ]);
  }

  // The torch belongs to the capture session; the server only asks while publishing.
  @override
  Future<bool> setTorch(bool on) async {
    final track = _connection == null ? null : _feed?.videoTrack;
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
  Future<void> setQuality(VideoQuality quality) async {
    _quality = quality;
    final sender = _sender;
    if (sender == null) return;
    final parameters = sender.parameters;
    for (final encoding in parameters.encodings ?? <RTCRtpEncoding>[]) {
      encoding
        ..maxBitrate = quality.maxBitrate
        ..maxFramerate = quality.maxFramerate
        ..scaleResolutionDownBy = quality.scaleDownBy;
    }
    // A sender that is closing refuses new parameters; the next start uses the quality anyway.
    try {
      await sender.setParameters(parameters);
    } on Object {
      return;
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
    _sender = null;
    _feed = null;
  }
}
