// Compile-time boundary doubles: verify the adapter against the public shapes
// it consumes. This does not replace a native HSTracker integration build.
import Foundation

enum FixtureCardClass: String { case invalid, neutral, mage }
struct FixtureCardSet { let rawValue: String }
struct FixtureCardRarity { let rawValue: String }
struct FixtureCard {
    let id: String
    let name: String
    let set: FixtureCardSet?
    let rarity: FixtureCardRarity
    let playerClass: FixtureCardClass
}
enum Cards {
    static func by(dbfId: Int, collectible: Bool) -> FixtureCard? { nil }
}
struct FixtureCollection {
    let collection: [Int: [Int]]
    let accountHi: Int64
    let accountLo: Int64
    let battleTag: String
    let dust: Int
    let cardbacks: [Int]
    let favorite_cardback: Int
    let favorite_heroes: [Int: Int]
    let player_records: [Int: [Int: [Int]]]
}
struct FixtureCollectionHelpers {
    func updateCollection() {}
    func getCollection() -> FixtureCollection? { nil }
}
enum CollectionHelpers { static let hearthstone = FixtureCollectionHelpers() }
