# Installation Guide

This guide explains how to build and install HDT Collection Exporter into Hearthstone Deck Tracker.

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

- `Collection Exporter`
- `Экспорт коллекции`

Both entries use the same export logic. The second one has Russian UI text.

## 7. Export Files

Start Hearthstone, log in, and wait for HDT to read the collection.

Then open:

```text
Plugins > Collection Exporter
```

or:

```text
Plugins > Экспорт коллекции
```

Choose an output folder and click:

- `Export JSON`
- `Export CSV`
- `Export Both`

Default output folder:

```text
Documents\HDT Collection Exports
```

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
