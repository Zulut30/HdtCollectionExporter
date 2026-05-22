# HDT Collection Exporter / Экспорт коллекции HDT

HDT Collection Exporter is a Hearthstone Deck Tracker plugin that exports a user's Hearthstone collection and related local profile data to JSON and CSV.

Экспорт коллекции HDT — это плагин для Hearthstone Deck Tracker, который экспортирует коллекцию Hearthstone пользователя и связанные локальные данные профиля в JSON и CSV.

The plugin is local-only: it does not send data to external services. The exported files are meant to be uploaded by the user manually to another website or tool.

Плагин работает только локально: он не отправляет данные во внешние сервисы. Экспортированные файлы пользователь может самостоятельно загрузить на внешний сайт или в другой инструмент.

## Plugin Entries / Расширения

The DLL exposes two HDT plugin entries:

- `Collection Exporter` — English UI.
- `Экспорт коллекции` — Russian UI.

Оба расширения используют один и тот же код экспорта. Отличаются только название, описание, меню, окно и статусы.

## Exported Data / Какие данные экспортируются

JSON export schema version: `2`.

JSON экспортирует:

- `exportedAt` — export timestamp in ISO format.
- `source` — `Hearthstone Deck Tracker plugin`.
- `version` — export schema version.
- `user.battleTag` — user's BattleTag, if HDT can read it.
- `user.accountHi` / `user.accountLo` — Hearthstone account identifiers exposed by HDT.
- `dust` — current dust amount.
- `cardBacks` — owned card back ids.
- `favoriteCardBack` — selected favorite card back id.
- `favoriteHeroes` — selected favorite hero portraits / hero skins.
- `playerRecords` — raw player records from Hearthstone/HDT, grouped by numeric `type` and `data`, with `wins`, `losses`, and `ties`.
- `cards` — owned cards with ids, metadata, normal/premium counts, trial counts, and totals.

CSV export keeps a stable table shape:

```text
cardId,dbfId,name,set,rarity,class,normal,golden,ownedTotal
```

In CSV, `golden` is a premium total: `golden + diamond + signature`. The full premium split is available in JSON as `golden`, `diamond`, `signature`, and `premiumTotal`.

## Privacy / Приватность

The JSON export includes user-identifying fields:

- `battleTag`
- `accountHi`
- `accountLo`

JSON экспорт содержит идентификаторы пользователя:

- `battleTag`
- `accountHi`
- `accountLo`

This is intentional for upload/import workflows, but any website accepting the file should clearly tell users what is being uploaded.

Это сделано намеренно для сценария загрузки/импорта, но сайт, принимающий файл, должен явно сообщать пользователю, какие данные загружаются.

## Build / Сборка

Requirements:

- Windows
- Visual Studio 2022 Build Tools or Visual Studio
- Hearthstone Deck Tracker installed
- Recommended: .NET Framework 4.7.2 Developer Pack / targeting pack

Build from the repository root:

```powershell
.\build.ps1
```

If HDT is installed in a custom location:

```powershell
.\build.ps1 -HDTInstallDir "C:\Users\<you>\AppData\Local\HearthstoneDeckTracker\app-1.52.14"
```

Output DLL:

```text
src\HdtCollectionExporter\bin\x86\Release\HdtCollectionExporter.dll
```

The build script includes a local fallback compiler path for machines without the .NET Framework 4.7.2 targeting pack. Installing the targeting pack is still the cleaner build setup.

## Install / Установка

Short version:

1. Build the plugin.
2. Open HDT.
3. Go to `Options > Tracker > Plugins`.
4. Click `Plugins Folder`.
5. Copy only this file into the plugins folder:

```text
HdtCollectionExporter.dll
```

Do not copy `.sln`, `.csproj`, source files, `bin`, or `obj`.

Подробная инструкция есть здесь:

- [English install guide](docs/INSTALL.en.md)
- [Русский гайд по установке](docs/INSTALL.ru.md)

## Use / Использование

1. Start Hearthstone and log in.
2. Start or restart HDT.
3. Enable either `Collection Exporter` or `Экспорт коллекции` in HDT plugin settings.
4. Open the plugin from HDT's `Plugins` menu.
5. Choose an output folder.
6. Click `Export JSON`, `Export CSV`, or `Export Both`.

Default output folder:

```text
Documents\HDT Collection Exports
```

## Project Structure / Структура проекта

```text
src/HdtCollectionExporter/
  HdtCollectionExporterPlugin.cs     HDT plugin entry points
  Services/HdtCollectionProvider.cs  Reads HDT collection data
  Services/CollectionExportService.cs Writes JSON and CSV
  Settings/PluginSettings.cs         XML settings
  UI/ExportWindow.xaml               WPF export window
  UI/ExportWindowText.cs             English/Russian UI text
  Models/                            Export DTOs

samples/
  sample-collection.json
  sample-collection.csv

docs/
  INSTALL.en.md
  INSTALL.ru.md
```

## Reference Repositories / Reference-репозитории

- [Hearthstone-Collection-Tracker](https://github.com/ko-vasilev/Hearthstone-Collection-Tracker): reference for HDT plugin structure and collection-oriented plugin UI.
- [Hearthstone_Card_Export](https://github.com/Phoenixy/Hearthstone_Card_Export): reference for export flow and CSV behavior.

The implementation here is separate code. Current collection access uses HDT's `CollectionHelpers.Hearthstone.GetCollection()` rather than direct manual process-memory access.
