# Гайд по установке

Этот гайд объясняет, как установить HDT Collection Exporter by Manacost — плагин для [HearthSim/Hearthstone-Deck-Tracker](https://github.com/HearthSim/Hearthstone-Deck-Tracker).

## 1. Требования

- Windows
- Установленный Hearthstone Deck Tracker

## 2. Скачать Release

Открой последний релиз и скачай:

```text
HdtCollectionExporter.dll
```

[Latest Release](https://github.com/Zulut30/HdtCollectionExporter/releases/latest)

## 3. Сборка из исходников, опционально

Если хочешь собрать плагин сам, установи Visual Studio 2022 Build Tools или Visual Studio, затем запусти:

Открой PowerShell в корне репозитория и запусти:

```powershell
.\build.ps1
```

Если скрипт не нашел HDT автоматически, укажи папку установленной версии HDT:

```powershell
.\build.ps1 -HDTInstallDir "C:\Users\<you>\AppData\Local\HearthstoneDeckTracker\app-1.52.14"
```

Готовый файл будет здесь:

```text
src\HdtCollectionExporter\bin\x86\Release\HdtCollectionExporter.dll
```

## 4. Открыть папку расширений HDT

В Hearthstone Deck Tracker:

1. Открой `Настройки`.
2. Перейди в `Трекер`.
3. Открой `Расширения`.
4. Нажми `Plugins Folder` / `Папка расширений`.

Обычно открывается папка:

```text
%AppData%\HearthstoneDeckTracker\Plugins
```

## 5. Скопировать правильный файл

Скопируй только:

```text
HdtCollectionExporter.dll
```

Не копируй:

- `HdtCollectionExporter.sln`
- `.csproj` файлы
- исходники `.cs`
- папки `bin` или `obj`

HDT загружает только `.dll` файлы плагинов.

## 6. Перезапустить HDT

Полностью закрой HDT, включая иконку возле часов, затем запусти HDT снова.

Если HDT был открыт во время копирования DLL, перезапуск обязателен: HDT может держать старую DLL загруженной.

## 7. Включить плагин

В `Настройки > Трекер > Расширения` включи один из вариантов:

- `Collection Exporter by Manacost`
- `Экспорт коллекции от Manacost`

Оба пункта используют один и тот же экспорт. Второй вариант имеет русские тексты интерфейса.

## 8. Экспорт файлов

Запусти Hearthstone, войди в аккаунт и подожди, пока HDT прочитает коллекцию.

Затем открой:

```text
Plugins > Collection Exporter by Manacost
```

или:

```text
Plugins > Экспорт коллекции от Manacost
```

Выбери папку и нажми:

- `Export JSON` / `Экспорт JSON`
- `Export CSV` / `Экспорт CSV`
- `Export Both` / `Экспортировать оба`
- `Changes JSON` / `Изменения JSON`
- `Changes CSV` / `Изменения CSV`
- `Changes Both` / `Изменения: оба`

Папка по умолчанию:

```text
Документы\HDT Collection Exports
```

Экспорт изменений сравнивает текущую коллекцию с последним локальным baseline и создает:

```text
hearthstone-collection-changes-YYYYMMDD-HHMMSS.json
hearthstone-collection-changes-YYYYMMDD-HHMMSS.csv
```

Инструменты baseline:

- `Текущая база` — сохранить текущую коллекцию как baseline.
- `Импорт JSON` — импортировать старый полный JSON экспорт как baseline.
- `Очистить` — удалить сохраненные baseline-файлы.

Если baseline еще нет, экспорт изменений создает его из текущей коллекции вместо ошибки. После успешного экспорта изменений baseline обновляется до текущей коллекции.

## Что экспортируется

JSON содержит:

- `user.battleTag`
- `user.accountHi`
- `user.accountLo`
- `dust`
- `cardBacks`
- `favoriteCardBack`
- `favoriteHeroes`
- `playerRecords`
- `cards`

CSV содержит фиксированные колонки:

```text
cardId,dbfId,name,set,rarity,class,normal,golden,ownedTotal
```

В CSV колонка `golden` — это общий premium count: золотые + diamond + signature. В JSON эти значения есть отдельно.

## Частые проблемы

### Плагин не появился

Проверь, что в папке расширений лежит:

```text
HdtCollectionExporter.dll
```

а не:

```text
HdtCollectionExporter.sln
```

После этого полностью перезапусти HDT.

### Экспорт пишет, что коллекция недоступна

Запусти Hearthstone, войди в аккаунт и подожди немного. HDT сможет экспортировать коллекцию только после того, как прочитает ее из клиента игры.

### Нет доступа на запись файла

Выбери другую папку экспорта, например:

```text
Документы\HDT Collection Exports
```

### Экспорт изменений создал baseline вместо файла изменений

Это значит, что предыдущего baseline не было. Измени коллекцию и запусти `Изменения JSON`, `Изменения CSV` или `Изменения: оба` еще раз.
