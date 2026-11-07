using System;
using System.IO;
using System.Linq;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Runtime;
using CablePlugin.Core.Templates;
using CablePlugin.Data;
using AcApp = Autodesk.AutoCAD.ApplicationServices.Core.Application;

namespace CablePlugin.Commands
{
    /// <summary>
    /// Настройка DWG-библиотеки пользовательских блоков однолинейки.
    /// </summary>
    public class LibraryCommands
    {
        private readonly TemplateRepository _templates = new TemplateRepository();
        private readonly BlockLibraryImporter _importer = new BlockLibraryImporter();

        [CommandMethod("CABLELIB", CommandFlags.Modal)]
        public void CableLib()
        {
            var doc = AcApp.DocumentManager.MdiActiveDocument;
            if (doc == null) return;
            var ed = doc.Editor;
            var db = doc.Database;

            var opts = new PromptKeywordOptions("\nБиблиотека блоков [Путь/Имена/Импорт/Список/Сброс]")
            {
                AllowNone = true
            };
            opts.Keywords.Add("Путь");
            opts.Keywords.Add("Имена");
            opts.Keywords.Add("Импорт");
            opts.Keywords.Add("Список");
            opts.Keywords.Add("Сброс");
            opts.Keywords.Default = "Путь";

            var res = ed.GetKeywords(opts);
            if (res.Status == PromptStatus.Cancel) return;
            var key = res.Status == PromptStatus.OK ? res.StringResult : "Путь";

            // Точное сравнение по ключевым словам AutoCAD
            if (IsKeyword(key, "Сброс"))
            {
                ClearLibrary(doc, db, ed);
                return;
            }

            if (IsKeyword(key, "Список"))
            {
                ListLibrary(db, ed);
                return;
            }

            if (IsKeyword(key, "Импорт"))
            {
                ImportNow(doc, db, ed);
                return;
            }

            if (IsKeyword(key, "Имена"))
            {
                SetNames(doc, db, ed);
                return;
            }

            SetPath(doc, db, ed);
        }

        private static bool IsKeyword(string value, string keyword)
        {
            if (string.IsNullOrEmpty(value) || string.IsNullOrEmpty(keyword))
                return false;
            return value.Equals(keyword, StringComparison.OrdinalIgnoreCase)
                   || value.StartsWith(keyword.Substring(0, 1), StringComparison.OrdinalIgnoreCase)
                      && keyword.StartsWith(value, StringComparison.OrdinalIgnoreCase);
        }

        private void SetPath(Autodesk.AutoCAD.ApplicationServices.Document doc, Database db, Editor ed)
        {
            TemplateSet set;
            using (var tr = db.TransactionManager.StartTransaction())
            {
                set = _templates.Load(db, tr);
                tr.Commit();
            }

            var current = set.PanelBlocks?.LibraryDwgPath ?? "";
            ed.WriteMessage(string.IsNullOrEmpty(current)
                ? "\nБиблиотека не задана."
                : "\nТекущая библиотека: " + current);

            string path = null;
            try
            {
                var ofd = new Microsoft.Win32.OpenFileDialog
                {
                    Title = "DWG-библиотека блоков однолинейки",
                    Filter = "Чертёж (*.dwg)|*.dwg|Все файлы (*.*)|*.*",
                    CheckFileExists = true
                };
                if (ofd.ShowDialog() == true)
                    path = ofd.FileName;
            }
            catch
            {
                var pfo = new PromptOpenFileOptions("\nФайл библиотеки DWG")
                {
                    Filter = "DWG (*.dwg)|*.dwg"
                };
                var pfr = ed.GetFileNameForOpen(pfo);
                if (pfr.Status == PromptStatus.OK)
                    path = pfr.StringResult;
            }

            if (string.IsNullOrWhiteSpace(path))
            {
                ed.WriteMessage("\n[CablePlugin] Путь не изменён.");
                return;
            }

            if (!File.Exists(path))
            {
                ed.WriteMessage("\n[CablePlugin] Файл не найден: " + path);
                return;
            }

            if (set.PanelBlocks == null)
                set.PanelBlocks = new PanelBlockTemplate();
            set.PanelBlocks.LibraryDwgPath = path;

            using (doc.LockDocument())
            using (var tr = db.TransactionManager.StartTransaction())
            {
                _templates.Save(db, tr, set);
                tr.Commit();
            }

            ed.WriteMessage("\n[CablePlugin] Библиотека сохранена: " + path);

            try
            {
                var names = _importer.ListBlockNames(path);
                ed.WriteMessage($"\n  Блоков в библиотеке: {names.Count}");
                foreach (var n in names.Take(30))
                    ed.WriteMessage("\n    " + n);
                if (names.Count > 30)
                    ed.WriteMessage($"\n    ... и ещё {names.Count - 30}");
            }
            catch (System.Exception ex)
            {
                ed.WriteMessage("\n  Не удалось прочитать блоки: " + ex.Message);
            }

            ed.WriteMessage("\nДалее: CABLELIB → Имена (сопоставление) или Импорт.");
        }

        private void SetNames(Autodesk.AutoCAD.ApplicationServices.Document doc, Database db, Editor ed)
        {
            TemplateSet set;
            using (var tr = db.TransactionManager.StartTransaction())
            {
                set = _templates.Load(db, tr);
                tr.Commit();
            }

            if (set.PanelBlocks == null)
                set.PanelBlocks = new PanelBlockTemplate();

            var pb = set.PanelBlocks;
            ed.WriteMessage("\nСопоставление: имя в чертеже ← имя в библиотеке");

            pb.BreakerBlockName = PromptName(ed, "Блок АВ в чертеже", pb.BreakerBlockName ?? "CABLE_QF");
            pb.LibraryBreakerName = PromptName(ed, "Блок АВ в библиотеке", string.IsNullOrEmpty(pb.LibraryBreakerName) ? pb.BreakerBlockName : pb.LibraryBreakerName);

            pb.BusBlockName = PromptName(ed, "Блок шины в чертеже", pb.BusBlockName ?? "CABLE_BUS");
            pb.LibraryBusName = PromptName(ed, "Блок шины в библиотеке", string.IsNullOrEmpty(pb.LibraryBusName) ? pb.BusBlockName : pb.LibraryBusName);

            pb.LoadBlockName = PromptName(ed, "Блок нагрузки в чертеже", pb.LoadBlockName ?? "CABLE_LOAD");
            pb.LibraryLoadName = PromptName(ed, "Блок нагрузки в библиотеке", string.IsNullOrEmpty(pb.LibraryLoadName) ? pb.LoadBlockName : pb.LibraryLoadName);

            var pko = new PromptKeywordOptions("\nПерезаписывать существующие блоки? [Да/Нет]") { AllowNone = true };
            pko.Keywords.Add("Да");
            pko.Keywords.Add("Нет");
            pko.Keywords.Default = pb.OverwriteExistingBlocks ? "Да" : "Нет";
            var pkr = ed.GetKeywords(pko);
            if (pkr.Status == PromptStatus.OK)
                pb.OverwriteExistingBlocks = pkr.StringResult.StartsWith("Д", StringComparison.OrdinalIgnoreCase);

            using (doc.LockDocument())
            using (var tr = db.TransactionManager.StartTransaction())
            {
                _templates.Save(db, tr, set);
                tr.Commit();
            }

            ed.WriteMessage("\n[CablePlugin] Сопоставление имён сохранено.");
        }

        private void ImportNow(Autodesk.AutoCAD.ApplicationServices.Document doc, Database db, Editor ed)
        {
            TemplateSet set;
            using (var tr = db.TransactionManager.StartTransaction())
            {
                set = _templates.Load(db, tr);
                tr.Commit();
            }

            var pb = set.PanelBlocks ?? new PanelBlockTemplate();
            if (string.IsNullOrWhiteSpace(pb.LibraryDwgPath))
            {
                ed.WriteMessage("\n[CablePlugin] Сначала укажите путь: CABLELIB → Путь");
                return;
            }

            using (doc.LockDocument())
            using (var tr = db.TransactionManager.StartTransaction())
            {
                BlockLibraryImporter.ImportResult ir;
                _importer.TryImportPanelBlocks(db, tr, pb, out ir);
                tr.Commit();
                ReportImport(ed, ir);
            }
        }

        private void ListLibrary(Database db, Editor ed)
        {
            TemplateSet set;
            using (var tr = db.TransactionManager.StartTransaction())
            {
                set = _templates.Load(db, tr);
                tr.Commit();
            }

            var pb = set.PanelBlocks ?? new PanelBlockTemplate();
            ed.WriteMessage("\n========== Библиотека блоков ==========");
            ed.WriteMessage("\n  DWG: " + (string.IsNullOrEmpty(pb.LibraryDwgPath) ? "(не задан)" : pb.LibraryDwgPath));
            ed.WriteMessage($"\n  АВ:  чертёж «{pb.BreakerBlockName}» ← библиотека «{pb.ResolveLibraryBreaker()}»");
            ed.WriteMessage($"\n  Шина: чертёж «{pb.BusBlockName}» ← библиотека «{pb.ResolveLibraryBus()}»");
            ed.WriteMessage($"\n  Нагр.: чертёж «{pb.LoadBlockName}» ← библиотека «{pb.ResolveLibraryLoad()}»");
            ed.WriteMessage($"\n  Overwrite: {pb.OverwriteExistingBlocks}");

            if (!string.IsNullOrWhiteSpace(pb.LibraryDwgPath) && File.Exists(pb.LibraryDwgPath))
            {
                try
                {
                    var names = _importer.ListBlockNames(pb.LibraryDwgPath);
                    ed.WriteMessage($"\n  Содержимое библиотеки ({names.Count}):");
                    foreach (var n in names.Take(40))
                        ed.WriteMessage("\n    " + n);
                    if (names.Count > 40)
                        ed.WriteMessage($"\n    ... +{names.Count - 40}");
                }
                catch (System.Exception ex)
                {
                    ed.WriteMessage("\n  Ошибка чтения: " + ex.Message);
                }
            }

            ed.WriteMessage("\n========================================");
        }

        private void ClearLibrary(Autodesk.AutoCAD.ApplicationServices.Document doc, Database db, Editor ed)
        {
            TemplateSet set;
            using (var tr = db.TransactionManager.StartTransaction())
            {
                set = _templates.Load(db, tr);
                tr.Commit();
            }

            if (set.PanelBlocks == null)
                set.PanelBlocks = new PanelBlockTemplate();

            set.PanelBlocks.LibraryDwgPath = string.Empty;
            set.PanelBlocks.LibraryBreakerName = string.Empty;
            set.PanelBlocks.LibraryBusName = string.Empty;
            set.PanelBlocks.LibraryLoadName = string.Empty;

            using (doc.LockDocument())
            using (var tr = db.TransactionManager.StartTransaction())
            {
                _templates.Save(db, tr, set);
                tr.Commit();
            }

            ed.WriteMessage("\n[CablePlugin] Путь к библиотеке сброшен. Будут использоваться встроенные/локальные блоки.");
        }

        private static string PromptName(Editor ed, string message, string defaultValue)
        {
            var opts = new PromptStringOptions("\n" + message + " <" + defaultValue + ">")
            {
                AllowSpaces = false,
                DefaultValue = defaultValue,
                UseDefaultValue = true
            };
            var r = ed.GetString(opts);
            if (r.Status != PromptStatus.OK) return defaultValue;
            return string.IsNullOrWhiteSpace(r.StringResult) ? defaultValue : r.StringResult.Trim();
        }

        private static void ReportImport(Editor ed, BlockLibraryImporter.ImportResult ir)
        {
            if (ir == null)
            {
                ed.WriteMessage("\n[CablePlugin] Импорт не выполнен.");
                return;
            }

            ed.WriteMessage($"\n[CablePlugin] Импорт: +{ir.Imported.Count}, пропуск {ir.Skipped.Count}, нет в lib {ir.MissingInLibrary.Count}");
            foreach (var s in ir.Imported)
                ed.WriteMessage("\n  + " + s);
            foreach (var s in ir.Skipped)
                ed.WriteMessage("\n  ~ " + s);
            foreach (var s in ir.MissingInLibrary)
                ed.WriteMessage("\n  ? нет в библиотеке: " + s);
            foreach (var s in ir.Errors)
                ed.WriteMessage("\n  ! " + s);
        }
    }
}
