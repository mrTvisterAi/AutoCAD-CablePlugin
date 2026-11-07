using System;
using System.Collections.Generic;
using System.Linq;

namespace CablePlugin.Core.Templates
{
    /// <summary>Описание колонки таблицы отчёта.</summary>
    public class TemplateColumn
    {
        public string Id { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public double Width { get; set; } = 20;
        public bool Visible { get; set; } = true;
        public int Order { get; set; }

        public TemplateColumn Clone()
        {
            return new TemplateColumn
            {
                Id = Id,
                Title = Title,
                Width = Width,
                Visible = Visible,
                Order = Order
            };
        }
    }

    /// <summary>Шаблон табличного отчёта (журнал / ведомость / щит).</summary>
    public class TableTemplate
    {
        public string Name { get; set; } = "По умолчанию";
        public string Kind { get; set; } = "Journal"; // Journal | Bom | Panel
        public List<TemplateColumn> Columns { get; set; } = new List<TemplateColumn>();
        public double RowHeight { get; set; } = 8;
        public double TextHeight { get; set; } = 2.5;

        public IEnumerable<TemplateColumn> VisibleColumns =>
            Columns.Where(c => c.Visible).OrderBy(c => c.Order);

        public TableTemplate Clone()
        {
            return new TableTemplate
            {
                Name = Name,
                Kind = Kind,
                RowHeight = RowHeight,
                TextHeight = TextHeight,
                Columns = Columns.Select(c => c.Clone()).ToList()
            };
        }
    }

    /// <summary>Параметры однолинейной схемы щита (блоки).</summary>
    public class PanelBlockTemplate
    {
        public string Name { get; set; } = "Однолинейка по умолчанию";

        /// <summary>Имя блока автоматического выключателя.</summary>
        public string BreakerBlockName { get; set; } = "CABLE_QF";

        /// <summary>Имя блока шины/сборки.</summary>
        public string BusBlockName { get; set; } = "CABLE_BUS";

        /// <summary>Имя блока оконечной нагрузки (упрощённо).</summary>
        public string LoadBlockName { get; set; } = "CABLE_LOAD";

        public double FeederSpacing { get; set; } = 30;
        public double VerticalSpan { get; set; } = 40;
        public double TextHeight { get; set; } = 2.0;
        public bool DrawWireLines { get; set; } = true;
        public bool LabelCable { get; set; } = true;
        public bool LabelBreaker { get; set; } = true;
        public bool LabelDestination { get; set; } = true;

        public PanelBlockTemplate Clone()
        {
            return new PanelBlockTemplate
            {
                Name = Name,
                BreakerBlockName = BreakerBlockName,
                BusBlockName = BusBlockName,
                LoadBlockName = LoadBlockName,
                FeederSpacing = FeederSpacing,
                VerticalSpan = VerticalSpan,
                TextHeight = TextHeight,
                DrawWireLines = DrawWireLines,
                LabelCable = LabelCable,
                LabelBreaker = LabelBreaker,
                LabelDestination = LabelDestination
            };
        }
    }

    /// <summary>Набор шаблонов плагина.</summary>
    public class TemplateSet
    {
        public TableTemplate Journal { get; set; }
        public TableTemplate Bom { get; set; }
        public TableTemplate Panel { get; set; }
        public PanelBlockTemplate PanelBlocks { get; set; }

        public static TemplateSet CreateDefault()
        {
            return new TemplateSet
            {
                Journal = DefaultTemplates.Journal(),
                Bom = DefaultTemplates.Bom(),
                Panel = DefaultTemplates.Panel(),
                PanelBlocks = new PanelBlockTemplate()
            };
        }
    }

    public static class DefaultTemplates
    {
        public static TableTemplate Journal()
        {
            return new TableTemplate
            {
                Name = "Журнал ГОСТ (базовый)",
                Kind = "Journal",
                Columns = new List<TemplateColumn>
                {
                    Col("num", "№", 10, 0),
                    Col("group", "Группа", 22, 1),
                    Col("from", "От", 18, 2),
                    Col("to", "До", 22, 3),
                    Col("mark", "Марка", 28, 4),
                    Col("cores_section", "n×S", 14, 5),
                    Col("count", "Кол-во", 12, 6),
                    Col("length", "L, м", 14, 7),
                    Col("laying", "Прокладка", 36, 8),
                    Col("pipe", "∅", 12, 9),
                    Col("current", "I, А", 12, 10),
                    Col("du", "ΔU %", 12, 11),
                    Col("breaker", "АВ", 12, 12),
                    Col("note", "Примечание", 24, 13)
                }
            };
        }

        public static TableTemplate Bom()
        {
            return new TableTemplate
            {
                Name = "Ведомость (базовая)",
                Kind = "Bom",
                Columns = new List<TemplateColumn>
                {
                    Col("mark", "Марка", 30, 0),
                    Col("cores", "Жилы", 12, 1),
                    Col("section", "Сечение", 14, 2),
                    Col("lines", "Линий", 12, 3),
                    Col("length", "Σ L, м", 16, 4)
                }
            };
        }

        public static TableTemplate Panel()
        {
            return new TableTemplate
            {
                Name = "Щит (таблица)",
                Kind = "Panel",
                Columns = new List<TemplateColumn>
                {
                    Col("num", "№", 10, 0),
                    Col("designation", "Обозн.", 20, 1),
                    Col("to", "Куда", 22, 2),
                    Col("cable", "Кабель", 28, 3),
                    Col("length", "L, м", 14, 4),
                    Col("current", "I, А", 12, 5),
                    Col("breaker", "АВ", 12, 6),
                    Col("du", "ΔU %", 12, 7),
                    Col("power", "P, кВт", 14, 8)
                }
            };
        }

        private static TemplateColumn Col(string id, string title, double width, int order)
        {
            return new TemplateColumn
            {
                Id = id,
                Title = title,
                Width = width,
                Order = order,
                Visible = true
            };
        }
    }
}
