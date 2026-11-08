using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Runtime;
using CablePlugin.Core.Models;
using CablePlugin.Core.Services;
using CablePlugin.Data;
using CablePlugin.UI;
using AcApp = Autodesk.AutoCAD.ApplicationServices.Core.Application;

namespace CablePlugin.Commands
{
    public class CableCommands
    {
        private readonly CableLineRepository _lineRepo = new CableLineRepository();
        private readonly PipeTableRepository _pipeRepo = new PipeTableRepository();
        private readonly CableLineCollector _collector = new CableLineCollector();
        private readonly ElectricalCalculator _electrical = new ElectricalCalculator();
        private readonly ExcelExportService _excel = new ExcelExportService();

        [CommandMethod("CABLEATTACH", CommandFlags.Modal | CommandFlags.UsePickSet)]
        public void CableAttach()
        {
            var doc = AcApp.DocumentManager.MdiActiveDocument;
            if (doc == null) return;
            var ed = doc.Editor;
            var db = doc.Database;

            var ids = SelectCurves(ed, "\nВыберите полилинии (трассы кабеля): ");
            if (ids == null || ids.Count == 0)
            {
                ed.WriteMessage("\nНичего не выбрано.");
                return;
            }

            List<PipeCorrespondence> pipeTable;
            double firstGeomLen;

            using (var tr = db.TransactionManager.StartTransaction())
            {
                pipeTable = _pipeRepo.Load(db, tr);
                firstGeomLen = PolylineLengthHelper.GetLength(ids[0], tr);
                tr.Commit();
            }

            var draft = new CableLine
            {
                Cable = new CableInfo { Mark = "ВВГнг(А)-LS", Cores = 3, Section = 1.5, CableCount = 1 },
                Segments = new List<CableSegment>
                {
                    new CableSegment { LayingMethod = "Гофра", Length = firstGeomLen, IsPipeDiameterAuto = true }
                },
                ReservePercent = 20,
                ScaleFactor = 1.0,
                RoundUpTo = 1.0,
                SyncLengthFromPolyline = true,
                Conductor = ConductorMaterial.Copper,
                GeometricLength = firstGeomLen,
                Voltage = 380,
                Phases = 3,
                CosPhi = 0.85
            };

            var owner = GetAcadOwnerHandle();
            var edited = CableDialogService.EditLine(draft, pipeTable, owner);
            if (edited == null)
            {
                ed.WriteMessage("\n[CablePlugin] Отменено.");
                return;
            }

            using (doc.LockDocument())
            using (var tr = db.TransactionManager.StartTransaction())
            {
                var pipeSelector = new PipeSelector(pipeTable);
                int saved = 0;

                foreach (ObjectId id in ids)
                {
                    if (!PolylineLengthHelper.IsSupportedCurve(id, tr))
                        continue;

                    var line = edited.Clone();
                    line.Id = Guid.NewGuid();

                    double geomLen = PolylineLengthHelper.GetLength(id, tr);
                    if (line.SyncLengthFromPolyline)
                        LengthCalculator.SyncSegmentsFromPolyline(line, geomLen);
                    else
                        line.GeometricLength = geomLen;

                    pipeSelector.ApplyToLine(line, forceAll: true);
                    LengthCalculator.Calculate(line);
                    _electrical.Calculate(line);
                    _lineRepo.Save(id, line, tr);
                    saved++;

                    ed.WriteMessage(
                        $"\n  {line.Cable.DisplayName} | L={line.TotalLength:0.###} м | " +
                        $"I={line.Current:0.##} А | ΔU={line.VoltageDropPercent:0.##}% | АВ={line.RecommendedBreaker} | " +
                        (line.IsAluminum ? "Al" : "Cu"));
                }

                tr.Commit();
                ed.WriteMessage($"\n[CablePlugin] Сохранено линий: {saved}");
            }
        }

        [CommandMethod("CABLEEDIT", CommandFlags.Modal | CommandFlags.UsePickSet)]
        public void CableEdit()
        {
            var doc = AcApp.DocumentManager.MdiActiveDocument;
            if (doc == null) return;
            var ed = doc.Editor;
            var db = doc.Database;

            var id = SelectSingleCurve(ed, "\nВыберите линию кабеля для редактирования: ");
            if (id.IsNull) return;

            CableLine existing;
            List<PipeCorrespondence> pipeTable;
            double geomLen;

            using (var tr = db.TransactionManager.StartTransaction())
            {
                existing = _lineRepo.Load(id, tr);
                pipeTable = _pipeRepo.Load(db, tr);
                geomLen = PolylineLengthHelper.GetLength(id, tr);

                if (existing == null)
                {
                    existing = new CableLine
                    {
                        Cable = new CableInfo { Mark = "ВВГнг(А)-LS", Cores = 3, Section = 1.5 },
                        Segments = new List<CableSegment>
                        {
                            new CableSegment { LayingMethod = "Гофра", Length = geomLen, IsPipeDiameterAuto = true }
                        },
                        ReservePercent = 20,
                        ScaleFactor = 1.0,
                        RoundUpTo = 1.0,
                        SyncLengthFromPolyline = true,
                        GeometricLength = geomLen
                    };
                }
                else if (existing.SyncLengthFromPolyline)
                {
                    LengthCalculator.SyncSegmentsFromPolyline(existing, geomLen);
                    LengthCalculator.Calculate(existing);
                    _electrical.Calculate(existing);
                }

                tr.Commit();
            }

            var owner = GetAcadOwnerHandle();
            var edited = CableDialogService.EditLine(existing, pipeTable, owner);
            if (edited == null)
            {
                ed.WriteMessage("\n[CablePlugin] Отменено.");
                return;
            }

            using (doc.LockDocument())
            using (var tr = db.TransactionManager.StartTransaction())
            {
                geomLen = PolylineLengthHelper.GetLength(id, tr);
                if (edited.SyncLengthFromPolyline)
                    LengthCalculator.SyncSegmentsFromPolyline(edited, geomLen);

                var pipeSelector = new PipeSelector(pipeTable);
                pipeSelector.ApplyToLine(edited, forceAll: false);
                LengthCalculator.Calculate(edited);
                _electrical.Calculate(edited);
                _lineRepo.Save(id, edited, tr);
                tr.Commit();

                ed.WriteMessage(
                    $"\n[CablePlugin] Сохранено: {edited.Cable?.DisplayName}, L={edited.TotalLength:0.###} м, " +
                    $"I={edited.Current:0.##} А, ΔU={edited.VoltageDropPercent:0.##}%, АВ={edited.RecommendedBreaker}, " +
                    (edited.IsAluminum ? "Al" : "Cu"));
            }
        }

        [CommandMethod("CABLEDICT", CommandFlags.Modal)]
        public void CableDict()
        {
            var doc = AcApp.DocumentManager.MdiActiveDocument;
            if (doc == null) return;
            var ed = doc.Editor;
            var db = doc.Database;

            List<PipeCorrespondence> current;
            using (var tr = db.TransactionManager.StartTransaction())
            {
                current = _pipeRepo.Load(db, tr);
                tr.Commit();
            }

            var owner = GetAcadOwnerHandle();
            var updated = CableDialogService.EditPipeTable(current, owner);
            if (updated == null)
            {
                ed.WriteMessage("\n[CablePlugin] Таблица труб: отменено.");
                return;
            }

            using (doc.LockDocument())
            using (var tr = db.TransactionManager.StartTransaction())
            {
                _pipeRepo.Save(db, tr, updated);
                tr.Commit();
            }

            ed.WriteMessage($"\n[CablePlugin] Таблица труб сохранена. Записей: {updated.Count}");

            var pko = new PromptKeywordOptions("\nОбновить ∅ гофры/труб на всех линиях по новой таблице? [Да/Нет]")
            {
                AllowNone = true
            };
            pko.Keywords.Add("Да");
            pko.Keywords.Add("Нет");
            pko.Keywords.Default = "Да";
            var pkr = ed.GetKeywords(pko);
            if (pkr.Status == PromptStatus.Cancel) return;
            if (pkr.Status == PromptStatus.OK && pkr.StringResult.StartsWith("Н", StringComparison.OrdinalIgnoreCase))
                return;

            using (doc.LockDocument())
            using (var tr = db.TransactionManager.StartTransaction())
            {
                var pipeSelector = new PipeSelector(updated);
                var items = _collector.CollectFromModelSpace(db, tr);
                int n = 0;
                foreach (var item in items)
                {
                    var line = item.Line;
                    pipeSelector.ApplyToLine(line, forceAll: true);
                    LengthCalculator.Calculate(line);
                    _electrical.Calculate(line);
                    // CollectFromModelSpace возвращает (ObjectId Id, CableLine Line)
                    _lineRepo.Save(item.Id, line, tr);
                    n++;
                }
                tr.Commit();
                ed.WriteMessage($"\n[CablePlugin] Диаметры обновлены на линиях: {n}");
            }
        }

        [CommandMethod("CABLEEXPORT", CommandFlags.Modal | CommandFlags.UsePickSet)]
        public void CableExport()
        {
            var doc = AcApp.DocumentManager.MdiActiveDocument;
            if (doc == null) return;
            var ed = doc.Editor;
            var db = doc.Database;

            List<CableLine> lines;

            var peo = new PromptSelectionOptions
            {
                MessageForAdding = "\nВыберите линии для экспорта (Enter — все из модели): ",
                AllowDuplicates = false
            };

            var filter = new SelectionFilter(new[]
            {
                new TypedValue((int)DxfCode.Start, "LWPOLYLINE,POLYLINE,LINE,ARC,SPLINE")
            });

            var selRes = ed.GetSelection(peo, filter);

            using (var tr = db.TransactionManager.StartTransaction())
            {
                if (selRes.Status == PromptStatus.OK && selRes.Value != null && selRes.Value.Count > 0)
                    lines = _collector.LoadMany(selRes.Value.GetObjectIds(), tr);
                else
                    lines = _collector.CollectFromModelSpace(db, tr).Select(x => x.Line).ToList();
                tr.Commit();
            }

            if (lines == null || lines.Count == 0)
            {
                ed.WriteMessage("\n[CablePlugin] Нет данных кабеля для экспорта.");
                return;
            }

            string path;
            try
            {
                var sfd = new Microsoft.Win32.SaveFileDialog
                {
                    Title = "Экспорт кабельных линий",
                    Filter = "Excel (*.xlsx)|*.xlsx",
                    DefaultExt = ".xlsx",
                    FileName = "CableLines.xlsx",
                    AddExtension = true
                };
                if (sfd.ShowDialog() != true)
                {
                    ed.WriteMessage("\n[CablePlugin] Экспорт отменён.");
                    return;
                }
                path = sfd.FileName;
            }
            catch
            {
                var opts = new PromptSaveFileOptions("\nФайл Excel")
                {
                    DialogCaption = "Экспорт кабельных линий",
                    Filter = "Excel (*.xlsx)|*.xlsx"
                };
                var pfr = ed.GetFileNameForSave(opts);
                if (pfr.Status != PromptStatus.OK) return;
                path = pfr.StringResult;
                if (!path.EndsWith(".xlsx", StringComparison.OrdinalIgnoreCase))
                    path += ".xlsx";
            }

            try
            {
                _excel.Export(lines, path);
                ed.WriteMessage($"\n[CablePlugin] Экспортировано линий: {lines.Count}\n  Файл: {path}");
            }
            catch (System.Exception ex)
            {
                ed.WriteMessage($"\n[CablePlugin] Ошибка экспорта: {ex.Message}");
            }
        }

        [CommandMethod("CABLEINFO", CommandFlags.Modal | CommandFlags.UsePickSet)]
        public void CableInfo()
        {
            var doc = AcApp.DocumentManager.MdiActiveDocument;
            if (doc == null) return;
            var ed = doc.Editor;
            var db = doc.Database;

            var id = SelectSingleCurve(ed, "\nВыберите линию кабеля: ");
            if (id.IsNull) return;

            using (var tr = db.TransactionManager.StartTransaction())
            {
                var line = _lineRepo.Load(id, tr);
                if (line == null)
                {
                    ed.WriteMessage("\nНа объекте нет данных кабеля.");
                    tr.Commit();
                    return;
                }

                ed.WriteMessage("\n========== Данные кабельной линии ==========");
                ed.WriteMessage($"\n  Группа:     {line.GroupName}");
                ed.WriteMessage($"\n  От / До:    {line.From} → {line.To}");
                ed.WriteMessage($"\n  Кабель:     {line.Cable?.DisplayName} x{line.Cable?.CableCount} ({(line.IsAluminum ? "Al" : "Cu")})");
                ed.WriteMessage($"\n  Запас:      {line.ReservePercent}% + {line.AdditionalLength} м");
                ed.WriteMessage($"\n  Длина геом: {line.GeometricLength:0.###} м");
                ed.WriteMessage($"\n  Длина итог: {line.TotalLength:0.###} м");
                ed.WriteMessage($"\n  Ток:        {line.Current:0.##} А");
                ed.WriteMessage($"\n  ΔU:         {line.VoltageDropPercent:0.##} %");
                ed.WriteMessage($"\n  АВ:         {line.RecommendedBreaker}");
                ed.WriteMessage($"\n  Sync L:     {line.SyncLengthFromPolyline}");
                ed.WriteMessage("\n============================================");
                tr.Commit();
            }
        }

        [CommandMethod("CABLERECALC", CommandFlags.Modal | CommandFlags.UsePickSet)]
        public void CableRecalc()
        {
            var doc = AcApp.DocumentManager.MdiActiveDocument;
            if (doc == null) return;
            var ed = doc.Editor;
            var db = doc.Database;

            var ids = SelectCurves(ed, "\nВыберите линии для пересчёта: ");
            if (ids == null || ids.Count == 0)
            {
                ed.WriteMessage("\nОтменено.");
                return;
            }

            using (doc.LockDocument())
            using (var tr = db.TransactionManager.StartTransaction())
            {
                var pipeTable = _pipeRepo.Load(db, tr);
                var pipeSelector = new PipeSelector(pipeTable);
                int count = 0;

                foreach (ObjectId id in ids)
                {
                    var line = _lineRepo.Load(id, tr);
                    if (line == null) continue;

                    double geom = PolylineLengthHelper.GetLength(id, tr);

                    if (line.SyncLengthFromPolyline)
                        LengthCalculator.SyncSegmentsFromPolyline(line, geom);
                    else
                        line.GeometricLength = geom;

                    pipeSelector.ApplyToLine(line, forceAll: false);
                    LengthCalculator.Calculate(line);
                    _electrical.Calculate(line);
                    _lineRepo.Save(id, line, tr);
                    count++;

                    ed.WriteMessage(
                        $"\n  {line.Cable?.DisplayName}: геом={geom:0.###} → L={line.TotalLength:0.###} м, " +
                        $"I={line.Current:0.##} А, ΔU={line.VoltageDropPercent:0.##}%, АВ={line.RecommendedBreaker}");
                }

                tr.Commit();
                ed.WriteMessage($"\n[CablePlugin] Пересчитано: {count}");
            }
        }

        [CommandMethod("CABLEREMOVE", CommandFlags.Modal | CommandFlags.UsePickSet)]
        public void CableRemove()
        {
            var doc = AcApp.DocumentManager.MdiActiveDocument;
            if (doc == null) return;
            var ed = doc.Editor;
            var db = doc.Database;

            var ids = SelectCurves(ed, "\nВыберите объекты для удаления данных кабеля: ");
            if (ids == null || ids.Count == 0) return;

            using (doc.LockDocument())
            using (var tr = db.TransactionManager.StartTransaction())
            {
                int n = 0;
                foreach (ObjectId id in ids)
                {
                    if (_lineRepo.Remove(id, tr))
                        n++;
                }
                tr.Commit();
                ed.WriteMessage($"\n[CablePlugin] Удалены данные с объектов: {n}");
            }
        }

        private static IntPtr GetAcadOwnerHandle()
        {
            try { return AcApp.MainWindow.Handle; }
            catch { return IntPtr.Zero; }
        }

        private static ObjectIdCollection SelectCurves(Editor ed, string message)
        {
            var peo = new PromptSelectionOptions
            {
                MessageForAdding = message,
                AllowDuplicates = false
            };

            var filter = new SelectionFilter(new[]
            {
                new TypedValue((int)DxfCode.Start, "LWPOLYLINE,POLYLINE,LINE,ARC,SPLINE")
            });

            var res = ed.GetSelection(peo, filter);
            if (res.Status != PromptStatus.OK)
                return null;

            return new ObjectIdCollection(res.Value.GetObjectIds());
        }

        private static ObjectId SelectSingleCurve(Editor ed, string message)
        {
            var peo = new PromptEntityOptions(message);
            peo.SetRejectMessage("\nНужна полилиния, отрезок, дуга или сплайн.");
            peo.AddAllowedClass(typeof(Polyline), exactMatch: false);
            peo.AddAllowedClass(typeof(Polyline2d), exactMatch: false);
            peo.AddAllowedClass(typeof(Polyline3d), exactMatch: false);
            peo.AddAllowedClass(typeof(Line), exactMatch: false);
            peo.AddAllowedClass(typeof(Arc), exactMatch: false);
            peo.AddAllowedClass(typeof(Spline), exactMatch: false);

            var res = ed.GetEntity(peo);
            return res.Status == PromptStatus.OK ? res.ObjectId : ObjectId.Null;
        }
    }
}
