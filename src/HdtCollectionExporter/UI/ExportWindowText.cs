using System;
using System.Collections.Generic;
using System.Globalization;
using HdtCollectionExporter.Models;

namespace HdtCollectionExporter.UI
{
    public sealed class ExportWindowText
    {
        public bool IsRussian { get; private set; }
        private ExportWindowText(bool russian) { IsRussian = russian; }
        public static ExportWindowText English() { return new ExportWindowText(false); }
        public static ExportWindowText Russian() { return new ExportWindowText(true); }
        public static ExportWindowText ForLanguage(string language)
        {
            var culture = CultureInfo.CurrentUICulture.Name;
            if(language == "auto")
            {
                // Read HDT's language when available; the system is the fallback.
                var config = Hearthstone_Deck_Tracker.Config.Instance;
                var member = config.GetType().GetField("Language");
                if(member != null) culture = Convert.ToString(member.GetValue(config));
            }
            return language == "ru" || (language != "en" && (culture ?? "").StartsWith("ru", StringComparison.OrdinalIgnoreCase)) ? Russian() : English();
        }
        public string this[string key]
        {
            get { string[] values; return Words.TryGetValue(key, out values) ? values[IsRussian ? 0 : 1] : key; }
        }
        public object GroupDisplay(CollectionGroup group, bool rarity)
        {
            var code = (group.Name ?? "").ToUpperInvariant();
            string title;
            if(string.IsNullOrEmpty(code)) title = IsRussian ? "Другие" : "Other";
            else if(rarity && Words.ContainsKey("Rarity" + code)) title = this["Rarity" + code];
            else if(IsRussian && SetNames.TryGetValue(code, out title)) { }
            else title = CultureInfo.InvariantCulture.TextInfo.ToTitleCase(code.Replace('_', ' ').ToLowerInvariant());
            var culture = CultureInfo.GetCultureInfo(IsRussian ? "ru-RU" : "en-US");
            var detail = group.Total > 0 ? string.Format(culture, IsRussian ? "{0:N0} из {1:N0} карт · {2:P0}" : "{0:N0} of {1:N0} cards · {2:P0}", group.CatalogOwned, group.Total, (double)group.CatalogOwned / group.Total)
                : string.Format(culture, IsRussian ? "{0:N0} уникальных карт" : "{0:N0} unique cards", group.Cards);
            return new { Title = title, Detail = detail + string.Format(culture, IsRussian ? "\n{0:N0} копий" : "\n{0:N0} copies", group.Copies), Completion = group.Total > 0 ? 100.0 * group.CatalogOwned / group.Total : 0.0 };
        }
        private static readonly Dictionary<string, string> SetNames = new Dictionary<string, string>
        {
            {"CORE", "Основной набор"},
            {"VANILLA", "Базовый набор"},
            {"LEGACY", "Старый набор"},
            {"LETTUCE", "Наемники"},
            {"EXPERT1", "Классический набор"},
            {"EVENT", "Событийные карты"},
            {"HERO_SKINS", "Портреты героев"},
            {"PLACEHOLDER_202204", "Основной набор"},
            {"NAXX", "Проклятие Наксрамаса"},
            {"FP1", "Проклятие Наксрамаса"},
            {"GVG", "Гоблины и гномы"},
            {"PE1", "Гоблины и гномы"},
            {"BRM", "Чёрная гора"},
            {"FP2", "Чёрная гора"},
            {"BLACKROCK_MOUNTAIN", "Чёрная гора"},
            {"TGT", "Большой турнир"},
            {"TEMP1", "Большой турнир"},
            {"LOE", "Лига исследователей"},
            {"OG", "Пробуждение древних богов"},
            {"OLD_GODS", "Пробуждение древних богов"},
            {"KARA", "Вечеринка в Каражане"},
            {"GANGS", "Злачный город Прибамбасск"},
            {"GADGETZAN", "Злачный город Прибамбасск"},
            {"UNGORO", "Экспедиция в Ун’Горо"},
            {"ICECROWN", "Рыцари Ледяного Трона"},
            {"LOOTAPALOOZA", "Кобольды и катакомбы"},
            {"GILNEAS", "Ведьмин лес"},
            {"BOOMSDAY", "Проект Бумного Дня"},
            {"TROLL", "Растахановы игрища"},
            {"DALARAN", "Возмездие теней"},
            {"ULDUM", "Спасители Ульдума"},
            {"DRAGONS", "Натиск драконов"},
            {"YEAR_OF_THE_DRAGON", "Пробуждение Галакронда"},
            {"BLACK_TEMPLE", "Руины Запределья"},
            {"DEMON_HUNTER_INITIATE", "Руины Запределья"},
            {"SCHOLOMANCE", "Некроситет"},
            {"DARKMOON_FAIRE", "Ярмарка безумия"},
            {"THE_BARRENS", "Закаленные Степями"},
            {"STORMWIND", "Сплоченные Штормградом"},
            {"ALTERAC_VALLEY", "Разделенные Альтераком"},
            {"THE_SUNKEN_CITY", "Путешествие в Затонувший город"},
            {"REVENDRETH", "Убийство в замке Нафрия"},
            {"RETURN_OF_THE_LICH_KING", "Марш Короля-лича"},
            {"PATH_OF_ARTHAS", "Марш Короля-лича"},
            {"BATTLE_OF_THE_BANDS", "Фестиваль легенд"},
            {"TITANS", "ТИТАНЫ"},
            {"WILD_WEST", "Битва в Бесплодных землях"},
            {"WHIZBANGS_WORKSHOP", "Мастерская Чудастера"},
            {"ISLAND_VACATION", "Раздор в тропиках"},
            {"SPACE", "Великая Запредельная Тьма"},
            {"EMERALD_DREAM", "Объятия Изумрудного Сна"},
            {"THE_LOST_CITY", "Затерянный город Ун'Горо"},
            {"TIME_TRAVEL", "Сквозь потоки времени"},
            {"WONDERS", "Пещеры времени"},
            {"CATACLYSM", "Катаклизм"},
        };
        private static readonly IDictionary<string, string[]> Words = new Dictionary<string, string[]>
        {
            {"RarityCOMMON", new[]{"Обычные", "Common"}},
            {"RarityRARE", new[]{"Редкие", "Rare"}},
            {"RarityEPIC", new[]{"Эпические", "Epic"}},
            {"RarityLEGENDARY", new[]{"Легендарные", "Legendary"}},
            {"RarityFREE", new[]{"Бесплатные", "Free"}},
            {"Title",new[]{"Ваша коллекция", "Your collection"}},
            {"Subtitle",new[]{"Снимки Hearthstone. Всё хранится на вашем компьютере.", "Hearthstone snapshots. Everything stays on your computer."}},
            {"Refresh",new[]{"↻  Обновить", "↻  Refresh"}},
            {"Unique",new[]{"УНИКАЛЬНЫЕ КАРТЫ", "UNIQUE CARDS"}},
            {"Copies",new[]{"КОПИИ", "COPIES"}},
            {"Premium",new[]{"ПРЕМИАЛЬНЫЕ", "PREMIUM"}},
            {"Dust",new[]{"ПЫЛЬ", "DUST"}},
            {"Account",new[]{"Ожидание коллекции", "Waiting for collection"}},
            {"Waiting",new[]{"Ожидание", "Waiting"}},
            {"Ready",new[]{"Данные из HDT", "Data from HDT"}},
            {"Stale",new[]{"Обновите снимок", "Refresh snapshot"}},
            {"Export",new[]{"Экспорт", "Export"}},
            {"History",new[]{"История", "History"}},
            {"Summary",new[]{"По наборам", "By set"}},
            {"Heading",new[]{"Сохранить коллекцию", "Save collection"}},
            {"Mode",new[]{"РЕЖИМ", "MODE"}},
            {"Format",new[]{"ФОРМАТ", "FORMAT"}},
            {"Full",new[]{"Полная коллекция", "Full collection"}},
            {"Changes",new[]{"Изменения с прошлого снимка", "Changes since previous snapshot"}},
            {"Folder",new[]{"ПАПКА СОХРАНЕНИЯ", "DESTINATION FOLDER"}},
            {"Browse",new[]{"Выбрать…", "Browse…"}},
            {"Save",new[]{"Сохранить снимок  →", "Save snapshot  →"}},
            {"SaveChanges",new[]{"Сохранить изменения  →", "Save changes  →"}},
            {"CreateBaseline",new[]{"Создать первый снимок  →", "Create first snapshot  →"}},
            {"Advanced",new[]{"Настройки и база сравнения", "Options and comparison baseline"}},
            {"Names",new[]{"Названия карт", "Card names"}},
            {"PremiumCsv",new[]{"Золотые копии в столбце CSV", "Golden copies in CSV column"}},
            {"Metadata",new[]{"Набор, редкость и класс", "Set, rarity and class"}},
            {"OptionsHint",new[]{"JSON всегда сохраняет реальные счётчики. Настройки не меняют историю и сравнение.", "JSON always retains actual counts. Options never alter history or comparisons."}},
            {"SetBaseline",new[]{"Текущая как база", "Use current as baseline"}},
            {"Import",new[]{"Импорт JSON…", "Import JSON…"}},
            {"Clear",new[]{"Сбросить базу", "Reset baseline"}},
            {"HistoryHint",new[]{"Каждый успешный экспорт сохраняет полный снимок. Выберите две даты одного аккаунта.", "Every successful export saves a full snapshot. Choose two dates for the same account."}},
            {"Earlier",new[]{"РАНЬШЕ", "EARLIER"}},
            {"Later",new[]{"ПОЗЖЕ", "LATER"}},
            {"Compare",new[]{"Экспорт сравнения", "Export comparison"}},
            {"HistoryBaseline",new[]{"Выбранный как база", "Use selected baseline"}},
            {"OpenHistory",new[]{"Папка истории", "History folder"}},
            {"Prune",new[]{"Оставить 30 последних…", "Keep latest 30…"}},
            {"PruneConfirm",new[]{"Удалить старые снимки, оставив 30 последних и действующую базу? Выгрузки в папке экспорта сохранятся.", "Delete older snapshots, keeping the latest 30 and the active baseline? Exported files will remain."}},
            {"Storage",new[]{"Снимков: {0} · {1:N1} МБ · автоматическое удаление отключено", "Snapshots: {0} · {1:N1} MB · automatic removal is off"}},
            {"SummaryHint",new[]{"Постоянные карты: уникальные / копии. Пробные копии: {0:N0}. Процент полноты требует каталога всех доступных карт.", "Permanent cards: unique / copies. Trial copies: {0:N0}. Completion percentage requires the full available-card catalog."}},
            {"SummaryCatalog",new[]{"Уникальные / каталог HDT · постоянные копии. Пробные копии: {0:N0}. Основной набор и служебные наборы исключены из сводки.", "Unique / HDT catalog · permanent copies. Trial copies: {0:N0}. Core and special sets are excluded from this summary."}},
            {"Sets",new[]{"Наборы", "Sets"}},
            {"Rarities",new[]{"Редкости", "Rarities"}},
            {"ReadTime",new[]{"Получено {0} · пробные копии учитываются отдельно", "Read {0} · trial copies are counted separately"}},
            {"PreviewFull",new[]{"{0:N0} уникальных карт · {1:N0} постоянных копий. Сводка и файл используют один снимок.", "{0:N0} unique cards · {1:N0} permanent copies. Preview and file use the same snapshot."}},
            {"PreviewChanges",new[]{"Изменений: {0} · добавлено карт: {1} · удалено: {2} · изменено: {3}", "Changes: {0} · cards added: {1} · removed: {2} · changed: {3}"}},
            {"FirstHint",new[]{"Сначала сохраним полную базу. Файл изменений появится при следующем сравнении.", "First, save a full baseline. A changes file can be created on the next comparison."}},
            {"LegacyCountsUnknown",new[]{"Старый снимок сохранён, но полнота счётчиков неизвестна. Создайте новую базу.", "The old snapshot is preserved, but count completeness is unknown. Create a new baseline."}},
            {"CorruptBaseline",new[]{"База повреждена. Выберите снимок из истории, импортируйте JSON или сохраните текущую как базу.", "The baseline is damaged. Import JSON or use the current collection as the baseline."}},
            {"Reading",new[]{"Читаю коллекцию из HDT…", "Reading collection from HDT…"}},
            {"Saving",new[]{"Подготавливаю и сохраняю файлы…", "Preparing and saving files…"}},
            {"WaitingGame",new[]{"Запустите Hearthstone и войдите в аккаунт. Коллекция появится после чтения HDT.", "Start Hearthstone and log in. The collection will appear after HDT reads it."}},
            {"WaitingRead",new[]{"HDT пока не получил коллекцию. Следующая попытка через 15 секунд; можно обновить вручную.", "HDT has not received the collection yet. Retrying in 15 seconds; you can also refresh manually."}},
            {"Failed",new[]{"Операция не завершена. Проверьте аккаунт, базу сравнения и доступ к папке. Подробности: {0}", "The operation failed. Check the account, baseline and destination access. Details: {0}"}},
            {"Saved",new[]{"Сохранено файлов: {0} · {1}. Полный снимок добавлен в историю.", "Files saved: {0} · {1}. A full snapshot was added to history."}},
            {"HistorySaved",new[]{"Сравнение сохранено: {0} файла · {1}. База сравнения сохранена без изменений.", "Comparison saved: {0} files · {1}. Baseline unchanged."}},
            {"BaselineSaved",new[]{"База сохранена. Файл изменений не создавался.", "Baseline saved. No changes file was created."}},
            {"BaselineSaveFailed",new[]{"Файлы экспорта сохранены, но база и история не обновлены. Проверьте доступ к папке данных.", "Export files were saved, but baseline/history were not updated. Check data-folder access."}},
            {"Partial",new[]{"Часть файлов сохранена: {0}. База не обновлена. Повторите экспорт после проверки папки.", "Some files were saved: {0}. Baseline was not advanced. Check the folder and retry."}},
            {"Cancelled",new[]{"Операция отменена.", "Operation cancelled."}},
            {"Cancel",new[]{"Отмена", "Cancel"}},
            {"OpenFolder",new[]{"Открыть папку", "Open folder"}},
            {"ShowFile",new[]{"Показать файл", "Show file"}},
            {"CopyPath",new[]{"Копировать путь", "Copy path"}},
            {"Copied",new[]{"Путь скопирован.", "Path copied."}},
            {"Imported",new[]{"Снимок импортирован. Для точного сравнения создайте новую базу с полными счётчиками.", "Snapshot imported. Create a new baseline with complete counts for accurate comparison."}},
            {"Cleared",new[]{"База сброшена. История сохранена.", "Baseline reset. History retained."}},
            {"ChooseHistory",new[]{"Выберите два действительных снимка: сначала ранний, затем поздний.", "Choose two valid snapshots: earlier first, then later."}},
            {"SettingsRecovery",new[]{"Настройки восстановлены по умолчанию.", "Default settings restored."}},
            {"SettingsSaveFailed",new[]{"Не удалось сохранить настройки. Проверьте доступ к папке данных HDT.", "Settings could not be saved. Check access to HDT's data folder."}}
        };
    }
}
