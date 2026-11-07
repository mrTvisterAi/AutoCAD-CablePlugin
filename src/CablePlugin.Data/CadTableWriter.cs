using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using CablePlugin.Core.Reports;
using CablePlugin.Core.Templates;

namespace CablePlugin.Data
{
    /// <summary>
    /// Вставка таблиц отчётов в чертёж AutoCAD (с учётом шаблонов колонок).
    /// </summary>
    public class CadTableWriter
    {
        public ObjectId InsertJournalTable(
            Database db,
            Transaction tr,
            BlockTableRecord space,
            Point3d insertionPoint,
            IList<JournalRow> rows,
            TableTemplate template = null)
        {
            template = template ?? DefaultTemplates.Journal();
            var cols = template.VisibleColumns.ToList();
            if (cols.Count == 0) return ObjectId.Null;

            int dataRows = rows?.Count ?? 0;
            var table = CreateTable(cols, dataRows + 1, insertionPoint, template.RowHeight);

            for (int c = 0; c < cols.Count; c++)
                table.Cells[0, c].TextString = cols[c].Title ?? cols[c].Id;

            for (int i = 0; i < dataRows; i++)
            {
                for (int c = 0; c < cols.Count; c++)
                {
                    var text = TemplateCellResolver.Journal(cols[c].Id, rows[i]);
                    table.Cells[i + 1, c].TextString = Truncate(text, 80);
                }
            }

            StyleHeader(table, cols.Count);
            space.AppendEntity(table);
            tr.AddNewlyCreatedDBObject(table, true);
            return table.ObjectId;
        }

        public ObjectId InsertBomTable(
            Database db,
            Transaction tr,
            BlockTableRecord space,
            Point3d insertionPoint,
            BillOfMaterials bom,
            TableTemplate template = null)
        {
            template = template ?? DefaultTemplates.Bom();
            var cols = template.VisibleColumns.ToList();
            var cables = bom?.Cables ?? new List<BomCableRow>();

            var table = CreateTable(cols, cables.Count + 1, insertionPoint, template.RowHeight);
            for (int c = 0; c < cols.Count; c++)
                table.Cells[0, c].TextString = cols[c].Title ?? cols[c].Id;

            for (int i = 0; i < cables.Count; i++)
            {
                for (int c = 0; c < cols.Count; c++)
                    table.Cells[i + 1, c].TextString = TemplateCellResolver.BomCable(cols[c].Id, cables[i]);
            }

            StyleHeader(table, cols.Count);
            space.AppendEntity(table);
            tr.AddNewlyCreatedDBObject(table, true);

            // Вторая таблица — трубы
            var pipes = bom?.Pipes ?? new List<BomPipeRow>();
            if (pipes.Count > 0)
            {
                var pt2 = new Point3d(
                    insertionPoint.X,
                    insertionPoint.Y - (cables.Count + 2) * (template.RowHeight > 0 ? template.RowHeight : 8) - 5,
                    insertionPoint.Z);

                var pipeHeaders = new[] { "Способ", "∅, мм", "Σ L, м", "Участков" };
                var widths = new[] { 28.0, 14.0, 16.0, 14.0 };
                var t2 = CreateTableFixed(pipeHeaders.Length, pipes.Count + 1, pt2, widths, template.RowHeight);
                for (int c = 0; c < pipeHeaders.Length; c++)
                    t2.Cells[0, c].TextString = pipeHeaders[c];

                for (int i = 0; i < pipes.Count; i++)
                {
                    var p = pipes[i];
                    t2.Cells[i + 1, 0].TextString = p.LayingMethod ?? "";
                    t2.Cells[i + 1, 1].TextString = p.Diameter?.ToString("0.##") ?? "";
                    t2.Cells[i + 1, 2].TextString = p.TotalLength.ToString("0.###");
                    t2.Cells[i + 1, 3].TextString = p.SegmentCount.ToString();
                }

                StyleHeader(t2, pipeHeaders.Length);
                space.AppendEntity(t2);
                tr.AddNewlyCreatedDBObject(t2, true);
            }

            return table.ObjectId;
        }

        public ObjectId InsertPanelTable(
            Database db,
            Transaction tr,
            BlockTableRecord space,
            Point3d insertionPoint,
            PanelReport panel,
            TableTemplate template = null)
        {
            template = template ?? DefaultTemplates.Panel();
            var cols = template.VisibleColumns.ToList();
            var feeders = panel?.Feeders ?? new List<PanelFeederRow>();

            var table = CreateTable(cols, feeders.Count + 1, insertionPoint, template.RowHeight);
            for (int c = 0; c < cols.Count; c++)
                table.Cells[0, c].TextString = cols[c].Title ?? cols[c].Id;

            for (int i = 0; i < feeders.Count; i++)
            {
                for (int c = 0; c < cols.Count; c++)
                    table.Cells[i + 1, c].TextString = TemplateCellResolver.Panel(cols[c].Id, feeders[i]);
            }

            StyleHeader(table, cols.Count);
            space.AppendEntity(table);
            tr.AddNewlyCreatedDBObject(table, true);

            var title = new DBText
            {
                Position = new Point3d(insertionPoint.X, insertionPoint.Y + 5, insertionPoint.Z),
                Height = template.TextHeight > 0 ? template.TextHeight : 2.5,
                TextString = $"Щит {panel?.PanelName}  |  Kс={panel?.DemandFactor:0.###}  |  ΣI={panel?.SumCurrent:0.##}А → Iспрос={panel?.DemandCurrent:0.##}А"
            };
            space.AppendEntity(title);
            tr.AddNewlyCreatedDBObject(title, true);

            return table.ObjectId;
        }

        private static Table CreateTable(List<TemplateColumn> cols, int rows, Point3d pt, double rowHeight)
        {
            var table = new Table();
            table.TableStyle = HostApplicationServices.WorkingDatabase.Tablestyle;
            table.SetSize(rows, cols.Count);
            table.SetRowHeight(rowHeight > 0 ? rowHeight : 8);
            table.Position = pt;

            for (int c = 0; c < cols.Count; c++)
                table.Columns[c].Width = cols[c].Width > 0 ? cols[c].Width : 18;

            return table;
        }

        private static Table CreateTableFixed(int colCount, int rows, Point3d pt, double[] widths, double rowHeight)
        {
            var table = new Table();
            table.TableStyle = HostApplicationServices.WorkingDatabase.Tablestyle;
            table.SetSize(rows, colCount);
            table.SetRowHeight(rowHeight > 0 ? rowHeight : 8);
            table.Position = pt;
            for (int c = 0; c < colCount; c++)
                table.Columns[c].Width = widths != null && c < widths.Length ? widths[c] : 18;
            return table;
        }

        private static void StyleHeader(Table table, int cols)
        {
            for (int c = 0; c < cols; c++)
                table.Cells[0, c].TextStyleId = HostApplicationServices.WorkingDatabase.Textstyle;
        }

        private static string Truncate(string s, int max)
        {
            if (string.IsNullOrEmpty(s) || s.Length <= max) return s ?? "";
            return s.Substring(0, max - 1) + "…";
        }
    }
}
