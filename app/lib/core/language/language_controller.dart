import 'package:flutter/widgets.dart';
import 'package:flutter_secure_storage/flutter_secure_storage.dart';

/// The language the app shows: the phone's, or one the person picked in Settings.
enum AppLanguage {
  device,
  portuguese,
  english;

  /// Null follows the phone's language.
  Locale? get locale => switch (this) {
    device => null,
    portuguese => const Locale('pt'),
    english => const Locale('en'),
  };

  static AppLanguage parse(String? name) => values.firstWhere(
    (language) => language.name == name,
    orElse: () => device,
  );
}

/// Where the choice is kept, on this phone only.
abstract interface class LanguageStore {
  Future<AppLanguage> read();

  Future<void> write(AppLanguage language);
}

class SecureLanguageStore implements LanguageStore {
  SecureLanguageStore([FlutterSecureStorage? storage])
    : _storage = storage ?? const FlutterSecureStorage();

  static const _key = 'language';

  final FlutterSecureStorage _storage;

  @override
  Future<AppLanguage> read() async =>
      AppLanguage.parse(await _storage.read(key: _key));

  @override
  Future<void> write(AppLanguage language) => language == AppLanguage.device
      ? _storage.delete(key: _key)
      : _storage.write(key: _key, value: language.name);
}

class LanguageController extends ChangeNotifier {
  LanguageController(this._store);

  final LanguageStore _store;
  AppLanguage _language = AppLanguage.device;

  AppLanguage get language => _language;

  Future<void> load() async {
    _language = await _store.read();
    notifyListeners();
  }

  /// Switches the app's language right away and remembers it.
  Future<void> choose(AppLanguage language) async {
    _language = language;
    notifyListeners();
    await _store.write(language);
  }
}

/// Makes the controller reachable from Settings, deep in the Monitor's tabs.
class LanguageScope extends InheritedNotifier<LanguageController> {
  const LanguageScope({
    required LanguageController controller,
    required super.child,
    super.key,
  }) : super(notifier: controller);

  static LanguageController of(BuildContext context) =>
      context.dependOnInheritedWidgetOfExactType<LanguageScope>()!.notifier!;
}
