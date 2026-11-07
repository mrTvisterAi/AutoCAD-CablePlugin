using System;
using System.Linq;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Runtime;
using CablePlugin.Core.Reports;
using CablePlugin.Core.Templates;
using CablePlugin.Data;
using AcApp = Autodesk.AutoCAD.ApplicationServices.Core.Application;

namespace CablePlugin.Commands
{
    /// <summary>
    /// Однолинейная схема блоками и управление шаблонами.
    /// </summary>
    public class SchemeCommands
    {
        private readonly CableLineCollector _collector = new CableLineCollector();
        private readonly TemplateRepository _templates = new TemplateRepository();
        private readonly PanelSchemeBuilder _scheme = new PanelSchemeBuilder();

        /// <summary>
        /// Однолинейная схема щита блоками CABLE_QF / CABLE_BUS / CABLE_LOAD.
        /// </summary>
        [CommandMethod("CABLESCHEME", CommandFlags.Modal)]
        public void CableScheme()
        {
            var doc = AcApp.DocumentManager.MdiActiveDocument;
            if (doc == null) return;
            var ed = doc.Editor;
            var db = doc.Database;

            System.Collections.Generic.List<Core.Models.CableLine> lines;
            TemplateSet set;

            using (var tr = db.TransactionManager.StartTransaction())
            {
                lines = _collector.CollectFromModelSpace(db, tr).Select(x => x.Line).ToList();
                set = _templates.Load(db, tr);
                tr.Commit();
            }

            if (lines.Count == 0)
            {
                ed.WriteMessage("\n[CablePlugin] Нет кабельных линий.");
                return;
            }

            var panels = ReportBuilder.ListPanels(lines);
            if (panels.Count == 0)
            {
                ed.WriteMessage("\n[CablePlugin] Заполните поле «От» (имя щита) в CABLEEDIT.");
                return;
            }

            ed.WriteMessage("\nЩиты: " + string.Join(", ", panels));

            var pso = new PromptStringOptions("\nИмя щита")
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
                ed.WriteMessage($"\n[CablePlugin] Нет линий от «{panelName}».");
                return;
            }

            var ppr = ed.GetPoint("\nТочка вставки схемы (шина): ");
            if (ppr.Status != PromptStatus.OK) return;

            using (doc.LockDocument())
            using (var tr = db.TransactionManager.StartTransaction())
            {
                var space = (BlockTableRecord)tr.GetObject(db.CurrentSpaceId, OpenMode.ForWrite);
                var blockTpl = set.PanelBlocks ?? new PanelBlockTemplate();
                _scheme.InsertScheme(db, tr, space, ppr.Value, report, blockTpl);
                tr.Commit();
            }

            ed.WriteMessage(
                $"\n[CablePlugin] Схема щита {report.PanelName}: фидеров={report.Feeders.Count}, " +
                $"блоки {set.PanelBlocks?.BreakerBlockName ?? "CABLE_QF"} / {set.PanelBlocks?.BusBlockName ?? "CABLE_BUS"} / {set.PanelBlocks?.LoadBlockName ?? "CABLE_LOAD"}");
        }

        /// <summary>
        /// Управление шаблонами: сброс к умолчанию / вывод состава / сохранение в чертёж.
        /// </summary>
        [CommandMethod("CABLETEMPLATE", CommandFlags.Modal)]
        public void CableTemplate()
        {
            var doc = AcApp.DocumentManager.MdiActiveDocument;
            if (doc == null) return;
            var ed = doc.Editor;
            var db = doc.Database;

            var opts = new PromptKeywordOptions("\nШаблоны [Список/Сброс/Сохранить]")
            {
                AllowNone = true
            };
            opts.Keywords.Add("Список");
            opts.Keywords.Add("Сброс");
            opts.Keywords.Add("Сохранить");
            opts.Keywords.Default = "Список";

            var res = ed.GetKeywords(opts);
            if (res.Status == PromptStatus.Cancel) return;
            var key = res.Status == PromptStatus.OK ? res.StringResult : "Список";

            if (key.StartsWith("Сб", StringComparison.OrdinalIgnoreCase) || key.Equals("Reset", StringComparison.OrdinalIgnoreCase))
            {
                var def = TemplateSet.CreateDefault();
                using (doc.LockDocument())
                using (var tr = db.TransactionManager.StartTransaction())
                {
                    _templates.Save(db, tr, def);
                    tr.Commit();
                }
                ed.WriteMessage("\n[CablePlugin] Шаблоны сброшены к значениям по умолчанию и сохранены в чертёж.");
                PrintTemplateSummary(ed, def);
                return;
            }

            if (key.StartsWith("Со", StringComparison.OrdinalIgnoreCase) || key.Equals("Save", StringComparison.OrdinalIgnoreCase))
            {
                // Сохранить текущие (или default, если ещё не было)
                using (doc.LockDocument())
                using (var tr = db.TransactionManager.StartTransaction())
                {
                    var set = _templates.Load(db, tr);
                    _templates.Save(db, tr, set);
                    tr.Commit();
                    ed.WriteMessage("\n[CablePlugin] Текущий набор шаблонов записан в чертёж.");
                    PrintTemplateSummary(ed, set);
                }
                return;
            }

            // Список
            using (var tr = db.TransactionManager.StartTransaction())
            {
                var set = _templates.Load(db, tr);
                tr.Commit();
                PrintTemplateSummary(ed, set);
            }
        }

        private static void PrintTemplateSummary(Editor ed, TemplateSet set)
        {
            ed.WriteMessage("\n========== Шаблоны CablePlugin ==========");
            PrintTable(ed, "Журнал", set.Journal);
            PrintTable(ed, "Ведомость", set.Bom);
            PrintTable(ed, "Щит (таблица)", set.Panel);

            var b = set.PanelBlocks ?? new PanelBlockTemplate();
            ed.WriteMessage($"\nОднолинейка: QF={b.BreakerBlockName}, BUS={b.BusBlockName}, LOAD={b.LoadBlockName}");
            ed.WriteMessage($"\n  шаг фидеров={b.FeederSpacing}, высота={b.VerticalSpan}, подписи: АВ={b.LabelBreaker}, кабель={b.LabelCable}, куда={b.LabelDestination}");
            ed.WriteMessage("\n===========================================");
            ed.WriteMessage("\nПодсказка: отредактируйте JSON шаблона в Named Dictionary (GrokCableDictionaries/TemplateSet) или сбросьте через CABLETEMPLATE → Сброс.");
        }

        private static void PrintTable(Editor ed, string title, TableTemplate t)
        {
            if (t == null)
            {
                ed.WriteMessage($"\n{title}: (нет)");
                return;
            }

            ed.WriteMessage($"\n{title}: «{t.Name}»");
            foreach (var c in t.VisibleColumns)
                ed.WriteMessage($"\n  [{c.Order}] {c.Id}: {c.Title} (w={c.Width})");

            var hidden = t.Columns?.Count(c => !c.Visible) ?? 0;
            if (hidden > 0)
                ed.WriteMessage($"\n  (скрыто колонок: {hidden})");
        }
    }
}
