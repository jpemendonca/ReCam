import 'package:flutter/material.dart';

import '../l10n/generated/app_localizations.dart';

class WatchTab extends StatelessWidget {
  const WatchTab({super.key});

  @override
  Widget build(BuildContext context) {
    return Center(child: Text(AppLocalizations.of(context).watchPlaceholder));
  }
}
