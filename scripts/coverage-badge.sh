#!/usr/bin/env bash
# Turns the line rate of a Cobertura report into a small SVG badge.
# usage: coverage-badge.sh <cobertura.xml> <output.svg>
set -euo pipefail

rate="$(sed -n 's/.*<coverage line-rate="\([0-9.]*\)".*/\1/p' "$1" | head -1)"
percent="$(awk -v rate="$rate" 'BEGIN { printf "%d", rate * 100 + 0.5 }')"
if [ "$percent" -ge 80 ]; then color="#2e7d32"; elif [ "$percent" -ge 60 ]; then color="#f9a825"; else color="#c62828"; fi

cat > "$2" <<SVG
<svg xmlns="http://www.w3.org/2000/svg" width="108" height="20" role="img" aria-label="coverage: ${percent}%">
<title>coverage: ${percent}%</title>
<rect width="63" height="20" fill="#555"/><rect x="63" width="45" height="20" fill="${color}"/>
<g fill="#fff" text-anchor="middle" font-family="Verdana,Geneva,sans-serif" font-size="11">
<text x="31.5" y="14">coverage</text><text x="85.5" y="14">${percent}%</text>
</g>
</svg>
SVG
echo "coverage: ${percent}%"
