#!/bin/bash
set -euo pipefail
ROOT="$(cd "$(dirname "$0")/.." && pwd)"
PROJECT="$ROOT/Jellyfin.Plugin.AnimeFillerSkipper/Jellyfin.Plugin.AnimeFillerSkipper.csproj"

grep -oPm1 '<Version>\K[^<]+' "$PROJECT" 2>/dev/null || echo "1.0.0.0"
