import Foundation

enum ManacostExportFormat {
    case json
    case csv
    case both

    var includesJson: Bool {
        return self == .json || self == .both
    }

    var includesCsv: Bool {
        return self == .csv || self == .both
    }
}

enum ManacostCollectionExportError: LocalizedError {
    case collectionUnavailable
    case invalidBaseline
    case partialExport([URL], String)

    var errorDescription: String? {
        switch self {
        case .collectionUnavailable:
            return "HSTracker could not read the Hearthstone collection. Start Hearthstone, log in, and wait for HSTracker to read the collection."
        case .partialExport(let files, let details):
            return "Some files were saved: \(files.map { $0.lastPathComponent }.joined(separator: ", ")). Baseline was not advanced. \(details)"
        case .invalidBaseline:
            return "Selected baseline is not a valid full Manacost collection JSON export."
        }
    }
}

struct ManacostExportOptions {
    let outputFolder: URL
    let includeCardNames: Bool
    let includeGoldenCount: Bool
    let includeMetadata: Bool

    static func defaultOutputFolder() -> URL {
        let documents = FileManager.default.urls(for: .documentDirectory, in: .userDomainMask).first
        return (documents ?? FileManager.default.homeDirectoryForCurrentUser)
            .appendingPathComponent("HSTracker Collection Exports", isDirectory: true)
    }
}

struct ManacostExportResult {
    let exportedAt: Date
    let cardCount: Int
    let changeCount: Int
    let baselineCreated: Bool
    let files: [URL]
    var warning: String? = nil
}

final class ManacostCollectionExporter {
    private static let exportSource = "HSTracker macOS export by Manacost"
    private static let exportVersion = 3

    private let baselineURL: URL
    private let store: ManacostSnapshotStore
    private var currentUser: ManacostUserProfileRecord?

    init(baselineURL: URL = ManacostCollectionExporter.defaultBaselineURL()) {
        self.baselineURL = baselineURL
        self.store = ManacostSnapshotStore(root: baselineURL.deletingLastPathComponent())
    }

    func export(format: ManacostExportFormat, options: ManacostExportOptions) throws -> ManacostExportResult {
        try prepareDirectory(options.outputFolder)
        let exportedAt = Date()
        let document = try makeDocument(exportedAt: exportedAt, options: options)
        let baseName = "hearthstone-collection-\(Self.fileStamp(exportedAt))"
        var files = [URL]()
        var staged = [(URL, URL)]()
        defer { for (temp, _) in staged { try? FileManager.default.removeItem(at: temp) } }

        if format.includesJson {
            let url = options.outputFolder.appendingPathComponent("\(baseName).json")
            let temp = url.appendingPathExtension("tmp")
            staged.append((temp, url))
            try writeFullJSON(document, options: options, to: temp)
        }

        if format.includesCsv {
            let url = options.outputFolder.appendingPathComponent("\(baseName).csv")
            let temp = url.appendingPathExtension("tmp")
            staged.append((temp, url))
            try writeCSV(document.cards, options: options, to: temp)
        }

        do {
            for (temp, url) in staged { try FileManager.default.moveItem(at: temp, to: url); files.append(url) }
        } catch { throw ManacostCollectionExportError.partialExport(files, error.localizedDescription) }
        var warning: String?
        do { try saveBaseline(document) } catch { warning = "Export files saved; baseline/history were not updated. " + error.localizedDescription }

        return ManacostExportResult(
            exportedAt: exportedAt,
            cardCount: document.cards.count,
            changeCount: 0,
            baselineCreated: false,
            files: files,
            warning: warning
        )
    }

    func exportChanges(format: ManacostExportFormat, options: ManacostExportOptions) throws -> ManacostExportResult {
        try prepareDirectory(options.outputFolder)
                let exportedAt = Date()
        let current = try makeDocument(exportedAt: exportedAt, options: options)
        if try store.baseline(current.user) == nil,
           let legacyData = try? Data(contentsOf: baselineURL),
           let legacy = try? JSONDecoder().decode(ManacostCollectionExportDocument.self, from: legacyData),
           legacy.user.accountHi == current.user.accountHi && legacy.user.accountLo == current.user.accountLo {
            try store.save(legacy, complete: false)
        }
        let stored = try store.baseline(current.user)
        let previous = stored?.completeCounts == true ? try stored?.document : nil

        guard let previous = previous else {
            try saveBaseline(current)
            return ManacostExportResult(
                exportedAt: exportedAt,
                cardCount: current.cards.count,
                changeCount: 0,
                baselineCreated: true,
                files: []
            )
        }

        let delta = buildDelta(previous: previous, current: current, exportedAt: exportedAt)
        let baseName = "hearthstone-collection-changes-\(Self.fileStamp(exportedAt))"
        var files = [URL]()
        var staged = [(URL, URL)]()
        defer { for (temp, _) in staged { try? FileManager.default.removeItem(at: temp) } }

        if format.includesJson {
            let url = options.outputFolder.appendingPathComponent("\(baseName).json")
            let temp = url.appendingPathExtension("tmp")
            staged.append((temp, url))
            try writeJSON(delta, to: temp)
        }

        if format.includesCsv {
            let url = options.outputFolder.appendingPathComponent("\(baseName).csv")
            let temp = url.appendingPathExtension("tmp")
            staged.append((temp, url))
            try writeDeltaCSV(delta.cards, to: temp)
        }

        do {
            for (temp, url) in staged { try FileManager.default.moveItem(at: temp, to: url); files.append(url) }
        } catch { throw ManacostCollectionExportError.partialExport(files, error.localizedDescription) }
        var warning: String?
        do { try saveBaseline(current) } catch { warning = "Export files saved; baseline/history were not updated. " + error.localizedDescription }

        return ManacostExportResult(
            exportedAt: exportedAt,
            cardCount: current.cards.count,
            changeCount: delta.summary.totalChanges,
            baselineCreated: false,
            files: files,
            warning: warning
        )
    }

    func setCurrentAsBaseline(options: ManacostExportOptions) throws -> Int {
        let document = try makeDocument(exportedAt: Date(), options: options)
        try saveBaseline(document)
        return document.cards.count
    }

    func clearBaseline() throws {
        guard let user = currentUser else { throw ManacostCollectionExportError.collectionUnavailable }
        try store.clear(user)
    }
    func historyFolder(options: ManacostExportOptions) throws -> URL {
        let document = try makeDocument(exportedAt: Date(), options: options)
        return try store.directory(document.user).appendingPathComponent("history")
    }
    func exportHistory(first: URL, second: URL, options: ManacostExportOptions) throws -> [URL] {
        let a = try store.read(first); let b = try store.read(second)
        let previous = try a.document; let current = try b.document
        guard a.completeCounts && b.completeCounts,
              previous.user.accountHi == current.user.accountHi && previous.user.accountLo == current.user.accountLo,
              previous.exportedAt < current.exportedAt else { throw ManacostCollectionExportError.invalidBaseline }
        let delta = buildDelta(previous: previous, current: current, exportedAt: Date())
        try prepareDirectory(options.outputFolder)
        let name = "hearthstone-collection-changes-history-" + UUID().uuidString
        let json = options.outputFolder.appendingPathComponent(name + ".json")
        let csv = options.outputFolder.appendingPathComponent(name + ".csv")
        try writeJSON(delta, to: json); try writeDeltaCSV(delta.cards, to: csv)
        return [json, csv]
    }

    private func makeDocument(exportedAt: Date, options: ManacostExportOptions) throws -> ManacostCollectionExportDocument {
        CollectionHelpers.hearthstone.updateCollection()
        guard let collection = CollectionHelpers.hearthstone.getCollection(), !collection.collection.isEmpty else {
            throw ManacostCollectionExportError.collectionUnavailable
        }

        currentUser = ManacostUserProfileRecord(battleTag: collection.battleTag, accountHi: UInt64(bitPattern: collection.accountHi), accountLo: UInt64(bitPattern: collection.accountLo))
        _ = try ManacostSnapshotStore.accountKey(currentUser!)
        let playerRecords = buildPlayerRecords(collection.player_records)
        let classStats = buildClassStats(playerRecords)
        return ManacostCollectionExportDocument(
            exportedAt: Self.isoString(exportedAt),
            source: Self.exportSource,
            version: Self.exportVersion,
            user: ManacostUserProfileRecord(
                battleTag: collection.battleTag,
                accountHi: UInt64(bitPattern: collection.accountHi),
                accountLo: UInt64(bitPattern: collection.accountLo)
            ),
            dust: collection.dust,
            cardBacks: collection.cardbacks.sorted(),
            favoriteCardBack: collection.favorite_cardback,
            favoriteHeroes: buildFavoriteHeroes(collection.favorite_heroes),
            playerRecords: playerRecords,
            classStats: classStats,
            favoriteClass: buildFavoriteClass(classStats, reason: "mostGames"),
            bestClassByWins: buildFavoriteClass(classStats, reason: "mostWins"),
            cards: buildCards(collection.collection, options: options)
        )
    }

    private func buildCards(_ collection: [Int: [Int]], options: ManacostExportOptions) -> [ManacostCollectionCardRecord] {
        return collection.map { dbfId, counts in
            let card = Cards.by(dbfId: dbfId, collectible: false)
            let normal = Self.safeCount(counts, 0)
            let golden = Self.safeCount(counts, 1)
            let diamond = Self.safeCount(counts, 2)
            let signature = Self.safeCount(counts, 3)

            return ManacostCollectionCardRecord(
                cardId: card?.id ?? "",
                dbfId: dbfId,
                name: card?.name ?? "",
                set: Self.normalized(card?.set?.rawValue),
                rarity: Self.normalized(card?.rarity.rawValue),
                cardClass: Self.normalized(card?.playerClass.rawValue),
                normal: normal,
                golden: golden,
                diamond: diamond,
                signature: signature,
                trialNormal: Self.safeCount(counts, 4),
                trialGolden: Self.safeCount(counts, 5),
                trialDiamond: Self.safeCount(counts, 6),
                trialSignature: Self.safeCount(counts, 7)
            )
        }
        .sorted { left, right in
            if left.dbfId == right.dbfId {
                return left.cardId < right.cardId
            }
            return left.dbfId < right.dbfId
        }
    }

    private func buildFavoriteHeroes(_ heroes: [Int: Int]) -> [ManacostFavoriteHeroRecord] {
        return heroes.map { heroKey, dbfId in
            let card = Cards.by(dbfId: dbfId, collectible: false)
            return ManacostFavoriteHeroRecord(
                heroKey: heroKey,
                dbfId: dbfId,
                cardId: card?.id ?? "",
                name: card?.name ?? "",
                cardClass: Self.normalized(card?.playerClass.rawValue)
            )
        }
        .sorted {
            if $0.heroKey == $1.heroKey {
                return $0.dbfId < $1.dbfId
            }
            return $0.heroKey < $1.heroKey
        }
    }

    private func buildPlayerRecords(_ records: [Int: [Int: [Int]]]) -> [ManacostPlayerRecordGroup] {
        return records.map { type, entries in
            let mapped = entries.map { data, counts in
                ManacostPlayerRecordEntry(
                    data: data,
                    wins: Self.safeCount(counts, 0),
                    losses: Self.safeCount(counts, 1),
                    ties: Self.safeCount(counts, 2)
                )
            }
            .sorted { $0.data < $1.data }
            return ManacostPlayerRecordGroup(type: type, records: mapped)
        }
        .sorted { $0.type < $1.type }
    }

    private func buildClassStats(_ groups: [ManacostPlayerRecordGroup]) -> [ManacostClassStatRecord] {
        var builders = [String: ManacostClassStatBuilder]()
        for group in groups {
            for record in group.records where record.data > 0 {
                guard let className = classNameFromHeroDbfId(record.data) else {
                    continue
                }
                var builder = builders[className] ?? ManacostClassStatBuilder(cardClass: className)
                builder.add(type: group.type, record: record)
                builders[className] = builder
            }
        }

        return builders.values.map { $0.record() }
            .sorted {
                if $0.games == $1.games {
                    if $0.wins == $1.wins {
                        return $0.cardClass < $1.cardClass
                    }
                    return $0.wins > $1.wins
                }
                return $0.games > $1.games
            }
    }

    private func classNameFromHeroDbfId(_ dbfId: Int) -> String? {
        guard let card = Cards.by(dbfId: dbfId, collectible: false),
              card.playerClass != .invalid,
              card.playerClass != .neutral else {
            return nil
        }
        return Self.normalized(card.playerClass.rawValue)
    }

    private func buildFavoriteClass(_ classStats: [ManacostClassStatRecord], reason: String) -> ManacostFavoriteClassRecord? {
        let stat: ManacostClassStatRecord?
        if reason == "mostWins" {
            stat = classStats.sorted {
                if $0.wins == $1.wins {
                    if $0.games == $1.games {
                        return $0.cardClass < $1.cardClass
                    }
                    return $0.games > $1.games
                }
                return $0.wins > $1.wins
            }.first
        } else {
            stat = classStats.first
        }

        guard let stat = stat else {
            return nil
        }

        return ManacostFavoriteClassRecord(
            cardClass: stat.cardClass,
            reason: reason,
            wins: stat.wins,
            losses: stat.losses,
            ties: stat.ties,
            games: stat.games,
            winrate: stat.winrate
        )
    }

    private func saveBaseline(_ document: ManacostCollectionExportDocument) throws {
        try store.save(document)
    }

    private func buildDelta(
        previous: ManacostCollectionExportDocument,
        current: ManacostCollectionExportDocument,
        exportedAt: Date
    ) -> ManacostCollectionDeltaExportDocument {
        let cardChanges = buildCardChanges(previous: previous.cards, current: current.cards)
        let cardBackChanges = buildIntListDelta(previous: previous.cardBacks, current: current.cardBacks)
        let favoriteHeroChanges = buildFavoriteHeroesDelta(previous: previous.favoriteHeroes, current: current.favoriteHeroes)
        let playerRecordChanges = buildPlayerRecordDeltas(previous: previous.playerRecords, current: current.playerRecords)
        let classStatChanges = buildClassStatDeltas(previous: previous.classStats, current: current.classStats)
        let dustChange = previous.dust == current.dust ? nil : numericChange(previous: previous.dust, current: current.dust)
        let favoriteCardBackChange = previous.favoriteCardBack == current.favoriteCardBack
            ? nil
            : numericChange(previous: previous.favoriteCardBack, current: current.favoriteCardBack)
        let userChange: ManacostValueChange<ManacostUserProfileRecord>? = previous.user.accountHi == current.user.accountHi && previous.user.accountLo == current.user.accountLo ? nil : ManacostValueChange(previous: previous.user, current: current.user)
        let favoriteClassChange: ManacostValueChange<ManacostFavoriteClassRecord>? = previous.favoriteClass == current.favoriteClass
            ? nil
            : ManacostValueChange(previous: previous.favoriteClass, current: current.favoriteClass)
        let bestClassByWinsChange: ManacostValueChange<ManacostFavoriteClassRecord>? = previous.bestClassByWins == current.bestClassByWins
            ? nil
            : ManacostValueChange(previous: previous.bestClassByWins, current: current.bestClassByWins)

        // Give the compiler explicit Int operands instead of a long overloaded
        // addition expression with optional/generic branches.
        let changeCounts: [Int] = [cardChanges.count, dustChange == nil ? 0 : 1,
            cardBackChanges.added.count, cardBackChanges.removed.count,
            favoriteCardBackChange == nil ? 0 : 1, favoriteHeroChanges.added.count,
            favoriteHeroChanges.removed.count, playerRecordChanges.count, classStatChanges.count,
            favoriteClassChange == nil ? 0 : 1, bestClassByWinsChange == nil ? 0 : 1, userChange == nil ? 0 : 1]
        let summary = ManacostCollectionDeltaSummary(
            totalChanges: changeCounts.reduce(0, +),
            cardChanges: cardChanges.count,
            cardsAdded: cardChanges.filter { $0.changeType == "added" }.count,
            cardsRemoved: cardChanges.filter { $0.changeType == "removed" }.count,
            cardsChanged: cardChanges.filter { $0.changeType == "changed" }.count,
            dustChanged: dustChange != nil,
            cardBacksAdded: cardBackChanges.added.count,
            cardBacksRemoved: cardBackChanges.removed.count,
            favoriteCardBackChanged: favoriteCardBackChange != nil,
            favoriteHeroesAdded: favoriteHeroChanges.added.count,
            favoriteHeroesRemoved: favoriteHeroChanges.removed.count,
            playerRecordChanges: playerRecordChanges.count,
            classStatChanges: classStatChanges.count,
            favoriteClassChanged: favoriteClassChange != nil,
            bestClassByWinsChanged: bestClassByWinsChange != nil,
            userChanged: userChange != nil
        )

        return ManacostCollectionDeltaExportDocument(
            exportedAt: Self.isoString(exportedAt),
            source: Self.exportSource,
            version: Self.exportVersion,
            exportType: "changes",
            baselineExportedAt: previous.exportedAt,
            currentExportedAt: current.exportedAt,
            summary: summary,
            user: userChange,
            dust: dustChange,
            cardBacks: cardBackChanges,
            favoriteCardBack: favoriteCardBackChange,
            favoriteHeroes: favoriteHeroChanges,
            playerRecords: playerRecordChanges,
            classStats: classStatChanges,
            favoriteClass: favoriteClassChange,
            bestClassByWins: bestClassByWinsChange,
            cards: cardChanges
        )
    }

    private func buildCardChanges(
        previous: [ManacostCollectionCardRecord],
        current: [ManacostCollectionCardRecord]
    ) -> [ManacostCollectionCardDeltaRecord] {
        let previousMap = cardLookup(previous)
        let currentMap = cardLookup(current)
        let keys = Set(previousMap.keys).union(currentMap.keys)

        return keys.compactMap { key in
            let old = previousMap[key]
            let new = currentMap[key]
            if old != nil && new != nil && old!.normal == new!.normal && old!.golden == new!.golden &&
               old!.diamond == new!.diamond && old!.signature == new!.signature && old!.trialNormal == new!.trialNormal &&
               old!.trialGolden == new!.trialGolden && old!.trialDiamond == new!.trialDiamond && old!.trialSignature == new!.trialSignature {
                return nil
            }

            let identity = new ?? old!
            return ManacostCollectionCardDeltaRecord(
                changeType: old == nil ? "added" : new == nil ? "removed" : "changed",
                cardId: identity.cardId,
                dbfId: identity.dbfId,
                name: identity.name,
                set: identity.set,
                rarity: identity.rarity,
                cardClass: identity.cardClass,
                previous: old,
                current: new,
                delta: cardCountDelta(previous: old, current: new)
            )
        }
        .sorted {
            if $0.dbfId == $1.dbfId {
                return $0.cardId < $1.cardId
            }
            return $0.dbfId < $1.dbfId
        }
    }

    private func buildPlayerRecordDeltas(
        previous: [ManacostPlayerRecordGroup],
        current: [ManacostPlayerRecordGroup]
    ) -> [ManacostPlayerRecordDelta] {
        let previousMap = playerRecordLookup(previous)
        let currentMap = playerRecordLookup(current)
        let keys = Set(previousMap.keys).union(currentMap.keys)

        return keys.compactMap { key in
            let old = previousMap[key]
            let new = currentMap[key]
            if old?.record == new?.record {
                return nil
            }
            let identity = new ?? old!
            return ManacostPlayerRecordDelta(
                type: identity.type,
                data: identity.data,
                previous: old?.record,
                current: new?.record,
                delta: ManacostPlayerRecordEntry(
                    data: identity.data,
                    wins: (new?.record.wins ?? 0) - (old?.record.wins ?? 0),
                    losses: (new?.record.losses ?? 0) - (old?.record.losses ?? 0),
                    ties: (new?.record.ties ?? 0) - (old?.record.ties ?? 0)
                )
            )
        }
        .sorted {
            if $0.type == $1.type {
                return $0.data < $1.data
            }
            return $0.type < $1.type
        }
    }

    private func buildClassStatDeltas(
        previous: [ManacostClassStatRecord],
        current: [ManacostClassStatRecord]
    ) -> [ManacostClassStatDelta] {
        let previousMap = classStatLookup(previous)
        let currentMap = classStatLookup(current)
        let keys = Set(previousMap.keys).union(currentMap.keys)

        return keys.compactMap { key in
            let old = previousMap[key]
            let new = currentMap[key]
            if old?.wins == new?.wins,
               old?.losses == new?.losses,
               old?.ties == new?.ties,
               old?.games == new?.games {
                return nil
            }
            let identity = new ?? old!
            return ManacostClassStatDelta(
                cardClass: identity.cardClass,
                previous: old,
                current: new,
                delta: ManacostClassStatCountDelta(
                    wins: (new?.wins ?? 0) - (old?.wins ?? 0),
                    losses: (new?.losses ?? 0) - (old?.losses ?? 0),
                    ties: (new?.ties ?? 0) - (old?.ties ?? 0),
                    games: (new?.games ?? 0) - (old?.games ?? 0)
                )
            )
        }
        .sorted {
            if abs($0.delta.games) == abs($1.delta.games) {
                return $0.cardClass < $1.cardClass
            }
            return abs($0.delta.games) > abs($1.delta.games)
        }
    }

    private func buildFavoriteHeroesDelta(
        previous: [ManacostFavoriteHeroRecord],
        current: [ManacostFavoriteHeroRecord]
    ) -> ManacostFavoriteHeroesDelta {
        let old = favoriteHeroLookup(previous)
        let new = favoriteHeroLookup(current)
        var added: [ManacostFavoriteHeroRecord] = []
        var removed: [ManacostFavoriteHeroRecord] = []
        for (key, value) in new where old[key] == nil { added.append(value) }
        for (key, value) in old where new[key] == nil { removed.append(value) }
        let ordered: (ManacostFavoriteHeroRecord, ManacostFavoriteHeroRecord) -> Bool = { a, b in
            a.heroKey == b.heroKey ? a.dbfId < b.dbfId : a.heroKey < b.heroKey
        }
        added.sort(by: ordered); removed.sort(by: ordered)
        return ManacostFavoriteHeroesDelta(added: added, removed: removed)
    }

    private func buildIntListDelta(previous: [Int], current: [Int]) -> ManacostIntListDelta {
        let old = Set(previous)
        let new = Set(current)
        return ManacostIntListDelta(
            added: Array(new.subtracting(old)).sorted(),
            removed: Array(old.subtracting(new)).sorted()
        )
    }

    private func numericChange(previous: Int, current: Int) -> ManacostNumericChange {
        return ManacostNumericChange(previous: previous, current: current, delta: current - previous)
    }

    private func cardCountDelta(
        previous: ManacostCollectionCardRecord?,
        current: ManacostCollectionCardRecord?
    ) -> ManacostCollectionCardCountDelta {
        return ManacostCollectionCardCountDelta(
            normal: (current?.normal ?? 0) - (previous?.normal ?? 0),
            golden: (current?.golden ?? 0) - (previous?.golden ?? 0),
            diamond: (current?.diamond ?? 0) - (previous?.diamond ?? 0),
            signature: (current?.signature ?? 0) - (previous?.signature ?? 0),
            premiumTotal: (current?.premiumTotal ?? 0) - (previous?.premiumTotal ?? 0),
            trialNormal: (current?.trialNormal ?? 0) - (previous?.trialNormal ?? 0),
            trialGolden: (current?.trialGolden ?? 0) - (previous?.trialGolden ?? 0),
            trialDiamond: (current?.trialDiamond ?? 0) - (previous?.trialDiamond ?? 0),
            trialSignature: (current?.trialSignature ?? 0) - (previous?.trialSignature ?? 0),
            ownedTotal: (current?.ownedTotal ?? 0) - (previous?.ownedTotal ?? 0)
        )
    }

    private func playerRecordLookup(
        _ groups: [ManacostPlayerRecordGroup]
    ) -> [String: (type: Int, data: Int, record: ManacostPlayerRecordEntry)] {
        var result = [String: (type: Int, data: Int, record: ManacostPlayerRecordEntry)]()
        for group in groups {
            for record in group.records {
                result["\(group.type):\(record.data)"] = (group.type, record.data, record)
            }
        }
        return result
    }

    private func cardLookup(_ cards: [ManacostCollectionCardRecord]) -> [String: ManacostCollectionCardRecord] {
        var result = [String: ManacostCollectionCardRecord]()
        for card in cards {
            result[cardKey(card)] = card
        }
        return result
    }

    private func classStatLookup(_ stats: [ManacostClassStatRecord]) -> [String: ManacostClassStatRecord] {
        var result = [String: ManacostClassStatRecord]()
        for stat in stats where !stat.cardClass.isEmpty {
            result[stat.cardClass] = stat
        }
        return result
    }

    private func favoriteHeroLookup(_ heroes: [ManacostFavoriteHeroRecord]) -> [String: ManacostFavoriteHeroRecord] {
        var result = [String: ManacostFavoriteHeroRecord]()
        for hero in heroes {
            result["\(hero.heroKey):\(hero.dbfId):\(hero.cardId)"] = hero
        }
        return result
    }

    private func cardKey(_ card: ManacostCollectionCardRecord) -> String {
        return card.cardId.isEmpty ? "dbf:\(card.dbfId)" : "card:\(card.cardId)"
    }

    private func writeJSON<T: Encodable>(_ value: T, to url: URL) throws {
        let encoder = JSONEncoder()
        if #available(macOS 10.13, *) {
            encoder.outputFormatting = [.prettyPrinted, .sortedKeys]
        } else {
            encoder.outputFormatting = [.prettyPrinted]
        }
        let data = try encoder.encode(value)
        try data.write(to: url, options: [.atomic])
    }

    private func writeFullJSON(_ document: ManacostCollectionExportDocument, options: ManacostExportOptions, to url: URL) throws {
        let encoded = try JSONEncoder().encode(document)
        var object = try JSONSerialization.jsonObject(with: encoded) as! [String: Any]
        var cards = object["cards"] as! [[String: Any]]
        for index in cards.indices {
            if !options.includeCardNames { cards[index]["name"] = "" }
            if !options.includeMetadata { for key in ["set", "rarity", "class"] { cards[index][key] = "" } }
        }
        object["cards"] = cards
        try JSONSerialization.data(withJSONObject: object, options: [.sortedKeys, .prettyPrinted]).write(to: url, options: .atomic)
    }

    private func writeCSV(_ cards: [ManacostCollectionCardRecord], options: ManacostExportOptions, to url: URL) throws {
        var lines = ["cardId,dbfId,name,set,rarity,class,normal,golden,ownedTotal"]
        for card in cards {
            lines.append([
                card.cardId,
                String(card.dbfId),
                options.includeCardNames ? card.name : "",
                options.includeMetadata ? card.set : "",
                options.includeMetadata ? card.rarity : "",
                options.includeMetadata ? card.cardClass : "",
                String(card.normal),
                options.includeGoldenCount ? String(card.golden) : "",
                String(card.ownedTotal)
            ].map(Self.escapeCSV).joined(separator: ","))
        }
        if let data = lines.joined(separator: "\n").data(using: .utf8) {
            try data.write(to: url, options: [.atomic])
        }
    }

    private func writeDeltaCSV(_ changes: [ManacostCollectionCardDeltaRecord], to url: URL) throws {
        var lines = [
            "changeType,cardId,dbfId,name,set,rarity,class,normalDelta,goldenDelta,ownedTotalDelta,previousNormal,previousGolden,previousOwnedTotal,currentNormal,currentGolden,currentOwnedTotal"
        ]
        for change in changes {
            let fields: [String] = [
                change.changeType,
                change.cardId,
                String(change.dbfId),
                change.name,
                change.set,
                change.rarity,
                change.cardClass,
                String(change.delta.normal),
                String(change.delta.golden),
                String(change.delta.ownedTotal),
                String(change.previous?.normal ?? 0),
                String(change.previous?.golden ?? 0),
                String(change.previous?.ownedTotal ?? 0),
                String(change.current?.normal ?? 0),
                String(change.current?.golden ?? 0),
                String(change.current?.ownedTotal ?? 0)
            ]
            lines.append(fields.map(Self.escapeCSV).joined(separator: ","))
        }
        if let data = lines.joined(separator: "\n").data(using: .utf8) {
            try data.write(to: url, options: [.atomic])
        }
    }

    private func prepareDirectory(_ url: URL) throws {
        try FileManager.default.createDirectory(at: url, withIntermediateDirectories: true, attributes: nil)
    }

    private static func defaultBaselineURL() -> URL {
        let appSupport = FileManager.default.urls(for: .applicationSupportDirectory, in: .userDomainMask).first
            ?? FileManager.default.homeDirectoryForCurrentUser.appendingPathComponent("Library/Application Support", isDirectory: true)
        return appSupport
            .appendingPathComponent("HSTracker/ManacostCollectionExporter", isDirectory: true)
            .appendingPathComponent("baseline.json")
    }

    private static func isoString(_ date: Date) -> String {
        let formatter = DateFormatter()
        formatter.locale = Locale(identifier: "en_US_POSIX")
        formatter.timeZone = TimeZone(secondsFromGMT: 0)
        formatter.dateFormat = "yyyy-MM-dd'T'HH:mm:ss.SSS'Z'"
        return formatter.string(from: date)
    }

    private static func fileStamp(_ date: Date) -> String {
        let formatter = DateFormatter()
        formatter.locale = Locale(identifier: "en_US_POSIX")
        formatter.dateFormat = "yyyyMMdd-HHmmss-SSS"
        return formatter.string(from: date) + "-" + UUID().uuidString
    }

    private static func safeCount(_ values: [Int], _ index: Int) -> Int {
        guard index >= 0, index < values.count else {
            return 0
        }
        return max(0, values[index])
    }

    private static func normalized(_ value: String?) -> String {
        return value?.uppercased() ?? ""
    }

    private static func escapeCSV(_ value: String) -> String {
        if !value.contains(",") && !value.contains("\"") && !value.contains("\n") && !value.contains("\r") {
            return value
        }
        return "\"\(value.replacingOccurrences(of: "\"", with: "\"\""))\""
    }
}

private struct ManacostClassStatBuilder {
    let cardClass: String
    private(set) var wins = 0
    private(set) var losses = 0
    private(set) var ties = 0
    private var recordTypes = [Int: ManacostClassRecordTypeBuilder]()

    init(cardClass: String) { self.cardClass = cardClass }

    mutating func add(type: Int, record: ManacostPlayerRecordEntry) {
        wins += record.wins
        losses += record.losses
        ties += record.ties
        var builder = recordTypes[type] ?? ManacostClassRecordTypeBuilder(type: type)
        builder.add(record)
        recordTypes[type] = builder
    }

    func record() -> ManacostClassStatRecord {
        let games = wins + losses + ties
        return ManacostClassStatRecord(
            cardClass: cardClass,
            wins: wins,
            losses: losses,
            ties: ties,
            games: games,
            winrate: games <= 0 ? 0 : Double(round((Double(wins) / Double(games)) * 10_000) / 10_000),
            recordTypes: recordTypes.values.map { $0.record() }
                .sorted { $0.games == $1.games ? $0.type < $1.type : $0.games > $1.games }
        )
    }
}

private struct ManacostClassRecordTypeBuilder {
    let type: Int
    private(set) var wins = 0
    private(set) var losses = 0
    private(set) var ties = 0
    private var heroDbfIds = Set<Int>()

    init(type: Int) { self.type = type }

    mutating func add(_ record: ManacostPlayerRecordEntry) {
        wins += record.wins
        losses += record.losses
        ties += record.ties
        heroDbfIds.insert(record.data)
    }

    func record() -> ManacostClassRecordTypeStat {
        let games = wins + losses + ties
        return ManacostClassRecordTypeStat(
            type: type,
            wins: wins,
            losses: losses,
            ties: ties,
            games: games,
            heroDbfIds: Array(heroDbfIds).sorted()
        )
    }
}
