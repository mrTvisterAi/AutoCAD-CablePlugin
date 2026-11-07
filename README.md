# AutoCAD Cable Plugin

Плагин AutoCAD (.NET / C#) для кабельных линий: данные, расчёты, отчёты, однолинейка с **пользовательскими блоками из DWG-библиотеки**.

## Команды (кратко)

| Группа | Команды |
|--------|---------|
| Данные | `CABLEATTACH` `CABLEEDIT` `CABLEDICT` `CABLEEXPORT` `CABLEINFO` `CABLERECALC` `CABLEREMOVE` |
| Отчёты | `CABLEJOURNAL` `CABLEBOM` `CABLEPANEL` |
| Схема | `CABLESCHEME` `CABLETEMPLATE` **`CABLELIB`** |

## Библиотека блоков (`CABLELIB`)

Подстановка своих условных обозначений вместо встроенных `CABLE_QF` / `CABLE_BUS` / `CABLE_LOAD`.

1. **Путь** — выбрать DWG с блоками  
2. **Имена** — сопоставить «имя в чертеже» ← «имя в библиотеке»  
3. **Импорт** — `WblockCloneObjects` в текущий чертёж  
4. **Список** — путь, маппинг, содержимое DWG  
5. **Сброс** — убрать путь (снова встроенные блоки)

При `CABLESCHEME` библиотека подключается автоматически (если путь задан). Если блока нет в DWG — создаётся встроенный fallback.

Параметры хранятся в чертеже: `GrokCableDictionaries` → `TemplateSet` → `panelBlocks.libraryDwgPath`.

### Пример библиотеки

В `MySymbols.dwg` лежат блоки `QF_IEC`, `BUSBAR`, `LOAD_X`. Тогда:

```
CABLELIB → Путь → MySymbols.dwg
CABLELIB → Имена
  АВ в чертеже: CABLE_QF    ← в библиотеке: QF_IEC
  Шина:         CABLE_BUS   ← BUSBAR
  Нагрузка:     CABLE_LOAD  ← LOAD_X
CABLELIB → Импорт
CABLESCHEME
```

## Сборка

```bash
dotnet build AutoCAD-CablePlugin.sln -c Release -p:AutoCADDir="C:\Program Files\Autodesk\AutoCAD 2025"
dotnet test tests/CablePlugin.Tests/CablePlugin.Tests.csproj
```

## Статус

- [x] Этапы 1–4: данные, отчёты, однолинейка, шаблоны
- [x] Импорт блоков из внешней DWG (`CABLELIB`)
- [ ] WPF-редактор шаблонов колонок
