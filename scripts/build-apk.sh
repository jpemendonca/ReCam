#!/usr/bin/env bash
# Builds the app with the version from Git (scripts/version.sh): shown in the app, and as the
# APK's version name. The version code is the commit count, so each build installs over the last.
# Extra arguments go to flutter build apk (default --debug).
set -euo pipefail

root="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
version=$(bash "$root/scripts/version.sh")
count=$(git -C "$root" rev-list --count HEAD 2>/dev/null || echo 1)
cd "$root/app"
flutter build apk "${@:---debug}" \
  --build-name="${version%%+*}" \
  --build-number="$count" \
  --dart-define=RECAM_VERSION="$version"
echo "Built ReCam $version"
