<p align="center">
  <img src="src/HdtCollectionExporter/Assets/manacost_logo.jpg" alt="Manacost banner" width="820" />
</p>

<h1 align="center">HDT Collection Exporter by Manacost</h1>

<p align="center">
  <strong>Collection JSON/CSV export plugin for <a href="https://github.com/HearthSim/Hearthstone-Deck-Tracker">HearthSim/Hearthstone-Deck-Tracker</a>, with source-level macOS adapter for <a href="https://github.com/HearthSim/HSTracker">HearthSim/HSTracker</a>.</strong>
</p>

<p align="center">
  <a href="https://github.com/HearthSim/Hearthstone-Deck-Tracker"><img alt="HDT Plugin" src="https://img.shields.io/badge/Hearthstone%20Deck%20Tracker-plugin-8A2BE2?style=for-the-badge"></a>
  <a href="https://github.com/Zulut30/HdtCollectionExporter/releases/latest"><img alt="Latest release" src="https://img.shields.io/github/v/release/Zulut30/HdtCollectionExporter?style=for-the-badge"></a>
  <img alt="Windows" src="https://img.shields.io/badge/Windows-supported-0078D6?style=for-the-badge&logo=windows">
  <img alt="macOS" src="https://img.shields.io/badge/macOS-source%20adapter-000000?style=for-the-badge&logo=apple">
  <img alt="Local only" src="https://img.shields.io/badge/privacy-local%20only-2F6F5E?style=for-the-badge">
</p>

<p align="center">
  <img alt="C Sharp" src="https://img.shields.io/badge/C%23-239120?style=for-the-badge&logo=csharp&logoColor=white">
  <img alt="Swift" src="https://img.shields.io/badge/Swift-HSTracker-FA7343?style=for-the-badge&logo=swift&logoColor=white">
  <img alt=".NET Framework" src="https://img.shields.io/badge/.NET%20Framework-4.7.2-512BD4?style=for-the-badge&logo=dotnet&logoColor=white">
  <img alt="WPF" src="https://img.shields.io/badge/UI-WPF-5C2D91?style=for-the-badge">
  <img alt="JSON" src="https://img.shields.io/badge/export-JSON-000000?style=for-the-badge&logo=json&logoColor=white">
  <img alt="CSV" src="https://img.shields.io/badge/export-CSV-217346?style=for-the-badge">
</p>

## Overview

HDT Collection Exporter by Manacost is a local plugin for [Hearthstone Deck Tracker](https://github.com/HearthSim/Hearthstone-Deck-Tracker). It exports a user's Hearthstone collection and local profile data to files that can later be uploaded manually to another site or tool. The repository also includes a Swift source adapter for [HSTracker](https://github.com/HearthSim/HSTracker) on macOS.

Экспорт коллекции HDT от Manacost — локальный плагин для [Hearthstone Deck Tracker](https://github.com/HearthSim/Hearthstone-Deck-Tracker). Он экспортирует коллекцию Hearthstone и локальные данные профиля в файлы, которые пользователь может вручную загрузить на сайт или в другой инструмент.

No network requests are made by the plugin during export.

## Features

- Full collection export to JSON.
- Full collection export to UTF-8 CSV.
- Changes-only export to JSON or CSV using a local baseline.
- Local baseline controls: set current, import old full JSON, clear baseline.
- Exports collection cards, dust, card backs, favorite card back, favorite heroes, raw player records, derived class stats, and basic user identifiers exposed by HDT.
- English and Russian plugin entries in one DLL.
- Manacost-branded WPF export window.
- Swift source adapter for HSTracker/macOS using the same JSON/CSV schema.

## Download

Download the latest release:

[**Latest Release**](https://github.com/Zulut30/HdtCollectionExporter/releases/latest)

Release assets:

- `HdtCollectionExporter.dll` — plugin DLL for HDT.
- `HdtCollectionExporter-vX.Y.Z.zip` — DLL plus install guides.

## Install

1. Download `HdtCollectionExporter.dll` from the latest release.
2. Open Hearthstone Deck Tracker.
3. Go to `Options > Tracker > Plugins`.
4. Click `Plugins Folder`.
5. Copy only `HdtCollectionExporter.dll` into that folder.
6. Fully restart HDT, including the tray icon.
7. Enable either `Collection Exporter by Manacost` or `Экспорт коллекции от Manacost`.

Usually the HDT plugin folder is:

```text
%AppData%\HearthstoneDeckTracker\Plugins
```

More details:

- [English install guide](docs/INSTALL.en.md)
- [Русский гайд по установке](docs/INSTALL.ru.md)

## macOS / HSTracker

HSTracker does not currently expose the same drop-in plugin API as HDT. For macOS users, this repository includes a source-level Swift adapter that can be added to an HSTracker fork or custom build:

```text
macos/HSTrackerManacostExporter/
```

It uses HSTracker's `CollectionHelpers.hearthstone.getCollection()` and exports the same local JSON/CSV schema, including changes-only export and class statistics.

Guides:

- [HSTracker macOS guide](docs/HSTRACKER_MACOS.en.md)
- [Гайд HSTracker macOS](docs/HSTRACKER_MACOS.ru.md)

## Exported Data

Full JSON export schema version: `3`.

Top-level JSON fields:

- `exportedAt`
- `source`
- `version`
- `user`
- `dust`
- `cardBacks`
- `favoriteCardBack`
- `favoriteHeroes`
- `playerRecords`
- `classStats`
- `favoriteClass`
- `bestClassByWins`
- `cards`

`classStats` is derived from HDT `playerRecords`: the plugin treats each non-zero `playerRecords.records[].data` value as a hero DBF ID, resolves it through HearthDb, and aggregates wins/losses/ties by `CardClass`. `recordTypes[].type` keeps the raw numeric HDT/Hearthstone record type, which is usually the game mode bucket. `favoriteClass` is the class with the most recorded games across the exported record buckets; `bestClassByWins` is the class with the most wins.

Full CSV keeps this stable header:

```text
cardId,dbfId,name,set,rarity,class,normal,golden,ownedTotal
```

In CSV, `golden` is the real golden-card count. `ownedTotal` includes normal, golden, diamond, and signature copies. The detailed premium split is available in JSON.

## Changes Export

The plugin stores a local baseline snapshot after a full export or when the user clicks `Set current`.

Changes export compares the current collection against the saved baseline:

- `Changes JSON` writes `hearthstone-collection-changes-YYYYMMDD-HHMMSS.json`.
- `Changes CSV` writes `hearthstone-collection-changes-YYYYMMDD-HHMMSS.csv`.
- `Changes Both` writes both files.

If no baseline exists yet, the plugin creates one from the current collection instead of failing. The next changes export will then contain only newer changes.

## Build

Requirements:

- Windows
- Hearthstone Deck Tracker installed
- Visual Studio 2022 Build Tools or Visual Studio
- Recommended: .NET Framework 4.7.2 Developer Pack / targeting pack

Build from the repository root:

```powershell
.\build.ps1
```

Build output:

```text
src\HdtCollectionExporter\bin\Release\HdtCollectionExporter.dll
```

## Project Structure

```text
src/HdtCollectionExporter/
  Assets/manacost_logo.jpg           Manacost banner/logo used in the UI and README
  HdtCollectionExporterPlugin.cs     HDT plugin entry points
  Services/HdtCollectionProvider.cs  Reads HDT collection data
  Services/CollectionExportService.cs Writes full and changes JSON/CSV
  Settings/PluginSettings.cs         XML settings
  UI/ExportWindow.xaml               WPF export window
  UI/ExportWindowText.cs             English/Russian UI text
  Models/                            Export DTOs

samples/
  sample-collection.json
  sample-collection.csv
  sample-collection-changes.json
  sample-collection-changes.csv
  hstracker-sample-collection.json
  hstracker-sample-collection.csv

macos/HSTrackerManacostExporter/
  ManacostCollectionExportModels.swift
  ManacostCollectionExporter.swift
  ManacostCollectionExportMenuController.swift
```

## Reference Repositories

- [HearthSim/Hearthstone-Deck-Tracker](https://github.com/HearthSim/Hearthstone-Deck-Tracker): target application and HDT plugin/runtime API.
- [HearthSim/HSTracker](https://github.com/HearthSim/HSTracker): Swift/macOS reference for HSTracker collection access via HearthMirror and `CollectionHelpers`.
- [Hearthstone-Collection-Tracker](https://github.com/ko-vasilev/Hearthstone-Collection-Tracker): reference for HDT plugin structure and collection-oriented plugin UI.
- [Hearthstone_Card_Export](https://github.com/Phoenixy/Hearthstone_Card_Export): reference for export flow and CSV behavior.

The implementation here is separate code. Current collection access uses HDT's `CollectionHelpers.Hearthstone.GetCollection()` rather than OCR, network calls, or manual process-memory reading.
