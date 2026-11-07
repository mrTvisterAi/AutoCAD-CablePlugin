using System;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using CablePlugin.Core.Reports;
using CablePlugin.Core.Templates;

namespace CablePlugin.Data
{
    /// <summary>
    /// Создание блоков однолинейки и вставка схемы щита.
    /// </summary>
    public class PanelSchemeBuilder
    {
        /// <summary>
        /// Гарантирует наличие блоков QF / BUS / LOAD в чертеже.
        /// </summary>
        public void EnsureBlocks(Database db, Transaction tr, PanelBlockTemplate template)
        {
            if (template == null) template = new PanelBlockTemplate();
            EnsureBreakerBlock(db, tr, template.BreakerBlockName);
            EnsureBusBlock(db, tr, template.BusBlockName);
            EnsureLoadBlock(db, tr, template.LoadBlockName);
        }

        /// <summary>
        /// Вставить однолинейную схему от точки вставки (шина сверху, фидеры вниз).
        /// </summary>
        public void InsertScheme(
            Database db,
            Transaction tr,
            BlockTableRecord space,
            Point3d origin,
            PanelReport panel,
            PanelBlockTemplate template)
        {
            if (panel == null || panel.Feeders == null || panel.Feeders.Count == 0)
                return;

            if (template == null) template = new PanelBlockTemplate();
            EnsureBlocks(db, tr, template);

            var bt = (BlockTable)tr.GetObject(db.BlockTableId, OpenMode.ForRead);
            int count = panel.Feeders.Count;
            double spacing = template.FeederSpacing > 0 ? template.FeederSpacing : 30;
            double span = template.VerticalSpan > 0 ? template.VerticalSpan : 40;
            double totalWidth = (count - 1) * spacing;

            // Шинопровод
            var busId = bt[template.BusBlockName];
            var busPos = new Point3d(origin.X + totalWidth / 2.0, origin.Y, origin.Z);
            InsertBlock(space, tr, busId, busPos, 1.0);

            // Заголовок
            var title = new DBText
            {
                Position = new Point3d(origin.X, origin.Y + 8, origin.Z),
                Height = template.TextHeight > 0 ? template.TextHeight * 1.2 : 2.5,
                TextString = $"Щит {panel.PanelName}  Kс={panel.DemandFactor:0.###}  ΣI={panel.SumCurrent:0.##}А → {panel.DemandCurrent:0.##}А"
            };
            space.AppendEntity(title);
            tr.AddNewlyCreatedDBObject(title, true);

            // Горизонтальная линия шины
            if (template.DrawWireLines && count > 1)
            {
                var busLine = new Line(
                    new Point3d(origin.X, origin.Y, origin.Z),
                    new Point3d(origin.X + totalWidth, origin.Y, origin.Z));
                space.AppendEntity(busLine);
                tr.AddNewlyCreatedDBObject(busLine, true);
            }

            var qfId = bt[template.BreakerBlockName];
            var loadId = bt[template.LoadBlockName];

            for (int i = 0; i < count; i++)
            {
                var f = panel.Feeders[i];
                double x = origin.X + i * spacing;
                var top = new Point3d(x, origin.Y, origin.Z);
                var mid = new Point3d(x, origin.Y - span * 0.45, origin.Z);
                var bot = new Point3d(x, origin.Y - span, origin.Z);

                if (template.DrawWireLines)
                {
                    var wire = new Line(top, bot);
                    space.AppendEntity(wire);
                    tr.AddNewlyCreatedDBObject(wire, true);
                }

                InsertBlock(space, tr, qfId, mid, 1.0);
                InsertBlock(space, tr, loadId, bot, 1.0);

                double th = template.TextHeight > 0 ? template.TextHeight : 2.0;

                if (template.LabelBreaker)
                {
                    AddText(space, tr,
                        new Point3d(x + 2, mid.Y + 1, origin.Z),
                        th,
                        string.IsNullOrWhiteSpace(f.Breaker) ? $"QF{f.Number}" : $"QF{f.Number} {f.Breaker}А");
                }

                if (template.LabelCable)
                {
                    AddText(space, tr,
                        new Point3d(x + 2, mid.Y - 4, origin.Z),
                        th * 0.9,
                        $"{f.CableMark} {f.Cores}х{f.Section}");
                }

                if (template.LabelDestination)
                {
                    var dest = string.IsNullOrWhiteSpace(f.To) ? f.Designation : f.To;
                    AddText(space, tr,
                        new Point3d(x + 2, bot.Y - 4, origin.Z),
                        th,
                        dest ?? "");
                }
            }
        }

        private static void InsertBlock(BlockTableRecord space, Transaction tr, ObjectId blockDefId, Point3d pos, double scale)
        {
            var br = new BlockReference(pos, blockDefId);
            br.ScaleFactors = new Scale3d(scale);
            space.AppendEntity(br);
            tr.AddNewlyCreatedDBObject(br, true);
        }

        private static void AddText(BlockTableRecord space, Transaction tr, Point3d pos, double height, string text)
        {
            var t = new DBText
            {
                Position = pos,
                Height = height,
                TextString = text ?? string.Empty
            };
            space.AppendEntity(t);
            tr.AddNewlyCreatedDBObject(t, true);
        }

        private static void EnsureBreakerBlock(Database db, Transaction tr, string name)
        {
            if (string.IsNullOrWhiteSpace(name)) name = "CABLE_QF";
            if (BlockExists(db, tr, name)) return;

            var bt = (BlockTable)tr.GetObject(db.BlockTableId, OpenMode.ForWrite);
            var btr = new BlockTableRecord { Name = name };
            bt.Add(btr);
            tr.AddNewlyCreatedDBObject(btr, true);

            // Условное обозначение АВ: прямоугольник + разрыв линии
            var rect = new Polyline();
            rect.AddVertexAt(0, new Point2d(-2, -3), 0, 0, 0);
            rect.AddVertexAt(1, new Point2d(2, -3), 0, 0, 0);
            rect.AddVertexAt(2, new Point2d(2, 3), 0, 0, 0);
            rect.AddVertexAt(3, new Point2d(-2, 3), 0, 0, 0);
            rect.Closed = true;
            btr.AppendEntity(rect);
            tr.AddNewlyCreatedDBObject(rect, true);

            var slash = new Line(new Point3d(-1.5, -2, 0), new Point3d(1.5, 2, 0));
            btr.AppendEntity(slash);
            tr.AddNewlyCreatedDBObject(slash, true);
        }

        private static void EnsureBusBlock(Database db, Transaction tr, string name)
        {
            if (string.IsNullOrWhiteSpace(name)) name = "CABLE_BUS";
            if (BlockExists(db, tr, name)) return;

            var bt = (BlockTable)tr.GetObject(db.BlockTableId, OpenMode.ForWrite);
            var btr = new BlockTableRecord { Name = name };
            bt.Add(btr);
            tr.AddNewlyCreatedDBObject(btr, true);

            // Толстая короткая шина
            var pl = new Polyline();
            pl.AddVertexAt(0, new Point2d(-6, -0.8), 0, 1.6, 1.6);
            pl.AddVertexAt(1, new Point2d(6, -0.8), 0, 1.6, 1.6);
            btr.AppendEntity(pl);
            tr.AddNewlyCreatedDBObject(pl, true);
        }

        private static void EnsureLoadBlock(Database db, Transaction tr, string name)
        {
            if (string.IsNullOrWhiteSpace(name)) name = "CABLE_LOAD";
            if (BlockExists(db, tr, name)) return;

            var bt = (BlockTable)tr.GetObject(db.BlockTableId, OpenMode.ForWrite);
            var btr = new BlockTableRecord { Name = name };
            bt.Add(btr);
            tr.AddNewlyCreatedDBObject(btr, true);

            // Круг нагрузки
            var circle = new Circle(Point3d.Origin, Vector3d.ZAxis, 2.0);
            btr.AppendEntity(circle);
            tr.AddNewlyCreatedDBObject(circle, true);

            var cross1 = new Line(new Point3d(-1.2, -1.2, 0), new Point3d(1.2, 1.2, 0));
            var cross2 = new Line(new Point3d(-1.2, 1.2, 0), new Point3d(1.2, -1.2, 0));
            btr.AppendEntity(cross1);
            btr.AppendEntity(cross2);
            tr.AddNewlyCreatedDBObject(cross1, true);
            tr.AddNewlyCreatedDBObject(cross2, true);
        }

        private static bool BlockExists(Database db, Transaction tr, string name)
        {
            var bt = (BlockTable)tr.GetObject(db.BlockTableId, OpenMode.ForRead);
            return bt.Has(name);
        }
    }
}
