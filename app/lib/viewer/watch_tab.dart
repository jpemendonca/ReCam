import 'package:flutter/material.dart';

import '../core/network/api_client.dart';
import 'camera_list_controller.dart';
import 'camera_list_view.dart';
import 'viewer_pairing_controller.dart';

class WatchTab extends StatelessWidget {
  const WatchTab({
    required this.pairing,
    required this.api,
    required this.cameraList,
    super.key,
  });

  final ViewerPairingController pairing;
  final ApiClient api;
  final CameraListFactory cameraList;

  @override
  Widget build(BuildContext context) {
    return ListenableBuilder(
      listenable: pairing,
      builder: (context, _) => switch (pairing.state) {
        ViewerPairingLoading() ||
        ViewerPairing() => const Center(child: CircularProgressIndicator()),
        // The shell shows the first-run screen instead while nothing is paired.
        ViewerNotPaired() => const SizedBox.shrink(),
        ViewerPaired(:final session) => CameraListView(
          api: api,
          session: session,
          cameraList: cameraList,
          onPairingLost: pairing.forget,
        ),
      },
    );
  }
}
