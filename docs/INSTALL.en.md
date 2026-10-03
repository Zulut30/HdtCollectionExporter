# Install Collection Exporter 1.6

Requires Windows and 64-bit Hearthstone Deck Tracker. Download the DLL or ZIP from [releases](https://github.com/Zulut30/HdtCollectionExporter/releases/latest).

1. Fully close HDT, including the tray icon.
2. Extract the ZIP and run `install.ps1` in PowerShell. It backs up the DLL/plugins.xml and combines the previous English/Russian entries while retaining the enabled state.
3. Start HDT. On a fresh install, enable `Collection Exporter by Manacost` under Options → Tracker → Plugins.
4. Open the exporter from the Plugins menu. Select Auto, Русский or English inside its window.

Manual install: copy only `HdtCollectionExporter.dll` into `%APPDATA%/HearthstoneDeckTracker/Plugins`, then restart HDT. Updating the old Russian entry manually may require enabling the new stable entry once. Do not copy HDT dependencies or source files.

## Export and history

Start Hearthstone and log in. The window shows collection data when HDT reads it. Choose full/changes, JSON/CSV/both and a destination, then use the fixed Save action. Preview and export use the same snapshot; refresh it after five minutes.

The first changes export saves a baseline without creating a delta file. Every successful export stores a full immutable snapshot per account. Compare two dates under History or use the selected snapshot as your baseline. History has no automatic retention limit. Optional pruning keeps 30 recent snapshots, active/backup baselines and damaged files for recovery, after confirmation.

Legacy exports are preserved with unknown count completeness; create a fresh baseline before accurate changes export. JSON schema 3 retains all actual permanent/trial counts. Trial copies are excluded from ownedTotal. Name/metadata toggles affect presentation; the golden toggle affects the CSV column. Internal history always keeps complete information. Export does not send files over the network.

## Build

Install Visual Studio Build Tools with the .NET desktop workload. Debug and Release target x64. A missing targeting pack is replaced by pinned NuGet reference assemblies. The unsafe legacy compiler fallback is retired.

```powershell
.\build.ps1
.\build.ps1 -HDTInstallDir 'D:\Apps\HearthstoneDeckTracker\app-1.58.6'
.\build.ps1 -PinnedDependencies
```

Output: `src/HdtCollectionExporter/bin/x64/Release/HdtCollectionExporter.dll`. See [release notes](RELEASE_NOTES-1.6.0.md). macOS uses the separate [HSTracker source adapter](HSTRACKER_MACOS.en.md).
