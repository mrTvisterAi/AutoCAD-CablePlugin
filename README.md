# AutoCAD Cable Plugin

Плагин AutoCAD (.NET / C#) для кабельных линий: данные на полилиниях, расчёты, отчёты, однолинейка блоками.

## Команды

### Данные
| Команда | Описание |
|---------|----------|
| `CABLEATTACH` / `CABLEEDIT` | WPF-диалог линии |
| `CABLEDICT` | Таблица труб |
| `CABLEEXPORT` | Excel по линиям |
| `CABLEINFO` / `CABLERECALC` / `CABLEREMOVE` | Просмотр / пересчёт / удаление |

### Отчёты
| Команда | Описание |
|---------|----------|
| `CABLEJOURNAL` | Кабельный журнал (Excel / таблица CAD) |
| `CABLEBOM` | Ведомость кабеля и труб |
| `CABLEPANEL` | Таблица щита + Kс |

### Схема и шаблоны
| Команда | Описание |
|---------|----------|
| `CABLESCHEME` | **Однолинейная схема щита** блоками `CABLE_QF`, `CABLE_BUS`, `CABLE_LOAD` |
| `CABLETEMPLATE` | Список / сброс / сохранение **шаблонов** колонок и параметров схемы |

## Блоки однолинейки

При первом `CABLESCHEME` в чертёж создаются:

| Блок | Назначение |
|------|------------|
| `CABLE_QF` | Автоматический выключатель |
| `CABLE_BUS` | Сборные шины |
| `CABLE_LOAD` | Нагрузка / уход линии |

Можно заменить геометрию блоков вручную — схема продолжит их использовать. Имена и шаг фидеров задаются в `PanelBlockTemplate` (чертёж: Named Dictionary `GrokCableDictionaries` → `TemplateSet`).

## Шаблоны таблиц

`TableTemplate` задаёт видимые колонки, заголовки и ширины для журнала / ведомости / таблицы щита.  
`CABLETEMPLATE` → **Список** — текущий состав; **Сброс** — factory defaults; **Сохранить** — запись в DWG.

## Сборка

```bash
dotnet build AutoCAD-CablePlugin.sln -c Release -p:AutoCADDir="C:\Program Files\Autodesk\AutoCAD 2025"
dotnet test tests/CablePlugin.Tests/CablePlugin.Tests.csproj
```

## Статус

- [x] Этапы 1–3: модель, Data, WPF, трубы, Excel линий
- [x] Журнал, ведомость, таблица щита
- [x] Однолинейка блоками (`CABLESCHEME`)
- [x] Шаблоны колонок и параметров схемы (`CABLETEMPLATE`)
- [ ] WPF-редактор шаблонов (колонки drag-and-drop)
