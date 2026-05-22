const MAX_BODY_BYTES = 2 * 1024 * 1024;
const LEADERBOARD_LIMIT = 50;
const LEADERBOARD_SCAN_LIMIT = 250;
const LEADERBOARD_INDEX_PATH = "leaderboard/index.json";

module.exports = async function handler(request, response) {
  setJsonHeaders(response);

  if (request.method === "OPTIONS") {
    return response.status(204).end();
  }

  try {
    if (request.method === "POST") {
      return await saveProfile(request, response);
    }

    if (request.method === "GET") {
      return await readProfile(request, response);
    }

    response.setHeader("Allow", "GET, POST, OPTIONS");
    return response.status(405).json({ ok: false, error: "Method not allowed" });
  } catch (error) {
    const statusCode = error.statusCode || 500;
    const message = statusCode === 500
      ? "Не удалось обработать профиль."
      : error.message;
    return response.status(statusCode).json({ ok: false, error: message });
  }
};

async function saveProfile(request, response) {
  const body = await readJsonBody(request);
  const blizzardId = normalizeBlizzardId(body.blizzardId);

  if (!blizzardId) {
    throw httpError(400, "Не найден Blizzard ID.");
  }

  const now = new Date().toISOString();
  const record = {
    recordVersion: 2,
    blizzardId,
    savedAt: now,
    source: "hdt-collection-card",
    user: normalizeUser(body.user),
    export: normalizeRecord(body.export),
    settings: normalizeRecord(body.settings),
    profile: normalizeRecord(body.profile),
    collection: normalizeCollection(body.collection),
  };

  const { put } = await import("@vercel/blob");
  const blob = await put(blobPath(blizzardId), JSON.stringify(record), {
    access: "private",
    addRandomSuffix: false,
    allowOverwrite: true,
    contentType: "application/json; charset=utf-8",
  });
  await updateLeaderboardIndex(record).catch(() => null);

  return response.status(200).json({
    ok: true,
    blizzardId,
    savedAt: now,
    pathname: blob.pathname,
    url: blob.url,
  });
}

async function readProfile(request, response) {
  const requestUrl = new URL(request.url || "/", "http://localhost");

  if (requestUrl.searchParams.get("leaderboard") === "1") {
    return await readLeaderboard(response);
  }

  const blizzardId = normalizeBlizzardId(requestUrl.searchParams.get("blizzardId"));
  if (!blizzardId) {
    throw httpError(400, "Не найден Blizzard ID.");
  }

  const record = await loadProfileRecord(blizzardId);
  if (!record) {
    return response.status(404).json({ ok: false, error: "Профиль не найден." });
  }

  const profile = publicProfileRecord(record, { includeCollection: true });
  if (hasProfileReadAccess(request)) {
    return response.status(200).json({ ok: true, profile, record });
  }

  return response.status(200).json({ ok: true, profile });
}

async function readLeaderboard(response) {
  const indexed = await readLeaderboardIndex().catch(() => null);
  if (indexed && indexed.length) {
    return response.status(200).json({
      ok: true,
      users: indexed.slice(0, LEADERBOARD_LIMIT),
      total: indexed.length,
    });
  }

  const users = await scanLeaderboardProfiles();
  return response.status(200).json({ ok: true, users, total: users.length });
}

async function readLeaderboardIndex() {
  const blob = await loadRawBlob(LEADERBOARD_INDEX_PATH);
  if (!blob) {
    return null;
  }

  const index = normalizeRecord(blob);
  return Array.isArray(index.users) ? index.users : null;
}

async function updateLeaderboardIndex(record) {
  const { put } = await import("@vercel/blob");
  const currentUsers = await readLeaderboardIndex().catch(() => []) || [];
  const nextUser = publicProfileRecord(record, { includeCollection: false });
  const users = [
    nextUser,
    ...currentUsers.filter((user) => user.blizzardId !== nextUser.blizzardId),
  ]
    .sort(compareLeaderboardUsers)
    .slice(0, LEADERBOARD_LIMIT);

  await put(LEADERBOARD_INDEX_PATH, JSON.stringify({ updatedAt: new Date().toISOString(), users }), {
    access: "private",
    addRandomSuffix: false,
    allowOverwrite: true,
    contentType: "application/json; charset=utf-8",
  });
}

async function scanLeaderboardProfiles() {
  const { list } = await import("@vercel/blob");
  const result = await list({ prefix: "profiles/", limit: LEADERBOARD_SCAN_LIMIT });
  const records = await Promise.all(
    result.blobs.map(async (blob) => {
      try {
        return await loadProfileRecord(blob.pathname);
      } catch {
        return null;
      }
    }),
  );

  const users = records
    .filter(Boolean)
    .map((record) => publicProfileRecord(record, { includeCollection: false }))
    .sort(compareLeaderboardUsers)
    .slice(0, LEADERBOARD_LIMIT);
  return users;
}

async function loadProfileRecord(blizzardIdOrPathname) {
  const value = stringValue(blizzardIdOrPathname);
  const pathname = value.startsWith("profiles/") ? value : blobPath(value);
  return await loadRawBlob(pathname);
}

async function loadRawBlob(pathname) {
  const { get } = await import("@vercel/blob");
  const blob = await get(pathname, { access: "private", useCache: false });

  if (!blob) {
    return null;
  }

  const text = await streamToText(blob.stream);
  return JSON.parse(text);
}

function publicProfileRecord(record, options = {}) {
  const user = normalizeRecord(record.user);
  const profile = normalizeRecord(record.profile);
  const collection = normalizeCollection(record.collection);
  const setBreakdown = Array.isArray(profile.setBreakdown) ? profile.setBreakdown : [];
  const playerName = stringValue(profile.playerName || user.battleTag || "Игрок Hearthstone");

  return {
    blizzardId: stringValue(record.blizzardId),
    savedAt: stringValue(record.savedAt),
    user: {
      battleTag: playerName,
    },
    export: {
      exportedAt: stringValue(record.export?.exportedAt),
      source: stringValue(record.export?.source),
      version: stringValue(record.export?.version),
    },
    settings: normalizeRecord(record.settings),
    profile: {
      playerName,
      favoriteClass: stringValue(profile.favoriteClass || "MAGE"),
      bestClass: stringValue(profile.bestClass || profile.favoriteClass || "MAGE"),
      ownedCards: number(profile.ownedCards),
      goldenCards: number(profile.goldenCards),
      dust: number(profile.dust),
      availableDust: number(profile.availableDust),
      coverageRatio: number(profile.coverageRatio),
      coverageText: stringValue(profile.coverageText),
      topClasses: Array.isArray(profile.topClasses) ? profile.topClasses : [],
      topSets: Array.isArray(profile.topSets) ? profile.topSets : [],
      setBreakdown,
      setBreakdownSummary: `${setBreakdown.length} дополнений`,
    },
    collection: options.includeCollection
      ? {
        cardCount: collection.cards.length,
        cards: collection.cards,
      }
      : undefined,
  };
}

function compareLeaderboardUsers(a, b) {
  const aProfile = normalizeRecord(a.profile);
  const bProfile = normalizeRecord(b.profile);
  return (
    number(bProfile.coverageRatio) - number(aProfile.coverageRatio) ||
    number(bProfile.ownedCards) - number(aProfile.ownedCards) ||
    number(bProfile.dust) - number(aProfile.dust) ||
    stringValue(b.savedAt).localeCompare(stringValue(a.savedAt))
  );
}

function hasProfileReadAccess(request) {
  const expectedToken = process.env.PROFILE_ADMIN_TOKEN;

  if (!expectedToken) {
    return false;
  }

  const headerToken = headerValue(request, "x-profile-admin-token");
  const authorization = headerValue(request, "authorization");
  const bearerToken = authorization.toLowerCase().startsWith("bearer ")
    ? authorization.slice(7).trim()
    : "";

  return headerToken === expectedToken || bearerToken === expectedToken;
}

function headerValue(request, name) {
  if (!request.headers) {
    return "";
  }

  if (typeof request.headers.get === "function") {
    return stringValue(request.headers.get(name));
  }

  return stringValue(request.headers[String(name).toLowerCase()] || request.headers[name]);
}

function setJsonHeaders(response) {
  response.setHeader("Content-Type", "application/json; charset=utf-8");
  response.setHeader("Cache-Control", "no-store");
}

async function readJsonBody(request) {
  if (request.body && typeof request.body === "object" && !Buffer.isBuffer(request.body)) {
    return request.body;
  }

  if (typeof request.body === "string" || Buffer.isBuffer(request.body)) {
    return parseJsonBody(Buffer.from(request.body).toString("utf8"));
  }

  const chunks = [];
  let totalBytes = 0;

  for await (const chunk of request) {
    const buffer = Buffer.isBuffer(chunk) ? chunk : Buffer.from(chunk);
    totalBytes += buffer.length;

    if (totalBytes > MAX_BODY_BYTES) {
      throw httpError(413, "Профиль слишком большой для сохранения.");
    }

    chunks.push(buffer);
  }

  return parseJsonBody(Buffer.concat(chunks).toString("utf8"));
}

function parseJsonBody(rawBody) {
  if (!rawBody) {
    return {};
  }

  try {
    return JSON.parse(rawBody);
  } catch {
    throw httpError(400, "Некорректный JSON.");
  }
}

async function streamToText(stream) {
  if (!stream) {
    return "";
  }

  if (typeof stream.getReader === "function") {
    const reader = stream.getReader();
    const decoder = new TextDecoder();
    let text = "";

    while (true) {
      const { done, value } = await reader.read();
      if (done) {
        return text;
      }
      text += decoder.decode(value, { stream: true });
    }
  }

  const chunks = [];
  for await (const chunk of stream) {
    chunks.push(Buffer.isBuffer(chunk) ? chunk : Buffer.from(chunk));
  }
  return Buffer.concat(chunks).toString("utf8");
}

function normalizeUser(user) {
  const value = normalizeRecord(user);
  return {
    battleTag: stringValue(value.battleTag),
    accountHi: stringValue(value.accountHi),
    accountLo: stringValue(value.accountLo),
  };
}

function normalizeRecord(value) {
  return value && typeof value === "object" && !Array.isArray(value) ? value : {};
}

function normalizeCollection(collection) {
  const value = normalizeRecord(collection);
  const cards = Array.isArray(value.cards) ? value.cards : [];
  return {
    cards: cards.slice(0, 9000).map(normalizeCard).filter(Boolean),
  };
}

function normalizeCard(card) {
  const value = normalizeRecord(card);
  const cardId = stringValue(value.cardId || value.id);

  if (!cardId && number(value.dbfId) <= 0) {
    return null;
  }

  return {
    cardId,
    dbfId: number(value.dbfId),
    name: stringValue(value.name),
    set: stringValue(value.set),
    rarity: stringValue(value.rarity),
    class: stringValue(value.class),
    normal: number(value.normal),
    golden: number(value.golden),
    diamond: number(value.diamond),
    signature: number(value.signature),
  };
}

function normalizeBlizzardId(value) {
  return stringValue(value)
    .replace(/\s+/g, "")
    .slice(0, 160);
}

function blobPath(blizzardId) {
  return `profiles/${encodeURIComponent(blizzardId)}.json`;
}

function stringValue(value) {
  return value == null ? "" : String(value).trim();
}

function number(value) {
  const parsed = Number(value);
  return Number.isFinite(parsed) ? parsed : 0;
}

function httpError(statusCode, message) {
  const error = new Error(message);
  error.statusCode = statusCode;
  return error;
}
