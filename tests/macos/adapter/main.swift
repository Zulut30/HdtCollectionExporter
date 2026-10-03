import Foundation

let fixtureURL = URL(fileURLWithPath: CommandLine.arguments[1])
let fixture = try JSONDecoder().decode(ManacostCollectionExportDocument.self, from: Data(contentsOf: fixtureURL))
let card = fixture.cards[0]
Cards.fixture[card.dbfId] = FixtureCard(id: card.cardId, name: card.name, set: FixtureCardSet(rawValue: card.set), rarity: FixtureCardRarity(rawValue: card.rarity), playerClass: .mage)
func collection(normal: Int = 2, accountLo: UInt64 = 9_007_199_254_740_993, tag: String = "Fixture#0001") -> FixtureCollection {
    FixtureCollection(collection: [card.dbfId: [normal, 1, 1, 1, 3, 2, 1, 1]], accountHi: Int64(bitPattern: UInt64.max), accountLo: Int64(bitPattern: accountLo), battleTag: tag, dust: 123, cardbacks: [], favorite_cardback: 0, favorite_heroes: [:], player_records: [:])
}
let root = FileManager.default.temporaryDirectory.appendingPathComponent(UUID().uuidString)
defer { try? FileManager.default.removeItem(at: root) }
let exporter = ManacostCollectionExporter(baselineURL: root.appendingPathComponent("baseline.json"))
let options = ManacostExportOptions(outputFolder: root.appendingPathComponent("exports"), includeCardNames: false, includeGoldenCount: false, includeMetadata: false)
CollectionHelpers.fixture = collection()
let full = try exporter.export(format: .both, options: options)
precondition(full.files.count == 2 && full.warning == nil)
let document = try JSONDecoder().decode(ManacostCollectionExportDocument.self, from: Data(contentsOf: full.files[0]))
precondition(document.user.accountHi == UInt64.max && document.cards[0].ownedTotal == 5)
precondition(document.cards[0].premiumTotal == 3 && document.cards[0].name.isEmpty)
let unchanged = try exporter.exportChanges(format: .both, options: options)
precondition(!unchanged.baselineCreated && unchanged.changeCount == 0)
CollectionHelpers.fixture = collection(normal: 3, tag: "Renamed#0002")
let changed = try exporter.exportChanges(format: .json, options: options)
let delta = try JSONDecoder().decode(ManacostCollectionDeltaExportDocument.self, from: Data(contentsOf: changed.files[0]))
precondition(changed.changeCount == 1 && delta.cards[0].delta.normal == 1 && delta.user == nil)
CollectionHelpers.fixture = collection(accountLo: 42)
let other = try exporter.exportChanges(format: .both, options: options)
precondition(other.baselineCreated && other.files.isEmpty)
print("Swift adapter fixture passed: schema3, UInt64, canonical counts, unchanged/changed delta, account isolation.")
