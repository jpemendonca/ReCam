#!/usr/bin/env bash
# Builds the ReCam images (server and detect) with the version from Git (scripts/version.sh).
# Extra arguments go to docker compose (default: -f compose.yaml build).
set -euo pipefail

root="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
export RECAM_VERSION
RECAM_VERSION=$(bash "$root/scripts/version.sh")
cd "$root/deploy"
if [ "$#" -eq 0 ]; then
  set -- -f compose.yaml build
fi
docker compose "$@"
echo "Built ReCam $RECAM_VERSION"
