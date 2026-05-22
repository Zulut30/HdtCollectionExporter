import AppKit
import Foundation

final class ManacostCollectionExportSettings {
    static let shared = ManacostCollectionExportSettings()

    private let defaults = UserDefaults.standard
    private let outputFolderKey = "manacost.collectionExporter.outputFolder"
    private let includeCardNamesKey = "manacost.collectionExporter.includeCardNames"
    private let includeGoldenCountKey = "manacost.collectionExporter.includeGoldenCount"
    private let includeMetadataKey = "manacost.collectionExporter.includeMetadata"
    private let lastExportTimeKey = "manacost.collectionExporter.lastExportTime"

    var outputFolder: URL {
        get {
            if let path = defaults.string(forKey: outputFolderKey), !path.isEmpty {
                return URL(fileURLWithPath: path, isDirectory: true)
            }
            return ManacostExportOptions.defaultOutputFolder()
        }
        set {
            defaults.set(newValue.path, forKey: outputFolderKey)
        }
    }

    var includeCardNames: Bool {
        get { bool(forKey: includeCardNamesKey, defaultValue: true) }
        set { defaults.set(newValue, forKey: includeCardNamesKey) }
    }

    var includeGoldenCount: Bool {
        get { bool(forKey: includeGoldenCountKey, defaultValue: true) }
        set { defaults.set(newValue, forKey: includeGoldenCountKey) }
    }

    var includeMetadata: Bool {
        get { bool(forKey: includeMetadataKey, defaultValue: true) }
        set { defaults.set(newValue, forKey: includeMetadataKey) }
    }

    var lastExportTime: Date? {
        get { defaults.object(forKey: lastExportTimeKey) as? Date }
        set { defaults.set(newValue, forKey: lastExportTimeKey) }
    }

    func options() -> ManacostExportOptions {
        return ManacostExportOptions(
            outputFolder: outputFolder,
            includeCardNames: includeCardNames,
            includeGoldenCount: includeGoldenCount,
            includeMetadata: includeMetadata
        )
    }

    private func bool(forKey key: String, defaultValue: Bool) -> Bool {
        if defaults.object(forKey: key) == nil {
            return defaultValue
        }
        return defaults.bool(forKey: key)
    }
}

final class ManacostCollectionExportMenuController: NSObject, NSMenuDelegate {
    private let settings = ManacostCollectionExportSettings.shared
    private let exporter = ManacostCollectionExporter()
    private weak var submenu: NSMenu?
    private weak var lastExportMenuItem: NSMenuItem?
    private weak var includeCardNamesItem: NSMenuItem?
    private weak var includeGoldenCountItem: NSMenuItem?
    private weak var includeMetadataItem: NSMenuItem?

    func install(in parentMenu: NSMenu) {
        if parentMenu.item(withTitle: "Manacost Export") != nil {
            return
        }

        parentMenu.addItem(NSMenuItem.separator())
        let item = NSMenuItem(title: "Manacost Export", action: nil, keyEquivalent: "")
        let menu = NSMenu(title: "Manacost Export")
        menu.delegate = self
        item.submenu = menu
        parentMenu.addItem(item)
        submenu = menu

        addMenuItem("Export JSON", action: #selector(exportJSON(_:)))
        addMenuItem("Export CSV", action: #selector(exportCSV(_:)))
        addMenuItem("Export Both", action: #selector(exportBoth(_:)))
        menu.addItem(NSMenuItem.separator())
        addMenuItem("Changes JSON", action: #selector(exportChangesJSON(_:)))
        addMenuItem("Changes CSV", action: #selector(exportChangesCSV(_:)))
        addMenuItem("Changes Both", action: #selector(exportChangesBoth(_:)))
        menu.addItem(NSMenuItem.separator())
        addMenuItem("Set Current as Baseline", action: #selector(setBaseline(_:)))
        addMenuItem("Clear Baseline", action: #selector(clearBaseline(_:)))
        menu.addItem(NSMenuItem.separator())
        addMenuItem("Choose Output Folder...", action: #selector(chooseOutputFolder(_:)))
        addMenuItem("Open Output Folder", action: #selector(openOutputFolder(_:)))
        menu.addItem(NSMenuItem.separator())
        includeCardNamesItem = addMenuItem("Include Card Names", action: #selector(toggleIncludeCardNames(_:)))
        includeGoldenCountItem = addMenuItem("Include Golden Count", action: #selector(toggleIncludeGoldenCount(_:)))
        includeMetadataItem = addMenuItem("Include Metadata", action: #selector(toggleIncludeMetadata(_:)))
        menu.addItem(NSMenuItem.separator())
        let lastItem = NSMenuItem(title: "", action: nil, keyEquivalent: "")
        lastItem.isEnabled = false
        menu.addItem(lastItem)
        lastExportMenuItem = lastItem
        updateState()
    }

    func menuNeedsUpdate(_ menu: NSMenu) {
        updateState()
    }

    @objc private func exportJSON(_ sender: NSMenuItem) {
        runExport { try exporter.export(format: .json, options: settings.options()) }
    }

    @objc private func exportCSV(_ sender: NSMenuItem) {
        runExport { try exporter.export(format: .csv, options: settings.options()) }
    }

    @objc private func exportBoth(_ sender: NSMenuItem) {
        runExport { try exporter.export(format: .both, options: settings.options()) }
    }

    @objc private func exportChangesJSON(_ sender: NSMenuItem) {
        runExport { try exporter.exportChanges(format: .json, options: settings.options()) }
    }

    @objc private func exportChangesCSV(_ sender: NSMenuItem) {
        runExport { try exporter.exportChanges(format: .csv, options: settings.options()) }
    }

    @objc private func exportChangesBoth(_ sender: NSMenuItem) {
        runExport { try exporter.exportChanges(format: .both, options: settings.options()) }
    }

    @objc private func setBaseline(_ sender: NSMenuItem) {
        do {
            let count = try exporter.setCurrentAsBaseline(options: settings.options())
            showInfo("Baseline Saved", message: "Saved current collection baseline with \(count) cards.")
        } catch {
            showError(error)
        }
    }

    @objc private func clearBaseline(_ sender: NSMenuItem) {
        do {
            try exporter.clearBaseline()
            showInfo("Baseline Cleared", message: "The local Manacost changes baseline was cleared.")
        } catch {
            showError(error)
        }
    }

    @objc private func chooseOutputFolder(_ sender: NSMenuItem) {
        let panel = NSOpenPanel()
        panel.title = "Choose Manacost Export Folder"
        panel.canChooseDirectories = true
        panel.canChooseFiles = false
        panel.canCreateDirectories = true
        panel.allowsMultipleSelection = false
        panel.directoryURL = settings.outputFolder
        if panel.runModal() == .OK, let url = panel.url {
            settings.outputFolder = url
            updateState()
        }
    }

    @objc private func openOutputFolder(_ sender: NSMenuItem) {
        try? FileManager.default.createDirectory(at: settings.outputFolder, withIntermediateDirectories: true, attributes: nil)
        NSWorkspace.shared.open(settings.outputFolder)
    }

    @objc private func toggleIncludeCardNames(_ sender: NSMenuItem) {
        settings.includeCardNames = !settings.includeCardNames
        updateState()
    }

    @objc private func toggleIncludeGoldenCount(_ sender: NSMenuItem) {
        settings.includeGoldenCount = !settings.includeGoldenCount
        updateState()
    }

    @objc private func toggleIncludeMetadata(_ sender: NSMenuItem) {
        settings.includeMetadata = !settings.includeMetadata
        updateState()
    }

    @discardableResult
    private func addMenuItem(_ title: String, action: Selector) -> NSMenuItem {
        let item = NSMenuItem(title: title, action: action, keyEquivalent: "")
        item.target = self
        submenu?.addItem(item)
        return item
    }

    private func runExport(_ work: () throws -> ManacostExportResult) {
        do {
            let result = try work()
            settings.lastExportTime = result.exportedAt
            updateState()

            if result.baselineCreated {
                showInfo(
                    "Baseline Created",
                    message: "No previous baseline was found. Current collection was saved as baseline; run changes export again after the collection changes."
                )
            } else {
                let names = result.files.map(\.lastPathComponent).joined(separator: "\n")
                showInfo(
                    "Export Complete",
                    message: "Exported \(result.cardCount) cards. Changes: \(result.changeCount).\n\n\(names)"
                )
            }
        } catch {
            showError(error)
        }
    }

    private func updateState() {
        includeCardNamesItem?.state = settings.includeCardNames ? .on : .off
        includeGoldenCountItem?.state = settings.includeGoldenCount ? .on : .off
        includeMetadataItem?.state = settings.includeMetadata ? .on : .off

        if let date = settings.lastExportTime {
            let formatter = DateFormatter()
            formatter.dateStyle = .medium
            formatter.timeStyle = .medium
            lastExportMenuItem?.title = "Last export: \(formatter.string(from: date))"
        } else {
            lastExportMenuItem?.title = "Last export: never"
        }
    }

    private func showInfo(_ title: String, message: String) {
        showAlert(title: title, message: message, style: .informational)
    }

    private func showError(_ error: Error) {
        showAlert(title: "Manacost Export Failed", message: error.localizedDescription, style: .warning)
    }

    private func showAlert(title: String, message: String, style: NSAlert.Style) {
        let alert = NSAlert()
        alert.messageText = title
        alert.informativeText = message
        alert.alertStyle = style
        alert.addButton(withTitle: "OK")
        alert.runModal()
    }
}
