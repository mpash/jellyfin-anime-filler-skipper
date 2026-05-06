# Jellyfin Anime Filler Skipper

A Jellyfin plugin that identifies filler episodes in anime series using data from [animefillerlist.com](https://www.animefillerlist.com) and exposes them as skippable media segments.

## Requirements

- Jellyfin server **10.11.0 or later**
- .NET 9.0 runtime

## Installation

### Via Jellyfin plugin repository (recommended)

1. Go to **Dashboard → Plugins → Repositories**
2. Add repository URL: `https://raw.githubusercontent.com/mitch/jellyfin-anime-filler-skipper/main/manifest.json`
3. Go to **Catalog**, find "Anime Filler Skipper", and install

> Full instructions & documentation: **[mitch.github.io/jellyfin-anime-filler-skipper](https://mitch.github.io/jellyfin-anime-filler-skipper)**

### Manual install

```bash
# Download the latest release zip and extract to your plugin directory
mkdir -p <jellyfin-data>/plugins/AnimeFillerSkipper/
cp Jellyfin.Plugin.AnimeFillerSkipper.dll <jellyfin-data>/plugins/AnimeFillerSkipper/
cp HtmlAgilityPack.dll <jellyfin-data>/plugins/AnimeFillerSkipper/
```

Restart Jellyfin.

## Features

- **Automatic filler detection** — Scheduled task scans your anime libraries, maps each series to its animefillerlist.com page, and caches filler/mixed-canon episode numbers
- **Media segment integration** — Filler episodes are exposed as full-episode segments via the Jellyfin segment API (segment type: `Unknown`)
- **ETag-based cache validation** — Conditional HTTP requests avoid re-downloading unchanged pages. 72-hour cache TTL with 2-second request delay to respect animefillerlist.com
- **Configurable** — Toggle whether mixed canon/filler episodes are treated as filler
- **Daily updates** — Filler data refreshes on schedule (default: 4:00 AM)

## Usage

1. After installation, run the **"Update Anime Filler Data"** scheduled task (Dashboard → Scheduled Tasks)
2. When a filler episode plays, the segment is available via the Jellyfin segments API
3. To enable skip prompts for `Unknown` segments, set the user preference `segmentTypeAction__Unknown` to `AskToSkip` or `Skip`:

```bash
curl -X POST "http://localhost:8096/DisplayPreferences/usersettings" \
  -H "X-Emby-Authorization: ..." \
  -d '{"segmentTypeAction__Unknown": "AskToSkip"}'
```

## Development

```bash
make build       # compile
make test        # run tests (66 tests, xUnit + Moq)
make publish     # publish for manual install
make install     # publish + copy to local Jellyfin plugin dir
make release     # publish + generate manifest + create zip for distribution
make clean       # clean all artifacts
make restore     # restore NuGet packages

# Override install path for non-standard setups
make install JELLYFIN_PLUGIN_DIR=/var/lib/jellyfin/plugins/AnimeFillerSkipper
```

### Releasing a new version

1. Update `<Version>` in `Jellyfin.Plugin.AnimeFillerSkipper.csproj`
2. Update `TARGET_ABI` in `scripts/generate-manifest.sh` if jellyfin package versions changed
3. Run `make release`
4. Commit and push the updated `manifest.json`
5. Create a GitHub release for `v<version>` and upload `release/jellyfin-anime-filler-skipper_<version>.zip`

## How it works

1. Scheduled task iterates all Series in your libraries
2. Each series name is converted to an animefillerlist.com slug (e.g. "Bleach" → "bleach")
3. Page is fetched (with `If-None-Match` ETag if cached) and parsed for filler episode numbers
4. Data cached in local JSON with configurable TTL (default 72h)
5. When Jellyfin generates segments for an episode, `FillerSegmentProvider` checks if the episode number is in the filler list
6. If filler, a segment spanning the entire episode (tick 0 → runtime) is created

## Plugin data paths

- Configuration: `<jellyfin-data>/plugins/configurations/Jellyfin.Plugin.AnimeFillerSkipper.xml`
- Filler cache: `<jellyfin-data>/anime_filler_data.json`

## Contributing

### Prerequisites

- [.NET SDK 9.0](https://dotnet.microsoft.com/download/dotnet/9.0)
- [Node.js](https://nodejs.org) (for Font Awesome Pro in docs)

### Setup

```bash
git clone https://github.com/mitch/jellyfin-anime-filler-skipper.git
cd jellyfin-anime-filler-skipper
dotnet restore
```

### Building & testing

```bash
make build       # compile
make test        # run all tests
make publish     # publish for manual install
```

### GitHub Pages docs

The project site at `mpash.github.io/jellyfin-anime-filler-skipper` is built from `docs/`. To develop locally with Font Awesome Pro icons:

```bash
export FORTAWESOME_TOKEN="your-font-awesome-token"
make fontawesome
open docs/index.html
```

If you don't have a Font Awesome Pro token, the docs still work — icons simply won't render.

### CI/CD

- **CI** (`.github/workflows/ci.yml`) — Builds + tests on every push and PR to `main`
- **Release** (`.github/workflows/release.yml`) — Triggered by version tags (`v*`). Builds, tests, creates a GitHub Release with the plugin zip, and updates `manifest.json`. Font Awesome Pro is included in the release build if `FORTAWESOME_TOKEN` is set as a [GitHub Secret](https://docs.github.com/en/actions/security-for-github-actions/security-guides/using-secrets-in-github-actions).

### Adding Font Awesome Pro to CI

1. Go to repo **Settings → Secrets and variables → Actions**
2. Add a new repository secret named `FORTAWESOME_TOKEN`
3. Paste your Font Awesome Pro token as the value

The release workflow will automatically pick it up. The token is never logged or exposed in build output.

### Project structure

See [AGENTS.md](AGENTS.md).
