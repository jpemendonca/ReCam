#!/usr/bin/env bash
# Checks scripts/version.sh on throwaway repositories.
set -euo pipefail

script="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)/version.sh"
work=$(mktemp -d)
trap 'rm -rf "$work"' EXIT

fail() {
  echo "version_test: $1" >&2
  exit 1
}

# arrange: outside a repository
[ "$(bash "$script" "$work")" = "dev" ] || fail "outside a repository it should print dev"

# arrange: a clean repository with one commit on a known date
git -C "$work" init -q
git -C "$work" -c user.name=t -c user.email=t@t commit -q --allow-empty -m first \
  --date="2026-09-27T10:00:00" 2>/dev/null
GIT_COMMITTER_DATE="2026-09-27T10:00:00" git -C "$work" -c user.name=t -c user.email=t@t \
  commit -q --amend --allow-empty --no-edit --date="2026-09-27T10:00:00"
hash=$(git -C "$work" rev-parse --short=7 HEAD)

# act + assert: clean
[ "$(bash "$script" "$work")" = "2026.09.27+$hash" ] || fail "clean tree: got $(bash "$script" "$work")"

# act + assert: with an uncommitted file
touch "$work/pending"
[ "$(bash "$script" "$work")" = "2026.09.27+$hash-dirty" ] || fail "dirty tree: got $(bash "$script" "$work")"

echo "version_test: ok"
