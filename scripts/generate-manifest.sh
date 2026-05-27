#!/bin/bash
set -euo pipefail

ROOT="$(cd "$(dirname "$0")/.." && pwd)"
MANIFEST="$ROOT/manifest.json"
PROJECT="$ROOT/Jellyfin.Plugin.AnimeFillerSkipper/Jellyfin.Plugin.AnimeFillerSkipper.csproj"
PUBLISH_DIR="$ROOT/publish"
RELEASE_DIR="$ROOT/release"
SCRIPTS_DIR="$ROOT/scripts"

# ── config ──────────────────────────────────────────────
OWNER="mpash"
GUID="a8f1c3e4-5d6b-4a2e-9f1c-7e8d3a5b6c2f"
CATEGORY="General"
TARGET_ABI="10.11.0.0"
DOWNLOAD_BASE="${DOWNLOAD_BASE:-https://github.com/mpash/jellyfin-anime-filler-skipper/releases/download}"

# ── helpers ─────────────────────────────────────────────
_die() { echo "ERROR: $*" >&2; exit 1; }

# ── read version from csproj ────────────────────────────
VERSION=$("$SCRIPTS_DIR/extract-version.sh" 2>/dev/null || \
  grep -oPm1 '<Version>\K[^<]+' "$PROJECT" 2>/dev/null || \
  echo "1.0.0.0")

PLUGIN_DLL="Jellyfin.Plugin.AnimeFillerSkipper.dll"
HTMLPACK_DLL="HtmlAgilityPack.dll"

# ── check publish output exists ─────────────────────────
if [ ! -f "$PUBLISH_DIR/$PLUGIN_DLL" ]; then
    _die "No publish output at $PUBLISH_DIR. Run 'make publish' first."
fi

# ── build zip ───────────────────────────────────────────
mkdir -p "$RELEASE_DIR"

RELEASE_TAG="${RELEASE_TAG:-v${VERSION}}"
if [ "$RELEASE_TAG" != "v${VERSION}" ]; then
    _die "Release tag $RELEASE_TAG does not match project version $VERSION (expected v${VERSION})."
fi
ZIP_NAME="jellyfin-anime-filler-skipper_${VERSION}.zip"
ZIP_PATH="$RELEASE_DIR/$ZIP_NAME"

rm -f "$ZIP_PATH"
(cd "$PUBLISH_DIR" && zip -q -r "$ZIP_PATH" "$PLUGIN_DLL" "$HTMLPACK_DLL")

CHECKSUM=$(md5 -q "$ZIP_PATH" 2>/dev/null || md5sum "$ZIP_PATH" | cut -d' ' -f1)
TIMESTAMP=$(date -u +"%Y-%m-%dT%H:%M:%SZ")
SOURCE_URL="${DOWNLOAD_BASE%/}/${RELEASE_TAG}/${ZIP_NAME}"

# ── upsert manifest entry ───────────────────────────────
python3 - "$VERSION" "$TARGET_ABI" "$SOURCE_URL" "$CHECKSUM" "$TIMESTAMP" "$MANIFEST" "$OWNER" "$RELEASE_TAG" <<'PYEOF'
import json, sys
version, abi, url, checksum, ts, path, owner, tag = sys.argv[1:]
entry = {
    "version": version,
    "changelog": f"See https://github.com/{owner}/jellyfin-anime-filler-skipper/releases/tag/{tag}",
    "targetAbi": abi,
    "sourceUrl": url,
    "checksum": checksum,
    "timestamp": ts
}
with open(path) as f:
    manifest = json.load(f)

versions = manifest[0]["versions"]
for index, current in enumerate(versions):
    if current["version"] == version:
        versions[index] = entry
        break
else:
    versions.insert(0, entry)

versions.sort(key=lambda item: [int(part) for part in item["version"].split(".")], reverse=True)

with open(path, "w") as f:
    json.dump(manifest, f, indent=2)
    f.write("\n")
PYEOF

echo "Upserted version $VERSION in manifest.json"

echo ""
echo "── Release artifacts ──────────────────────────────────"
echo "  Zip:     $ZIP_PATH"
echo "  Checksum: $CHECKSUM"
echo "  Manifest: $MANIFEST"
echo ""
echo "── To publish ─────────────────────────────────────────"
echo "  1. Push the version tag:"
echo "     git tag v$VERSION && git push origin v$VERSION"
echo "  2. Create a GitHub release at v$VERSION and upload $ZIP_NAME"
echo "  3. Commit and push the updated manifest.json"
echo "  4. Users add repo: https://raw.githubusercontent.com/$OWNER/jellyfin-anime-filler-skipper/main/manifest.json"
