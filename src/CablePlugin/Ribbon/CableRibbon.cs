using System;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.Windows;
using AdW = Autodesk.Windows;

namespace CablePlugin.Ribbon
{
    /// <summary>
    /// Вкладка ленты «Кабель» с командами плагина.
    /// </summary>
    public static class CableRibbon
    {
        private const string TabId = "CABLEPLUGIN_TAB";
        private static bool _built;

        public static void EnsureRibbon()
        {
            try
            {
                var ribbon = ComponentManager.Ribbon;
                if (ribbon == null) return;

                // Уже есть
                foreach (RibbonTab t in ribbon.Tabs)
                {
                    if (t.Id == TabId)
                    {
                        t.IsVisible = true;
                        return;
                    }
                }

                var tab = new RibbonTab
                {
                    Title = "Кабель",
                    Id = TabId,
                    IsActive = false
                };

                tab.Panels.Add(CreatePanel("Данные", new[]
                {
                    Btn("Привязать", "CABLEATTACH", "Данные кабеля на полилинии"),
                    Btn("Изменить", "CABLEEDIT", "Полный диалог линии"),
                    Btn("Свойства", "CABLEPROPS", "Палитра свойств"),
                    Btn("Инфо", "CABLEINFO", "Просмотр в командной строке"),
                    Btn("Пересчёт", "CABLERECALC", "Пересчитать длины и электрику"),
                    Btn("Удалить", "CABLEREMOVE", "Снять данные с объектов")
                }));

                tab.Panels.Add(CreatePanel("Справочники", new[]
                {
                    Btn("Трубы", "CABLEDICT", "Таблица ∅ труб/гофры"),
                    Btn("Шаблоны", "CABLETEMPLATE", "Шаблоны отчётов"),
                    Btn("Библиотека", "CABLELIB", "Блоки из DWG")
                }));

                tab.Panels.Add(CreatePanel("Отчёты", new[]
                {
                    Btn("Excel линий", "CABLEEXPORT", "Экспорт линий"),
                    Btn("Журнал", "CABLEJOURNAL", "Кабельный журнал"),
                    Btn("Ведомость", "CABLEBOM", "Ведомость объёмов"),
                    Btn("Щит", "CABLEPANEL", "Таблица щита"),
                    Btn("Схема", "CABLESCHEME", "Однолинейка блоками")
                }));

                ribbon.Tabs.Add(tab);
                _built = true;
            }
            catch (System.Exception ex)
            {
                try
                {
                    Application.DocumentManager.MdiActiveDocument?.Editor
                        .WriteMessage("\n[CablePlugin] Лента: " + ex.Message);
                }
                catch { /* ignore */ }
            }
        }

        private static RibbonPanel CreatePanel(string title, RibbonButton[] buttons)
        {
            var source = new RibbonPanelSource { Title = title };
            var row = new RibbonRowPanel();
            foreach (var b in buttons)
                row.Items.Add(b);
            source.Items.Add(row);
            return new RibbonPanel { Source = source };
        }

        private static RibbonButton Btn(string text, string command, string tooltip)
        {
            return new RibbonButton
            {
                Text = text,
                ShowText = true,
                Size = RibbonItemSize.Large,
                Orientation = System.Windows.Controls.Orientation.Vertical,
                CommandHandler = new RibbonCommandHandler(command),
                ToolTip = tooltip
            };
        }

        private sealed class RibbonCommandHandler : System.Windows.Input.ICommand
        {
            private readonly string _command;

            public RibbonCommandHandler(string command) => _command = command;

            public bool CanExecute(object parameter) => true;

            public event EventHandler CanExecuteChanged
            {
                add { }
                remove { }
            }

            public void Execute(object parameter)
            {
                var doc = Application.DocumentManager.MdiActiveDocument;
                if (doc == null) return;
                // Пробел в конце — выполнить команду
                doc.SendStringToExecute(_command + " ", true, false, true);
            }
        }
    }
}
