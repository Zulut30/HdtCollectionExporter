Collection Exporter 1.6.0 adds a redesigned dark Manacost window, a single export action with full/changes modes, account summary, local immutable snapshot history, and comparisons between two historical snapshots.

- One stable HDT plugin entry, with Russian, English and automatic language selection.
- Account baselines use exact 64-bit account identifiers; BattleTag renaming and presentation options cannot create inventory changes.
- Full permanent, golden, diamond, signature and trial counts are retained internally. Public JSON remains schema 3; CSV headers remain compatible.
- Atomic file replacement, settings backups, checksummed history, unique filenames, cancellation and explicit partial-export outcomes.
- Set completion uses HDT's collectible-card catalog; core and special sets are omitted from the set summary.
- Reproducible x64 Debug/Release builds and automated domain/import/STA checks. The unsafe legacy WPF compiler fallback is retired; a missing targeting pack can be downloaded as a pinned reference package.
- Swift source adapter gains account-specific history and complete counts. It remains source integration for HSTracker, not a macOS drop-in plugin. CI builds the export adapter with boundary doubles, runs shared fixtures for inventory/deltas and models/storage, and parses the AppKit menu; native HSTracker UI still requires a macOS integration build and manual validation.

Upgrade while HDT is fully closed. The included `install.ps1` preserves the enabled state of the previous English/Russian entries and backs up the old DLL and plugins.xml. A manual DLL replacement may require enabling `Collection Exporter by Manacost` once. Legacy exports are preserved; their unknown count completeness requires a fresh baseline before accurate changes export.
