# Гайд по установке

Этот гайд объясняет, как собрать и установить HDT Collection Exporter by Manacost в Hearthstone Deck Tracker.

## 1. Требования

- Windows
- Установленный Hearthstone Deck Tracker
- Visual Studio 2022 Build Tools или Visual Studio
- Желательно: .NET Framework 4.7.2 Developer Pack / targeting pack

## 2. Сборка плагина

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

## 3. Открыть папку расширений HDT

В Hearthstone Deck Tracker:

1. Открой `Настройки`.
2. Перейди в `Трекер`.
3. Открой `Расширения`.
4. Нажми `Plugins Folder` / `Папка расширений`.

Обычно открывается папка:

```text
%AppData%\HearthstoneDeckTracker\Plugins
```

## 4. Скопировать правильный файл

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

## 5. Перезапустить HDT

Полностью закрой HDT, включая иконку возле часов, затем запусти HDT снова.

Если HDT был открыт во время копирования DLL, перезапуск обязателен: HDT может держать старую DLL загруженной.

## 6. Включить плагин

В `Настройки > Трекер > Расширения` включи один из вариантов:

- `Collection Exporter by Manacost`
- `Экспорт коллекции от Manacost`

Оба пункта используют один и тот же экспорт. Второй вариант имеет русские тексты интерфейса.

## 7. Экспорт файлов

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
- `Export Changes` / `Экспорт изменений`

Папка по умолчанию:

```text
Документы\HDT Collection Exports
```

`Export Changes` работает после хотя бы одного полного экспорта. Плагин сравнивает текущую коллекцию с последним локальным baseline и создает:

```text
hearthstone-collection-changes-YYYYMMDD-HHMMSS.json
hearthstone-collection-changes-YYYYMMDD-HHMMSS.csv
```

После успешного экспорта изменений baseline обновляется до текущей коллекции.

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

### Export Changes пишет, что предыдущего экспорта нет

Сначала запусти `Export Both` один раз. После этого `Export Changes` будет выгружать только отличия с момента последнего успешного полного экспорта или экспорта изменений.
