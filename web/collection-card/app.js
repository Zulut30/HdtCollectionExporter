const classMeta = {
  DEATHKNIGHT: { label: "Рыцарь смерти", slug: "deathknight", accent: "#77d5ff" },
  DEMONHUNTER: { label: "Охотник на демонов", slug: "demonhunter", accent: "#b4f067" },
  DRUID: { label: "Друид", slug: "druid", accent: "#f1a35c" },
  HUNTER: { label: "Охотник", slug: "hunter", accent: "#7fcf68" },
  MAGE: { label: "Маг", slug: "mage", accent: "#5bbdff" },
  PALADIN: { label: "Паладин", slug: "paladin", accent: "#f2cf72" },
  PRIEST: { label: "Жрец", slug: "priest", accent: "#f4efe1" },
  ROGUE: { label: "Разбойник", slug: "rogue", accent: "#f0df71" },
  SHAMAN: { label: "Шаман", slug: "shaman", accent: "#66adff" },
  WARLOCK: { label: "Чернокнижник", slug: "warlock", accent: "#ba8cff" },
  WARRIOR: { label: "Воин", slug: "warrior", accent: "#df735c" },
  NEUTRAL: { label: "Нейтральные", slug: "neutral", accent: "#c9c1a4" },
};

const ignoredSetCodes = new Set(["CORE", "VANILLA", "LEGACY", "LETTUCE"]);
const hiddenProfileSetCodes = new Set([
  ...ignoredSetCodes,
  "EVENT",
  "HERO_SKINS",
  "PLACEHOLDER_202204",
]);
const zeroDustSetCodes = new Set([
  "CORE",
  "VANILLA",
  "LEGACY",
  "LETTUCE",
  "EVENT",
  "HERO_SKINS",
  "PLACEHOLDER_202204",
]);
const hearthstoneJsonLocalUrl = "assets/data/cards.ruRU.collectible.min.json";
const hearthstoneJsonRemoteUrl = "https://api.hearthstonejson.com/v1/latest/ruRU/cards.collectible.json";

let cardLookupPromise = null;

const defaultSettings = {
  countDuplicates: true,
  includeGolden: true,
};

const setTranslations = {
  CORE: "Основной набор",
  VANILLA: "Базовый набор",
  LEGACY: "Старый набор",
  LETTUCE: "Наемники",
  EXPERT1: "Классический набор",
  EVENT: "Событийные карты",
  HERO_SKINS: "Портреты героев",
  PLACEHOLDER_202204: "Основной набор",
  NAXX: "Проклятие Наксрамаса",
  FP1: "Проклятие Наксрамаса",
  GVG: "Гоблины и гномы",
  PE1: "Гоблины и гномы",
  BRM: "Чёрная гора",
  FP2: "Чёрная гора",
  BLACKROCK_MOUNTAIN: "Чёрная гора",
  TGT: "Большой турнир",
  TEMP1: "Большой турнир",
  LOE: "Лига исследователей",
  OG: "Пробуждение древних богов",
  OLD_GODS: "Пробуждение древних богов",
  KARA: "Вечеринка в Каражане",
  GANGS: "Злачный город Прибамбасск",
  GADGETZAN: "Злачный город Прибамбасск",
  UNGORO: "Экспедиция в Ун’Горо",
  ICECROWN: "Рыцари Ледяного Трона",
  LOOTAPALOOZA: "Кобольды и катакомбы",
  GILNEAS: "Ведьмин лес",
  BOOMSDAY: "Проект Бумного Дня",
  TROLL: "Растахановы игрища",
  DALARAN: "Возмездие теней",
  ULDUM: "Спасители Ульдума",
  DRAGONS: "Натиск драконов",
  YEAR_OF_THE_DRAGON: "Пробуждение Галакронда",
  BLACK_TEMPLE: "Руины Запределья",
  DEMON_HUNTER_INITIATE: "Руины Запределья",
  SCHOLOMANCE: "Некроситет",
  DARKMOON_FAIRE: "Ярмарка безумия",
  THE_BARRENS: "Закаленные Степями",
  STORMWIND: "Сплоченные Штормградом",
  ALTERAC_VALLEY: "Разделенные Альтераком",
  THE_SUNKEN_CITY: "Путешествие в Затонувший город",
  REVENDRETH: "Убийство в замке Нафрия",
  RETURN_OF_THE_LICH_KING: "Марш Короля-лича",
  PATH_OF_ARTHAS: "Марш Короля-лича",
  BATTLE_OF_THE_BANDS: "Фестиваль легенд",
  TITANS: "ТИТАНЫ",
  WILD_WEST: "Битва в Бесплодных землях",
  WHIZBANGS_WORKSHOP: "Мастерская Чудастера",
  ISLAND_VACATION: "Раздор в тропиках",
  SPACE: "Великая Запредельная Тьма",
  EMERALD_DREAM: "Объятия Изумрудного Сна",
  THE_LOST_CITY: "Затерянный город Ун'Горо",
  TIME_TRAVEL: "Сквозь потоки времени",
  WONDERS: "Пещеры времени",
  CATACLYSM: "Катаклизм",
};

const elements = {
  dropZone: document.querySelector("#dropZone"),
  fileInput: document.querySelector("#fileInput"),
  chooseFileButton: document.querySelector("#chooseFileButton"),
  exportButton: document.querySelector("#exportButton"),
  resetButton: document.querySelector("#resetButton"),
  countDuplicates: document.querySelector("#countDuplicates"),
  includeGolden: document.querySelector("#includeGolden"),
  statusText: document.querySelector("#statusText"),
  fileName: document.querySelector("#fileName"),
  schemaVersion: document.querySelector("#schemaVersion"),
  exportedAt: document.querySelector("#exportedAt"),
  profileCard: document.querySelector("#profileCard"),
  heroArt: document.querySelector("#heroArt"),
  classIcon: document.querySelector("#classIcon"),
  sourceLabel: document.querySelector("#sourceLabel"),
  playerName: document.querySelector("#playerName"),
  favoriteClass: document.querySelector("#favoriteClass"),
  bestClass: document.querySelector("#bestClass"),
  ownedCards: document.querySelector("#ownedCards"),
  premiumCards: document.querySelector("#premiumCards"),
  dustCount: document.querySelector("#dustCount"),
  coverage: document.querySelector("#coverage"),
  coverageText: document.querySelector("#coverageText"),
  coverageBar: document.querySelector("#coverageBar"),
  topClasses: document.querySelector("#topClasses"),
  topSets: document.querySelector("#topSets"),
  expensiveCards: document.querySelector("#expensiveCards"),
  setsSummary: document.querySelector("#setsSummary"),
  setBreakdown: document.querySelector("#setBreakdown"),
};

let currentProfile = null;
let currentCollectionData = null;
let currentFileName = "";
let currentCardLookup = null;
let currentSettings = { ...defaultSettings };

const emptyProfile = {
  loaded: false,
  fileName: "Файл не выбран",
  schemaVersion: "Ожидание",
  exportedAt: "Ожидание",
  sourceLabel: "Ожидаю экспорт",
  playerName: "Твой профиль Hearthstone",
  favoriteClass: "MAGE",
  bestClass: "DRUID",
  ownedCards: null,
  goldenCards: null,
  dust: null,
  coverageRatio: 0,
  coverageText: "Загрузи JSON-экспорт",
  topClasses: [
    { className: "MAGE", value: 0, caption: "готово" },
    { className: "DRUID", value: 0, caption: "готово" },
    { className: "PALADIN", value: 0, caption: "готово" },
  ],
  topSets: [
    { name: "Дополнения", owned: 0 },
    { name: "Золотые", owned: 0 },
    { name: "Пыль", owned: 0 },
  ],
  expensiveGoldenCards: [
    { name: "Золотые карты", caption: "появятся после загрузки" },
  ],
  setBreakdown: [],
  setBreakdownSummary: "Загрузи JSON-экспорт",
};

renderProfile(emptyProfile);

elements.chooseFileButton.addEventListener("click", () => elements.fileInput.click());
elements.fileInput.addEventListener("change", () => {
  const file = elements.fileInput.files && elements.fileInput.files[0];
  if (file) {
    readFile(file);
  }
});

elements.countDuplicates.addEventListener("change", () => {
  currentSettings.countDuplicates = elements.countDuplicates.checked;
  refreshProfileFromSettings();
});

elements.includeGolden.addEventListener("change", () => {
  currentSettings.includeGolden = elements.includeGolden.checked;
  refreshProfileFromSettings();
});

elements.resetButton.addEventListener("click", () => {
  elements.fileInput.value = "";
  currentProfile = null;
  currentCollectionData = null;
  currentFileName = "";
  currentCardLookup = null;
  renderProfile(emptyProfile);
  setStatus("Готов к полному JSON-экспорту коллекции.");
});

elements.exportButton.addEventListener("click", () => {
  if (!currentProfile) {
    return;
  }
  exportProfilePng().catch((error) => {
    setStatus(`Не удалось скачать PNG: ${error.message}`, true);
  });
});

["dragenter", "dragover"].forEach((eventName) => {
  elements.dropZone.addEventListener(eventName, (event) => {
    event.preventDefault();
    elements.dropZone.classList.add("is-dragging");
  });
});

["dragleave", "drop"].forEach((eventName) => {
  elements.dropZone.addEventListener(eventName, (event) => {
    event.preventDefault();
    elements.dropZone.classList.remove("is-dragging");
  });
});

elements.dropZone.addEventListener("drop", (event) => {
  const file = event.dataTransfer.files && event.dataTransfer.files[0];
  if (file) {
    readFile(file);
  }
});

elements.dropZone.addEventListener("keydown", (event) => {
  if (event.key === "Enter" || event.key === " ") {
    event.preventDefault();
    elements.fileInput.click();
  }
});

function readFile(file) {
  if (!file.name.toLowerCase().endsWith(".json")) {
    setStatus("Выбери JSON-файл экспорта.", true);
    return;
  }

  const reader = new FileReader();
  reader.onload = async () => {
    try {
      const data = JSON.parse(String(reader.result || ""));
      setStatus("Загружаю русские названия карт из HearthstoneJSON...");
      const cardLookup = await loadCardLookup();
      currentCollectionData = data;
      currentFileName = file.name;
      currentCardLookup = cardLookup;
      const profile = buildProfile(data, file.name, cardLookup, currentSettings);
      currentProfile = profile;
      renderProfile(profile);
      const lookupStatus = cardLookup.loaded
        ? " Русские названия HearthstoneJSON применены."
        : " Справочник HearthstoneJSON недоступен, использую названия из файла.";
      setStatus(`Загружено: ${profile.cardRowsLabel} из ${file.name}.${lookupStatus}`);
    } catch (error) {
      setStatus(error.message, true);
    }
  };
  reader.onerror = () => setStatus("Не удалось прочитать выбранный файл.", true);
  reader.readAsText(file);
}

function refreshProfileFromSettings() {
  currentSettings = {
    countDuplicates: elements.countDuplicates.checked,
    includeGolden: elements.includeGolden.checked,
  };

  if (!currentCollectionData || !currentFileName) {
    renderProfile(emptyProfile);
    return;
  }

  try {
    const profile = buildProfile(currentCollectionData, currentFileName, currentCardLookup, currentSettings);
    currentProfile = profile;
    renderProfile(profile);
    setStatus(`Пересчитано: ${profile.cardRowsLabel}.`);
  } catch (error) {
    setStatus(error.message, true);
  }
}

function buildProfile(data, fileName, cardLookup, settings = defaultSettings) {
  if (!data || typeof data !== "object") {
    throw new Error("Этот файл не похож на экспорт коллекции.");
  }

  if (String(data.exportType || "").toLowerCase() === "changes") {
    throw new Error("Файл изменений не подходит. Нужен полный JSON-экспорт коллекции.");
  }

  const cards = Array.isArray(data.cards) ? data.cards : [];
  if (!cards.length) {
    throw new Error("В экспорте не найдены карты.");
  }

  const visibleRows = cards.filter((card) => !isIgnoredSet(card.set));
  const ownedRows = visibleRows.filter((card) => selectedOwnedCount(card, settings) > 0);
  const ownedCards = sum(cards, (card) => selectedOwnedCount(card, settings));
  const goldenCards = sum(cards, (card) => selectedGoldenCount(card, settings));
  const collectionDust = sum(cards, (card) => cardDustValue(card, cardLookup, settings));
  const setBreakdown = buildSetBreakdown(cards, cardLookup, settings);

  const classRows = buildClassRows(data, cards, settings);
  const playableClasses = classRows.filter((row) => row.className !== "NEUTRAL");
  const favoriteClass = normalizeClass(data.favoriteClass && data.favoriteClass.class) ||
    (playableClasses[0] && playableClasses[0].className) ||
    "MAGE";
  const bestClass = normalizeClass(data.bestClassByWins && data.bestClassByWins.class) ||
    findBestClassByWins(data.classStats) ||
    favoriteClass;

  const coverageRatio = visibleRows.length ? ownedRows.length / visibleRows.length : 0;
  const completionLabel = `${Math.round(clamp(coverageRatio, 0, 1) * 100)}% всей коллекции`;
  const exportedDate = formatDate(data.exportedAt);
  const cardRowsLabel = `${formatNumber(cards.length)} строк карт`;

  return {
    loaded: true,
    fileName,
    schemaVersion: data.version ? `v${data.version}` : "Неизвестно",
    exportedAt: exportedDate,
    sourceLabel: sourceName(data.source),
    playerName: profileName(data.user),
    favoriteClass,
    bestClass,
    ownedCards,
    goldenCards,
    dust: collectionDust,
    availableDust: number(data.dust),
    coverageRatio,
    coverageText: completionLabel,
    topClasses: classRows.slice(0, 3),
    topSets: setBreakdown.slice(0, 3),
    expensiveGoldenCards: buildExpensiveGoldenCards(cards, cardLookup, settings),
    setBreakdown,
    setBreakdownSummary: setBreakdown.length
      ? `${formatNumber(setBreakdown.length)} дополнений · ${formatNumber(sum(setBreakdown, (row) => row.owned))} карт`
      : "Нет карт в выбранных дополнениях",
    cardRowsLabel,
  };
}

function buildClassRows(data, cards, settings) {
  if (Array.isArray(data.classStats) && data.classStats.length) {
    return data.classStats
      .map((row) => {
        const className = normalizeClass(row.class);
        if (!className) {
          return null;
        }
        const games = number(row.games);
        const wins = number(row.wins);
        return {
          className,
          value: games,
          caption: games > 0 ? `${formatNumber(games)} игр, ${formatNumber(wins)} побед` : "нет игр",
        };
      })
      .filter(Boolean)
      .sort((a, b) => b.value - a.value || a.className.localeCompare(b.className));
  }

  const byClass = new Map();
  cards.forEach((card) => {
    const className = normalizeClass(card.class);
    const owned = selectedOwnedCount(card, settings);
    if (!className || owned <= 0) {
      return;
    }

    const current = byClass.get(className) || { className, value: 0, rows: 0 };
    current.value += owned;
    current.rows += 1;
    byClass.set(className, current);
  });

  return Array.from(byClass.values())
    .map((row) => ({
      className: row.className,
      value: row.value,
      caption: `${formatNumber(row.value)} карт`,
    }))
    .sort((a, b) => {
      if (a.className === "NEUTRAL") {
        return 1;
      }
      if (b.className === "NEUTRAL") {
        return -1;
      }
      return b.value - a.value || a.className.localeCompare(b.className);
    });
}

function buildTopSets(cards) {
  const bySet = new Map();

  cards.forEach((card) => {
    const setName = String(card.set || "Неизвестно").trim() || "Неизвестно";
    const normalized = setName.toUpperCase();
    const owned = number(card.ownedTotal);
    if (owned <= 0 || isHiddenProfileSet(normalized)) {
      return;
    }

    const current = bySet.get(normalized) || { name: translateSetName(setName), owned: 0 };
    current.owned += owned;
    bySet.set(normalized, current);
  });

  const rows = Array.from(bySet.values())
    .sort((a, b) => b.owned - a.owned || a.name.localeCompare(b.name, "ru"))
    .slice(0, 3);

  if (rows.length) {
    return rows;
  }

  return [
    { name: "Дополнения", owned: 0 },
    { name: "Золотые", owned: 0 },
    { name: "Пыль", owned: 0 },
  ];
}

function buildSetBreakdown(cards, cardLookup, settings) {
  const bySet = new Map();

  cards.forEach((card) => {
    const meta = findCardMeta(card, cardLookup);
    const setName = String(card.set || (meta && meta.set) || "Неизвестно").trim() || "Неизвестно";
    const normalized = setName.toUpperCase();
    const owned = selectedOwnedCount(card, settings);
    if (owned <= 0 || isHiddenProfileSet(normalized)) {
      return;
    }

    const current = bySet.get(normalized) || {
      code: normalized,
      name: translateSetName(setName),
      owned: 0,
      unique: 0,
      golden: 0,
      dust: 0,
    };
    current.owned += owned;
    current.unique += 1;
    current.golden += selectedGoldenCount(card, settings);
    current.dust += cardDustValue(card, cardLookup, settings);
    bySet.set(normalized, current);
  });

  return Array.from(bySet.values())
    .sort((a, b) => b.owned - a.owned || a.name.localeCompare(b.name, "ru"));
}

function selectedOwnedCount(card, settings) {
  return selectedNormalCount(card, settings) + selectedPremiumCount(card, settings);
}

function selectedNormalCount(card, settings) {
  return selectedCopyCount(number(card.normal), settings);
}

function selectedPremiumCount(card, settings) {
  if (!settings.includeGolden) {
    return 0;
  }
  return selectedCopyCount(
    number(card.golden) + number(card.diamond) + number(card.signature),
    settings,
  );
}

function selectedGoldenCount(card, settings) {
  if (!settings.includeGolden) {
    return 0;
  }
  return selectedCopyCount(number(card.golden), settings);
}

function selectedCopyCount(value, settings) {
  const copies = number(value);
  if (copies <= 0) {
    return 0;
  }
  return settings.countDuplicates ? copies : 1;
}

function cardDustValue(card, cardLookup, settings) {
  const meta = findCardMeta(card, cardLookup);
  const rarity = normalizeRarity(card.rarity || (meta && meta.rarity));
  const setCode = String(card.set || (meta && meta.set) || "").trim();
  const normalValue = selectedNormalCount(card, settings) * normalDisenchantValue(rarity, meta, setCode);
  const goldenValue = selectedGoldenCount(card, settings) * goldenDisenchantValue(rarity, meta, setCode);
  return normalValue + goldenValue;
}

function loadCardLookup() {
  if (!cardLookupPromise) {
    cardLookupPromise = fetchCardLookup();
  }
  return cardLookupPromise;
}

async function fetchCardLookup() {
  const localLookup = await fetchCardLookupSource(hearthstoneJsonLocalUrl).catch(() => null);
  if (localLookup) {
    return { ...localLookup, loaded: true, source: "local" };
  }

  const remoteLookup = await fetchCardLookupSource(hearthstoneJsonRemoteUrl).catch(() => null);
  if (remoteLookup) {
    return { ...remoteLookup, loaded: true, source: "remote" };
  }

  return { loaded: false, source: "none", byId: new Map(), byDbf: new Map(), count: 0 };
}

async function fetchCardLookupSource(url) {
  const response = await fetch(url, { cache: "force-cache" });
  if (!response.ok) {
    throw new Error(`HearthstoneJSON HTTP ${response.status}`);
  }

  const cards = await response.json();
  if (!Array.isArray(cards)) {
    throw new Error("HearthstoneJSON returned an unexpected payload.");
  }

  const byId = new Map();
  const byDbf = new Map();
  cards.forEach((card) => {
    const id = String(card.id || "").trim();
    const dbfId = number(card.dbfId);
    const meta = {
      id,
      dbfId,
      name: String(card.name || "").trim(),
      rarity: normalizeRarity(card.rarity),
      set: String(card.set || "").trim(),
      dust: Array.isArray(card.dust) ? card.dust.map(number) : null,
    };

    if (id) {
      byId.set(id, meta);
    }
    if (dbfId > 0) {
      byDbf.set(dbfId, meta);
    }
  });

  return { byId, byDbf, count: cards.length };
}

function buildExpensiveGoldenCards(cards, cardLookup, settings) {
  return cards
    .map((card) => {
      const meta = findCardMeta(card, cardLookup);
      const golden = selectedGoldenCount(card, settings);
      const setCode = String(card.set || (meta && meta.set) || "").trim();
      const rarity = normalizeRarity(card.rarity || (meta && meta.rarity));
      const cardId = String(card.cardId || card.id || (meta && meta.id) || "").trim();
      const craftCost = goldenCraftCost(rarity, meta, setCode);

      if (golden <= 0 || isHiddenProfileSet(setCode) || craftCost <= 0) {
        return null;
      }

      return {
        cardId,
        name: cardDisplayName(card, meta),
        rarity,
        golden,
        craftCost,
        totalValue: craftCost * golden,
      };
    })
    .filter(Boolean)
    .sort((a, b) => (
      b.totalValue - a.totalValue ||
      b.craftCost - a.craftCost ||
      a.name.localeCompare(b.name, "ru")
    ))
    .slice(0, 3);
}

function findCardMeta(card, cardLookup) {
  if (!cardLookup || !cardLookup.loaded) {
    return null;
  }

  const cardId = String(card.cardId || card.id || "").trim();
  if (cardId && cardLookup.byId.has(cardId)) {
    return cardLookup.byId.get(cardId);
  }

  const dbfId = number(card.dbfId);
  if (dbfId > 0 && cardLookup.byDbf.has(dbfId)) {
    return cardLookup.byDbf.get(dbfId);
  }

  return null;
}

function cardDisplayName(card, meta) {
  return String((meta && meta.name) || card.name || card.cardId || card.dbfId || "Неизвестная карта").trim();
}

function cardLocalImagePath(cardId) {
  const safeId = String(cardId || "").trim();
  return safeId ? `assets/card_renders/${safeId}.png` : "";
}

function cardRemoteImageUrl(cardId) {
  const safeId = encodeURIComponent(String(cardId || "").trim());
  return safeId ? `https://art.hearthstonejson.com/v1/render/latest/ruRU/256x/${safeId}.png` : "";
}

function normalizeRarity(value) {
  return String(value || "").trim().toUpperCase();
}

function normalDisenchantValue(rarity, meta, setCode = "") {
  if (meta && Array.isArray(meta.dust) && number(meta.dust[2]) > 0) {
    return number(meta.dust[2]);
  }
  if (isZeroDustCard(rarity, meta, setCode)) {
    return 0;
  }

  const values = {
    LEGENDARY: 400,
    EPIC: 100,
    RARE: 20,
    COMMON: 5,
    FREE: 0,
  };
  return values[rarity] || 0;
}

function goldenDisenchantValue(rarity, meta, setCode = "") {
  if (meta && Array.isArray(meta.dust) && number(meta.dust[3]) > 0) {
    return number(meta.dust[3]);
  }
  if (isZeroDustCard(rarity, meta, setCode)) {
    return 0;
  }

  const values = {
    LEGENDARY: 1600,
    EPIC: 400,
    RARE: 100,
    COMMON: 50,
    FREE: 0,
  };
  return values[rarity] || 0;
}

function goldenCraftCost(rarity, meta, setCode = "") {
  if (meta && Array.isArray(meta.dust) && number(meta.dust[1]) > 0) {
    return number(meta.dust[1]);
  }
  if (isZeroDustCard(rarity, meta, setCode)) {
    return 0;
  }

  const costs = {
    LEGENDARY: 3200,
    EPIC: 1600,
    RARE: 800,
    COMMON: 400,
    FREE: 0,
  };
  return costs[rarity] || 0;
}

function isZeroDustCard(rarity, meta, setCode = "") {
  const set = String(setCode || (meta && meta.set) || "").trim().toUpperCase();
  return rarity === "FREE" || zeroDustSetCodes.has(set);
}

function rarityLabel(rarity) {
  const labels = {
    LEGENDARY: "легендарная",
    EPIC: "эпическая",
    RARE: "редкая",
    COMMON: "обычная",
    FREE: "бесплатная",
  };
  return labels[rarity] || "карта";
}

function goldenCaption(row) {
  if (row.caption) {
    return row.caption;
  }

  return `${rarityLabel(row.rarity)} · ${formatNumber(row.golden)} ${pluralRu(row.golden, "золотая", "золотые", "золотых")} · ${formatNumber(row.totalValue)} пыли`;
}

function pluralRu(value, one, few, many) {
  const count = Math.abs(number(value));
  const mod10 = count % 10;
  const mod100 = count % 100;
  if (mod10 === 1 && mod100 !== 11) {
    return one;
  }
  if (mod10 >= 2 && mod10 <= 4 && (mod100 < 12 || mod100 > 14)) {
    return few;
  }
  return many;
}

function renderProfile(profile) {
  const favoriteMeta = getClassMeta(profile.favoriteClass);
  const bestMeta = getClassMeta(profile.bestClass);

  elements.profileCard.style.setProperty("--class-accent", favoriteMeta.accent);
  elements.profileCard.classList.toggle("is-empty", !profile.loaded);
  elements.heroArt.src = `assets/class_art/${favoriteMeta.slug}.png`;
  elements.classIcon.src = classIconPath(favoriteMeta);
  elements.classIcon.alt = "";
  elements.fileName.textContent = profile.fileName;
  elements.schemaVersion.textContent = profile.schemaVersion;
  elements.exportedAt.textContent = profile.exportedAt;
  elements.sourceLabel.textContent = profile.sourceLabel;
  elements.playerName.textContent = profile.playerName;
  elements.favoriteClass.textContent = favoriteMeta.label;
  elements.bestClass.textContent = bestMeta.label;
  elements.ownedCards.textContent = profile.ownedCards == null ? "-" : formatNumber(profile.ownedCards);
  elements.premiumCards.textContent = profile.goldenCards == null ? "-" : formatNumber(profile.goldenCards);
  elements.dustCount.textContent = profile.dust == null ? "-" : formatNumber(profile.dust);
  elements.coverage.textContent = profile.loaded ? `${Math.round(profile.coverageRatio * 100)}%` : "-";
  elements.coverageText.textContent = profile.coverageText;
  elements.coverageBar.style.width = `${Math.round(clamp(profile.coverageRatio, 0, 1) * 100)}%`;
  elements.exportButton.disabled = !profile.loaded;

  renderTopClasses(profile.topClasses);
  renderTopSets(profile.topSets);
  renderExpensiveCards(profile.expensiveGoldenCards);
  renderSetBreakdown(profile);
}

function renderTopClasses(rows) {
  elements.topClasses.replaceChildren();
  const fragment = document.createDocumentFragment();
  rows.forEach((row) => {
    const meta = getClassMeta(row.className);
    const item = document.createElement("div");
    item.className = "class-item";
    item.innerHTML = `
      <img src="${classIconPath(meta)}" alt="" loading="lazy" decoding="async" />
      <div>
        <strong>${escapeHtml(meta.label)}</strong>
        <span>${escapeHtml(row.caption)}</span>
      </div>
    `;
    fragment.appendChild(item);
  });
  elements.topClasses.appendChild(fragment);
}

function renderTopSets(rows) {
  elements.topSets.replaceChildren();
  const fragment = document.createDocumentFragment();
  rows.forEach((row) => {
    const item = document.createElement("div");
    item.className = "set-item";
    item.innerHTML = `
      <strong>${escapeHtml(row.name)}</strong>
      <span>${formatNumber(row.owned)} карт</span>
    `;
    fragment.appendChild(item);
  });
  elements.topSets.appendChild(fragment);
}

function renderExpensiveCards(rows) {
  elements.expensiveCards.replaceChildren();
  const visibleRows = rows && rows.length
    ? rows
    : [{ name: "Золотых карт не найдено", caption: "в выбранных дополнениях" }];

  const fragment = document.createDocumentFragment();
  visibleRows.forEach((row) => {
    const item = document.createElement("div");
    item.className = "gold-card-item";
    const localImage = cardLocalImagePath(row.cardId);
    const remoteImage = cardRemoteImageUrl(row.cardId);
    item.innerHTML = `
      <div class="gold-card-art">
        ${localImage ? `<img src="${escapeHtml(localImage)}" alt="" loading="lazy" decoding="async" />` : `<img src="assets/ui/card.webp" alt="" loading="lazy" decoding="async" />`}
      </div>
      <div class="gold-card-copy">
        <strong>${escapeHtml(row.name)}</strong>
        <span>${escapeHtml(goldenCaption(row))}</span>
      </div>
    `;
    const image = item.querySelector("img");
    if (image && remoteImage) {
      image.addEventListener("error", () => {
        if (image.dataset.remoteLoaded === "true") {
          image.src = "assets/ui/card.webp";
          return;
        }
        image.dataset.remoteLoaded = "true";
        image.src = remoteImage;
      });
    }
    fragment.appendChild(item);
  });
  elements.expensiveCards.appendChild(fragment);
}

function renderSetBreakdown(profile) {
  elements.setsSummary.textContent = profile.setBreakdownSummary;
  elements.setBreakdown.replaceChildren();

  if (!profile.loaded) {
    const empty = document.createElement("div");
    empty.className = "expansion-empty";
    empty.textContent = "После загрузки здесь появится список дополнений.";
    elements.setBreakdown.appendChild(empty);
    return;
  }

  const fragment = document.createDocumentFragment();
  profile.setBreakdown.forEach((row) => {
    const item = document.createElement("div");
    item.className = "expansion-item";
    item.innerHTML = `
      <div>
        <strong>${escapeHtml(row.name)}</strong>
        <span>${formatNumber(row.unique)} ${pluralRu(row.unique, "уникальная", "уникальные", "уникальных")}</span>
      </div>
      <div class="expansion-stats">
        <span>${formatNumber(row.owned)} карт</span>
        <span>${formatNumber(row.golden)} золотых</span>
        <span>${formatNumber(row.dust)} пыли</span>
      </div>
    `;
    fragment.appendChild(item);
  });
  elements.setBreakdown.appendChild(fragment);
}

async function exportProfilePng() {
  setStatus("Готовлю PNG...");
  const pngUrl = await renderProfileCanvas(currentProfile);
  const link = document.createElement("a");
  link.href = pngUrl;
  link.download = `${slugify(currentProfile.playerName)}-collection-card.png`;
  link.click();
  setStatus("PNG скачан.");
}

async function renderProfileCanvas(profile) {
  const width = 1600;
  const height = 1320;
  const canvas = document.createElement("canvas");
  canvas.width = width;
  canvas.height = height;
  const ctx = canvas.getContext("2d");
  const favoriteMeta = getClassMeta(profile.favoriteClass);
  const bestMeta = getClassMeta(profile.bestClass);
  const art = await loadImage(`assets/class_art/${favoriteMeta.slug}.png`);
  const icon = await loadImage(classIconPath(favoriteMeta));
  const logo = await loadImage("assets/ui/manacost_logo.jpg");
  const gold = await loadImage("assets/ui/gold.webp");
  const expensiveImages = await Promise.all(
    profile.expensiveGoldenCards
      .slice(0, 3)
      .map((row) => loadOptionalImage(cardLocalImagePath(row.cardId))),
  );

  drawBackground(ctx, width, height, favoriteMeta);
  drawImageCover(ctx, art, 0, 0, width, 610);
  drawHeroOverlay(ctx, width, height, favoriteMeta);

  roundRect(ctx, 28, 28, width - 56, height - 56, 18);
  ctx.strokeStyle = "rgba(225, 191, 105, 0.56)";
  ctx.lineWidth = 3;
  ctx.stroke();

  roundRect(ctx, 52, 52, width - 104, height - 104, 12);
  ctx.strokeStyle = "rgba(225, 191, 105, 0.24)";
  ctx.lineWidth = 2;
  ctx.stroke();

  drawImageCover(ctx, logo, 92, 78, 86, 86);
  drawText(ctx, "Карточка коллекции Manacost", 198, 110, {
    size: 34,
    weight: 850,
    color: "#f4f2e8",
  });
  drawText(ctx, profile.sourceLabel, 198, 150, {
    size: 23,
    weight: 750,
    color: "#aeb8ac",
  });

  drawCardPanel(ctx, 92, 520, width - 184, 720);
  drawMedallion(ctx, icon, 134, 560, favoriteMeta);
  drawLabel(ctx, "ПРОФИЛЬ ИГРОКА", 250, 586);
  drawText(ctx, profile.playerName, 250, 654, {
    size: fitText(ctx, profile.playerName, 720, 82, 44, 900),
    weight: 900,
    color: "#f4f2e8",
  });

  drawInfoPill(ctx, 1050, 570, 340, 86, "Любимый класс", favoriteMeta.label);
  drawInfoPill(ctx, 1050, 674, 340, 86, "Лучший по победам", bestMeta.label);

  const metrics = [
    ["Карт в коллекции", formatNumber(profile.ownedCards)],
    ["Золотые карты", formatNumber(profile.goldenCards)],
    ["Пыль при распылении", formatNumber(profile.dust)],
    ["Покрытие", `${Math.round(profile.coverageRatio * 100)}%`],
  ];
  metrics.forEach((metric, index) => {
    const x = 134 + index * 330;
    drawMetric(ctx, x, 760, 296, 126, metric[0], metric[1], index === 2 ? gold : null);
  });

  drawProgress(ctx, 134, 920, 1256, 34, profile.coverageRatio, favoriteMeta);
  drawText(ctx, profile.coverageText, 134, 984, {
    size: 28,
    weight: 800,
    color: "#f5e6b0",
  });

  drawText(ctx, "Топ классов", 134, 1038, {
    size: 22,
    weight: 850,
    color: "#aeb8ac",
  });
  profile.topClasses.slice(0, 3).forEach((row, index) => {
    const meta = getClassMeta(row.className);
    drawText(ctx, `${meta.label} - ${row.caption}`, 310 + index * 350, 1038, {
      size: 22,
      weight: 800,
      color: "#f4f2e8",
    });
  });

  drawText(ctx, "Топ дополнений", 134, 1102, {
    size: 22,
    weight: 850,
    color: "#aeb8ac",
  });
  profile.topSets.slice(0, 3).forEach((row, index) => {
    const text = `${row.name} - ${formatNumber(row.owned)} карт`;
    drawText(ctx, text, 370 + index * 390, 1102, {
      size: fitText(ctx, text, 365, 22, 15, 800),
      weight: 800,
      color: "#f4f2e8",
    });
  });

  drawText(ctx, "Дорогие золотые", 134, 1168, {
    size: 22,
    weight: 850,
    color: "#aeb8ac",
  });
  profile.expensiveGoldenCards.slice(0, 3).forEach((row, index) => {
    drawGoldenCardTile(ctx, row, expensiveImages[index], 390 + index * 365, 1118, 330, 104);
  });

  return await canvasToDataUrl(canvas);
}

function drawBackground(ctx, width, height, meta) {
  const base = ctx.createLinearGradient(0, 0, width, height);
  base.addColorStop(0, "#0b100f");
  base.addColorStop(0.45, "#17231d");
  base.addColorStop(1, "#0c1110");
  ctx.fillStyle = base;
  ctx.fillRect(0, 0, width, height);

  const glow = ctx.createRadialGradient(width * 0.72, height * 0.16, 0, width * 0.72, height * 0.16, 680);
  glow.addColorStop(0, hexToRgba(meta.accent, 0.42));
  glow.addColorStop(1, "rgba(0,0,0,0)");
  ctx.fillStyle = glow;
  ctx.fillRect(0, 0, width, height);
}

function drawHeroOverlay(ctx, width, height, meta) {
  const vertical = ctx.createLinearGradient(0, 0, 0, height);
  vertical.addColorStop(0, "rgba(8,12,11,0.05)");
  vertical.addColorStop(0.45, "rgba(12,17,16,0.18)");
  vertical.addColorStop(0.64, "rgba(17,24,21,0.96)");
  vertical.addColorStop(1, "#101614");
  ctx.fillStyle = vertical;
  ctx.fillRect(0, 0, width, height);

  const side = ctx.createLinearGradient(0, 0, width, 0);
  side.addColorStop(0, "rgba(8,12,11,0.86)");
  side.addColorStop(0.52, "rgba(8,12,11,0.08)");
  side.addColorStop(1, hexToRgba(meta.accent, 0.22));
  ctx.fillStyle = side;
  ctx.fillRect(0, 0, width, height);
}

function drawCardPanel(ctx, x, y, width, height) {
  roundRect(ctx, x, y, width, height, 18);
  ctx.fillStyle = "rgba(8, 12, 11, 0.72)";
  ctx.fill();
  ctx.strokeStyle = "rgba(244, 242, 232, 0.14)";
  ctx.lineWidth = 2;
  ctx.stroke();
}

function drawMedallion(ctx, image, x, y, meta) {
  roundRect(ctx, x, y, 92, 92, 16);
  ctx.fillStyle = hexToRgba(meta.accent, 0.24);
  ctx.fill();
  ctx.strokeStyle = "rgba(225, 191, 105, 0.64)";
  ctx.lineWidth = 2;
  ctx.stroke();
  ctx.drawImage(image, x + 16, y + 16, 60, 60);
}

function drawInfoPill(ctx, x, y, width, height, label, value) {
  roundRect(ctx, x, y, width, height, 14);
  ctx.fillStyle = "rgba(255,255,255,0.07)";
  ctx.fill();
  ctx.strokeStyle = "rgba(244,242,232,0.12)";
  ctx.lineWidth = 2;
  ctx.stroke();
  drawLabel(ctx, label, x + 24, y + 30);
  drawText(ctx, value, x + 24, y + 65, { size: 31, weight: 900, color: "#f5e6b0" });
}

function drawMetric(ctx, x, y, width, height, label, value, icon) {
  roundRect(ctx, x, y, width, height, 14);
  ctx.fillStyle = "rgba(255,255,255,0.065)";
  ctx.fill();
  ctx.strokeStyle = "rgba(244,242,232,0.12)";
  ctx.lineWidth = 2;
  ctx.stroke();

  drawText(ctx, label, x + 24, y + 38, { size: 22, weight: 750, color: "#aeb8ac" });
  if (icon) {
    ctx.drawImage(icon, x + 24, y + 62, 42, 42);
    drawText(ctx, value, x + 78, y + 102, {
      size: fitText(ctx, value, width - 104, 46, 30, 900),
      weight: 900,
      color: "#f4f2e8",
    });
  } else {
    drawText(ctx, value, x + 24, y + 102, {
      size: fitText(ctx, value, width - 48, 48, 30, 900),
      weight: 900,
      color: "#f4f2e8",
    });
  }
}

function drawGoldenCardTile(ctx, row, image, x, y, width, height) {
  roundRect(ctx, x, y, width, height, 14);
  const fill = ctx.createLinearGradient(x, y, x + width, y + height);
  fill.addColorStop(0, "rgba(225,191,105,0.2)");
  fill.addColorStop(0.48, "rgba(255,255,255,0.07)");
  fill.addColorStop(1, "rgba(8,12,11,0.72)");
  ctx.fillStyle = fill;
  ctx.fill();
  ctx.strokeStyle = "rgba(225,191,105,0.34)";
  ctx.lineWidth = 2;
  ctx.stroke();

  roundRect(ctx, x + 12, y + 10, 56, 84, 8);
  ctx.fillStyle = "rgba(0,0,0,0.28)";
  ctx.fill();
  ctx.strokeStyle = "rgba(255,225,144,0.62)";
  ctx.lineWidth = 2;
  ctx.stroke();

  if (image) {
    ctx.save();
    roundRect(ctx, x + 12, y + 10, 56, 84, 8);
    ctx.clip();
    drawImageCover(ctx, image, x + 12, y + 10, 56, 84);
    ctx.restore();
  } else {
    drawText(ctx, "★", x + 28, y + 64, { size: 34, weight: 900, color: "#e1bf69" });
  }

  const name = String(row.name || "Золотая карта");
  drawText(ctx, name, x + 82, y + 36, {
    size: fitText(ctx, name, width - 98, 18, 12, 900),
    weight: 900,
    color: "#f4f2e8",
  });

  const caption = `${formatNumber(row.golden || 0)} ${pluralRu(row.golden || 0, "золотая", "золотые", "золотых")} · ${formatNumber(row.totalValue || 0)} пыли`;
  drawText(ctx, caption, x + 82, y + 68, {
    size: fitText(ctx, caption, width - 98, 16, 11, 750),
    weight: 750,
    color: "#f5e6b0",
  });
}

function drawProgress(ctx, x, y, width, height, ratio, meta) {
  roundRect(ctx, x, y, width, height, 999);
  ctx.fillStyle = "rgba(255,255,255,0.09)";
  ctx.fill();
  ctx.strokeStyle = "rgba(225,191,105,0.24)";
  ctx.lineWidth = 2;
  ctx.stroke();

  const filled = Math.max(height, width * clamp(ratio, 0, 1));
  roundRect(ctx, x, y, filled, height, 999);
  const fill = ctx.createLinearGradient(x, y, x + width, y);
  fill.addColorStop(0, meta.accent);
  fill.addColorStop(1, "#e1bf69");
  ctx.fillStyle = fill;
  ctx.fill();
}

function drawImageCover(ctx, image, x, y, width, height) {
  const scale = Math.max(width / image.width, height / image.height);
  const drawWidth = image.width * scale;
  const drawHeight = image.height * scale;
  const drawX = x + (width - drawWidth) / 2;
  const drawY = y + (height - drawHeight) / 2;
  ctx.drawImage(image, drawX, drawY, drawWidth, drawHeight);
}

function drawText(ctx, text, x, y, options) {
  ctx.fillStyle = options.color;
  ctx.font = `${options.weight || 700} ${options.size}px Inter, Segoe UI, Arial, sans-serif`;
  ctx.textBaseline = "alphabetic";
  ctx.fillText(String(text), x, y);
}

function drawLabel(ctx, text, x, y) {
  ctx.fillStyle = "#788579";
  ctx.font = "850 18px Inter, Segoe UI, Arial, sans-serif";
  ctx.textBaseline = "alphabetic";
  ctx.fillText(String(text).toUpperCase(), x, y);
}

function fitText(ctx, text, maxWidth, startSize, minSize, weight) {
  for (let size = startSize; size >= minSize; size -= 2) {
    ctx.font = `${weight || 900} ${size}px Inter, Segoe UI, Arial, sans-serif`;
    if (ctx.measureText(String(text)).width <= maxWidth) {
      return size;
    }
  }
  return minSize;
}

function roundRect(ctx, x, y, width, height, radius) {
  const r = Math.min(radius, width / 2, height / 2);
  ctx.beginPath();
  ctx.moveTo(x + r, y);
  ctx.lineTo(x + width - r, y);
  ctx.quadraticCurveTo(x + width, y, x + width, y + r);
  ctx.lineTo(x + width, y + height - r);
  ctx.quadraticCurveTo(x + width, y + height, x + width - r, y + height);
  ctx.lineTo(x + r, y + height);
  ctx.quadraticCurveTo(x, y + height, x, y + height - r);
  ctx.lineTo(x, y + r);
  ctx.quadraticCurveTo(x, y, x + r, y);
  ctx.closePath();
}

function hexToRgba(hex, alpha) {
  const normalized = hex.replace("#", "");
  const value = parseInt(normalized, 16);
  const red = (value >> 16) & 255;
  const green = (value >> 8) & 255;
  const blue = value & 255;
  return `rgba(${red}, ${green}, ${blue}, ${alpha})`;
}

function loadImage(url) {
  return new Promise((resolve, reject) => {
    const image = new Image();
    image.onload = () => resolve(image);
    image.onerror = () => reject(new Error(`Не удалось загрузить ${url}`));
    image.src = url;
  });
}

function loadOptionalImage(url) {
  if (!url) {
    return Promise.resolve(null);
  }
  return loadImage(url).catch(() => null);
}

function canvasToDataUrl(canvas) {
  return new Promise((resolve, reject) => {
    const reader = new FileReader();
    reader.onload = () => resolve(reader.result);
    reader.onerror = () => reject(new Error("Не удалось подготовить PNG."));
    canvas.toBlob((blob) => {
      if (!blob) {
        reject(new Error("Не удалось отрисовать карточку."));
        return;
      }
      reader.readAsDataURL(blob);
    }, "image/png");
  });
}

function setStatus(message, isError = false) {
  elements.statusText.textContent = message;
  elements.statusText.classList.toggle("is-error", isError);
}

function sourceName(source) {
  const value = String(source || "").trim();
  if (!value) {
    return "Экспорт коллекции";
  }
  if (value.toLowerCase().includes("hstracker")) {
    return "Экспорт HSTracker";
  }
  if (value.toLowerCase().includes("hearthstone deck tracker")) {
    return "Экспорт HDT";
  }
  return "Экспорт коллекции";
}

function profileName(user) {
  if (user && typeof user === "object" && String(user.battleTag || "").trim()) {
    return String(user.battleTag).trim();
  }
  return "Неизвестный игрок";
}

function findBestClassByWins(classStats) {
  if (!Array.isArray(classStats)) {
    return "";
  }

  const best = classStats
    .map((row) => ({ className: normalizeClass(row.class), wins: number(row.wins), games: number(row.games) }))
    .filter((row) => row.className)
    .sort((a, b) => b.wins - a.wins || b.games - a.games || a.className.localeCompare(b.className))[0];

  return best ? best.className : "";
}

function getClassMeta(className) {
  return classMeta[normalizeClass(className)] || classMeta.MAGE;
}

function normalizeClass(value) {
  return String(value || "")
    .replace(/[\s_-]+/g, "")
    .toUpperCase();
}

function classIconPath(meta) {
  const extension = meta.slug === "neutral" ? "webp" : "png";
  return `assets/class_icon/${meta.slug}.${extension}`;
}

function isIgnoredSet(value) {
  return ignoredSetCodes.has(String(value || "").trim().toUpperCase());
}

function isHiddenProfileSet(value) {
  return hiddenProfileSetCodes.has(String(value || "").trim().toUpperCase());
}

function translateSetName(value) {
  const normalized = String(value || "").trim().toUpperCase();
  return setTranslations[normalized] || prettySetName(value);
}

function prettySetName(value) {
  return String(value || "Неизвестно")
    .toLowerCase()
    .split(/[_\s-]+/)
    .filter(Boolean)
    .map((part) => part.charAt(0).toUpperCase() + part.slice(1))
    .join(" ");
}

function formatDate(value) {
  if (!value) {
    return "Неизвестно";
  }

  const date = new Date(value);
  if (Number.isNaN(date.getTime())) {
    return "Неизвестно";
  }

  return new Intl.DateTimeFormat("ru-RU", {
    year: "numeric",
    month: "short",
    day: "2-digit",
    hour: "2-digit",
    minute: "2-digit",
  }).format(date);
}

function formatNumber(value) {
  return new Intl.NumberFormat("ru-RU").format(number(value));
}

function number(value) {
  const parsed = Number(value);
  return Number.isFinite(parsed) ? parsed : 0;
}

function sum(items, selector) {
  return items.reduce((total, item) => total + selector(item), 0);
}

function clamp(value, min, max) {
  return Math.min(max, Math.max(min, value));
}

function escapeHtml(value) {
  return String(value)
    .replace(/&/g, "&amp;")
    .replace(/</g, "&lt;")
    .replace(/>/g, "&gt;")
    .replace(/"/g, "&quot;");
}

function slugify(value) {
  const slug = String(value || "collection-card")
    .toLowerCase()
    .replace(/[^a-z0-9]+/g, "-")
    .replace(/^-+|-+$/g, "");
  return slug || "collection-card";
}
