import Foundation
import CryptoKit

// Source adapter for HSTracker; public JSON remains schema 3.
struct ManacostStoredSnapshot: Codable {
    let snapshotVersion: Int
    let completeCounts: Bool
    let checksum: String
    let data: Data
    var document: ManacostCollectionExportDocument {
        get throws { try JSONDecoder().decode(ManacostCollectionExportDocument.self, from: data) }
    }
}

final class ManacostSnapshotStore {
    let root: URL
    init(root: URL) { self.root = root }
    static func accountKey(_ user: ManacostUserProfileRecord) throws -> String {
        guard user.accountHi != 0 || user.accountLo != 0 else { throw ManacostCollectionExportError.invalidBaseline }
        return hash(Data("\(user.accountHi):\(user.accountLo)".utf8))
    }
    func directory(_ user: ManacostUserProfileRecord) throws -> URL {
        return root.appendingPathComponent("accounts").appendingPathComponent(try Self.accountKey(user))
    }
    func baseline(_ user: ManacostUserProfileRecord) throws -> ManacostStoredSnapshot? {
        let url = try directory(user).appendingPathComponent("baseline.json")
        let candidates = [url, url.appendingPathExtension("bak")]
        guard candidates.contains(where: { FileManager.default.fileExists(atPath: $0.path) }) else { return nil }
        for candidate in candidates {
            if let stored = try? read(candidate), let document = try? stored.document,
               document.user.accountHi == user.accountHi && document.user.accountLo == user.accountLo { return stored }
        }
        throw ManacostCollectionExportError.invalidBaseline
    }
    @discardableResult func save(_ document: ManacostCollectionExportDocument, complete: Bool = true) throws -> URL {
        let encoder = JSONEncoder(); encoder.outputFormatting = [.sortedKeys]
        let data = try encoder.encode(document)
        let stored = ManacostStoredSnapshot(snapshotVersion: 1, completeCounts: complete, checksum: Self.hash(data), data: data)
        let dir = try directory(document.user)
        let history = dir.appendingPathComponent("history")
        try FileManager.default.createDirectory(at: history, withIntermediateDirectories: true)
        let file = history.appendingPathComponent("\(Int(Date().timeIntervalSince1970 * 1000))-\(UUID().uuidString).json")
        let encoded = try encoder.encode(stored)
        let temp = file.appendingPathExtension("tmp")
        defer { try? FileManager.default.removeItem(at: temp) }
        try encoded.write(to: temp, options: .atomic)
        try FileManager.default.moveItem(at: temp, to: file)
        let active = dir.appendingPathComponent("baseline.json")
        if FileManager.default.fileExists(atPath: active.path) {
            try Data(contentsOf: active).write(to: active.appendingPathExtension("bak"), options: .atomic)
        }
        try encoded.write(to: active, options: .atomic)
        return file
    }
    func read(_ url: URL) throws -> ManacostStoredSnapshot {
        let stored = try JSONDecoder().decode(ManacostStoredSnapshot.self, from: Data(contentsOf: url))
        guard stored.snapshotVersion == 1, stored.checksum == Self.hash(stored.data) else { throw ManacostCollectionExportError.invalidBaseline }
        _ = try Self.accountKey(stored.document.user)
        return stored
    }
    func clear(_ user: ManacostUserProfileRecord) throws {
        let url = try directory(user).appendingPathComponent("baseline.json")
        for file in [url, url.appendingPathExtension("bak")] where FileManager.default.fileExists(atPath: file.path) { try FileManager.default.removeItem(at: file) }
    }
    func history(_ user: ManacostUserProfileRecord) throws -> [URL] {
        let dir = try directory(user).appendingPathComponent("history")
        guard FileManager.default.fileExists(atPath: dir.path) else { return [] }
        return try FileManager.default.contentsOfDirectory(at: dir, includingPropertiesForKeys: nil).filter { $0.pathExtension == "json" }.sorted { $0.lastPathComponent > $1.lastPathComponent }
    }
    private static func hash(_ data: Data) -> String { SHA256.hash(data: data).map { String(format: "%02x", $0) }.joined() }
}
