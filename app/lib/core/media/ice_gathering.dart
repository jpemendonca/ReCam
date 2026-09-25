import 'dart:async';

import 'package:flutter_webrtc/flutter_webrtc.dart';

/// Waits until the connection has all its local candidates, so the offer carries them and
/// no trickle ICE is needed. On a LAN this takes milliseconds; [timeout] caps odd networks.
Future<void> waitForIceGathering(
  RTCPeerConnection connection, {
  Duration timeout = const Duration(seconds: 3),
}) async {
  if (connection.iceGatheringState ==
      RTCIceGatheringState.RTCIceGatheringStateComplete) {
    return;
  }
  final done = Completer<void>();
  connection.onIceGatheringState = (state) {
    if (state == RTCIceGatheringState.RTCIceGatheringStateComplete &&
        !done.isCompleted) {
      done.complete();
    }
  };
  await done.future.timeout(timeout, onTimeout: () {});
}
