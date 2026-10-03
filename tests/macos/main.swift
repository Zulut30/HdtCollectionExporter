import Foundation

// HSTracker APIs are replaced only in this focused storage/models fixture check.
enum ManacostCollectionExportError: Error { case invalidBaseline }
let fixture = URL(fileURLWithPath: CommandLine.arguments[1])
let document = try JSONDecoder().decode(ManacostCollectionExportDocument.self, from: Data(contentsOf: fixture))
precondition(document.user.accountHi == UInt64.max)
precondition(document.user.accountLo == 9_007_199_254_740_993)
precondition(document.cards[0].ownedTotal == 5 && document.cards[0].premiumTotal == 3)
let root = FileManager.default.temporaryDirectory.appendingPathComponent(UUID().uuidString)
defer { try? FileManager.default.removeItem(at: root) }
let store = ManacostSnapshotStore(root: root)
let first = try store.save(document)
let original = try Data(contentsOf: first)
_ = try store.save(document)
let reread = try Data(contentsOf: first)
let history = try store.history(document.user)
let readDocument = try store.read(first).document
precondition(reread == original)
precondition(history.count == 2)
precondition(readDocument.user.accountHi == UInt64.max)
try store.clear(document.user)
let baseline = try store.baseline(document.user)
let retained = try store.history(document.user)
precondition(baseline == nil)
precondition(retained.count == 2)
print("Swift models/store interoperability passed (UInt64, counts, immutable history, reset).")
