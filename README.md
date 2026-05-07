# <img src="docs/anime-skip-plugin-icon.png" width="48" align="left" alt=""> Jellyfin Anime Filler Skipper

> ⚠️ **Work in progress.** This plugin is under active development and has not been thoroughly tested. Download and use at your own risk. A stable, tested release is in development.

A Jellyfin plugin that identifies filler episodes in anime series using data from [animefillerlist.com](https://www.animefillerlist.com) and exposes them as skippable media segments.

## Requirements

- Jellyfin server **10.11.0 or later**
- .NET 9.0 runtime

## Installation

### Via Jellyfin plugin repository (recommended)

1. Go to **Dashboard → Plugins → Repositories**
2. Add repository URL: `https://raw.githubusercontent.com/mpash/jellyfin-anime-filler-skipper/main/manifest.json`
3. Go to **Catalog**, find "Anime Filler Skipper", and install

> Full instructions & documentation: **[mpash.github.io/jellyfin-anime-filler-skipper](https://mpash.github.io/jellyfin-anime-filler-skipper)**

### Manual install

```bash
# Download the latest release zip and extract to your plugin directory
mkdir -p <jellyfin-data>/plugins/AnimeFillerSkipper/
cp Jellyfin.Plugin.AnimeFillerSkipper.dll <jellyfin-data>/plugins/AnimeFillerSkipper/
cp HtmlAgilityPack.dll <jellyfin-data>/plugins/AnimeFillerSkipper/
```

Restart Jellyfin.

#### Verify download integrity

Each release zip carries an MD5 checksum for integrity verification. Before extracting, compare:

```bash
# macOS
md5 -q jellyfin-anime-filler-skipper_1.0.0.0.zip

# Linux
md5sum jellyfin-anime-filler-skipper_1.0.0.0.zip

# Compare against the checksum listed on the releases page or in manifest.json
```

The expected checksums are published on the [releases page](https://mpash.github.io/jellyfin-anime-filler-skipper/#releases) and in [manifest.json](manifest.json).

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

### Setup

```bash
git clone https://github.com/mpash/jellyfin-anime-filler-skipper.git
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

The project site at `mpash.github.io/jellyfin-anime-filler-skipper` is built from `docs/`. Open locally:

```bash
open docs/index.html
```

Icons use Font Awesome Free (CDN). No setup required.

### CI/CD

- **CI** (`.github/workflows/ci.yml`) — Builds + tests on every push and PR to `main`
- **Release** (`.github/workflows/release.yml`) — Triggered by version tags (`v*`). Builds, tests, creates a GitHub Release with the plugin zip, and updates `manifest.json`.

### Project structure

See [AGENTS.md](AGENTS.md).
