# Проверки Collection Exporter 1.6.0

Дата: 3 октября 2026 года. Основная платформа: Windows, установленный HDT 1.58.6.9003, .NET Framework 4.7.2, x64.

Изменения этапов 1.4.2/1.5.0/1.6.0 объединены в один выпуск 1.6.0: полные счётчики, разделение аккаунтов, безопасная запись, один плагин, новый интерфейс, предварительная сводка, история и сравнение дат. Публичный JSON сохраняет схему v3, заголовки CSV сохранены. Классический WPF-проект не переведён на другой runtime.

## Автоматические проверки

Чистый локальный запуск: **44 пройдено, 0 ошибок, 0 пропущено** в MSTest; **4 пройдено, 0 ошибок** в Node.js. В таблице приведены точные имена проверок; восемь вариантов `AllCountVariantsProduceDeltas` выполняются отдельно.

| Требование | Доказательство |
| --- | --- |
| Постоянные, золотые, алмазные, особые и пробные счётчики | `PremiumAndTrialCountsRemainCanonical`, `AllCountVariantsProduceDeltas`, `SummarySeparatesOwnedAndTrial` |
| Настройки представления не искажают базу | `PresentationOptionsDoNotCreateChanges` |
| Переименование BattleTag и перевод названий не создают изменения карт | `MetadataAndBattleTagDoNotCreateChanges` |
| Точные 64-битные идентификаторы и разделение аккаунтов | `AccountKeysPreserveUInt64`, `AccountsHaveIndependentBaselines`, `UnknownAccountRejected`, `ImportedAccountMustMatch` |
| Первая база не создаёт файл изменений | `FirstDeltaCreatesBaselineWithoutFiles` |
| Предварительная сводка и экспорт используют одни данные, включая аккаунт и рубашки | `PreviewAndExportUseSameSnapshot` |
| Добавленные/удалённые карты, пыль и рубашки | `AddedAndRemovedCardsAreCompared`, `NonCardChangesAreIncluded` |
| Пустые данные и отмена не записываются | `MissingProviderDataRejected`, `CancellationDoesNotWrite`, `CancelledReadCannotCommitLateResult` |
| История сохраняется, даты сортируются по времени, выбор базы меняет только указатель | `HistoryIsImmutableAndRebuildable`, `HistoryOrdersByInstantAndCountsOwnedCards`, `HistorySelectionChangesOnlyPointer` |
| Повреждения выявляются, резервная база восстанавливается | `CorruptedHistoryMarked`, `BackupRecoversPointer`, `CorruptBaselineCanBeRepairedByFullExport` |
| Старые файлы сохраняются, неполные счётчики не принимаются за нули | `LegacyImportIsPreservedAndUntrusted`, `DeltaImportRejected` |
| Сбой записи не перезаписывает файл, частичный результат явно сообщается | `AtomicReplaceKeepsBackup`, `BatchFailureReportsPartialFiles`, `StageFailureExposesNoFinalFiles`, `BaselineFailureReportsExportedFiles` |
| Уникальные имена и совместимый CSV с кавычками/переносами | `RapidExportsHaveUniqueNames`, `CsvEscapesAndRetainsHeader` |
| База, изменённая после чтения, требует нового предварительного сравнения | `BaselineChangedAfterPreviewRequiresRefresh` |
| Сравнение только полных снимков одного аккаунта в правильном порядке | `HistoryCompareRequiresSameAccountAndChronology` |
| Очистка сохраняет активную/резервную базу и повреждённые файлы | `PrunePreservesActiveBaselineAndCorruptFiles` |
| Процент заполнения использует полный каталог и исключает служебные наборы | `CompletionUsesCatalogAndExcludesSpecialSets`; Node.js `completion uses the full catalog, excludes special sets and ignores duplicate rows` |
| Миграция языка и восстановление настроек | `RussianSettingsMigrateWithoutDeletion`, `SettingsRoundTripAndBackupRecovery` |
| Схема v3 и точные аккаунты в JS; файл изменений не подменяет полную коллекцию | Node.js `schema3 import retains permanent/premium counts and dust`, `account IDs beyond JS integer precision are extracted exactly`, `changes and empty collections are rejected` |
| Один IPlugin, русское/английское окно, загрузка x64 и создание шаблонов сводки | `scripts/check-plugin-load.ps1`: Amd64, одна запись, обе локализации; шаблоны списков реально создаются с тестовыми данными |

Команды воспроизводимого запуска:

```powershell
./build.ps1 -Configuration Release -PinnedDependencies
dotnet test tests/HdtCollectionExporter.Tests/HdtCollectionExporter.Tests.csproj -c Release
powershell.exe -NoProfile -STA -File scripts/check-plugin-load.ps1 -PluginPath src/HdtCollectionExporter/bin/x64/Release/HdtCollectionExporter.dll -HdtDirectory 'artifacts/dependencies/hdt/Hearthstone Deck Tracker'
npm run check
node --test tests/web/import.test.cjs
```

CI `.github/workflows/ci.yml` повторяет Debug/Release, MSTest, STA и веб-проверки на Windows. На macOS он компилирует экспортный Swift-адаптер с заменами только внешних API HSTracker, проверяет реальные методы полного/дельта-экспорта и разделения аккаунтов, общий JSON-fixture, неизменность истории и её сохранение после сброса. AppKit-меню проходит проверку синтаксиса. Это не сборка самого HSTracker.

Проверки Windows Debug/Release и macOS успешно завершены: [GitHub Actions](https://github.com/Zulut30/HdtCollectionExporter/actions/runs/37129037036).

## Проверки установленного HDT

Проверены резервное копирование DLL/plugins.xml, объединение прежних русской и английской записей с сохранением включения, повторный запуск HDT и автоматическая синхронизация DLL в его текущую папку. Установщик из распакованного релизного ZIP отдельно проверен на чистом профиле и синтетическом профиле с двумя записями; посторонний плагин сохранён.

На настоящей коллекции: **5 395 уникальных карт, 11 829 постоянных копий, 2 092 премиальные копии, 9 535 пыли**. Полный JSON и CSV содержат 8 170 строк, включая карты с нулевым количеством; суммы совпадают со сводкой. Два снимка этой же коллекции сравнены через окно истории: ноль изменений, JSON/CSV сравнения сохранены, контрольная сумма указателя текущей базы не изменилась.

Настоящий JSON проверен локально функцией импорта сайта с локальным каталогом: постоянные копии и пыль совпадают, идентификаторы аккаунта извлекаются без потери точности. Данные аккаунта и снимки экрана не добавлены в Git и не публикуются на сайте.

При проверке новой сводки обнаружена попытка двустороннего WPF-binding к неизменяемому значению процента. Привязка исправлена на `OneWay`, STA-проверка расширена реальным созданием шаблонов списков. Исправленная вкладка открыта повторно на настоящей коллекции; после перезапуска новых событий падения HDT не обнаружено.

## Границы подтверждённого результата

Процент покрытия кода не измерялся. Проверки защищают основные действия с инвентарём, файлами и историей; ветви каждой отдельной записи статистики классов/героев не являются исчерпывающим покрытием. Проверка окна выполняется на текущем Windows-экране; все конфигурации DPI не проверены.

Нативная интеграция Swift-файлов в совместимую сборку HSTracker и ручная проверка меню на Mac остаются отдельной платформенной проверкой. macOS-архив содержит исходники адаптера, а не готовое расширение для установки. Основной Windows-пакет включает x64 DLL, установщик и инструкции; перед установкой публичного пакета проверяются SHA256 и совпадение установленной DLL с опубликованной.
