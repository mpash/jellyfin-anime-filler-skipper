#!/bin/bash
# ────────────────────────────────────────────────────────
# Downloads Font Awesome Pro using $FORTAWESOME_TOKEN.
# Run locally or in CI. Skips gracefully if token not set.
# ────────────────────────────────────────────────────────
set -euo pipefail

ROOT="$(cd "$(dirname "$0")/.." && pwd)"
DOCS="$ROOT/docs"
FA_DIR="$DOCS/fontawesome"

# ── check if already installed ─────────────────────────
if [ -f "$FA_DIR/css/all.min.css" ]; then
    echo "Font Awesome files already present at $FA_DIR"
    exit 0
fi

# ── check for token ────────────────────────────────────
if [ -z "${FORTAWESOME_TOKEN:-}" ]; then
    echo "SKIP: \$FORTAWESOME_TOKEN not set. Font Awesome Pro icons won't be available." >&2
    echo "      Set the env var locally or add as a GitHub Secret in CI." >&2
    exit 0
fi

# ── configure npm auth ─────────────────────────────────
cleanup_npm() {
    npm config delete "//npm.fontawesome.com/:_authToken" 2>/dev/null || true
    npm config delete "@fortawesome:registry" 2>/dev/null || true
}
trap cleanup_npm EXIT

echo "Configuring Font Awesome npm registry..."
npm config set "@fortawesome:registry" "https://npm.fontawesome.com/"
npm config set "//npm.fontawesome.com/:_authToken" "$FORTAWESOME_TOKEN"

# ── install FA Pro in temp dir ─────────────────────────
TMPDIR=$(mktemp -d)
trap 'rm -rf "$TMPDIR"; cleanup_npm' EXIT

cd "$TMPDIR"
npm init -y --silent
npm install @fortawesome/fontawesome-pro@latest --silent

VERSION=$(node -e "console.log(require('@fortawesome/fontawesome-pro/package.json').version)")

# ── copy to docs ───────────────────────────────────────
rm -rf "$FA_DIR"
mkdir -p "$FA_DIR/css" "$FA_DIR/webfonts"

cp node_modules/@fortawesome/fontawesome-pro/css/all.min.css "$FA_DIR/css/"
cp node_modules/@fortawesome/fontawesome-pro/webfonts/* "$FA_DIR/webfonts/"

# fix relative font paths in css
sed -i '' 's|\.\./webfonts/|../fontawesome/webfonts/|g' "$FA_DIR/css/all.min.css" 2>/dev/null || \
  sed -i 's|\.\./webfonts/|../fontawesome/webfonts/|g' "$FA_DIR/css/all.min.css"

echo "Font Awesome Pro $VERSION installed to docs/fontawesome/"
