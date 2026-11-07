# AutoCAD Cable Plugin

Плагин AutoCAD (.NET / C#) для кабельных линий: данные на полилиниях, расчёты, отчёты, однолинейка, **палитра свойств**.

## Команды

| Группа | Команды |
|--------|---------|
| Данные | `CABLEATTACH` `CABLEEDIT` `CABLEDICT` `CABLEEXPORT` `CABLEINFO` `CABLERECALC` `CABLEREMOVE` |
| Отчёты | `CABLEJOURNAL` `CABLEBOM` `CABLEPANEL` |
| Схема | `CABLESCHEME` `CABLETEMPLATE` `CABLELIB` |
| Свойства | **`CABLEPROPS`** — палитра при выделении полилинии |

## Палитра свойств (`CABLEPROPS`)

Док-панель **«Свойства кабеля»**:

1. Выполните `CABLEPROPS` (панель можно пристыковать слева/справа).
2. Выделите полилинию — подтянутся группа, от/до, марка, длины, электрика, участки.
3. Отредактируйте поля → **Применить** (запись в Extension Dictionary объекта).
4. **Полный…** — открывает диалог `CABLEEDIT`.
5. Несохранённые правки не сбрасываются при смене выделения, пока не нажмёте Применить или не смените выбор после сохранения.

Если на объекте ещё нет данных кабеля, палитра показывает черновик — заполните и сохраните **Применить**.

## Библиотека блоков (`CABLELIB`)

Свои условные обозначения из DWG: Путь → Имена → Импорт → `CABLESCHEME`.

## Сборка

```bash
dotnet build AutoCAD-CablePlugin.sln -c Release -p:AutoCADDir="C:\Program Files\Autodesk\AutoCAD 2025"
dotnet test tests/CablePlugin.Tests/CablePlugin.Tests.csproj
```

## Статус

- [x] Данные, расчёты, отчёты, однолинейка, шаблоны, библиотека блоков
- [x] Палитра свойств при выделении (`CABLEPROPS`)
- [ ] Интеграция в стандартный Properties Palette (OPM / IDynamicProperty)
