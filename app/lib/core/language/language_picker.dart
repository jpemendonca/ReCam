import 'package:flutter/material.dart';

import '../../l10n/generated/app_localizations.dart';
import 'language_controller.dart';

/// Settings' language choice: the phone's language, Portuguese or English.
class LanguagePicker extends StatelessWidget {
  const LanguagePicker({super.key});

  @override
  Widget build(BuildContext context) {
    final l10n = AppLocalizations.of(context);
    final controller = LanguageScope.of(context);
    return Column(
      crossAxisAlignment: CrossAxisAlignment.stretch,
      children: [
        Text(l10n.languageTitle, style: Theme.of(context).textTheme.titleLarge),
        const SizedBox(height: 16),
        SegmentedButton<AppLanguage>(
          key: const Key('language-picker'),
          showSelectedIcon: false,
          segments: [
            ButtonSegment(
              value: AppLanguage.device,
              label: Text(l10n.languageDevice),
            ),
            ButtonSegment(
              value: AppLanguage.portuguese,
              label: Text(l10n.languagePortuguese),
            ),
            ButtonSegment(
              value: AppLanguage.english,
              label: Text(l10n.languageEnglish),
            ),
          ],
          selected: {controller.language},
          onSelectionChanged: (chosen) => controller.choose(chosen.single),
        ),
      ],
    );
  }
}
