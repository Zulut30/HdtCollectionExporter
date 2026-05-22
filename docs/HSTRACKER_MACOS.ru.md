# Поддержка HSTracker на macOS

В репозиторий добавлен source-level адаптер экспорта Manacost для [HearthSim/HSTracker](https://github.com/HearthSim/HSTracker).

HSTracker — нативное Swift/macOS приложение. В отличие от Hearthstone Deck Tracker на Windows, в текущем HSTracker не найден drop-in `.dll` plugin API. Поэтому поддержка macOS сделана как Swift-адаптер для форка, PR или кастомной сборки HSTracker.

## Что было изучено

Reference-файлы HSTracker:

- `HSTracker/Hearthstone/Collection.swift`
- `HSTracker/Hearthstone/CollectionHelper.swift`
- `HSTracker/HearthMirror/MirrorHelper.swift`
- `HSTracker/HSReplay/HSReplayNetHelper.swift`
- `HSTracker/HSReplay/HSReplayAPI.swift`
- `HSTracker/Database/Cards.swift`

Выводы:

- HSTracker читает коллекцию через `MirrorHelper.getCollection()`.
- `CollectionHelpers.hearthstone.getCollection()` возвращает нормализованную Swift-модель `Collection`.
- Модель `Collection` содержит карты, dust, рубашки, любимую рубашку, любимых героев и player records.
- Порядок счетчиков карт совпадает с HDT: normal, golden, diamond, signature, trial normal, trial golden, trial diamond, trial signature.
- DBF ID и метаданные карт HSTracker получает через свою базу `Cards`.
- Публичного загрузчика сторонних плагинов в HSTracker source не найдено.
- `gold` встречается в старом/вспомогательном DTO `UploadCollectionData`, но нормализованная модель HSTracker `Collection`, которую использует приложение, сейчас его не отдает; адаптер экспортирует `dust`, но не gold.

## Добавленный адаптер

Файлы:

```text
macos/HSTrackerManacostExporter/
  ManacostCollectionExportModels.swift
  ManacostCollectionExporter.swift
  ManacostCollectionExportMenuController.swift
```

Адаптер экспортирует тот же формат, что Windows HDT plugin:

- JSON schema version `3`
- UTF-8 CSV
- полный экспорт
- экспорт только изменений через локальный baseline
- выбор output folder
- переключатель include card names
- переключатель include golden count
- переключатель include metadata
- только локальный экспорт, без сетевых запросов

## Какие данные экспортируются

HSTracker adapter экспортирует:

- `user`: `battleTag`, `accountHi`, `accountLo`
- `dust`
- `cardBacks`
- `favoriteCardBack`
- `favoriteHeroes`
- raw `playerRecords`
- рассчитанные `classStats`
- `favoriteClass`
- `bestClassByWins`
- `cards`

`classStats` строится из `playerRecords`: каждое ненулевое значение `records[].data` считается hero DBF ID, резолвится через HSTracker `Cards.by(dbfId:collectible:)` и группируется по классу карты.

## Как встроить в HSTracker

1. Сделай fork или clone HSTracker.
2. Скопируй файлы из `macos/HSTrackerManacostExporter/` в target HSTracker, например:

```text
HSTracker/Hearthstone/ManacostExport/
```

3. Добавь скопированные Swift-файлы в Xcode target HSTracker.
4. В `AppDelegate` сохрани menu controller:

```swift
private let manacostExportMenuController = ManacostCollectionExportMenuController()
```

5. В `AppDelegate.buildMenu()`, после получения меню `Decks`, установи submenu:

```swift
if let deckMenu = NSApplication.shared.mainMenu?
    .item(withTitle: String.localizedString("Decks", comment: ""))?
    .submenu {
    manacostExportMenuController.install(in: deckMenu)
}
```

6. Собери и запусти HSTracker на macOS.
7. Запусти Hearthstone, войди в аккаунт, дождись чтения коллекции и используй:

```text
Decks -> Manacost Export
```

Папка экспорта по умолчанию:

```text
~/Documents/HSTracker Collection Exports
```

Baseline-файл:

```text
~/Library/Application Support/HSTracker/ManacostCollectionExporter/baseline.json
```

## Важно

Адаптер использует модели HSTracker/HearthMirror и не читает память процесса вручную. Экспортированные данные никуда не отправляются.

Готовый installable HSTracker plugin bundle нельзя сделать без поддержки внешних плагинов в HSTracker или без включения адаптера в сборку HSTracker.
