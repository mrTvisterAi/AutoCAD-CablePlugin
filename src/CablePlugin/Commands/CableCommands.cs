using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Runtime;
using CablePlugin.Core.Models;
using CablePlugin.Core.Services;
using CablePlugin.Data;

namespace CablePlugin.Commands
{
    /// <summary>
    /// Основные команды плагина (этапы 1–3).
    /// </summary>
    public class CableCommands
    {
        private readonly CableLineRepository _lineRepo = new CableLineRepository();
        private readonly PipeTableRepository _pipeRepo = new PipeTableRepository();
        private readonly ElectricalCalculator _electrical = new ElectricalCalculator();

        /// <summary>
        /// Привязать / создать данные кабеля на выбранных полилиниях.
        /// </summary>
        [CommandMethod("CABLEATTACH", CommandFlags.Modal | CommandFlags.UsePickSet)]
        public void CableAttach()
        {
            var doc = Application.DocumentManager.MdiActiveDocument;
            if (doc == null) return;
            var ed = doc.Editor;
            var db = doc.Database;

            var ids = SelectCurves(ed, "\nВыберите полилинии (трассы кабеля): ");
            if (ids == null || ids.Count == 0)
            {
                ed.WriteMessage("\nНичего не выбрано.");
                return;
            }

            // Базовые параметры с командной строки (минимальный UI до появления WPF-диалога)
            var mark = PromptString(ed, "\nМарка кабеля", "ВВГнг(А)-LS");
            if (mark == null) return;

            var cores = PromptInt(ed, "\nКоличество жил", 3);
            if (cores == null) return;

            var section = PromptDouble(ed, "\nСечение, мм²", 1.5);
            if (section == null) return;

            var cableCount = PromptInt(ed, "\nКоличество кабелей в линии", 1);
            if (cableCount == null) return;

            var group = PromptString(ed, "\nГруппа (имя)", "");
            if (group == null) return;

            var from = PromptString(ed, "\nНачало линии (от)", "");
            if (from == null) return;

            var to = PromptString(ed, "\nКонец линии (до)", "");
            if (to == null) return;

            var reserve = PromptDouble(ed, "\nЗапас, %", 20);
            if (reserve == null) return;

            var addLen = PromptDouble(ed, "\nДоп. длина на опуски, м", 0);
            if (addLen == null) return;

            var scale = PromptDouble(ed, "\nМасштабный коэффициент (1.0 если чертёж в метрах)", 1.0);
            if (scale == null) return;

            // Сегменты прокладки: упрощённо один сегмент = вся длина, способ с клавиатуры
            var method = PromptString(ed, "\nСпособ прокладки (Лоток/Гофра/Труба/Кабель-канал)", "Гофра");
            if (method == null) return;

            using (doc.LockDocument())
            using (var tr = db.TransactionManager.StartTransaction())
            {
                var pipeTable = _pipeRepo.Load(db, tr);
                var pipeSelector = new PipeSelector(pipeTable);
                int saved = 0;

                foreach (ObjectId id in ids)
                {
                    if (!PolylineLengthHelper.IsSupportedCurve(id, tr))
                    {
                        ed.WriteMessage($"\nПропуск {id}: неподдерживаемый тип.");
                        continue;
                    }

                    double geomLen = PolylineLengthHelper.GetLength(id, tr);

                    var line = new CableLine
                    {
                        GroupName = group,
                        From = from,
                        To = to,
                        Cable = new CableInfo
                        {
                            Mark = mark,
                            Cores = cores.Value,
                            Section = section.Value,
                            CableCount = Math.Max(1, cableCount.Value)
                        },
                        Segments = new List<CableSegment>
                        {
                            new CableSegment
                            {
                                LayingMethod = method,
                                Length = geomLen * (scale.Value <= 0 ? 1.0 : scale.Value),
                                IsPipeDiameterAuto = true
                            }
                        },
                        ReservePercent = reserve.Value,
                        AdditionalLength = addLen.Value,
                        ScaleFactor = 1.0, // длина сегмента уже с масштабом
                        RoundUpTo = 1.0,
                        GeometricLength = geomLen
                    };

                    pipeSelector.ApplyToLine(line);
                    LengthCalculator.Calculate(line);
                    _electrical.Calculate(line);

                    _lineRepo.Save(id, line, tr);
                    saved++;

                    ed.WriteMessage(
                        $"\n  {line.Cable.DisplayName} | L={line.TotalLength:0.###} м | " +
                        $"I={line.Current:0.##} А | ΔU={line.VoltageDropPercent:0.##}% | АВ={line.RecommendedBreaker}");
                }

                tr.Commit();
                ed.WriteMessage($"\n[CablePlugin] Сохранено линий: {saved}");
            }
        }

        /// <summary>
        /// Показать данные кабеля выбранного объекта.
        /// </summary>
        [CommandMethod("CABLEINFO", CommandFlags.Modal | CommandFlags.UsePickSet)]
        public void CableInfo()
        {
            var doc = Application.DocumentManager.MdiActiveDocument;
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
                    ed.WriteMessage("\nНа объекте нет данных кабеля. Используйте CABLEATTACH.");
                    tr.Commit();
                    return;
                }

                ed.WriteMessage("\n========== Данные кабельной линии ==========");
                ed.WriteMessage($"\n  Группа:     {line.GroupName}");
                ed.WriteMessage($"\n  От:         {line.From}");
                ed.WriteMessage($"\n  До:         {line.To}");
                ed.WriteMessage($"\n  Кабель:     {line.Cable?.DisplayName} x{line.Cable?.CableCount}");
                ed.WriteMessage($"\n  Запас:      {line.ReservePercent}% + {line.AdditionalLength} м");
                ed.WriteMessage($"\n  Длина геом: {line.GeometricLength:0.###} м");
                ed.WriteMessage($"\n  Длина итог: {line.TotalLength:0.###} м");
                ed.WriteMessage($"\n  Ток:        {line.Current:0.##} А");
                ed.WriteMessage($"\n  ΔU:         {line.VoltageDropPercent:0.##} %");
                ed.WriteMessage($"\n  АВ:         {line.RecommendedBreaker}");

                if (line.Segments != null && line.Segments.Count > 0)
                {
                    ed.WriteMessage("\n  --- Участки прокладки ---");
                    int i = 1;
                    foreach (var s in line.Segments)
                    {
                        var pipe = s.PipeDiameter.HasValue ? $", ∅{s.PipeDiameter} мм" : "";
                        ed.WriteMessage($"\n  {i}. {s.LayingMethod}: {s.Length:0.###} м{pipe}");
                        i++;
                    }
                }

                ed.WriteMessage("\n============================================");
                tr.Commit();
            }
        }

        /// <summary>
        /// Пересчитать длины и электрику для выбранных (или всех) линий.
        /// </summary>
        [CommandMethod("CABLERECALC", CommandFlags.Modal | CommandFlags.UsePickSet)]
        public void CableRecalc()
        {
            var doc = Application.DocumentManager.MdiActiveDocument;
            if (doc == null) return;
            var ed = doc.Editor;
            var db = doc.Database;

            var ids = SelectCurves(ed, "\nВыберите линии для пересчёта (Enter = отмена): ");
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

                    // Обновить геометрическую длину, если один сегмент «авто»
                    double geomLen = PolylineLengthHelper.GetLength(id, tr);
                    if (line.Segments != null && line.Segments.Count == 1 && line.ScaleFactor == 1.0)
                    {
                        // Сохраняем пропорции только если пользователь не задавал сегменты вручную с разными длинами
                        // Здесь просто обновляем GeometricLength; длины сегментов считаем источником истины
                    }

                    line.GeometricLength = geomLen;
                    pipeSelector.ApplyToLine(line);
                    LengthCalculator.Calculate(line);
                    _electrical.Calculate(line);
                    _lineRepo.Save(id, line, tr);
                    count++;

                    ed.WriteMessage(
                        $"\n  {line.Cable?.DisplayName}: L={line.TotalLength:0.###} м, " +
                        $"I={line.Current:0.##} А, ΔU={line.VoltageDropPercent:0.##}%, АВ={line.RecommendedBreaker}");
                }

                tr.Commit();
                ed.WriteMessage($"\n[CablePlugin] Пересчитано: {count}");
            }
        }

        /// <summary>
        /// Удалить данные кабеля с объектов.
        /// </summary>
        [CommandMethod("CABLEREMOVE", CommandFlags.Modal | CommandFlags.UsePickSet)]
        public void CableRemove()
        {
            var doc = Application.DocumentManager.MdiActiveDocument;
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

        /// <summary>
        /// Редактирование — пока через повторный ATTACH (WPF-диалог на следующем шаге).
        /// </summary>
        [CommandMethod("CABLEEDIT", CommandFlags.Modal | CommandFlags.UsePickSet)]
        public void CableEdit()
        {
            var doc = Application.DocumentManager.MdiActiveDocument;
            if (doc == null) return;
            var ed = doc.Editor;

            ed.WriteMessage("\n[CablePlugin] CABLEEDIT: пока используйте CABLEINFO для просмотра и CABLEATTACH для перезаписи.");
            ed.WriteMessage("\nWPF-диалог редактирования будет добавлен следующим шагом.");

            // Показать info по выбранному
            CableInfo();
        }

        // ===== Helpers =====

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

        private static string PromptString(Editor ed, string message, string defaultValue)
        {
            var opts = new PromptStringOptions(message + (string.IsNullOrEmpty(defaultValue) ? "" : $" <{defaultValue}>"));
            opts.AllowSpaces = true;
            var res = ed.GetString(opts);
            if (res.Status == PromptStatus.Cancel) return null;
            if (res.Status != PromptStatus.OK) return defaultValue;
            return string.IsNullOrWhiteSpace(res.StringResult) ? defaultValue : res.StringResult.Trim();
        }

        private static int? PromptInt(Editor ed, string message, int defaultValue)
        {
            var opts = new PromptIntegerOptions(message + $" <{defaultValue}>")
            {
                AllowNone = true,
                AllowNegative = false,
                AllowZero = false,
                DefaultValue = defaultValue,
                UseDefaultValue = true
            };
            var res = ed.GetInteger(opts);
            if (res.Status == PromptStatus.Cancel) return null;
            if (res.Status == PromptStatus.None) return defaultValue;
            return res.Status == PromptStatus.OK ? res.Value : defaultValue;
        }

        private static double? PromptDouble(Editor ed, string message, double defaultValue)
        {
            var opts = new PromptDoubleOptions(message + $" <{defaultValue}>")
            {
                AllowNone = true,
                AllowNegative = false,
                DefaultValue = defaultValue,
                UseDefaultValue = true
            };
            var res = ed.GetDouble(opts);
            if (res.Status == PromptStatus.Cancel) return null;
            if (res.Status == PromptStatus.None) return defaultValue;
            return res.Status == PromptStatus.OK ? res.Value : defaultValue;
        }
    }
}
