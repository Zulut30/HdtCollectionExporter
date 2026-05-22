# Installation Guide

This guide explains how to build and install HDT Collection Exporter by Manacost into Hearthstone Deck Tracker.

## 1. Requirements

- Windows
- Hearthstone Deck Tracker installed
- Visual Studio 2022 Build Tools or Visual Studio
- Recommended: .NET Framework 4.7.2 Developer Pack / targeting pack

## 2. Build The Plugin

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
src\HdtCollectionExporter\bin\x86\Release\HdtCollectionExporter.dll
```

## 3. Open The HDT Plugins Folder

In Hearthstone Deck Tracker:

1. Open `Options`.
2. Go to `Tracker`.
3. Open `Plugins`.
4. Click `Plugins Folder`.

This usually opens:

```text
%AppData%\HearthstoneDeckTracker\Plugins
```

## 4. Copy The Correct File

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

## 5. Restart HDT

Fully close HDT, including the tray icon near the clock, then start HDT again.

If HDT was already running while you copied the DLL, restart is important because HDT may keep the old DLL loaded.

## 6. Enable The Plugin

In `Options > Tracker > Plugins`, enable either:

- `Collection Exporter by Manacost`
- `Экспорт коллекции от Manacost`

Both entries use the same export logic. The second one has Russian UI text.

## 7. Export Files

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
