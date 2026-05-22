# HDT Collection Exporter by Manacost / Экспорт коллекции HDT от Manacost

HDT Collection Exporter by Manacost is a Hearthstone Deck Tracker plugin that exports a user's Hearthstone collection and related local profile data to JSON and CSV.

Экспорт коллекции HDT от Manacost — это плагин для Hearthstone Deck Tracker, который экспортирует коллекцию Hearthstone пользователя и связанные локальные данные профиля в JSON и CSV.

The plugin is local-only: it does not send data to external services. The exported files are meant to be uploaded by the user manually to another website or tool.

Плагин работает только локально: он не отправляет данные во внешние сервисы. Экспортированные файлы пользователь может самостоятельно загрузить на внешний сайт или в другой инструмент.

## Plugin Entries / Расширения

The DLL exposes two HDT plugin entries:

- `Collection Exporter by Manacost` — English UI.
- `Экспорт коллекции от Manacost` — Russian UI.

Both entries use the same export logic and are built by the Manacost team. They differ only by name, description, menu text, window text, and status messages.

Оба расширения используют один и тот же код экспорта и сделаны командой Manacost. Отличаются только название, описание, меню, окно и статусы.

## Exported Data / Какие данные экспортируются

JSON export schema version: `2`.

JSON export includes:

- `exportedAt` — export timestamp in ISO format.
- `source` — `Hearthstone Deck Tracker plugin by Manacost`.
- `version` — export schema version.
- `user.battleTag` — user's BattleTag, if HDT can read it.
- `user.accountHi` / `user.accountLo` — Hearthstone account identifiers exposed by HDT.
- `dust` — current dust amount.
- `cardBacks` — owned card back ids.
- `favoriteCardBack` — selected favorite card back id.
- `favoriteHeroes` — selected favorite hero portraits / hero skins.
- `playerRecords` — raw player records from Hearthstone/HDT, grouped by numeric `type` and `data`, with `wins`, `losses`, and `ties`.
- `cards` — owned cards with ids, metadata, normal/premium counts, trial counts, and totals.

JSON содержит:

- `exportedAt` — время экспорта в ISO-формате.
- `source` — `Hearthstone Deck Tracker plugin by Manacost`.
- `version` — версия схемы экспорта.
- `user.battleTag` — BattleTag пользователя, если HDT может его прочитать.
- `user.accountHi` / `user.accountLo` — идентификаторы Hearthstone-аккаунта, доступные через HDT.
- `dust` — текущее количество пыли.
- `cardBacks` — id доступных рубашек карт.
- `favoriteCardBack` — id выбранной любимой рубашки.
- `favoriteHeroes` — выбранные любимые портреты героев / скины.
- `playerRecords` — сырые записи статистики игрока из Hearthstone/HDT, сгруппированные по numeric `type` и `data`, с `wins`, `losses`, `ties`.
- `cards` — карты в коллекции с id, метаданными, обычными/премиум количествами, trial-количествами и итогами.

CSV export keeps a stable table shape:

```text
cardId,dbfId,name,set,rarity,class,normal,golden,ownedTotal
```

In CSV, `golden` is a premium total: `golden + diamond + signature`. The full premium split is available in JSON as `golden`, `diamond`, `signature`, and `premiumTotal`.

В CSV колонка `golden` означает общий premium count: золотые + diamond + signature. Полная разбивка доступна в JSON в полях `golden`, `diamond`, `signature`, `premiumTotal`.

## Website Import Compatibility / Совместимость с сайтами

The files are designed to be easy for websites to parse:

- Full JSON has stable top-level fields and arrays. A website can read `version`, then import `cards`, `dust`, `cardBacks`, favorites, and profile data.
- Full CSV is intentionally flat and keeps the fixed header `cardId,dbfId,name,set,rarity,class,normal,golden,ownedTotal`.
- Change exports use `exportType: "changes"` and include a `summary` block, so a website can quickly decide whether anything changed before reading all records.

Файлы специально сделаны удобными для сайтов:

- Полный JSON имеет стабильные верхнеуровневые поля и массивы. Сайт читает `version`, затем импортирует `cards`, `dust`, `cardBacks`, избранное и данные профиля.
- Полный CSV плоский и сохраняет фиксированный заголовок `cardId,dbfId,name,set,rarity,class,normal,golden,ownedTotal`.
- Экспорт изменений использует `exportType: "changes"` и блок `summary`, поэтому сайт может быстро понять, есть ли изменения, не разбирая весь файл.

## Change Exports / Экспорт изменений

After any full export, the plugin stores a local baseline snapshot inside the HDT plugin data folder. The baseline is not sent anywhere.

Use the changes export buttons to compare the current collection with that baseline:

- `Changes JSON` writes `hearthstone-collection-changes-YYYYMMDD-HHMMSS.json`.
- `Changes CSV` writes `hearthstone-collection-changes-YYYYMMDD-HHMMSS.csv`.
- `Changes Both` writes both files.

The changes JSON includes:

- `summary` — counters for cards, dust, card backs, favorite card back, favorite heroes, player records, and user profile changes.
- `cards` — only card records whose counts changed, with `changeType`, `previous`, `current`, and `delta`.
- `dust`, `cardBacks`, `favoriteCardBack`, `favoriteHeroes`, `playerRecords`, `user` — only changed profile sections.

The changes CSV has this header:

```text
changeType,cardId,dbfId,name,set,rarity,class,normalDelta,goldenDelta,ownedTotalDelta,previousNormal,previousGolden,previousOwnedTotal,currentNormal,currentGolden,currentOwnedTotal
```

The baseline block in the UI can:

- `Set current` — save the current collection as the baseline without creating an upload file.
- `Import JSON` — import an older full JSON export as the baseline.
- `Clear` — remove saved baseline files.

If no previous baseline exists, a changes export now creates a baseline from the current collection instead of failing. After any successful changes export, the plugin updates the local baseline to the current collection. The next change export will contain only newer changes.

После любого полного экспорта плагин сохраняет локальный baseline-снимок в папке данных HDT-плагина. Этот baseline никуда не отправляется.

Кнопки экспорта изменений сравнивают текущую коллекцию с baseline:

- `Изменения JSON` создает `hearthstone-collection-changes-YYYYMMDD-HHMMSS.json`.
- `Изменения CSV` создает `hearthstone-collection-changes-YYYYMMDD-HHMMSS.csv`.
- `Изменения: оба` создает оба файла.

JSON изменений содержит:

- `summary` — счетчики изменений карт, пыли, рубашек, любимой рубашки, любимых героев, статистики и профиля.
- `cards` — только карты, у которых изменились количества, с `changeType`, `previous`, `current`, `delta`.
- `dust`, `cardBacks`, `favoriteCardBack`, `favoriteHeroes`, `playerRecords`, `user` — только измененные секции профиля.

Блок baseline в UI умеет:

- `Текущая база` — сохранить текущую коллекцию как baseline без создания файла для загрузки.
- `Импорт JSON` — импортировать старый полный JSON экспорт как baseline.
- `Очистить` — удалить сохраненные baseline-файлы.

Если baseline еще нет, экспорт изменений теперь создает baseline из текущей коллекции вместо ошибки. После успешного экспорта изменений baseline обновляется до текущей коллекции. Следующий экспорт изменений будет содержать только более новые изменения.

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

Подробные инструкции:

- [English install guide](docs/INSTALL.en.md)
- [Русский гайд по установке](docs/INSTALL.ru.md)

## Use / Использование

1. Start Hearthstone and log in.
2. Start or restart HDT.
3. Enable either `Collection Exporter by Manacost` or `Экспорт коллекции от Manacost` in HDT plugin settings.
4. Open the plugin from HDT's `Plugins` menu.
5. Choose an output folder.
6. Click `Export JSON`, `Export CSV`, `Export Both`, `Changes JSON`, `Changes CSV`, or `Changes Both`.

Default output folder:

```text
Documents\HDT Collection Exports
```

## Project Structure / Структура проекта

```text
src/HdtCollectionExporter/
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

docs/
  INSTALL.en.md
  INSTALL.ru.md
```

## Reference Repositories / Reference-репозитории

- [Hearthstone-Collection-Tracker](https://github.com/ko-vasilev/Hearthstone-Collection-Tracker): reference for HDT plugin structure and collection-oriented plugin UI.
- [Hearthstone_Card_Export](https://github.com/Phoenixy/Hearthstone_Card_Export): reference for export flow and CSV behavior.

The implementation here is separate code. Current collection access uses HDT's `CollectionHelpers.Hearthstone.GetCollection()` rather than direct manual process-memory access.
