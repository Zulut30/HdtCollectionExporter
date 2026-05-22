import Foundation

struct ManacostUserProfileRecord: Codable, Equatable {
    let battleTag: String
    let accountHi: Int64
    let accountLo: Int64
}

struct ManacostFavoriteHeroRecord: Codable, Equatable {
    let heroKey: Int
    let dbfId: Int
    let cardId: String
    let name: String
    let cardClass: String

    private enum CodingKeys: String, CodingKey {
        case heroKey, dbfId, cardId, name
        case cardClass = "class"
    }
}

struct ManacostPlayerRecordGroup: Codable, Equatable {
    let type: Int
    let records: [ManacostPlayerRecordEntry]
}

struct ManacostPlayerRecordEntry: Codable, Equatable {
    let data: Int
    let wins: Int
    let losses: Int
    let ties: Int
}

struct ManacostClassStatRecord: Codable, Equatable {
    let cardClass: String
    let wins: Int
    let losses: Int
    let ties: Int
    let games: Int
    let winrate: Double
    let recordTypes: [ManacostClassRecordTypeStat]

    private enum CodingKeys: String, CodingKey {
        case cardClass = "class"
        case wins, losses, ties, games, winrate, recordTypes
    }
}

struct ManacostClassRecordTypeStat: Codable, Equatable {
    let type: Int
    let wins: Int
    let losses: Int
    let ties: Int
    let games: Int
    let heroDbfIds: [Int]
}

struct ManacostFavoriteClassRecord: Codable, Equatable {
    let cardClass: String
    let reason: String
    let wins: Int
    let losses: Int
    let ties: Int
    let games: Int
    let winrate: Double

    private enum CodingKeys: String, CodingKey {
        case cardClass = "class"
        case reason, wins, losses, ties, games, winrate
    }
}

struct ManacostCollectionCardRecord: Codable, Equatable {
    let cardId: String
    let dbfId: Int
    let name: String
    let set: String
    let rarity: String
    let cardClass: String
    let normal: Int
    let golden: Int
    let diamond: Int
    let signature: Int
    let premiumTotal: Int
    let trialNormal: Int
    let trialGolden: Int
    let trialDiamond: Int
    let trialSignature: Int
    let ownedTotal: Int

    init(
        cardId: String,
        dbfId: Int,
        name: String,
        set: String,
        rarity: String,
        cardClass: String,
        normal: Int,
        golden: Int,
        diamond: Int,
        signature: Int,
        trialNormal: Int,
        trialGolden: Int,
        trialDiamond: Int,
        trialSignature: Int
    ) {
        self.cardId = cardId
        self.dbfId = dbfId
        self.name = name
        self.set = set
        self.rarity = rarity
        self.cardClass = cardClass
        self.normal = normal
        self.golden = golden
        self.diamond = diamond
        self.signature = signature
        self.premiumTotal = golden + diamond + signature
        self.trialNormal = trialNormal
        self.trialGolden = trialGolden
        self.trialDiamond = trialDiamond
        self.trialSignature = trialSignature
        self.ownedTotal = normal + self.premiumTotal
    }

    private enum CodingKeys: String, CodingKey {
        case cardId, dbfId, name, set, rarity
        case cardClass = "class"
        case normal, golden, diamond, signature, premiumTotal,
             trialNormal, trialGolden, trialDiamond, trialSignature, ownedTotal
    }
}

struct ManacostCollectionExportDocument: Codable {
    let exportedAt: String
    let source: String
    let version: Int
    let user: ManacostUserProfileRecord?
    let dust: Int
    let cardBacks: [Int]
    let favoriteCardBack: Int
    let favoriteHeroes: [ManacostFavoriteHeroRecord]
    let playerRecords: [ManacostPlayerRecordGroup]
    let classStats: [ManacostClassStatRecord]
    let favoriteClass: ManacostFavoriteClassRecord?
    let bestClassByWins: ManacostFavoriteClassRecord?
    let cards: [ManacostCollectionCardRecord]

    init(
        exportedAt: String,
        source: String,
        version: Int,
        user: ManacostUserProfileRecord?,
        dust: Int,
        cardBacks: [Int],
        favoriteCardBack: Int,
        favoriteHeroes: [ManacostFavoriteHeroRecord],
        playerRecords: [ManacostPlayerRecordGroup],
        classStats: [ManacostClassStatRecord],
        favoriteClass: ManacostFavoriteClassRecord?,
        bestClassByWins: ManacostFavoriteClassRecord?,
        cards: [ManacostCollectionCardRecord]
    ) {
        self.exportedAt = exportedAt
        self.source = source
        self.version = version
        self.user = user
        self.dust = dust
        self.cardBacks = cardBacks
        self.favoriteCardBack = favoriteCardBack
        self.favoriteHeroes = favoriteHeroes
        self.playerRecords = playerRecords
        self.classStats = classStats
        self.favoriteClass = favoriteClass
        self.bestClassByWins = bestClassByWins
        self.cards = cards
    }

    private enum CodingKeys: String, CodingKey {
        case exportedAt, source, version, user, dust, cardBacks, favoriteCardBack,
             favoriteHeroes, playerRecords, classStats, favoriteClass, bestClassByWins, cards
    }

    init(from decoder: Decoder) throws {
        let container = try decoder.container(keyedBy: CodingKeys.self)
        exportedAt = try container.decodeIfPresent(String.self, forKey: .exportedAt) ?? ""
        source = try container.decodeIfPresent(String.self, forKey: .source) ?? ""
        version = try container.decodeIfPresent(Int.self, forKey: .version) ?? 1
        user = try container.decodeIfPresent(ManacostUserProfileRecord.self, forKey: .user)
        dust = try container.decodeIfPresent(Int.self, forKey: .dust) ?? 0
        cardBacks = try container.decodeIfPresent([Int].self, forKey: .cardBacks) ?? []
        favoriteCardBack = try container.decodeIfPresent(Int.self, forKey: .favoriteCardBack) ?? 0
        favoriteHeroes = try container.decodeIfPresent([ManacostFavoriteHeroRecord].self, forKey: .favoriteHeroes) ?? []
        playerRecords = try container.decodeIfPresent([ManacostPlayerRecordGroup].self, forKey: .playerRecords) ?? []
        classStats = try container.decodeIfPresent([ManacostClassStatRecord].self, forKey: .classStats) ?? []
        favoriteClass = try container.decodeIfPresent(ManacostFavoriteClassRecord.self, forKey: .favoriteClass)
        bestClassByWins = try container.decodeIfPresent(ManacostFavoriteClassRecord.self, forKey: .bestClassByWins)
        cards = try container.decodeIfPresent([ManacostCollectionCardRecord].self, forKey: .cards) ?? []
    }
}

struct ManacostCollectionDeltaExportDocument: Codable {
    let exportedAt: String
    let source: String
    let version: Int
    let exportType: String
    let baselineExportedAt: String
    let currentExportedAt: String
    let summary: ManacostCollectionDeltaSummary
    let user: ManacostValueChange<ManacostUserProfileRecord>?
    let dust: ManacostNumericChange?
    let cardBacks: ManacostIntListDelta
    let favoriteCardBack: ManacostNumericChange?
    let favoriteHeroes: ManacostFavoriteHeroesDelta
    let playerRecords: [ManacostPlayerRecordDelta]
    let classStats: [ManacostClassStatDelta]
    let favoriteClass: ManacostValueChange<ManacostFavoriteClassRecord>?
    let bestClassByWins: ManacostValueChange<ManacostFavoriteClassRecord>?
    let cards: [ManacostCollectionCardDeltaRecord]
}

struct ManacostCollectionDeltaSummary: Codable {
    let totalChanges: Int
    let cardChanges: Int
    let cardsAdded: Int
    let cardsRemoved: Int
    let cardsChanged: Int
    let dustChanged: Bool
    let cardBacksAdded: Int
    let cardBacksRemoved: Int
    let favoriteCardBackChanged: Bool
    let favoriteHeroesAdded: Int
    let favoriteHeroesRemoved: Int
    let playerRecordChanges: Int
    let classStatChanges: Int
    let favoriteClassChanged: Bool
    let bestClassByWinsChanged: Bool
    let userChanged: Bool
}

struct ManacostValueChange<T: Codable>: Codable {
    let previous: T?
    let current: T?
}

struct ManacostNumericChange: Codable {
    let previous: Int
    let current: Int
    let delta: Int
}

struct ManacostIntListDelta: Codable {
    let added: [Int]
    let removed: [Int]
}

struct ManacostFavoriteHeroesDelta: Codable {
    let added: [ManacostFavoriteHeroRecord]
    let removed: [ManacostFavoriteHeroRecord]
}

struct ManacostPlayerRecordDelta: Codable {
    let type: Int
    let data: Int
    let previous: ManacostPlayerRecordEntry?
    let current: ManacostPlayerRecordEntry?
    let delta: ManacostPlayerRecordEntry
}

struct ManacostClassStatDelta: Codable {
    let cardClass: String
    let previous: ManacostClassStatRecord?
    let current: ManacostClassStatRecord?
    let delta: ManacostClassStatCountDelta

    private enum CodingKeys: String, CodingKey {
        case cardClass = "class"
        case previous, current, delta
    }
}

struct ManacostClassStatCountDelta: Codable {
    let wins: Int
    let losses: Int
    let ties: Int
    let games: Int
}

struct ManacostCollectionCardDeltaRecord: Codable {
    let changeType: String
    let cardId: String
    let dbfId: Int
    let name: String
    let set: String
    let rarity: String
    let cardClass: String
    let previous: ManacostCollectionCardRecord?
    let current: ManacostCollectionCardRecord?
    let delta: ManacostCollectionCardCountDelta

    private enum CodingKeys: String, CodingKey {
        case changeType, cardId, dbfId, name, set, rarity
        case cardClass = "class"
        case previous, current, delta
    }
}

struct ManacostCollectionCardCountDelta: Codable {
    let normal: Int
    let golden: Int
    let diamond: Int
    let signature: Int
    let premiumTotal: Int
    let trialNormal: Int
    let trialGolden: Int
    let trialDiamond: Int
    let trialSignature: Int
    let ownedTotal: Int
}
