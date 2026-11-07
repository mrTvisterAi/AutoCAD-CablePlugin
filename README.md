# AutoCAD Cable Plugin

Плагин для AutoCAD (.NET / C#) для работы с кабельными линиями на основе полилиний.

## Возможности (Этапы 1–3)

- Привязка данных кабеля к полилиниям (Extension Dictionary)
- WPF-диалог редактирования с несколькими участками прокладки
- Редактор таблицы труб (`CABLEDICT`)
- Автоподбор диаметра трубы/гофры
- Расчёт длины (масштаб, запас %, опуски), тока, ΔU% (ГОСТ), подбор АВ
- **Экспорт в Excel** (`CABLEEXPORT`)
- Юнит-тесты ядра

## Команды AutoCAD

| Команда | Описание |
|---------|----------|
| `CABLEATTACH` | Выбор трасс → WPF-диалог → сохранение |
| `CABLEEDIT` | Редактирование линии |
| `CABLEDICT` | Таблица соответствия марка+жилы+сечение → ∅ трубы |
| `CABLEEXPORT` | Экспорт в **Excel** (.xlsx): выбор или все линии модели |
| `CABLEINFO` | Просмотр в командной строке |
| `CABLERECALC` | Пересчёт длин и электрики |
| `CABLEREMOVE` | Удалить данные с объектов |

## Экспорт Excel (`CABLEEXPORT`)

Формируется файл `.xlsx` (ClosedXML, Excel на ПК не обязателен) с листами:

1. **Линии** — сводка: группа, от/до, марка, сечение, длины, ток, ΔU%, АВ  
2. **Участки** — разбивка по способам прокладки и диаметрам труб  
3. **Сводка** — агрегаты по марке/сечению, способам прокладки, ∅ труб  

Если ничего не выбрать и нажать Enter — экспортируются **все** линии из Model Space.

## Структура решения

```
AutoCAD-CablePlugin/
├── src/
│   ├── CablePlugin.Core/     # Модель, расчёты, ExcelExportService
│   ├── CablePlugin.Data/     # DWG storage, Collector
│   ├── CablePlugin.UI/       # WPF
│   └── CablePlugin/          # Команды
├── tests/
│   └── CablePlugin.Tests/
└── AutoCAD-CablePlugin.sln
```

## Сборка

```bash
dotnet build AutoCAD-CablePlugin.sln -c Release -p:AutoCADDir="C:\Program Files\Autodesk\AutoCAD 2025"
dotnet test tests/CablePlugin.Tests/CablePlugin.Tests.csproj
```

При `NETLOAD` рядом с `CablePlugin.dll` должны быть Core, Data, UI, ClosedXML и зависимости (System.Text.Json и т.д.).

## Статус

- [x] Модель данных и расчёты
- [x] Юнит-тесты
- [x] Слой Data
- [x] Команды ATTACH / EDIT / DICT / INFO / RECALC / REMOVE
- [x] WPF-диалоги
- [x] Экспорт в Excel (`CABLEEXPORT`)
- [ ] Этап 4: кабельный журнал, ведомость объёмов, схема щита
