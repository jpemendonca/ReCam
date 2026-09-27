/// This build's version, from Git at build time (scripts/build-apk.sh passes it with
/// --dart-define), or "dev" for builds without it, like the tests'.
const appVersion = String.fromEnvironment('RECAM_VERSION', defaultValue: 'dev');
