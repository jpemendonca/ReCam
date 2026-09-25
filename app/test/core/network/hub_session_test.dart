import 'package:flutter_test/flutter_test.dart';
import 'package:recam/core/network/hub_session.dart';

import '../../support/fakes.dart';

void main() {
  group('HubSession', () {
    test('whenServerIsDown_retriesWithBackoffUntilConnected', () async {
      // arrange
      final client = FakeHubClient()..connectResults.addAll([false, false]);
      final delays = <Duration>[];
      final session = HubSession(
        client: client,
        delay: (duration) async => delays.add(duration),
      );

      // act
      session.start();
      await settle();

      // assert
      expect(client.connectCalls, 3);
      expect(delays, [const Duration(seconds: 1), const Duration(seconds: 2)]);
      expect(session.connected, isTrue);
      await session.stop();
    });

    test('whenConnectionDrops_reconnectsAndRunsOnConnectedAgain', () async {
      // arrange
      final client = FakeHubClient();
      var connectedCount = 0;
      final session = HubSession(client: client, delay: (_) async {})
        ..onConnected = () => connectedCount++;
      session.start();
      await settle();

      // act
      client.drop();
      await settle();

      // assert
      expect(connectedCount, 2);
      expect(session.connected, isTrue);
      await session.stop();
    });

    test('whenHeartbeatFails_reconnects', () async {
      // arrange
      final client = FakeHubClient()..heartbeatResult = false;
      var connectedCount = 0;
      final session = HubSession(client: client, delay: (_) async {})
        ..onConnected = () => connectedCount++;
      session.start();
      await settle();

      // act
      await session.checkConnection();
      await settle();

      // assert
      expect(client.disconnected, isTrue);
      expect(client.connectCalls, 2);
      expect(connectedCount, 2);
      await session.stop();
    });

    test('whenHeartbeatSucceeds_keepsTheConnection', () async {
      // arrange
      final client = FakeHubClient();
      final session = HubSession(client: client, delay: (_) async {});
      session.start();
      await settle();

      // act
      await session.checkConnection();
      await settle();

      // assert
      expect(client.connectCalls, 1);
      expect(session.connected, isTrue);
      await session.stop();
    });

    test('whenConnectionClosesRightAway_reconnects', () async {
      // arrange
      final client = _ClosesDuringConnect();
      final session = HubSession(client: client, delay: (_) async {});

      // act
      session.start();
      await settle();

      // assert
      expect(client.connectCalls, greaterThanOrEqualTo(2));
      await session.stop();
    });

    test('stop_disconnectsAndStaysOffline', () async {
      // arrange
      final client = FakeHubClient();
      final session = HubSession(client: client, delay: (_) async {});
      session.start();
      await settle();

      // act
      await session.stop();
      await settle();

      // assert
      expect(client.disconnected, isTrue);
      expect(session.connected, isFalse);
      expect(client.connectCalls, 1);
    });
  });
}

/// A server that drops the first connection before connect() even returns.
class _ClosesDuringConnect extends FakeHubClient {
  @override
  Future<bool> connect() async {
    final first = connectCalls == 0;
    final connected = await super.connect();
    if (first) drop();
    return connected;
  }
}
