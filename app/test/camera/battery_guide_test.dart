```dart
import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:recam/camera/battery_guide.dart';
import 'package:recam/camera/battery_guide_screen.dart';
import 'package:recam/l10n/generated/app_localizations.dart';

import '../support/fakes.dart';

void main() {
  late FakeBatteryOptimization optimization;
  late BatteryGuideController controller;

  setUp(() {
    optimization = FakeBatteryOptimization();
    controller = BatteryGuideController(
      optimization: optimization,
    );
  });

  // ============================================================
  // CONTROLLER
  // ============================================================

  group('BatteryGuideController PRO', () {
    group('Manufacturer detection', () {
      final cases = <(String, BatteryGuide)>[
        ('samsung', BatteryGuide.samsung),
        ('Samsung', BatteryGuide.samsung),
        ('SAMSUNG', BatteryGuide.samsung),
        ('Xiaomi', BatteryGuide.xiaomi),
        ('xiaomi', BatteryGuide.xiaomi),
        ('Redmi', BatteryGuide.xiaomi),
        ('redmi', BatteryGuide.xiaomi),
        ('motorola', BatteryGuide.generic),
        ('Motorola', BatteryGuide.generic),
        ('Google', BatteryGuide.generic),
        ('OnePlus', BatteryGuide.generic),
        ('unknown', BatteryGuide.generic),
        ('', BatteryGuide.generic),
      ];

      for (final (maker, expectedGuide) in cases) {
        test(
          'maker_$maker_returns_${expectedGuide.name}',
          () async {
            optimization.maker = maker;

            final status = await controller.status();

            expect(status.guide, expectedGuide);
          },
        );
      }
    });

    group('Complete status', () {
      test('default_state_is_all_good', () async {
        final status = await controller.status();

        expect(status.allGood, isTrue);
        expect(status.missing, isEmpty);
      });

      test('all_checks_are_detected_when_everything_is_wrong', () async {
        optimization
          ..ignored = false
          ..backgroundRestricted = true
          ..notifications = false;

        final status = await controller.status();

        expect(status.allGood, isFalse);

        expect(
          status.missing,
          containsAll([
            BatteryCheck.optimization,
            BatteryCheck.background,
            BatteryCheck.notifications,
          ]),
        );

        expect(status.missing.length, 3);
      });

      test('only_optimization_can_be_missing', () async {
        optimization
          ..ignored = false
          ..backgroundRestricted = false
          ..notifications = true;

        final status = await controller.status();

        expect(status.allGood, isFalse);
        expect(
          status.missing,
          equals({BatteryCheck.optimization}),
        );
      });

      test('only_background_can_be_missing', () async {
        optimization
          ..ignored = true
          ..backgroundRestricted = true
          ..notifications = true;

        final status = await controller.status();

        expect(status.allGood, isFalse);
        expect(
          status.missing,
          equals({BatteryCheck.background}),
        );
      });

      test('only_notifications_can_be_missing', () async {
        optimization
          ..ignored = true
          ..backgroundRestricted = false
          ..notifications = false;

        final status = await controller.status();

        expect(status.allGood, isFalse);
        expect(
          status.missing,
          equals({BatteryCheck.notifications}),
        );
      });

      test('two_checks_can_be_missing', () async {
        optimization
          ..ignored = false
          ..backgroundRestricted = true
          ..notifications = true;

        final status = await controller.status();

        expect(status.allGood, isFalse);

        expect(
          status.missing,
          equals({
            BatteryCheck.optimization,
            BatteryCheck.background,
          }),
        );
      });
    });

    // ==========================================================
    // FIX ACTIONS
    // ==========================================================

    group('fix', () {
      test('optimization_opens_battery_optimization_request', () async {
        await controller.fix(BatteryCheck.optimization);

        expect(optimization.requests, 1);
        expect(optimization.settingsOpened, 0);
        expect(optimization.notificationRequests, 0);
      });

      test('background_opens_android_settings', () async {
        await controller.fix(BatteryCheck.background);

        expect(optimization.requests, 0);
        expect(optimization.settingsOpened, 1);
        expect(optimization.notificationRequests, 0);
      });

      test('notifications_requests_notification_permission', () async {
        await controller.fix(BatteryCheck.notifications);

        expect(optimization.requests, 0);
        expect(optimization.settingsOpened, 0);
        expect(optimization.notificationRequests, 1);
      });

      test('each_fix_is_independent', () async {
        await controller.fix(BatteryCheck.optimization);
        await controller.fix(BatteryCheck.background);
        await controller.fix(BatteryCheck.notifications);

        expect(optimization.requests, 1);
        expect(optimization.settingsOpened, 1);
        expect(optimization.notificationRequests, 1);
      });

      test('repeated_fix_calls_are_counted', () async {
        await controller.fix(BatteryCheck.optimization);
        await controller.fix(BatteryCheck.optimization);
        await controller.fix(BatteryCheck.notifications);
        await controller.fix(BatteryCheck.notifications);

        expect(optimization.requests, 2);
        expect(optimization.notificationRequests, 2);
      });
    });

    // ==========================================================
    // STATUS REFRESH
    // ==========================================================

    group('status refresh', () {
      test('status_reflects_changes_between_calls', () async {
        optimization.backgroundRestricted = true;

        var status = await controller.status();

        expect(status.missing, contains(BatteryCheck.background));

        optimization.backgroundRestricted = false;

        status = await controller.status();

        expect(status.missing, isNot(contains(BatteryCheck.background)));
        expect(status.allGood, isTrue);
      });

      test('can_transition_from_bad_to_good', () async {
        optimization
          ..ignored = false
          ..backgroundRestricted = true
          ..notifications = false;

        var status = await controller.status();

        expect(status.allGood, isFalse);

        optimization
          ..ignored = true
          ..backgroundRestricted = false
          ..notifications = true;

        status = await controller.status();

        expect(status.allGood, isTrue);
        expect(status.missing, isEmpty);
      });

      test('can_transition_from_good_to_bad', () async {
        var status = await controller.status();

        expect(status.allGood, isTrue);

        optimization.backgroundRestricted = true;

        status = await controller.status();

        expect(status.allGood, isFalse);
        expect(status.missing, contains(BatteryCheck.background));
      });
    });

    // ==========================================================
    // DEVICE-SPECIFIC GUIDES
    // ==========================================================

    group('device guides', () {
      test('samsung_uses_samsung_guide', () async {
        optimization.maker = 'Samsung';

        final status = await controller.status();

        expect(status.guide, BatteryGuide.samsung);
      });

      test('xiaomi_uses_xiaomi_guide', () async {
        optimization.maker = 'Xiaomi';

        final status = await controller.status();

        expect(status.guide, BatteryGuide.xiaomi);
      });

      test('redmi_uses_xiaomi_guide', () async {
        optimization.maker = 'Redmi';

        final status = await controller.status();

        expect(status.guide, BatteryGuide.xiaomi);
      });

      test('motorola_uses_generic_guide', () async {
        optimization.maker = 'Motorola';

        final status = await controller.status();

        expect(status.guide, BatteryGuide.generic);
      });

      test('unknown_manufacturer_uses_generic_guide', () async {
        optimization.maker = 'UnknownBrand';

        final status = await controller.status();

        expect(status.guide, BatteryGuide.generic);
      });
    });
  });

  // ============================================================
  // SCREEN
  // ============================================================

  group('BatteryGuideScreen PRO', () {
    Future<void> show(WidgetTester tester) async {
      await tester.pumpWidget(
        MaterialApp(
          localizationsDelegates:
              AppLocalizations.localizationsDelegates,
          supportedLocales: AppLocalizations.supportedLocales,
          home: BatteryGuideScreen(
            controller: controller,
          ),
        ),
      );

      await tester.pumpAndSettle();
    }

    Finder checkCard(BatteryCheck check) {
      return find.byKey(
        Key('battery-check-${check.name}'),
      );
    }

    Finder fixButton(BatteryCheck check) {
      return find.descendant(
        of: checkCard(check),
        matching: find.text('Fix'),
      );
    }

    // ==========================================================
    // INITIAL STATE
    // ==========================================================

    testWidgets(
      'all_checks_show_as_completed_when_everything_is_good',
      (tester) async {
        await show(tester);

        expect(
          find.byIcon(Icons.check_circle),
          findsNWidgets(3),
        );

        expect(
          find.text('Fix'),
          findsNothing,
        );
      },
    );

    testWidgets(
      'optimization_shows_fix_when_missing',
      (tester) async {
        optimization.ignored = false;

        await show(tester);

        expect(
          fixButton(BatteryCheck.optimization),
          findsOneWidget,
        );

        expect(
          fixButton(BatteryCheck.background),
          findsNothing,
        );

        expect(
          fixButton(BatteryCheck.notifications),
          findsNothing,
        );
      },
    );

    testWidgets(
      'background_shows_fix_when_missing',
      (tester) async {
        optimization.backgroundRestricted = true;

        await show(tester);

        expect(
          fixButton(BatteryCheck.background),
          findsOneWidget,
        );
      },
    );

    testWidgets(
      'notifications_show_fix_when_missing',
      (tester) async {
        optimization.notifications = false;

        await show(tester);

        expect(
          fixButton(BatteryCheck.notifications),
          findsOneWidget,
        );
      },
    );

    // ==========================================================
    // MULTIPLE ERRORS
    // ==========================================================

    testWidgets(
      'all_three_checks_show_fix_buttons_when_all_are_missing',
      (tester) async {
        optimization
          ..ignored = false
          ..backgroundRestricted = true
          ..notifications = false;

        await show(tester);

        expect(
          fixButton(BatteryCheck.optimization),
          findsOneWidget,
        );

        expect(
          fixButton(BatteryCheck.background),
          findsOneWidget,
        );

        expect(
          fixButton(BatteryCheck.notifications),
          findsOneWidget,
        );

        expect(
          find.byIcon(Icons.check_circle),
          findsNothing,
        );
      },
    );

    // ==========================================================
    // FIX BUTTON
    // ==========================================================

    testWidgets(
      'tapping_optimization_fix_calls_controller',
      (tester) async {
        optimization.ignored = false;

        await show(tester);

        await tester.tap(
          fixButton(BatteryCheck.optimization),
        );

        await tester.pumpAndSettle();

        expect(optimization.requests, 1);
      },
    );

    testWidgets(
      'tapping_background_fix_opens_settings',
      (tester) async {
        optimization.backgroundRestricted = true;

        await show(tester);

        await tester.tap(
          fixButton(BatteryCheck.background),
        );

        await tester.pumpAndSettle();

        expect(optimization.settingsOpened, 1);
      },
    );

    testWidgets(
      'tapping_notification_fix_requests_permission',
      (tester) async {
        optimization.notifications = false;

        await show(tester);

        await tester.tap(
          fixButton(BatteryCheck.notifications),
        );

        await tester.pumpAndSettle();

        expect(
          optimization.notificationRequests,
          1,
        );
      },
    );

    // ==========================================================
    // LIFECYCLE
    // ==========================================================

    testWidgets(
      'returning_from_settings_refreshes_status',
      (tester) async {
        optimization.backgroundRestricted = true;

        await show(tester);

        expect(
          fixButton(BatteryCheck.background),
          findsOneWidget,
        );

        await tester.tap(
          fixButton(BatteryCheck.background),
        );

        await tester.pumpAndSettle();

        optimization.backgroundRestricted = false;

        tester.binding.handleAppLifecycleStateChanged(
          AppLifecycleState.paused,
        );

        tester.binding.handleAppLifecycleStateChanged(
          AppLifecycleState.resumed,
        );

        await tester.pumpAndSettle();

        expect(
          fixButton(BatteryCheck.background),
          findsNothing,
        );

        expect(
          find.byIcon(Icons.check_circle),
          findsNWidgets(3),
        );
      },
    );

    testWidgets(
      'multiple_lifecycle_changes_do_not_crash',
      (tester) async {
        await show(tester);

        for (var i = 0; i < 3; i++) {
          tester.binding.handleAppLifecycleStateChanged(
            AppLifecycleState.paused,
          );

          tester.binding.handleAppLifecycleStateChanged(
            AppLifecycleState.resumed,
          );

          await tester.pump();
        }

        expect(
          find.byIcon(Icons.check_circle),
          findsNWidgets(3),
        );
      },
    );

    // ==========================================================
    // XIAOMI / REDMI
    // ==========================================================

    testWidgets(
      'xiaomi_displays_autostart_instruction',
      (tester) async {
        optimization.maker = 'Xiaomi';

        await show(tester);

        expect(
          find.textContaining('Autostart'),
          findsOneWidget,
        );
      },
    );

    testWidgets(
      'xiaomi_displays_lock_instruction',
      (tester) async {
        optimization.maker = 'Xiaomi';

        await show(tester);

        expect(
          find.textContaining('lock ReCam'),
          findsOneWidget,
        );
      },
    );

    testWidgets(
      'xiaomi_does_not_show_checkbox',
      (tester) async {
        optimization.maker = 'Xiaomi';

        await show(tester);

        expect(
          find.byType(Checkbox),
          findsNothing,
        );
      },
    );

    testWidgets(
      'xiaomi_has_continue_camera_button',
      (tester) async {
        optimization.maker = 'Xiaomi';

        await show(tester);

        expect(
          find.text('Continue to camera mode'),
          findsOneWidget,
        );
      },
    );

    testWidgets(
      'redmi_uses_xiaomi_instructions',
      (tester) async {
        optimization.maker = 'Redmi';

        await show(tester);

        expect(
          find.textContaining('Autostart'),
          findsOneWidget,
        );

        expect(
          find.textContaining('lock ReCam'),
          findsOneWidget,
        );
      },
    );

    // ==========================================================
    // ACCESSIBILITY / STRUCTURE
    // ==========================================================

    testWidgets(
      'each_check_has_a_unique_key',
      (tester) async {
        await show(tester);

        expect(
          checkCard(BatteryCheck.optimization),
          findsOneWidget,
        );

        expect(
          checkCard(BatteryCheck.background),
          findsOneWidget,
        );

        expect(
          checkCard(BatteryCheck.notifications),
          findsOneWidget,
        );
      },
    );

    testWidgets(
      'screen_can_be_built_multiple_times',
      (tester) async {
        await show(tester);

        await tester.pumpWidget(
          MaterialApp(
            localizationsDelegates:
                AppLocalizations.localizationsDelegates,
            supportedLocales:
                AppLocalizations.supportedLocales,
            home: BatteryGuideScreen(
              controller: controller,
            ),
          ),
        );

        await tester.pumpAndSettle();

        expect(
          find.byIcon(Icons.check_circle),
          findsNWidgets(3),
        );
      },
    );
  });
}
```

### O que foi acrescentado

Esta versão PRO amplia bastante a cobertura:

* ✅ Samsung em diferentes capitalizações
* ✅ Xiaomi
* ✅ Redmi
* ✅ Motorola
* ✅ fabricantes desconhecidos
* ✅ fabricante vazio
* ✅ todos os checks OK
* ✅ todos os checks com problema
* ✅ cada problema individualmente
* ✅ combinações de dois problemas
* ✅ transição **ruim → boa**
* ✅ transição **boa → ruim**
* ✅ chamadas repetidas de `fix()`
* ✅ teste individual de cada ação Android
* ✅ atualização de status
* ✅ retorno das configurações Android
* ✅ múltiplas mudanças de lifecycle
* ✅ testes específicos para Xiaomi
* ✅ testes específicos para Redmi
* ✅ presença/ausência dos botões `Fix`
* ✅ contagem dos ícones `check_circle`
* ✅ chaves únicas dos componentes
* ✅ reconstrução da tela
* ✅ localização através de `AppLocalizations`
* ✅ interação real com os widgets usando `WidgetTester`

**Observação importante:** os testes de fabricante com `Samsung`, `SAMSUNG`, `xiaomi`, etc. são ótimos para uma versão PRO **se o `BatteryGuideController` realmente normalizar maiúsculas/minúsculas**. Se a implementação atual não fizer isso, esses casos vão falhar — e isso é útil, porque o teste revela uma possível melhoria no código de produção.

Se você quiser levar isso para um nível ainda maior, o próximo passo seria uma **suíte PRO ULTRA**, com `mocktail`, testes de exceções, testes de chamadas assíncronas, cobertura de 90%+, testes de localização em `pt-BR`/inglês e testes de regressão.
