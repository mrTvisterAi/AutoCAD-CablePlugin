using System;
using System.Collections.Generic;
using System.IO;
using Autodesk.AutoCAD.DatabaseServices;
using CablePlugin.Core.Templates;

namespace CablePlugin.Data
{
    /// <summary>
    /// Импорт определений блоков из внешней DWG-библиотеки в текущий чертёж.
    /// </summary>
    public class BlockLibraryImporter
    {
        public class ImportResult
        {
            public List<string> Imported { get; } = new List<string>();
            public List<string> Skipped { get; } = new List<string>();
            public List<string> MissingInLibrary { get; } = new List<string>();
            public List<string> Errors { get; } = new List<string>();

            public bool HasErrors => Errors.Count > 0;
        }

        /// <summary>
        /// Импортировать блоки схемы щита согласно шаблону.
        /// Если LibraryDwgPath пуст или файл не найден — ничего не делает (false).
        /// </summary>
        public bool TryImportPanelBlocks(Database destDb, Transaction destTr, PanelBlockTemplate template, out ImportResult result)
        {
            result = new ImportResult();
            if (template == null || string.IsNullOrWhiteSpace(template.LibraryDwgPath))
                return false;

            var path = template.LibraryDwgPath.Trim();
            if (!File.Exists(path))
            {
                result.Errors.Add("Файл библиотеки не найден: " + path);
                return false;
            }

            var map = new List<(string SourceName, string DestName)>
            {
                (template.ResolveLibraryBreaker(), template.BreakerBlockName ?? "CABLE_QF"),
                (template.ResolveLibraryBus(), template.BusBlockName ?? "CABLE_BUS"),
                (template.ResolveLibraryLoad(), template.LoadBlockName ?? "CABLE_LOAD")
            };

            return ImportNamedBlocks(destDb, destTr, path, map, template.OverwriteExistingBlocks, result);
        }

        /// <summary>
        /// Импорт произвольного набора блоков (sourceName → destName) из DWG.
        /// </summary>
        public bool ImportNamedBlocks(
            Database destDb,
            Transaction destTr,
            string libraryPath,
            IList<(string SourceName, string DestName)> blocks,
            bool overwrite,
            ImportResult result = null)
        {
            if (result == null) result = new ImportResult();
            if (destDb == null || destTr == null)
            {
                result.Errors.Add("Нет базы/транзакции назначения.");
                return false;
            }

            if (string.IsNullOrWhiteSpace(libraryPath) || !File.Exists(libraryPath))
            {
                result.Errors.Add("Библиотека не найдена: " + libraryPath);
                return false;
            }

            using (var sourceDb = new Database(false, true))
            {
                try
                {
                    sourceDb.ReadDwgFile(libraryPath, FileOpenMode.OpenForReadAndAllShare, true, null);
                    sourceDb.CloseInput(true);
                }
                catch (System.Exception ex)
                {
                    result.Errors.Add("Не удалось открыть DWG: " + ex.Message);
                    return false;
                }

                using (var srcTr = sourceDb.TransactionManager.StartTransaction())
                {
                    var srcBt = (BlockTable)srcTr.GetObject(sourceDb.BlockTableId, OpenMode.ForRead);
                    var destBt = (BlockTable)destTr.GetObject(destDb.BlockTableId, OpenMode.ForWrite);

                    var idsToClone = new ObjectIdCollection();
                    var renameMap = new Dictionary<ObjectId, string>();

                    foreach (var pair in blocks)
                    {
                        var srcName = pair.SourceName?.Trim();
                        var destName = pair.DestName?.Trim();
                        if (string.IsNullOrEmpty(srcName) || string.IsNullOrEmpty(destName))
                            continue;

                        if (!srcBt.Has(srcName))
                        {
                            result.MissingInLibrary.Add(srcName);
                            continue;
                        }

                        if (destBt.Has(destName))
                        {
                            if (!overwrite)
                            {
                                result.Skipped.Add(destName + " (уже есть)");
                                continue;
                            }

                            // Удаляем старое определение, если нет ссылок проще перезаписать через Clone с Unique
                            try
                            {
                                var oldId = destBt[destName];
                                var oldBtr = (BlockTableRecord)destTr.GetObject(oldId, OpenMode.ForWrite);
                                // Не удаляем, если есть вхождения — переименуем временный клон позже
                                if (HasReferences(oldBtr))
                                {
                                    // Клонируем под временным именем, затем подменим — упрощённо: skip + message
                                    result.Skipped.Add(destName + " (есть вхождения, overwrite пропущен)");
                                    continue;
                                }

                                oldBtr.Erase();
                            }
                            catch (System.Exception ex)
                            {
                                result.Skipped.Add(destName + " (" + ex.Message + ")");
                                continue;
                            }
                        }

                        var srcId = srcBt[srcName];
                        idsToClone.Add(srcId);
                        renameMap[srcId] = destName;
                    }

                    if (idsToClone.Count == 0)
                    {
                        srcTr.Commit();
                        return result.Imported.Count > 0;
                    }

                    var idMap = new IdMapping();
                    sourceDb.WblockCloneObjects(
                        idsToClone,
                        destDb.BlockTableId,
                        idMap,
                        DuplicateRecordCloning.Replace,
                        false);

                    // Переименовать, если имя в библиотеке ≠ целевому
                    foreach (IdPair pair in idMap)
                    {
                        if (!pair.IsCloned) continue;
                        if (!renameMap.TryGetValue(pair.Key, out var destName))
                            continue;

                        try
                        {
                            var btr = destTr.GetObject(pair.Value, OpenMode.ForWrite) as BlockTableRecord;
                            if (btr == null) continue;

                            if (!string.Equals(btr.Name, destName, StringComparison.OrdinalIgnoreCase))
                            {
                                // Если целевое имя занято после clone — Replace уже должен был отработать
                                if (destBt.Has(destName) && destBt[destName] != btr.ObjectId)
                                {
                                    result.Skipped.Add(destName + " (конфликт имени после clone)");
                                    continue;
                                }

                                btr.Name = destName;
                            }

                            result.Imported.Add(destName + " ← " + GetSourceName(srcBt, pair.Key));
                        }
                        catch (System.Exception ex)
                        {
                            result.Errors.Add("Переименование: " + ex.Message);
                        }
                    }

                    srcTr.Commit();
                }
            }

            return result.Imported.Count > 0 || result.Skipped.Count > 0;
        }

        /// <summary>
        /// Список имён блоков в DWG (без *Model_Space и т.п.).
        /// </summary>
        public List<string> ListBlockNames(string libraryPath)
        {
            var list = new List<string>();
            if (string.IsNullOrWhiteSpace(libraryPath) || !File.Exists(libraryPath))
                return list;

            using (var db = new Database(false, true))
            {
                db.ReadDwgFile(libraryPath, FileOpenMode.OpenForReadAndAllShare, true, null);
                db.CloseInput(true);

                using (var tr = db.TransactionManager.StartTransaction())
                {
                    var bt = (BlockTable)tr.GetObject(db.BlockTableId, OpenMode.ForRead);
                    foreach (ObjectId id in bt)
                    {
                        var btr = (BlockTableRecord)tr.GetObject(id, OpenMode.ForRead);
                        if (btr.IsLayout || btr.IsAnonymous) continue;
                        if (btr.Name.StartsWith("*", StringComparison.Ordinal)) continue;
                        list.Add(btr.Name);
                    }
                    tr.Commit();
                }
            }

            list.Sort(StringComparer.OrdinalIgnoreCase);
            return list;
        }

        private static bool HasReferences(BlockTableRecord btr)
        {
            var ids = btr.GetBlockReferenceIds(true, true);
            return ids != null && ids.Count > 0;
        }

        private static string GetSourceName(BlockTable srcBt, ObjectId id)
        {
            try
            {
                foreach (ObjectId bid in srcBt)
                {
                    if (bid == id)
                    {
                        // name only via transaction would be better; use safe fallback
                        return id.Handle.ToString();
                    }
                }
            }
            catch { /* ignore */ }
            return id.Handle.ToString();
        }
    }
}
