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
let cardLookupWarmupScheduled = false;

const defaultSettings = {
  countDuplicates: true,
  includeGolden: true,
};

const localProfileSaveHosts = new Set(["", "localhost", "127.0.0.1", "::1"]);
const viewRoutes = {
  scan: "/",
  leaderboard: "/leaderboard",
};
const viewTitles = {
  scan: "Карточка коллекции Manacost",
  leaderboard: "Таблица лидеров коллекций Manacost",
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
  brandLink: document.querySelector(".brand"),
  scanTabButton: document.querySelector("#scanTabButton"),
  leaderboardTabButton: document.querySelector("#leaderboardTabButton"),
  scanView: document.querySelector("#scanView"),
  leaderboardView: document.querySelector("#leaderboardView"),
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
  refreshLeaderboardButton: document.querySelector("#refreshLeaderboardButton"),
  leaderboardStatus: document.querySelector("#leaderboardStatus"),
  leaderboardRows: document.querySelector("#leaderboardRows"),
  leaderboardEmpty: document.querySelector("#leaderboardEmpty"),
  leaderboardProfile: document.querySelector("#leaderboardProfile"),
  leaderboardClassIcon: document.querySelector("#leaderboardClassIcon"),
  leaderboardProfileMeta: document.querySelector("#leaderboardProfileMeta"),
  leaderboardProfileName: document.querySelector("#leaderboardProfileName"),
  leaderboardProfileSubtitle: document.querySelector("#leaderboardProfileSubtitle"),
  leaderboardOwnedCards: document.querySelector("#leaderboardOwnedCards"),
  leaderboardGoldenCards: document.querySelector("#leaderboardGoldenCards"),
  leaderboardDust: document.querySelector("#leaderboardDust"),
  leaderboardCoverage: document.querySelector("#leaderboardCoverage"),
  leaderboardSetsSummary: document.querySelector("#leaderboardSetsSummary"),
  leaderboardSetBreakdown: document.querySelector("#leaderboardSetBreakdown"),
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
let currentUserIdentifiers = {};
let currentCardLookup = null;
let currentSettings = { ...defaultSettings };
let currentSelectedSetCode = "";
let currentModalCards = [];
let currentModalVisibleCards = [];
let currentModalSet = null;
let currentModalSort = "owned";
let leaderboardProfiles = [];
let selectedLeaderboardId = "";
let leaderboardLoaded = false;
let leaderboardRequestId = 0;
let currentLeaderboardHasCollection = false;
const leaderboardProfileCache = new Map();
const ownedCardMapCache = new WeakMap();
let activeView = "scan";
let scanState = null;
let scanRendered = false;
let profileReadRequestId = 0;
let profileSaveRequestId = 0;

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

elements.brandLink.addEventListener("click", (event) => {
  event.preventDefault();
  switchView("scan");
});
elements.scanTabButton.addEventListener("click", () => switchView("scan"));
elements.leaderboardTabButton.addEventListener("click", () => switchView("leaderboard"));
elements.refreshLeaderboardButton.addEventListener("click", () => loadLeaderboard({ force: true }));
window.addEventListener("popstate", () => switchView(viewFromLocation(), { updateUrl: false }));

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
  profileReadRequestId += 1;
  profileSaveRequestId += 1;
  elements.fileInput.value = "";
  currentProfile = null;
  currentCollectionData = null;
  currentFileName = "";
  currentUserIdentifiers = {};
  currentCardLookup = null;
  currentSelectedSetCode = "";
  currentModalCards = [];
  currentModalVisibleCards = [];
  currentModalSet = null;
  currentModalSort = "owned";
  scanState = null;
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

initializeCurrentView();

function readFile(file) {
  if (!file.name.toLowerCase().endsWith(".json")) {
    setStatus("Выбери JSON-файл экспорта.", true);
    return;
  }

  const readRequestId = ++profileReadRequestId;
  const reader = new FileReader();
  reader.onload = async () => {
    try {
      const rawText = String(reader.result || "");
      const data = JSON.parse(rawText);
      const userIdentifiers = extractRawUserIdentifiers(rawText);
      setStatus("Загружаю русские названия карт из HearthstoneJSON...");
      const cardLookup = await loadCardLookup();
      if (readRequestId !== profileReadRequestId) {
        return;
      }

      currentCollectionData = data;
      currentFileName = file.name;
      currentUserIdentifiers = userIdentifiers;
      currentCardLookup = cardLookup;
      const profile = buildProfile(data, file.name, cardLookup, currentSettings);
      currentProfile = profile;
      renderProfile(profile);
      saveScanState();
      const lookupStatus = cardLookup.loaded
        ? " Русские названия HearthstoneJSON применены."
        : " Справочник HearthstoneJSON недоступен, использую названия из файла.";
      setStatus(`Загружено: ${profile.cardRowsLabel} из ${file.name}.${lookupStatus}`);
      queueProfileSave(data, profile, file.name, userIdentifiers);
    } catch (error) {
      if (readRequestId === profileReadRequestId) {
        setStatus(error.message, true);
      }
    }
  };
  reader.onerror = () => setStatus("Не удалось прочитать выбранный файл.", true);
  reader.readAsText(file);
}

function queueProfileSave(data, profile, fileName, userIdentifiers = {}) {
  const saveRequestId = ++profileSaveRequestId;
  saveProfileRecord(data, profile, fileName, userIdentifiers).then((saveStatus) => {
    if (saveRequestId !== profileSaveRequestId || activeView !== "scan" || !saveStatus) {
      return;
    }

    setStatus(`${elements.statusText.textContent}${saveStatus}`);
  }).catch(() => null);
}

async function saveProfileRecord(data, profile, fileName, userIdentifiers = {}) {
  const blizzardId = profileStorageId(data && data.user, userIdentifiers);

  if (!blizzardId) {
    return " \u041d\u0435 \u043d\u0430\u0439\u0434\u0435\u043d Blizzard ID, \u043f\u0440\u043e\u0444\u0438\u043b\u044c \u043d\u0435 \u0441\u043e\u0445\u0440\u0430\u043d\u0451\u043d.";
  }

  if (shouldSkipRemoteProfileSave()) {
    return "";
  }

  try {
    const response = await fetch("/api/profile", {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify(buildStoredProfile(data, profile, fileName, blizzardId, userIdentifiers)),
    });
    const result = await response.json().catch(() => ({}));

    if (result.skipped) {
      return " Демо-профиль не сохраняется в лидерборд.";
    }

    if (!response.ok || !result.ok) {
      return " \u0421\u043e\u0445\u0440\u0430\u043d\u0435\u043d\u0438\u0435 \u043f\u043e Blizzard ID \u0441\u0435\u0439\u0447\u0430\u0441 \u043d\u0435\u0434\u043e\u0441\u0442\u0443\u043f\u043d\u043e.";
    }

    return " \u041f\u0440\u043e\u0444\u0438\u043b\u044c \u0441\u043e\u0445\u0440\u0430\u043d\u0451\u043d \u043f\u043e Blizzard ID.";
  } catch {
    return " \u0421\u043e\u0445\u0440\u0430\u043d\u0435\u043d\u0438\u0435 \u043f\u043e Blizzard ID \u0441\u0435\u0439\u0447\u0430\u0441 \u043d\u0435\u0434\u043e\u0441\u0442\u0443\u043f\u043d\u043e.";
  }
}

function buildStoredProfile(data, profile, fileName, blizzardId, userIdentifiers = {}) {
  const user = data && data.user && typeof data.user === "object" ? data.user : {};

  return {
    blizzardId,
    user: {
      battleTag: stringValue(userIdentifiers.battleTag || user.battleTag),
      accountHi: stringValue(userIdentifiers.accountHi || user.accountHi),
      accountLo: stringValue(userIdentifiers.accountLo || user.accountLo),
    },
    export: {
      fileName,
      version: stringValue(data && data.version),
      exportedAt: stringValue(data && data.exportedAt),
      source: stringValue(data && data.source),
    },
    settings: {
      countDuplicates: Boolean(currentSettings.countDuplicates),
      includeGolden: Boolean(currentSettings.includeGolden),
    },
    profile: {
      playerName: profile.playerName,
      favoriteClass: profile.favoriteClass,
      bestClass: profile.bestClass,
      ownedCards: profile.ownedCards,
      goldenCards: profile.goldenCards,
      dust: profile.dust,
      availableDust: profile.availableDust,
      coverageRatio: profile.coverageRatio,
      coverageText: profile.coverageText,
      topClasses: profile.topClasses,
      topSets: profile.topSets,
      setBreakdown: profile.setBreakdown.map((row) => ({
        code: row.code,
        name: row.name,
        owned: row.owned,
        unique: row.unique,
        golden: row.golden,
        dust: row.dust,
      })),
    },
    collection: {
      cards: compactStoredCards(data && data.cards),
    },
  };
}

function compactStoredCards(cards) {
  if (!Array.isArray(cards)) {
    return [];
  }

  return cards
    .filter((card) => selectedOwnedCount(card, currentSettings) > 0 && !isHiddenProfileSet(card.set))
    .map((card) => ({
      cardId: stringValue(card.cardId || card.id),
      dbfId: number(card.dbfId),
      name: stringValue(card.name),
      set: stringValue(card.set),
      rarity: stringValue(card.rarity),
      class: stringValue(card.class),
      normal: number(card.normal),
      golden: number(card.golden),
      diamond: number(card.diamond),
      signature: number(card.signature),
    }));
}

function shouldSkipRemoteProfileSave() {
  return localProfileSaveHosts.has(window.location.hostname) || window.location.protocol === "file:";
}

function profileStorageId(user, userIdentifiers = {}) {
  const value = user && typeof user === "object" ? user : {};

  const accountHi = stringValue(userIdentifiers.accountHi || value.accountHi).replace(/\s+/g, "");
  const accountLo = stringValue(userIdentifiers.accountLo || value.accountLo).replace(/\s+/g, "");
  if (accountHi && accountLo) {
    return `${accountHi}-${accountLo}`;
  }

  return stringValue(userIdentifiers.battleTag || value.battleTag).replace(/\s+/g, "");
}

function stringValue(value) {
  return value == null ? "" : String(value).trim();
}

function extractRawUserIdentifiers(rawText) {
  return {
    battleTag: rawJsonValue(rawText, "battleTag"),
    accountHi: rawJsonValue(rawText, "accountHi"),
    accountLo: rawJsonValue(rawText, "accountLo"),
  };
}

function rawJsonValue(rawText, key) {
  // Keep large Blizzard account ids exact; JSON.parse rounds unsafe integers.
  const safeKey = key.replace(/[.*+?^${}()|[\]\\]/g, "\\$&");
  const match = new RegExp(`"${safeKey}"\\s*:\\s*("(?:\\\\.|[^"\\\\])*"|-?\\d+)`).exec(rawText);

  if (!match) {
    return "";
  }

  const value = match[1];
  if (!value.startsWith("\"")) {
    return value;
  }

  try {
    return JSON.parse(value);
  } catch {
    return "";
  }
}

async function refreshProfileFromSettings() {
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
    saveScanState();
    setStatus(`Пересчитано: ${profile.cardRowsLabel}.`);
    queueProfileSave(currentCollectionData, profile, currentFileName, currentUserIdentifiers);
  } catch (error) {
    setStatus(error.message, true);
  }
}

function initializeCurrentView() {
  const view = viewFromLocation();
  switchView(view, { replace: true });
}

function viewFromLocation() {
  const path = window.location.pathname.replace(/\/+$/, "") || "/";
  return path.toLowerCase() === viewRoutes.leaderboard ? "leaderboard" : "scan";
}

function switchView(view, options = {}) {
  view = view === "leaderboard" ? "leaderboard" : "scan";
  const isLeaderboard = view === "leaderboard";
  if (activeView === "scan" && isLeaderboard && scanRendered) {
    saveScanState();
  }

  elements.scanView.hidden = isLeaderboard;
  elements.leaderboardView.hidden = !isLeaderboard;
  elements.scanTabButton.classList.toggle("is-active", !isLeaderboard);
  elements.leaderboardTabButton.classList.toggle("is-active", isLeaderboard);
  elements.scanTabButton.setAttribute("aria-pressed", isLeaderboard ? "false" : "true");
  elements.leaderboardTabButton.setAttribute("aria-pressed", isLeaderboard ? "true" : "false");
  setCurrentNav(elements.scanTabButton, !isLeaderboard);
  setCurrentNav(elements.leaderboardTabButton, isLeaderboard);
  closeCardLightbox();
  closeCollectionModal();
  activeView = view;
  document.title = viewTitles[view] || viewTitles.scan;

  if (options.updateUrl !== false) {
    updateViewUrl(view, { replace: Boolean(options.replace) });
  }

  if (isLeaderboard) {
    loadLeaderboard();
  } else {
    restoreScanState();
    scheduleCardLookupWarmup();
  }
}

function setCurrentNav(element, isCurrent) {
  if (isCurrent) {
    element.setAttribute("aria-current", "page");
  } else {
    element.removeAttribute("aria-current");
  }
}

function updateViewUrl(view, options = {}) {
  if (!window.history || typeof window.history.pushState !== "function") {
    return;
  }

  const nextUrl = new URL(window.location.href);
  nextUrl.pathname = viewRoutes[view] || viewRoutes.scan;
  nextUrl.search = "";
  nextUrl.hash = "";

  if (nextUrl.href === window.location.href && !options.replace) {
    return;
  }

  const method = options.replace ? "replaceState" : "pushState";
  window.history[method]({ view }, "", nextUrl);
}

function ensureScanRendered() {
  if (!scanRendered) {
    renderProfile(currentProfile || emptyProfile);
  }
}

function saveScanState() {
  scanState = {
    profile: currentProfile,
    collectionData: currentCollectionData,
    fileName: currentFileName,
    userIdentifiers: { ...currentUserIdentifiers },
    cardLookup: currentCardLookup,
    settings: { ...currentSettings },
    selectedSetCode: currentSelectedSetCode,
  };
}

function restoreScanState() {
  if (!scanState) {
    currentProfile = null;
    currentCollectionData = null;
    currentFileName = "";
    currentUserIdentifiers = {};
    currentCardLookup = null;
    currentSettings = {
      countDuplicates: elements.countDuplicates.checked,
      includeGolden: elements.includeGolden.checked,
    };
    currentSelectedSetCode = "";
    renderProfile(emptyProfile);
    return;
  }

  currentProfile = scanState.profile;
  currentCollectionData = scanState.collectionData;
  currentFileName = scanState.fileName;
  currentUserIdentifiers = { ...scanState.userIdentifiers };
  currentCardLookup = scanState.cardLookup;
  currentSettings = { ...scanState.settings };
  currentSelectedSetCode = scanState.selectedSetCode || "";
  elements.countDuplicates.checked = currentSettings.countDuplicates;
  elements.includeGolden.checked = currentSettings.includeGolden;
  renderProfile(currentProfile || emptyProfile);
}

async function loadLeaderboard(options = {}) {
  if (leaderboardLoaded && !options.force) {
    return;
  }

  if (options.force) {
    leaderboardProfileCache.clear();
  }

  elements.leaderboardStatus.textContent = "Загружаю профили...";
  elements.refreshLeaderboardButton.disabled = true;

  try {
    const response = await fetch("/api/profile?leaderboard=1", { cache: options.force ? "reload" : "force-cache" });
    const result = await response.json().catch(() => ({}));

    if (!response.ok || !result.ok) {
      throw new Error(result.error || "Не удалось загрузить таблицу лидеров.");
    }

    leaderboardProfiles = Array.isArray(result.users) ? result.users : [];
    leaderboardLoaded = true;
    seedLeaderboardProfileCache(leaderboardProfiles);
    renderLeaderboardRows();
    updateLeaderboardStatus();

    const hasSelected = leaderboardProfiles.some((profile) => profile.blizzardId === selectedLeaderboardId);
    if (leaderboardProfiles.length && (!selectedLeaderboardId || !hasSelected)) {
      await selectLeaderboardProfile(leaderboardProfiles[0].blizzardId, { fetchDetails: false });
    }
  } catch (error) {
    leaderboardLoaded = false;
    leaderboardProfiles = [];
    renderLeaderboardRows();
    elements.leaderboardStatus.textContent = isLocalHost()
      ? "На локальном сервере лидерборд появится после деплоя на Vercel"
      : error.message;
  } finally {
    elements.refreshLeaderboardButton.disabled = false;
  }
}

function renderLeaderboardRows() {
  elements.leaderboardRows.replaceChildren();

  if (!leaderboardProfiles.length) {
    const row = document.createElement("tr");
    row.className = "leaderboard-empty-row";
    const cell = document.createElement("td");
    cell.className = "leaderboard-empty-cell";
    cell.colSpan = 7;
    cell.textContent = "Сохранённых публичных профилей пока нет.";
    row.appendChild(cell);
    elements.leaderboardRows.appendChild(row);
    return;
  }

  const fragment = document.createDocumentFragment();
  leaderboardProfiles.forEach((record, index) => {
    const profile = record.profile || {};
    const row = document.createElement("tr");
    const selectProfile = () => selectLeaderboardProfile(record.blizzardId);
    row.classList.toggle("is-selected", record.blizzardId === selectedLeaderboardId);
    row.tabIndex = 0;
    row.setAttribute("role", "button");
    row.setAttribute("aria-selected", record.blizzardId === selectedLeaderboardId ? "true" : "false");
    row.setAttribute("aria-label", `Открыть коллекцию ${profile.playerName || record.user?.battleTag || "игрока"}`);
    row.addEventListener("click", (event) => {
      if (!event.target.closest("button")) {
        selectProfile();
      }
    });
    row.addEventListener("keydown", (event) => {
      if (event.key === "Enter" || event.key === " ") {
        event.preventDefault();
        selectProfile();
      }
    });

    row.appendChild(tableCell(`#${index + 1}`, "leaderboard-rank"));

    const nameCell = document.createElement("td");
    const button = document.createElement("button");
    button.type = "button";
    button.className = "leaderboard-name-button";
    button.textContent = profile.playerName || record.user?.battleTag || "Игрок Hearthstone";
    button.addEventListener("click", (event) => {
      event.stopPropagation();
      selectProfile();
    });
    nameCell.appendChild(button);
    row.appendChild(nameCell);

    row.appendChild(tableCell(`${Math.round(number(profile.coverageRatio) * 100)}%`));
    row.appendChild(tableCell(formatNumber(profile.ownedCards)));
    row.appendChild(tableCell(formatNumber(profile.goldenCards)));
    row.appendChild(tableCell(formatNumber(profile.dust)));
    row.appendChild(tableCell(formatShortDate(record.savedAt)));
    fragment.appendChild(row);
  });

  elements.leaderboardRows.appendChild(fragment);
}

async function selectLeaderboardProfile(blizzardId, options = {}) {
  if (!blizzardId) {
    return;
  }

  const shouldFetchDetails = options.fetchDetails !== false;
  const requestId = ++leaderboardRequestId;
  selectedLeaderboardId = blizzardId;
  renderLeaderboardRows();
  const cached = leaderboardProfileCache.get(blizzardId);
  const summaryRecord = cached?.record || leaderboardProfiles.find((record) => record.blizzardId === blizzardId);

  if (summaryRecord) {
    applyLeaderboardRecord(summaryRecord, { preserveSelectedSet: Boolean(options.preserveSelectedSet) });
  }

  if (!shouldFetchDetails || cached?.hasCollection) {
    updateLeaderboardStatus();
    return summaryRecord;
  }

  elements.leaderboardStatus.textContent = "Открываю коллекцию...";

  try {
    const publicRecord = await fetchLeaderboardProfileRecord(blizzardId, { force: Boolean(options.force) });

    if (requestId !== leaderboardRequestId) {
      return publicRecord;
    }

    applyLeaderboardRecord(publicRecord, { preserveSelectedSet: Boolean(options.preserveSelectedSet) });
    updateLeaderboardStatus();
    return publicRecord;
  } catch (error) {
    if (requestId === leaderboardRequestId) {
      elements.leaderboardStatus.textContent = error.message;
    }
    return null;
  }
}

function seedLeaderboardProfileCache(records) {
  records.forEach((record) => {
    if (!record?.blizzardId) {
      return;
    }

    const cached = leaderboardProfileCache.get(record.blizzardId);
    if (!cached || !cached.hasCollection) {
      leaderboardProfileCache.set(record.blizzardId, {
        record,
        hasCollection: hasCollectionPayload(record),
      });
    }
  });
}

async function fetchLeaderboardProfileRecord(blizzardId, options = {}) {
  const cached = leaderboardProfileCache.get(blizzardId);
  if (cached?.hasCollection && !options.force) {
    return cached.record;
  }

  const response = await fetch(`/api/profile?blizzardId=${encodeURIComponent(blizzardId)}`, {
    cache: options.force ? "reload" : "force-cache",
  });
  const result = await response.json().catch(() => ({}));

  if (!response.ok || !result.ok || !result.profile) {
    throw new Error(result.error || "Не удалось открыть коллекцию игрока.");
  }

  leaderboardProfileCache.set(blizzardId, {
    record: result.profile,
    hasCollection: hasCollectionPayload(result.profile),
  });
  return result.profile;
}

function applyLeaderboardRecord(publicRecord, options = {}) {
  currentLeaderboardHasCollection = hasCollectionPayload(publicRecord);
  currentCollectionData = { cards: publicRecord.collection?.cards || [] };
  currentFileName = publicRecord.user?.battleTag || "leaderboard-profile.json";
  currentUserIdentifiers = {};
  currentSettings = {
    countDuplicates: publicRecord.settings?.countDuplicates !== false,
    includeGolden: publicRecord.settings?.includeGolden !== false,
  };

  const profile = profileFromPublicRecord(publicRecord);
  currentProfile = profile;
  if (!options.preserveSelectedSet) {
    currentSelectedSetCode = "";
  }
  renderLeaderboardProfile(profile, publicRecord);
  return profile;
}

function hasCollectionPayload(record) {
  return Boolean(record?.collection && Array.isArray(record.collection.cards));
}

function updateLeaderboardStatus(message = "") {
  if (message) {
    elements.leaderboardStatus.textContent = message;
    return;
  }

  elements.leaderboardStatus.textContent = leaderboardProfiles.length
    ? `${formatNumber(leaderboardProfiles.length)} игроков`
    : "Пока нет сохранённых профилей";
}

function renderLeaderboardProfile(profile, record) {
  const classMeta = getClassMeta(profile.favoriteClass);
  elements.leaderboardProfile.hidden = false;
  elements.leaderboardEmpty.hidden = true;
  elements.leaderboardProfile.style.setProperty("--class-accent", classMeta.accent);
  elements.leaderboardClassIcon.src = classIconPath(classMeta);
  elements.leaderboardProfileMeta.textContent = `Обновлено ${formatDate(record.savedAt)}`;
  elements.leaderboardProfileName.textContent = profile.playerName;
  const savedRows = number(record.collection?.cardCount);
  elements.leaderboardProfileSubtitle.textContent = savedRows > 0
    ? `${classMeta.label} · ${formatNumber(savedRows)} строк карт в сохранённом профиле`
    : `${classMeta.label} · ${formatNumber(profile.ownedCards)} карт в коллекции`;
  elements.leaderboardOwnedCards.textContent = formatNumber(profile.ownedCards);
  elements.leaderboardGoldenCards.textContent = formatNumber(profile.goldenCards);
  elements.leaderboardDust.textContent = formatNumber(profile.dust);
  elements.leaderboardCoverage.textContent = `${Math.round(profile.coverageRatio * 100)}%`;
  elements.leaderboardSetsSummary.textContent = profile.setBreakdownSummary;
  renderLeaderboardSetBreakdown(profile);
}

function renderLeaderboardSetBreakdown(profile) {
  elements.leaderboardSetBreakdown.replaceChildren();

  if (!profile.setBreakdown.length) {
    const empty = document.createElement("div");
    empty.className = "expansion-empty";
    empty.textContent = "У игрока нет сохранённых дополнений.";
    elements.leaderboardSetBreakdown.appendChild(empty);
    return;
  }

  const fragment = document.createDocumentFragment();
  profile.setBreakdown.forEach((row) => {
    const item = document.createElement("button");
    item.type = "button";
    item.className = "expansion-item";
    item.classList.toggle("is-selected", row.code === currentSelectedSetCode);
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
      openLeaderboardSet(profile, row);
    });
    fragment.appendChild(item);
  });
  elements.leaderboardSetBreakdown.appendChild(fragment);
}

async function openLeaderboardSet(profile, row) {
  currentSelectedSetCode = row.code;
  renderLeaderboardSetBreakdown(profile);

  try {
    if (!currentLeaderboardHasCollection && selectedLeaderboardId) {
      updateLeaderboardStatus("Загружаю карты игрока...");
      const publicRecord = await fetchLeaderboardProfileRecord(selectedLeaderboardId);
      profile = applyLeaderboardRecord(publicRecord, { preserveSelectedSet: true });
      row = profile.setBreakdown.find((item) => item.code === currentSelectedSetCode) || row;
    }

    currentCardLookup = currentCardLookup || await loadCardLookup();
    openCollectionModal(profile, row);
    updateLeaderboardStatus();
  } catch (error) {
    elements.leaderboardStatus.textContent = error.message;
  }
}

function profileFromPublicRecord(record) {
  const profile = record.profile || {};
  const setBreakdown = Array.isArray(profile.setBreakdown) ? profile.setBreakdown : [];

  return {
    loaded: true,
    fileName: record.user?.battleTag || "Публичный профиль",
    schemaVersion: record.export?.version ? `v${record.export.version}` : "Публичный",
    exportedAt: formatDate(record.export?.exportedAt || record.savedAt),
    sourceLabel: "Публичный профиль",
    playerName: profile.playerName || record.user?.battleTag || "Игрок Hearthstone",
    favoriteClass: profile.favoriteClass || "MAGE",
    bestClass: profile.bestClass || profile.favoriteClass || "MAGE",
    ownedCards: number(profile.ownedCards),
    goldenCards: number(profile.goldenCards),
    dust: number(profile.dust),
    availableDust: number(profile.availableDust),
    coverageRatio: number(profile.coverageRatio),
    coverageText: profile.coverageText || `${Math.round(number(profile.coverageRatio) * 100)}% всей коллекции`,
    topClasses: Array.isArray(profile.topClasses) ? profile.topClasses : [],
    topSets: Array.isArray(profile.topSets) ? profile.topSets : [],
    setBreakdown,
    setBreakdownSummary: setBreakdown.length
      ? `${formatNumber(setBreakdown.length)} дополнений · ${formatNumber(sum(setBreakdown, (row) => number(row.owned)))} карт`
      : "Нет сохранённых дополнений",
    cardRowsLabel: `${formatNumber(record.collection?.cardCount || 0)} строк карт`,
  };
}

function tableCell(text, className = "") {
  const cell = document.createElement("td");
  cell.textContent = text;
  if (className) {
    cell.className = className;
  }
  return cell;
}

function isLocalHost() {
  return localProfileSaveHosts.has(window.location.hostname) || window.location.protocol === "file:";
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

function scheduleCardLookupWarmup() {
  if (cardLookupPromise || cardLookupWarmupScheduled || activeView !== "scan") {
    return;
  }
  cardLookupWarmupScheduled = true;

  const warmup = () => {
    if (activeView === "scan") {
      loadCardLookup().catch(() => null);
    }
  };

  if ("requestIdleCallback" in window) {
    window.requestIdleCallback(warmup, { timeout: 1800 });
  } else {
    window.setTimeout(warmup, 600);
  }
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

  return { loaded: false, source: "none", byId: new Map(), byDbf: new Map(), bySet: new Map(), all: [], count: 0 };
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
  const bySet = new Map();
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

    const setCode = String(meta.set || "").trim().toUpperCase();
    if (id && setCode) {
      const setCards = bySet.get(setCode) || [];
      setCards.push(meta);
      bySet.set(setCode, setCards);
    }
  });

  return { byId, byDbf, bySet, all: Array.from(byId.values()), count: cards.length };
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

function getOwnedCardMap(cards, cardLookup, settings) {
  if (!Array.isArray(cards)) {
    return new Map();
  }

  const cacheKey = [
    settings.countDuplicates ? "d" : "u",
    settings.includeGolden ? "g" : "n",
    cardLookup?.loaded ? cardLookup.count : 0,
  ].join(":");
  let cache = ownedCardMapCache.get(cards);
  if (!cache) {
    cache = new Map();
    ownedCardMapCache.set(cards, cache);
  }

  if (!cache.has(cacheKey)) {
    cache.set(cacheKey, buildOwnedCardMap(cards, cardLookup, settings));
  }

  return cache.get(cacheKey);
}

function lookupCardsForSet(cardLookup, setCode) {
  if (!cardLookup?.loaded || !cardLookup.bySet) {
    return [];
  }

  const seen = new Set();
  const rows = [];
  setLookupCodes(setCode).forEach((code) => {
    const setCards = cardLookup.bySet.get(code) || [];
    setCards.forEach((meta) => {
      if (!meta.id || seen.has(meta.id)) {
        return;
      }
      seen.add(meta.id);
      rows.push(meta);
    });
  });
  return rows;
}

function buildSelectedSetCards(cards, cardLookup, settings, setCode) {
  if (!setCode) {
    return [];
  }

  const ownedById = getOwnedCardMap(cards, cardLookup, settings);
  const lookupCodes = new Set(setLookupCodes(setCode));
  const lookupCards = lookupCardsForSet(cardLookup, setCode);

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
  scanRendered = true;
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
    item.style.setProperty("--card-index", Math.min(index, 28));
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

function formatShortDate(value) {
  if (!value) {
    return "-";
  }

  const date = new Date(value);
  if (Number.isNaN(date.getTime())) {
    return "-";
  }

  return new Intl.DateTimeFormat("ru-RU", {
    day: "2-digit",
    month: "2-digit",
    year: "2-digit",
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
