.PHONY: build test publish install clean restore manifest release bump-patch bump-minor bump-major

PROJECT = Jellyfin.Plugin.AnimeFillerSkipper/Jellyfin.Plugin.AnimeFillerSkipper.csproj
TEST_PROJECT = tests/Jellyfin.Plugin.AnimeFillerSkipper.Tests/Jellyfin.Plugin.AnimeFillerSkipper.Tests.csproj
OUT_DIR = publish
RELEASE_DIR = release
PLUGIN_NAME = Jellyfin.Plugin.AnimeFillerSkipper
DLL = $(PLUGIN_NAME).dll
HTMLPACK = HtmlAgilityPack.dll
BASH = bash
ifeq ($(OS),Windows_NT)
BASH = cmd /c bash
endif
VERSION ?= $$(grep -oPm1 '<Version>\K[^<]+' $(PROJECT) || echo 1.0.0.0)
JELLYFIN_PLUGIN_DIR ?= $(HOME)/.local/share/jellyfin/plugins/$(PLUGIN_NAME)

build:
	dotnet build $(PROJECT)

test:
	dotnet test $(TEST_PROJECT)

publish: clean
	dotnet publish $(PROJECT) -c Release -o $(OUT_DIR)

install: publish
	mkdir -p $(JELLYFIN_PLUGIN_DIR)
	cp $(OUT_DIR)/$(DLL) $(JELLYFIN_PLUGIN_DIR)/
	cp $(OUT_DIR)/$(HTMLPACK) $(JELLYFIN_PLUGIN_DIR)/
	@echo "Installed to $(JELLYFIN_PLUGIN_DIR)"
	@echo "Restart Jellyfin to load the plugin."

release: publish
	scripts/generate-manifest.sh
	cp manifest.json docs/manifest.json
	@echo ""
	@echo "Release files ready in $(RELEASE_DIR)/"
	@echo "Manifest updated at manifest.json"

manifest: release
	@echo "manifest.json is ready. Host this file:"
	@echo "  https://raw.githubusercontent.com/mpash/jellyfin-anime-filler-skipper/main/manifest.json"

clean:
	dotnet clean $(PROJECT) -c Release
	dotnet clean $(TEST_PROJECT) -c Release
	rm -rf $(OUT_DIR) $(RELEASE_DIR)

restore:
	dotnet restore

# ── version bumping ──────────────────────────────────────
# Version format: major.minor.patch.build (e.g. 1.0.0.0)

bump-patch:
	@$(BASH) scripts/bump-version.sh 2

bump-minor:
	@$(BASH) scripts/bump-version.sh 1

bump-major:
	@$(BASH) scripts/bump-version.sh 0
