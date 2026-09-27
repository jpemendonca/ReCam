#!/usr/bin/env bash
# Prints the build's version from Git: the commit's date and short hash, like
# 2026.09.27+4b4d231, with -dirty when the working tree has uncommitted changes. Prints "dev"
# outside a Git repository. Used by scripts/build-*.sh for the Docker image and the APK.
set -euo pipefail

dir="${1:-$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)}"
if ! git -C "$dir" rev-parse --verify HEAD >/dev/null 2>&1; then
  echo dev
  exit 0
fi

date=$(git -C "$dir" log -1 --date=format:%Y.%m.%d --format=%cd)
hash=$(git -C "$dir" rev-parse --short=7 HEAD)
dirty=""
if [ -n "$(git -C "$dir" status --porcelain)" ]; then
  dirty="-dirty"
fi
echo "$date+$hash$dirty"
