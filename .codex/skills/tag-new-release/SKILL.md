---
name: tag-new-release
description: Tag a new release for this Jellyfin plugin repository. Use when the user asks to create, prepare, tag, retag, or fix a release tag for jellyfin-anime-filler-skipper, especially when the Git tag must match the plugin version in Jellyfin.Plugin.AnimeFillerSkipper.csproj.
---

# Tag New Release

## Rule

Release tag must equal `v<Version>` from `Jellyfin.Plugin.AnimeFillerSkipper/Jellyfin.Plugin.AnimeFillerSkipper.csproj`.

Example: `<Version>1.0.3.0</Version>` requires tag `v1.0.3.0`.

Do not create `v0.1.xa`, `main`, or marketing-only tags for installable plugin releases.

## Workflow

1. Inspect state:
   ```bash
   git status --short
   git tag --points-at HEAD
   grep -oPm1 '<Version>\K[^<]+' Jellyfin.Plugin.AnimeFillerSkipper/Jellyfin.Plugin.AnimeFillerSkipper.csproj
   ```

2. If preparing a new version, update all three project version fields together:
   ```xml
   <Version>1.0.3.0</Version>
   <AssemblyVersion>1.0.3.0</AssemblyVersion>
   <FileVersion>1.0.3.0</FileVersion>
   ```

   Prefer `make bump-patch`, `make bump-minor`, or `make bump-major` when appropriate.

3. Validate before tagging:
   ```bash
   dotnet build --configuration Release
   ```

   Run `dotnet test --configuration Release` if .NET 9 runtime is available locally. If missing, say so; GitHub Actions installs `.NET 9.0.x`.

4. Commit the version bump before tagging:
   ```bash
   git add Jellyfin.Plugin.AnimeFillerSkipper/Jellyfin.Plugin.AnimeFillerSkipper.csproj
   git commit -m "chore: bump plugin version to <version>"
   ```

5. Create and push the matching tag:
   ```bash
   git tag v<version>
   git push origin main
   git push origin v<version>
   ```

The release workflow builds the zip, creates/updates the GitHub Release, computes MD5, and upserts `manifest.json` plus `docs/manifest.json`.

## Fixing A Bad Tag

If Actions fails with:

```text
Tag vX does not match project version Y
```

Fix the project version or tag so they match. If the tag already exists on the wrong commit:

```bash
git tag -f v<version>
git push --force origin v<version>
```

Only force-push a release tag after confirming it is the intended correction.

## Manifest

Do not hand-edit manifest checksums for a new release. Let `.github/workflows/release.yml` update them from the uploaded asset.

Manual manifest edits are only for repairing old releases. In that case, verify the asset MD5 matches the exact `sourceUrl`.
