using System;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using CablePlugin.Core.Reports;

namespace CablePlugin.Data
{
    /// <summary>
    /// Вставка таблиц отчётов в чертёж AutoCAD.
    /// </summary>
    public class CadTableWriter
    {
        public ObjectId InsertJournalTable(
            Database db,
            Transaction tr,
            BlockTableRecord space,
            Point3d insertionPoint,
            IList<JournalRow> rows)
        {
            var headers = new[]
            {
                "№", "Группа", "От", "До", "Марка", "n×S", "L, м", "Прокладка", "I, А", "ΔU %", "АВ"
            };

            int dataRows = rows?.Count ?? 0;
            var table = CreateTable(headers.Length, dataRows + 1, insertionPoint, 18, 8);

            for (int c = 0; c < headers.Length; c++)
                table.Cells[0, c].TextString = headers[c];

            for (int i = 0; i < dataRows; i++)
            {
                var r = rows[i];
                int row = i + 1;
                table.Cells[row, 0].TextString = r.Number.ToString();
                table.Cells[row, 1].TextString = r.GroupName ?? "";
                table.Cells[row, 2].TextString = r.From ?? "";
                table.Cells[row, 3].TextString = r.To ?? "";
                table.Cells[row, 4].TextString = r.CableMark ?? "";
                table.Cells[row, 5].TextString = $"{r.Cores}х{r.Section}";
                table.Cells[row, 6].TextString = r.Length.ToString("0.###");
                table.Cells[row, 7].TextString = Truncate(r.LayingMethods, 40);
                table.Cells[row, 8].TextString = r.Current.ToString("0.##");
                table.Cells[row, 9].TextString = r.VoltageDropPercent.ToString("0.##");
                table.Cells[row, 10].TextString = r.Breaker ?? "";
            }

            StyleHeader(table, headers.Length);
            space.AppendEntity(table);
            tr.AddNewlyCreatedDBObject(table, true);
            return table.ObjectId;
        }

        public ObjectId InsertBomTable(
            Database db,
            Transaction tr,
            BlockTableRecord space,
            Point3d insertionPoint,
            BillOfMaterials bom)
        {
            var cables = bom?.Cables ?? new List<BomCableRow>();
            var pipes = bom?.Pipes ?? new List<BomPipeRow>();

            // Одна таблица: сначала кабели, пустая строка-разделитель логически через заголовок второй секции
            int totalRows = 1 + cables.Count + 2 + pipes.Count; // header cables + data + header pipes label + header pipes + data
            // Упростим: две таблицы друг под другом
            var id1 = InsertSimpleTable(space, tr, insertionPoint,
                new[] { "Марка", "Жилы", "Сечение", "Линий", "Σ L, м" },
                (row, col) =>
                {
                    if (row == 0) return null; // header already set
                    var c = cables[row - 1];
                    switch (col)
                    {
                        case 0: return c.Mark;
                        case 1: return c.Cores.ToString();
                        case 2: return c.Section.ToString("0.###");
                        case 3: return c.LineCount.ToString();
                        case 4: return c.TotalLength.ToString("0.###");
                        default: return "";
                    }
                },
                cables.Count);

            var pt2 = new Point3d(insertionPoint.X, insertionPoint.Y - (cables.Count + 2) * 8 - 5, insertionPoint.Z);
            InsertSimpleTable(space, tr, pt2,
                new[] { "Способ", "∅, мм", "Σ L, м", "Участков" },
                (row, col) =>
                {
                    if (row == 0) return null;
                    var p = pipes[row - 1];
                    switch (col)
                    {
                        case 0: return p.LayingMethod;
                        case 1: return p.Diameter?.ToString("0.##") ?? "";
                        case 2: return p.TotalLength.ToString("0.###");
                        case 3: return p.SegmentCount.ToString();
                        default: return "";
                    }
                },
                pipes.Count);

            return id1;
        }

        public ObjectId InsertPanelTable(
            Database db,
            Transaction tr,
            BlockTableRecord space,
            Point3d insertionPoint,
            PanelReport panel)
        {
            var feeders = panel?.Feeders ?? new List<PanelFeederRow>();
            // +2 строки под заголовок щита и итоги — делаем обычную таблицу фидеров + текст итогов отдельно через ячейки title

            var headers = new[]
            {
                "№", "Обозн.", "Куда", "Кабель", "L, м", "I, А", "АВ", "ΔU %"
            };

            var table = CreateTable(headers.Length, feeders.Count + 1, insertionPoint, 20, 8);
            for (int c = 0; c < headers.Length; c++)
                table.Cells[0, c].TextString = headers[c];

            for (int i = 0; i < feeders.Count; i++)
            {
                var f = feeders[i];
                int row = i + 1;
                table.Cells[row, 0].TextString = f.Number.ToString();
                table.Cells[row, 1].TextString = f.Designation ?? "";
                table.Cells[row, 2].TextString = f.To ?? "";
                table.Cells[row, 3].TextString = $"{f.CableMark} {f.Cores}х{f.Section}";
                table.Cells[row, 4].TextString = f.Length.ToString("0.###");
                table.Cells[row, 5].TextString = f.Current.ToString("0.##");
                table.Cells[row, 6].TextString = f.Breaker ?? "";
                table.Cells[row, 7].TextString = f.VoltageDropPercent.ToString("0.##");
            }

            StyleHeader(table, headers.Length);
            space.AppendEntity(table);
            tr.AddNewlyCreatedDBObject(table, true);

            // Подпись щита над таблицей
            var title = new DBText
            {
                Position = new Point3d(insertionPoint.X, insertionPoint.Y + 5, insertionPoint.Z),
                Height = 2.5,
                TextString = $"Щит {panel.PanelName}  |  Kс={panel.DemandFactor:0.###}  |  ΣI={panel.SumCurrent:0.##}А → Iспрос={panel.DemandCurrent:0.##}А"
            };
            space.AppendEntity(title);
            tr.AddNewlyCreatedDBObject(title, true);

            return table.ObjectId;
        }

        private ObjectId InsertSimpleTable(
            BlockTableRecord space,
            Transaction tr,
            Point3d pt,
            string[] headers,
            Func<int, int, string> cellText,
            int dataRowCount)
        {
            var table = CreateTable(headers.Length, dataRowCount + 1, pt, 22, 8);
            for (int c = 0; c < headers.Length; c++)
                table.Cells[0, c].TextString = headers[c];

            for (int r = 1; r <= dataRowCount; r++)
            {
                for (int c = 0; c < headers.Length; c++)
                {
                    var text = cellText(r, c);
                    if (text != null)
                        table.Cells[r, c].TextString = text;
                }
            }

            StyleHeader(table, headers.Length);
            space.AppendEntity(table);
            tr.AddNewlyCreatedDBObject(table, true);
            return table.ObjectId;
        }

        private static Table CreateTable(int cols, int rows, Point3d pt, double colWidth, double rowHeight)
        {
            var table = new Table();
            table.TableStyle = HostApplicationServices.WorkingDatabase.Tablestyle;
            table.SetSize(rows, cols);
            table.SetRowHeight(rowHeight);
            table.SetColumnWidth(colWidth);
            table.Position = pt;
            return table;
        }

        private static void StyleHeader(Table table, int cols)
        {
            for (int c = 0; c < cols; c++)
            {
                table.Cells[0, c].TextStyleId = HostApplicationServices.WorkingDatabase.Textstyle;
            }
        }

        private static string Truncate(string s, int max)
        {
            if (string.IsNullOrEmpty(s) || s.Length <= max) return s ?? "";
            return s.Substring(0, max - 1) + "…";
        }
    }
}
