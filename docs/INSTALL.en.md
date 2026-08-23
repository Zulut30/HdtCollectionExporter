# Installation Guide

This guide explains how to install HDT Collection Exporter by Manacost, a plugin for [HearthSim/Hearthstone-Deck-Tracker](https://github.com/HearthSim/Hearthstone-Deck-Tracker).

For macOS/HSTracker, see [HSTracker macOS Support](HSTRACKER_MACOS.en.md). HSTracker currently needs the Swift source adapter to be added to an HSTracker build; it cannot load the Windows HDT DLL.

## 1. Requirements

- Windows
- Hearthstone Deck Tracker installed

## 2. Download The Release

Open the latest release and download:

```text
HdtCollectionExporter.dll
```

[Latest Release](https://github.com/Zulut30/HdtCollectionExporter/releases/latest)

## 3. Build From Source, Optional

If you want to build the plugin yourself, install Visual Studio 2022 Build Tools or Visual Studio, then run:

Open PowerShell in the repository root and run:

```powershell
.\build.ps1
```

If the script cannot find HDT automatically, pass the HDT app folder:

```powershell
.\build.ps1 -HDTInstallDir "C:\Users\<you>\AppData\Local\HearthstoneDeckTracker\app-1.52.14"
```

The build output is:

```text
src\HdtCollectionExporter\bin\Release\HdtCollectionExporter.dll
```

## 4. Open The HDT Plugins Folder

In Hearthstone Deck Tracker:

1. Open `Options`.
2. Go to `Tracker`.
3. Open `Plugins`.
4. Click `Plugins Folder`.

This usually opens:

```text
%AppData%\HearthstoneDeckTracker\Plugins
```

## 5. Copy The Correct File

Copy only:

```text
HdtCollectionExporter.dll
```

Do not copy:

- `HdtCollectionExporter.sln`
- `.csproj` files
- source `.cs` files
- `bin` or `obj` folders

HDT loads `.dll` plugin files only.

## 6. Restart HDT

Fully close HDT, including the tray icon near the clock, then start HDT again.

If HDT was already running while you copied the DLL, restart is important because HDT may keep the old DLL loaded.

## 7. Enable The Plugin

In `Options > Tracker > Plugins`, enable either:

- `Collection Exporter by Manacost`
- `Экспорт коллекции от Manacost`

Both entries use the same export logic. The second one has Russian UI text.

## 8. Export Files

Start Hearthstone, log in, and wait for HDT to read the collection.

Then open:

```text
Plugins > Collection Exporter by Manacost
```

or:

```text
Plugins > Экспорт коллекции от Manacost
```

Choose an output folder and click:

- `Export JSON`
- `Export CSV`
- `Export Both`
- `Changes JSON`
- `Changes CSV`
- `Changes Both`

Default output folder:

```text
Documents\HDT Collection Exports
```

Changes export compares the current collection with the last local baseline and creates:

```text
hearthstone-collection-changes-YYYYMMDD-HHMMSS.json
hearthstone-collection-changes-YYYYMMDD-HHMMSS.csv
```

The baseline tools are:

- `Set current` — save the current collection as the baseline.
- `Import JSON` — import an older full JSON export as the baseline.
- `Clear` — remove saved baseline files.

If there is no baseline yet, changes export creates one from the current collection instead of failing. After a successful changes export, the baseline is updated to the current collection.

## Exported Data

Full JSON schema version: `3`.

JSON includes collection cards, dust, card backs, favorite card back, favorite heroes, raw `playerRecords`, derived `classStats`, `favoriteClass`, `bestClassByWins`, and basic user identifiers exposed by HDT.

`classStats` is derived from `playerRecords`: each non-zero `records[].data` value is treated as a hero DBF ID, resolved through HearthDb, and grouped by `CardClass`. `recordTypes[].type` keeps the raw numeric HDT/Hearthstone record type, usually the game mode bucket.

In CSV, `golden` is the real golden-card count. `ownedTotal` includes normal, golden, diamond, and signature copies. The detailed premium split is available in JSON.

## Troubleshooting

### The plugin does not appear

Check that the plugins folder contains:

```text
HdtCollectionExporter.dll
```

and not:

```text
HdtCollectionExporter.sln
```

Then fully restart HDT.

### Export says collection data is unavailable

Start Hearthstone, log in, and wait a moment. HDT can export collection data only after it has read the collection from the running game/client.

### Cannot write file

Choose another output folder, for example:

```text
Documents\HDT Collection Exports
```

### Changes export creates a baseline instead of writing changes

That means there was no previous baseline. Make changes to the collection and run `Changes JSON`, `Changes CSV`, or `Changes Both` again.
