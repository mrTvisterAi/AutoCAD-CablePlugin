using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Runtime;
using CablePlugin.Core.Models;
using CablePlugin.Core.Reports;
using CablePlugin.Core.Services;
using CablePlugin.Data;
using AcApp = Autodesk.AutoCAD.ApplicationServices.Core.Application;

namespace CablePlugin.Commands
{
    /// <summary>
    /// Команды отчётов (этап 4).
    /// </summary>
    public class ReportCommands
    {
        private readonly CableLineCollector _collector = new CableLineCollector();
        private readonly ReportExcelExportService _excel = new ReportExcelExportService();
        private readonly CadTableWriter _tables = new CadTableWriter();

        /// <summary>
        /// Кабельный журнал → Excel и/или таблица в чертеже.
        /// </summary>
        [CommandMethod("CABLEJOURNAL", CommandFlags.Modal)]
        public void CableJournal()
        {
            var doc = AcApp.DocumentManager.MdiActiveDocument;
            if (doc == null) return;
            var ed = doc.Editor;
            var db = doc.Database;

            var lines = LoadAllLines(db, ed);
            if (lines == null || lines.Count == 0) return;

            var rows = ReportBuilder.BuildJournal(lines);
            var mode = AskOutputMode(ed, "журнала");
            if (mode == OutputMode.Cancel) return;

            if (mode == OutputMode.Excel || mode == OutputMode.Both)
            {
                var path = AskSavePath(ed, "Кабельный_журнал.xlsx", "Кабельный журнал");
                if (path != null)
                {
                    try
                    {
                        _excel.ExportJournal(rows, path);
                        ed.WriteMessage($"\n[CablePlugin] Журнал Excel: {path}");
                    }
                    catch (System.Exception ex)
                    {
                        ed.WriteMessage($"\n[CablePlugin] Ошибка Excel: {ex.Message}");
                    }
                }
            }

            if (mode == OutputMode.Drawing || mode == OutputMode.Both)
            {
                InsertJournalInDrawing(doc, db, ed, rows);
            }

            ed.WriteMessage($"\n[CablePlugin] Строк журнала: {rows.Count}");
        }

        /// <summary>
        /// Ведомость объёмов (кабели + трубы/гофра).
        /// </summary>
        [CommandMethod("CABLEBOM", CommandFlags.Modal)]
        public void CableBom()
        {
            var doc = AcApp.DocumentManager.MdiActiveDocument;
            if (doc == null) return;
            var ed = doc.Editor;
            var db = doc.Database;

            var lines = LoadAllLines(db, ed);
            if (lines == null || lines.Count == 0) return;

            var bom = ReportBuilder.BuildBom(lines);
            var mode = AskOutputMode(ed, "ведомости");
            if (mode == OutputMode.Cancel) return;

            if (mode == OutputMode.Excel || mode == OutputMode.Both)
            {
                var path = AskSavePath(ed, "Ведомость_кабеля.xlsx", "Ведомость объёмов");
                if (path != null)
                {
                    try
                    {
                        _excel.ExportBom(bom, path);
                        ed.WriteMessage($"\n[CablePlugin] Ведомость Excel: {path}");
                    }
                    catch (System.Exception ex)
                    {
                        ed.WriteMessage($"\n[CablePlugin] Ошибка Excel: {ex.Message}");
                    }
                }
            }

            if (mode == OutputMode.Drawing || mode == OutputMode.Both)
            {
                InsertBomInDrawing(doc, db, ed, bom);
            }

            ed.WriteMessage($"\n[CablePlugin] Позиций кабеля: {bom.Cables.Count}, труб/гофры: {bom.Pipes.Count}");
        }

        /// <summary>
        /// Таблица отходящих линий щита с коэффициентом спроса.
        /// </summary>
        [CommandMethod("CABLEPANEL", CommandFlags.Modal)]
        public void CablePanel()
        {
            var doc = AcApp.DocumentManager.MdiActiveDocument;
            if (doc == null) return;
            var ed = doc.Editor;
            var db = doc.Database;

            var lines = LoadAllLines(db, ed);
            if (lines == null || lines.Count == 0) return;

            var panels = ReportBuilder.ListPanels(lines);
            if (panels.Count == 0)
            {
                ed.WriteMessage("\n[CablePlugin] Нет заполненных полей «От» (имя щита). Укажите в CABLEEDIT.");
                return;
            }

            ed.WriteMessage("\nДоступные щиты: " + string.Join(", ", panels));

            var pso = new PromptStringOptions("\nИмя щита (поле «От»)")
            {
                AllowSpaces = true,
                DefaultValue = panels[0],
                UseDefaultValue = true
            };
            var psr = ed.GetString(pso);
            if (psr.Status != PromptStatus.OK) return;
            var panelName = string.IsNullOrWhiteSpace(psr.StringResult) ? panels[0] : psr.StringResult.Trim();

            var pdo = new PromptDoubleOptions("\nКоэффициент спроса")
            {
                AllowNegative = false,
                AllowZero = false,
                DefaultValue = 1.0,
                UseDefaultValue = true
            };
            var pdr = ed.GetDouble(pdo);
            if (pdr.Status == PromptStatus.Cancel) return;
            double ks = pdr.Status == PromptStatus.OK ? pdr.Value : 1.0;

            var report = ReportBuilder.BuildPanel(lines, panelName, ks);
            if (report.Feeders.Count == 0)
            {
                ed.WriteMessage($"\n[CablePlugin] Нет линий от щита «{panelName}».");
                return;
            }

            var mode = AskOutputMode(ed, "щита");
            if (mode == OutputMode.Cancel) return;

            if (mode == OutputMode.Excel || mode == OutputMode.Both)
            {
                var path = AskSavePath(ed, $"Щит_{panelName}.xlsx", "Схема щита");
                if (path != null)
                {
                    try
                    {
                        _excel.ExportPanel(report, path);
                        ed.WriteMessage($"\n[CablePlugin] Щит Excel: {path}");
                    }
                    catch (System.Exception ex)
                    {
                        ed.WriteMessage($"\n[CablePlugin] Ошибка Excel: {ex.Message}");
                    }
                }
            }

            if (mode == OutputMode.Drawing || mode == OutputMode.Both)
            {
                InsertPanelInDrawing(doc, db, ed, report);
            }

            ed.WriteMessage(
                $"\n[CablePlugin] Щит {report.PanelName}: фидеров={report.Feeders.Count}, " +
                $"ΣI={report.SumCurrent:0.##}А, Iспрос={report.DemandCurrent:0.##}А");
        }

        // ===== helpers =====

        private List<CableLine> LoadAllLines(Database db, Editor ed)
        {
            using (var tr = db.TransactionManager.StartTransaction())
            {
                var list = _collector.CollectFromModelSpace(db, tr).Select(x => x.Line).ToList();
                tr.Commit();
                if (list.Count == 0)
                    ed.WriteMessage("\n[CablePlugin] В модели нет кабельных линий.");
                return list;
            }
        }

        private enum OutputMode { Cancel, Excel, Drawing, Both }

        private static OutputMode AskOutputMode(Editor ed, string what)
        {
            var opts = new PromptKeywordOptions($"\nВывод {what} [Excel/Чертёж/Оба]")
            {
                AllowNone = true
            };
            opts.Keywords.Add("Excel");
            opts.Keywords.Add("Чертёж");
            opts.Keywords.Add("Оба");
            opts.Keywords.Default = "Excel";

            var res = ed.GetKeywords(opts);
            if (res.Status == PromptStatus.Cancel) return OutputMode.Cancel;
            if (res.Status != PromptStatus.OK && res.Status != PromptStatus.None)
                return OutputMode.Excel;

            var k = res.StringResult ?? "Excel";
            if (k.StartsWith("Ч", StringComparison.OrdinalIgnoreCase) || k.Equals("Drawing", StringComparison.OrdinalIgnoreCase))
                return OutputMode.Drawing;
            if (k.StartsWith("О", StringComparison.OrdinalIgnoreCase) || k.Equals("Both", StringComparison.OrdinalIgnoreCase))
                return OutputMode.Both;
            return OutputMode.Excel;
        }

        private static string AskSavePath(Editor ed, string defaultName, string title)
        {
            try
            {
                var sfd = new Microsoft.Win32.SaveFileDialog
                {
                    Title = title,
                    Filter = "Excel (*.xlsx)|*.xlsx",
                    DefaultExt = ".xlsx",
                    FileName = defaultName,
                    AddExtension = true
                };
                if (sfd.ShowDialog() == true)
                    return sfd.FileName;
                return null;
            }
            catch
            {
                var opts = new PromptSaveFileOptions("\nФайл Excel")
                {
                    DialogCaption = title,
                    Filter = "Excel (*.xlsx)|*.xlsx"
                };
                var pfr = ed.GetFileNameForSave(opts);
                if (pfr.Status != PromptStatus.OK) return null;
                var path = pfr.StringResult;
                if (!path.EndsWith(".xlsx", StringComparison.OrdinalIgnoreCase))
                    path += ".xlsx";
                return path;
            }
        }

        private void InsertJournalInDrawing(
            Autodesk.AutoCAD.ApplicationServices.Document doc,
            Database db, Editor ed, IList<JournalRow> rows)
        {
            var ppr = ed.GetPoint("\nТочка вставки таблицы журнала: ");
            if (ppr.Status != PromptStatus.OK) return;

            using (doc.LockDocument())
            using (var tr = db.TransactionManager.StartTransaction())
            {
                var bt = (BlockTable)tr.GetObject(db.BlockTableId, OpenMode.ForRead);
                var space = (BlockTableRecord)tr.GetObject(db.CurrentSpaceId, OpenMode.ForWrite);
                _tables.InsertJournalTable(db, tr, space, ppr.Value, rows);
                tr.Commit();
            }
            ed.WriteMessage("\n[CablePlugin] Таблица журнала вставлена в чертёж.");
        }

        private void InsertBomInDrawing(
            Autodesk.AutoCAD.ApplicationServices.Document doc,
            Database db, Editor ed, BillOfMaterials bom)
        {
            var ppr = ed.GetPoint("\nТочка вставки ведомости: ");
            if (ppr.Status != PromptStatus.OK) return;

            using (doc.LockDocument())
            using (var tr = db.TransactionManager.StartTransaction())
            {
                var space = (BlockTableRecord)tr.GetObject(db.CurrentSpaceId, OpenMode.ForWrite);
                _tables.InsertBomTable(db, tr, space, ppr.Value, bom);
                tr.Commit();
            }
            ed.WriteMessage("\n[CablePlugin] Ведомость вставлена в чертёж.");
        }

        private void InsertPanelInDrawing(
            Autodesk.AutoCAD.ApplicationServices.Document doc,
            Database db, Editor ed, PanelReport report)
        {
            var ppr = ed.GetPoint("\nТочка вставки таблицы щита: ");
            if (ppr.Status != PromptStatus.OK) return;

            using (doc.LockDocument())
            using (var tr = db.TransactionManager.StartTransaction())
            {
                var space = (BlockTableRecord)tr.GetObject(db.CurrentSpaceId, OpenMode.ForWrite);
                _tables.InsertPanelTable(db, tr, space, ppr.Value, report);
                tr.Commit();
            }
            ed.WriteMessage("\n[CablePlugin] Таблица щита вставлена в чертёж.");
        }
    }
}
