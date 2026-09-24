#!/usr/bin/env bash
set -euo pipefail

root="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
ran=0

if [ -f "$root/server/Recam.slnx" ]; then
  echo "== server =="
  dotnet format "$root/server/Recam.slnx" --verify-no-changes
  dotnet build "$root/server/Recam.slnx" -warnaserror
  dotnet test --solution "$root/server/Recam.slnx" --no-build
  ran=1
fi

if [ -f "$root/app/pubspec.yaml" ]; then
  echo "== app =="
  (
    cd "$root/app"
    dart format --output=none --set-exit-if-changed .
    flutter analyze
    flutter test
  )
  ran=1
fi

if [ "$ran" -eq 0 ]; then
  echo "gate: no area exists yet (server/Recam.slnx, app/pubspec.yaml)"
fi
