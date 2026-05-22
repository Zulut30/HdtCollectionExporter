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

const cardSetAliases = {
  FP1: ["NAXX"],
  PE1: ["GVG"],
  FP2: ["BRM"],
  BLACKROCK_MOUNTAIN: ["BRM"],
  TEMP1: ["TGT"],
  OLD_GODS: ["OG"],
  GADGETZAN: ["GANGS"],
  BLACK_TEMPLE: ["DEMON_HUNTER_INITIATE"],
  DEMON_HUNTER_INITIATE: ["BLACK_TEMPLE"],
};

const elements = {
  dropZone: document.querySelector("#dropZone"),
  fileInput: document.querySelector("#fileInput"),
  chooseFileButton: document.querySelector("#chooseFileButton"),
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
  setsSummary: document.querySelector("#setsSummary"),
  setBreakdown: document.querySelector("#setBreakdown"),
  collectionModal: document.querySelector("#collectionModal"),
  modalScrim: document.querySelector(".modal-scrim"),
  modalCloseButton: document.querySelector("#modalCloseButton"),
  modalSetTitle: document.querySelector("#modalSetTitle"),
  modalSetSubtitle: document.querySelector("#modalSetSubtitle"),
  modalSetStats: document.querySelector("#modalSetStats"),
  modalSortControls: document.querySelector("#modalSortControls"),
  modalCardGallery: document.querySelector("#modalCardGallery"),
  cardLightbox: document.querySelector("#cardLightbox"),
  lightboxScrim: document.querySelector(".lightbox-scrim"),
  lightboxCloseButton: document.querySelector("#lightboxCloseButton"),
  lightboxImage: document.querySelector("#lightboxImage"),
  lightboxTitle: document.querySelector("#lightboxTitle"),
  lightboxMeta: document.querySelector("#lightboxMeta"),
  lightboxBadges: document.querySelector("#lightboxBadges"),
};

let currentProfile = null;
let currentCollectionData = null;
let currentFileName = "";
let currentCardLookup = null;
let currentSettings = { ...defaultSettings };
let currentSelectedSetCode = "";
let currentModalCards = [];
let currentModalVisibleCards = [];
let currentModalSet = null;
let currentModalSort = "owned";

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
  setBreakdown: [],
  setBreakdownSummary: "Загрузи JSON-экспорт",
  selectedSetCards: [],
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
  currentSelectedSetCode = "";
  currentModalCards = [];
  currentModalVisibleCards = [];
  currentModalSet = null;
  currentModalSort = "owned";
  closeCardLightbox();
  closeCollectionModal();
  renderProfile(emptyProfile);
  setStatus("Готов к полному JSON-экспорту коллекции.");
});

elements.modalCloseButton.addEventListener("click", closeCollectionModal);
elements.modalScrim.addEventListener("click", closeCollectionModal);
elements.lightboxCloseButton.addEventListener("click", closeCardLightbox);
elements.lightboxScrim.addEventListener("click", closeCardLightbox);
elements.modalSortControls.addEventListener("click", (event) => {
  const button = event.target.closest("[data-sort]");
  if (!button || !elements.modalSortControls.contains(button)) {
    return;
  }

  currentModalSort = button.dataset.sort || "owned";
  renderModalSortControls();
  renderModalCardGallery();
});

document.addEventListener("keydown", (event) => {
  if (event.key !== "Escape") {
    return;
  }

  if (elements.cardLightbox.classList.contains("is-open")) {
    closeCardLightbox();
    return;
  }

  if (elements.collectionModal.classList.contains("is-open")) {
    closeCollectionModal();
  }
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

  return { loaded: false, source: "none", byId: new Map(), byDbf: new Map(), all: [], count: 0 };
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

  return { byId, byDbf, all: Array.from(byId.values()), count: cards.length };
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

function setLookupCodes(setCode) {
  const normalized = String(setCode || "").trim().toUpperCase();
  return Array.from(new Set([normalized, ...(cardSetAliases[normalized] || [])]));
}

function buildOwnedCardMap(cards, cardLookup, settings) {
  const byCardId = new Map();
  cards.forEach((card) => {
    if (isHiddenProfileSet(card.set)) {
      return;
    }

    const meta = findCardMeta(card, cardLookup);
    const cardId = String(card.cardId || card.id || (meta && meta.id) || "").trim();
    if (!cardId) {
      return;
    }

    const current = byCardId.get(cardId) || { normal: 0, golden: 0, owned: 0 };
    const normal = selectedNormalCount(card, settings);
    const golden = selectedGoldenCount(card, settings);
    current.normal += normal;
    current.golden += golden;
    current.owned += normal + selectedPremiumCount(card, settings);
    byCardId.set(cardId, current);
  });
  return byCardId;
}

function buildSelectedSetCards(cards, cardLookup, settings, setCode) {
  if (!setCode) {
    return [];
  }

  const ownedById = buildOwnedCardMap(cards, cardLookup, settings);
  const lookupCodes = new Set(setLookupCodes(setCode));
  const lookupCards = cardLookup && cardLookup.loaded && Array.isArray(cardLookup.all)
    ? cardLookup.all.filter((meta) => lookupCodes.has(String(meta.set || "").trim().toUpperCase()))
    : [];

  const sourceCards = lookupCards.length
    ? lookupCards
    : cards
      .map((card) => findCardMeta(card, cardLookup) || {
        id: String(card.cardId || card.id || "").trim(),
        dbfId: number(card.dbfId),
        name: cardDisplayName(card, null),
        rarity: normalizeRarity(card.rarity),
        set: String(card.set || "").trim(),
      })
      .filter((meta) => meta.id && lookupCodes.has(String(meta.set || "").trim().toUpperCase()));

  return sourceCards
    .map((meta) => {
      const owned = ownedById.get(meta.id) || { owned: 0, golden: 0 };
      return {
        cardId: meta.id,
        dbfId: number(meta.dbfId),
        name: meta.name || meta.id,
        rarity: normalizeRarity(meta.rarity),
        normal: owned.normal || 0,
        owned: owned.owned,
        golden: owned.golden,
      };
    })
    .sort((a, b) => (
      Number(a.owned <= 0) - Number(b.owned <= 0) ||
      rarityWeight(b.rarity) - rarityWeight(a.rarity) ||
      a.name.localeCompare(b.name, "ru") ||
      a.dbfId - b.dbfId
    ));
}

function rarityWeight(rarity) {
  const weights = {
    LEGENDARY: 5,
    EPIC: 4,
    RARE: 3,
    COMMON: 2,
    FREE: 1,
  };
  return weights[rarity] || 0;
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

  ensureSelectedSet(profile);
  renderTopClasses(profile.topClasses);
  renderTopSets(profile.topSets);
  renderSetBreakdown(profile);
  refreshOpenCollectionModal(profile);
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
    const item = document.createElement("button");
    item.type = "button";
    item.className = "expansion-item";
    item.classList.toggle("is-selected", row.code === currentSelectedSetCode);
    item.setAttribute("aria-pressed", row.code === currentSelectedSetCode ? "true" : "false");
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
    item.addEventListener("click", () => {
      currentSelectedSetCode = row.code;
      renderSetBreakdown(profile);
      openCollectionModal(profile, row);
    });
    fragment.appendChild(item);
  });
  elements.setBreakdown.appendChild(fragment);
}

function openCollectionModal(profile, setRow, options = {}) {
  if (!profile.loaded || !setRow) {
    return;
  }

  currentModalSet = setRow;
  currentModalCards = buildSelectedSetCards(
    currentCollectionData.cards || [],
    currentCardLookup,
    currentSettings,
    setRow.code,
  );

  const ownedCount = currentModalCards.filter((row) => row.owned > 0).length;
  const goldenCount = currentModalCards.filter((row) => row.golden > 0).length;
  elements.modalSetTitle.textContent = setRow.name;
  renderModalStats(ownedCount, currentModalCards.length, goldenCount);
  elements.modalSetSubtitle.textContent = `${formatNumber(setRow.owned)} карт в коллекции · ${formatNumber(goldenCount)} ${pluralRu(goldenCount, "карта", "карты", "карт")} с золотыми копиями`;
  renderModalSortControls();
  renderModalCardGallery();

  elements.collectionModal.classList.add("is-open");
  elements.collectionModal.setAttribute("aria-hidden", "false");
  document.body.classList.add("modal-open");

  if (!options.preserveFocus) {
    elements.modalCloseButton.focus({ preventScroll: true });
  }
}

function closeCollectionModal() {
  elements.collectionModal.classList.remove("is-open");
  elements.collectionModal.setAttribute("aria-hidden", "true");
  closeCardLightbox();
  document.body.classList.remove("modal-open");
}

function refreshOpenCollectionModal(profile) {
  if (!elements.collectionModal.classList.contains("is-open") || !currentModalSet) {
    return;
  }

  const freshSet = profile.setBreakdown.find((row) => row.code === currentModalSet.code);
  if (!freshSet) {
    closeCollectionModal();
    return;
  }

  openCollectionModal(profile, freshSet, { preserveFocus: true });
}

function renderModalStats(ownedCount, totalCount, goldenCount) {
  const missingCount = Math.max(0, totalCount - ownedCount);
  elements.modalSetStats.innerHTML = `
    <span class="modal-stat is-owned"><small>Есть</small><strong>${formatNumber(ownedCount)} / ${formatNumber(totalCount)}</strong></span>
    <span class="modal-stat is-golden"><small>Золотые</small><strong>${formatNumber(goldenCount)}</strong></span>
    <span class="modal-stat is-missing"><small>Нет</small><strong>${formatNumber(missingCount)}</strong></span>
  `;
}

function renderModalSortControls() {
  elements.modalSortControls.querySelectorAll("[data-sort]").forEach((button) => {
    const isActive = button.dataset.sort === currentModalSort;
    button.classList.toggle("is-active", isActive);
    button.setAttribute("aria-pressed", isActive ? "true" : "false");
  });
}

function sortedModalCards() {
  const rows = [...currentModalCards];
  return rows.sort((a, b) => {
    const ownershipOrder = Number(a.owned <= 0) - Number(b.owned <= 0);
    const rarityOrder = rarityWeight(b.rarity) - rarityWeight(a.rarity);
    const nameOrder = a.name.localeCompare(b.name, "ru");
    const idOrder = a.dbfId - b.dbfId;

    if (currentModalSort === "rarity") {
      return rarityOrder || ownershipOrder || nameOrder || idOrder;
    }

    return ownershipOrder || rarityOrder || nameOrder || idOrder;
  });
}

function renderModalCardGallery() {
  elements.modalCardGallery.replaceChildren();

  if (!currentModalCards.length) {
    currentModalVisibleCards = [];
    const empty = document.createElement("div");
    empty.className = "gallery-empty";
    empty.textContent = "Не удалось найти карты этого дополнения в справочнике.";
    elements.modalCardGallery.appendChild(empty);
    return;
  }

  const fragment = document.createDocumentFragment();
  currentModalVisibleCards = sortedModalCards();
  currentModalVisibleCards.forEach((row, index) => {
    const item = document.createElement("button");
    item.type = "button";
    item.className = `modal-card ${row.owned > 0 ? "is-owned" : "is-missing"}`;
    item.title = row.owned > 0
      ? `${row.name}: ${copySummary(row)}`
      : `${row.name}: нет в коллекции`;
    item.innerHTML = `
      <img src="${escapeHtml(cardRemoteImageUrl(row.cardId))}" alt="${escapeHtml(row.name)}" loading="lazy" decoding="async" />
      <span class="card-indicator-row">
        ${copyBadges(row, { inline: true })}
      </span>
    `;
    item.addEventListener("click", () => openCardLightbox(index));
    fragment.appendChild(item);
  });
  elements.modalCardGallery.appendChild(fragment);
}

function openCardLightbox(index) {
  const row = currentModalVisibleCards[index];
  if (!row) {
    return;
  }

  elements.lightboxImage.src = cardRemoteImageUrl(row.cardId);
  elements.lightboxImage.alt = row.name;
  elements.lightboxTitle.textContent = row.name;
  elements.lightboxMeta.textContent = row.owned > 0
    ? `${copySummary(row)} · ${rarityLabel(row.rarity)}`
    : `Нет в коллекции · ${rarityLabel(row.rarity)}`;
  elements.lightboxBadges.innerHTML = copyBadgeItems(row, { showZeroGolden: true });
  elements.cardLightbox.classList.add("is-open");
  elements.cardLightbox.setAttribute("aria-hidden", "false");
  elements.lightboxCloseButton.focus({ preventScroll: true });
}

function closeCardLightbox() {
  elements.cardLightbox.classList.remove("is-open");
  elements.cardLightbox.setAttribute("aria-hidden", "true");
}

function copyBadges(row, options = {}) {
  const badges = copyBadgeItems(row, options);
  const className = options.inline ? "copy-badges is-inline" : "copy-badges";
  return badges ? `<span class="${className}">${badges}</span>` : "";
}

function copyBadgeItems(row, options = {}) {
  const normal = number(row.normal);
  const golden = number(row.golden);
  const badges = [];

  if (normal > 0) {
    badges.push(`<span class="copy-badge is-normal" title="Обычные копии"><span class="copy-badge-mark">×</span><span class="copy-badge-value">${formatNumber(normal)}</span></span>`);
  } else if (row.owned <= 0) {
    badges.push('<span class="copy-badge is-zero" title="Нет обычных копий"><span class="copy-badge-value">0</span></span>');
  }

  if (golden > 0 || options.showZeroGolden) {
    const zeroClass = golden > 0 ? "" : " is-zero";
    badges.push(`<span class="copy-badge is-golden${zeroClass}" title="Золотые копии"><span class="copy-badge-mark">★</span><span class="copy-badge-value">${formatNumber(golden)}</span></span>`);
  }

  return badges.join("");
}

function copySummary(row) {
  const normal = number(row.normal);
  const golden = number(row.golden);
  const parts = [];

  if (normal > 0) {
    parts.push(`${formatNumber(normal)} ${pluralRu(normal, "обычная", "обычные", "обычных")}`);
  }
  if (golden > 0) {
    parts.push(`${formatNumber(golden)} ${pluralRu(golden, "золотая", "золотые", "золотых")}`);
  }

  return parts.length ? parts.join(" · ") : "Нет в коллекции";
}

function ensureSelectedSet(profile) {
  if (!profile.loaded || !profile.setBreakdown.length) {
    currentSelectedSetCode = "";
    return;
  }

  const hasSelected = profile.setBreakdown.some((row) => row.code === currentSelectedSetCode);
  if (!hasSelected) {
    currentSelectedSetCode = profile.setBreakdown[0].code;
  }
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
