#!/usr/bin/env bash
# bump-version.sh <position>
#   0 = major   1.2.3.0 -> 2.0.0.0
#   1 = minor   1.2.3.0 -> 1.3.0.0
#   2 = patch   1.2.3.0 -> 1.2.4.0
set -euo pipefail

POS="${1:?Usage: bump-version.sh <0|1|2>}"
if [[ ! "$POS" =~ ^[0-2]$ ]]; then
    echo "Usage: bump-version.sh <0|1|2> (major|minor|patch)" >&2
    exit 2
fi

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
CSPROJ="$ROOT/Jellyfin.Plugin.AnimeFillerSkipper/Jellyfin.Plugin.AnimeFillerSkipper.csproj"

ver="$(grep -oPm1 '<Version>\K[^<]+' "$CSPROJ")"
IFS='.' read -r major minor patch build <<< "$ver"

if [[ -z "${major:-}" || -z "${minor:-}" || -z "${patch:-}" || -z "${build:-}" ]]; then
    echo "Invalid version '$ver'. Expected major.minor.patch.build." >&2
    exit 1
fi

parts=("$major" "$minor" "$patch" "$build")
parts[$POS]=$((parts[$POS] + 1))
for ((i = POS + 1; i <= 2; i++)); do
    parts[$i]=0
done

new="${parts[0]}.${parts[1]}.${parts[2]}.${parts[3]}"

sed_inplace() {
    if [[ "${OSTYPE:-}" == darwin* ]]; then
        sed -i '' "$@"
    else
        sed -i "$@"
    fi
}

sed_inplace "s|<Version>$ver</Version>|<Version>$new</Version>|" "$CSPROJ"
sed_inplace "s|<AssemblyVersion>$ver</AssemblyVersion>|<AssemblyVersion>$new</AssemblyVersion>|" "$CSPROJ"
sed_inplace "s|<FileVersion>$ver</FileVersion>|<FileVersion>$new</FileVersion>|" "$CSPROJ"

echo "$ver -> $new"
