# AutoCAD Cable Plugin

Плагин для AutoCAD (.NET / C#) для работы с кабельными линиями на основе полилиний.

## Возможности (Этапы 1–3)

- Привязка данных кабеля к полилиниям (Extension Dictionary)
- Несколько способов прокладки на одной линии (лоток, кабель-канал, гофра и т.д.)
- Автоматический подбор диаметра трубы/гофры по таблице соответствия
- Расчёт длины с учётом масштаба, процента запаса и дополнительных опусков
- Расчёт тока, потерь напряжения (ГОСТ Р 50571.5.52-2011 прил. G)
- Подбор номинала автоматического выключателя
- Юнит-тесты ядра

## Команды AutoCAD

| Команда | Описание |
|---------|----------|
| `CABLEATTACH` | Привязать данные кабеля к выбранным полилиниям |
| `CABLEINFO` | Показать данные кабеля выбранного объекта |
| `CABLERECALC` | Пересчитать длины и электрику |
| `CABLEREMOVE` | Удалить данные кабеля с объектов |
| `CABLEEDIT` | Редактирование (пока через info + attach; WPF — далее) |

## Структура решения

```
AutoCAD-CablePlugin/
├── src/
│   ├── CablePlugin.Core/     # Модель данных и расчёты (без AutoCAD API)
│   ├── CablePlugin.Data/     # Extension Dictionary, Named Dictionary, длина полилинии
│   └── CablePlugin/          # Команды AutoCAD, IExtensionApplication
├── tests/
│   └── CablePlugin.Tests/    # xUnit-тесты ядра
└── AutoCAD-CablePlugin.sln
```

## Требования

- AutoCAD 2021–2025+ (по умолчанию путь сборки: `C:\Program Files\Autodesk\AutoCAD 2024`)
- Visual Studio 2022
- .NET Framework 4.8

## Сборка

```bash
# Указать путь к вашей версии AutoCAD при необходимости:
dotnet build AutoCAD-CablePlugin.sln -c Release -p:AutoCADDir="C:\Program Files\Autodesk\AutoCAD 2025"

# Только тесты ядра (без AutoCAD DLL):
dotnet test tests/CablePlugin.Tests/CablePlugin.Tests.csproj
```

## Загрузка в AutoCAD

1. Соберите `CablePlugin.dll` (и рядом `CablePlugin.Core.dll`, `CablePlugin.Data.dll`, `System.Text.Json.dll`).
2. В AutoCAD: команда `NETLOAD` → выберите `CablePlugin.dll`.
3. Либо добавьте bundle / автозагрузку через `PackageContents.xml` (будет позже).

## Хранение данных

- На каждой полилинии: Extension Dictionary `GrokCableData` → Xrecord `CableLine` (JSON).
- В чертеже: Named Dictionary `GrokCableDictionaries` → таблица труб `PipeTable`.

## Статус

- [x] Модель данных и расчёты
- [x] Юнит-тесты ядра
- [x] Слой Data (чтение/запись в DWG)
- [x] Команды CABLEATTACH / INFO / RECALC / REMOVE
- [ ] WPF-диалог редактирования (несколько сегментов прокладки)
- [ ] Редактор таблицы труб
- [ ] Экспорт в Excel
- [ ] Этап 4: журнал, ведомость, схема щита
