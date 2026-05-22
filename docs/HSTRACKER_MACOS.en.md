# HSTracker macOS Support

This repository now includes a source-level Manacost exporter adapter for [HearthSim/HSTracker](https://github.com/HearthSim/HSTracker).

HSTracker is a native Swift/macOS application. Unlike Hearthstone Deck Tracker on Windows, the current HSTracker codebase does not expose a drop-in `.dll` plugin API. The macOS support in this repository is therefore a Swift source adapter intended for an HSTracker fork, PR, or custom build.

## What Was Studied

Reference files in HSTracker:

- `HSTracker/Hearthstone/Collection.swift`
- `HSTracker/Hearthstone/CollectionHelper.swift`
- `HSTracker/HearthMirror/MirrorHelper.swift`
- `HSTracker/HSReplay/HSReplayNetHelper.swift`
- `HSTracker/HSReplay/HSReplayAPI.swift`
- `HSTracker/Database/Cards.swift`

Important findings:

- HSTracker reads collection data through `MirrorHelper.getCollection()`.
- `CollectionHelpers.hearthstone.getCollection()` returns the normalized Swift `Collection`.
- The HSTracker `Collection` model exposes cards, dust, card backs, favorite card back, favorite heroes, and player records.
- The card count order matches HDT: normal, golden, diamond, signature, trial normal, trial golden, trial diamond, trial signature.
- HSTracker already derives DBF IDs through its `Cards` database.
- No public third-party plugin loader was found in the HSTracker source.
- `gold` exists in an older/auxiliary `UploadCollectionData` DTO, but the normalized HSTracker `Collection` used by the app does not currently expose it; the adapter exports `dust`, not gold.

## Added Source Adapter

Files:

```text
macos/HSTrackerManacostExporter/
  ManacostCollectionExportModels.swift
  ManacostCollectionExporter.swift
  ManacostCollectionExportMenuController.swift
```

The adapter exports the same site-friendly schema as the Windows HDT plugin:

- JSON schema version `3`
- UTF-8 CSV
- full export
- changes-only export with local baseline
- output folder selection
- include card names toggle
- include golden count toggle
- include metadata toggle
- local-only export, no network requests

## Exported Data

The HSTracker adapter exports:

- `user`: `battleTag`, `accountHi`, `accountLo`
- `dust`
- `cardBacks`
- `favoriteCardBack`
- `favoriteHeroes`
- raw `playerRecords`
- derived `classStats`
- `favoriteClass`
- `bestClassByWins`
- `cards`

`classStats` is derived from `playerRecords`: each non-zero `records[].data` value is treated as a hero DBF ID, resolved through HSTracker `Cards.by(dbfId:collectible:)`, and grouped by card class.

## Integration Guide

1. Fork or clone HSTracker.
2. Copy the files from `macos/HSTrackerManacostExporter/` into the HSTracker app target, for example:

```text
HSTracker/Hearthstone/ManacostExport/
```

3. Add the copied Swift files to the HSTracker Xcode target.
4. Retain a menu controller in `AppDelegate`:

```swift
private let manacostExportMenuController = ManacostCollectionExportMenuController()
```

5. In `AppDelegate.buildMenu()`, after the `Decks` menu is available, install the submenu:

```swift
if let deckMenu = NSApplication.shared.mainMenu?
    .item(withTitle: String.localizedString("Decks", comment: ""))?
    .submenu {
    manacostExportMenuController.install(in: deckMenu)
}
```

6. Build and run HSTracker on macOS.
7. Start Hearthstone, log in, wait for HSTracker to read the collection, then use:

```text
Decks -> Manacost Export
```

Default export folder:

```text
~/Documents/HSTracker Collection Exports
```

Baseline file:

```text
~/Library/Application Support/HSTracker/ManacostCollectionExporter/baseline.json
```

## Notes

The adapter intentionally uses HSTracker/HearthMirror collection models instead of manual process memory reading. It does not send exported data anywhere.

This repository cannot produce a ready-to-install HSTracker plugin bundle until HSTracker exposes an external plugin mechanism or the adapter is merged into an HSTracker build.
