import 'dart:async';

import 'package:flutter/material.dart';

import '../core/network/api_client.dart';
import '../l10n/generated/app_localizations.dart';
import 'live_view_controller.dart';

/// "Watch all": up to [maxCameras] cameras live at once, in a grid (bullet 13.3). Each live view
/// makes its phone send video, and a weak Monitor decodes every one, so the list never opens this
/// by itself and never more than [maxCameras].
class MultiLiveScreen extends StatefulWidget {
  const MultiLiveScreen({
    required this.cameras,
    required this.create,
    super.key,
  });

  static const maxCameras = 4;

  final List<CameraInfo> cameras;

  /// Builds one camera's live view; the screen starts and closes it.
  final LiveViewController Function(String cameraId) create;

  @override
  State<MultiLiveScreen> createState() => _MultiLiveScreenState();
}

class _MultiLiveScreenState extends State<MultiLiveScreen> {
  late final List<CameraInfo> _cameras = widget.cameras
      .take(MultiLiveScreen.maxCameras)
      .toList();
  late final List<LiveViewController> _lives = [
    for (final camera in _cameras) widget.create(camera.id),
  ];

  @override
  void initState() {
    super.initState();
    for (final live in _lives) {
      unawaited(live.start());
    }
  }

  @override
  void dispose() {
    for (final live in _lives) {
      unawaited(live.close());
      live.dispose();
    }
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    final l10n = AppLocalizations.of(context);
    return Scaffold(
      backgroundColor: Colors.black,
      appBar: AppBar(title: Text(l10n.watchAll)),
      body: GridView.count(
        crossAxisCount: _cameras.length > 1 ? 2 : 1,
        childAspectRatio: 16 / 9,
        mainAxisSpacing: 2,
        crossAxisSpacing: 2,
        children: [
          for (var index = 0; index < _cameras.length; index++)
            _LiveTile(name: _cameras[index].name, live: _lives[index]),
        ],
      ),
    );
  }
}

class _LiveTile extends StatelessWidget {
  const _LiveTile({required this.name, required this.live});

  final String name;
  final LiveViewController live;

  @override
  Widget build(BuildContext context) {
    final l10n = AppLocalizations.of(context);
    return ListenableBuilder(
      listenable: live,
      builder: (context, _) => Stack(
        fit: StackFit.expand,
        children: [
          live.viewer.buildVideo(),
          switch (live.state) {
            LivePlaying() => const SizedBox.shrink(),
            LiveConnecting() => const Center(
              child: CircularProgressIndicator(color: Colors.white),
            ),
            LiveFailed() => Center(
              child: Padding(
                padding: const EdgeInsets.all(8),
                child: Text(
                  l10n.liveFailed,
                  textAlign: TextAlign.center,
                  style: const TextStyle(color: Colors.white70, fontSize: 12),
                ),
              ),
            ),
          },
          Positioned(
            left: 6,
            bottom: 6,
            child: Text(
              name,
              style: const TextStyle(
                color: Colors.white,
                shadows: [Shadow(blurRadius: 4)],
              ),
            ),
          ),
        ],
      ),
    );
  }
}
