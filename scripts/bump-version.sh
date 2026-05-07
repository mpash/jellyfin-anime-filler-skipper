#!/bin/bash
# bump-version.sh <position>
#   0 = major   1.2.3.0 → 2.0.0.0
#   1 = minor   1.2.3.0 → 1.3.0.0
#   2 = patch   1.2.3.0 → 1.2.4.0
set -euo pipefail

POS="${1:?Usage: bump-version.sh <0|1|2> (major|minor|patch)}"
ROOT="$(cd "$(dirname "$0")/.." && pwd)"
CSPROJ="$ROOT/Jellyfin.Plugin.AnimeFillerSkipper/Jellyfin.Plugin.AnimeFillerSkipper.csproj"

ver=$(grep '<Version>' "$CSPROJ" | head -1 | sed 's/.*<Version>\(.*\)<\/Version>.*/\1/')
IFS='.' read -ra p <<< "$ver"

p[$POS]=$((p[POS] + 1))
for ((i = POS + 1; i <= 2; i++)); do
    p[$i]=0
done

new="${p[0]}.${p[1]}.${p[2]}.${p[3]}"

sed_inplace() {
    if [[ "$OSTYPE" == darwin* ]]; then
        sed -i '' "$@"
    else
        sed -i "$@"
    fi
}

sed_inplace "s|<Version>$ver</Version>|<Version>$new</Version>|" "$CSPROJ"
sed_inplace "s|<AssemblyVersion>$ver</AssemblyVersion>|<AssemblyVersion>$new</AssemblyVersion>|" "$CSPROJ"
sed_inplace "s|<FileVersion>$ver</FileVersion>|<FileVersion>$new</FileVersion>|" "$CSPROJ"

echo "$ver → $new"
