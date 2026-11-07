using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;
using CablePlugin.Core.Models;

namespace CablePlugin.Data
{
    /// <summary>
    /// Сбор всех кабельных линий из пространства модели (и опционально листов).
    /// </summary>
    public class CableLineCollector
    {
        private readonly CableLineRepository _repo = new CableLineRepository();

        /// <summary>
        /// Найти все объекты с данными кабеля в ModelSpace.
        /// </summary>
        public List<(ObjectId Id, CableLine Line)> CollectFromModelSpace(Database db, Transaction tr)
        {
            var result = new List<(ObjectId, CableLine)>();
            if (db == null || tr == null)
                return result;

            var bt = (BlockTable)tr.GetObject(db.BlockTableId, OpenMode.ForRead);
            var ms = (BlockTableRecord)tr.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForRead);

            foreach (ObjectId id in ms)
            {
                if (!PolylineLengthHelper.IsSupportedCurve(id, tr))
                    continue;

                var line = _repo.Load(id, tr);
                if (line != null)
                    result.Add((id, line));
            }

            return result;
        }

        /// <summary>
        /// Загрузить линии по списку ObjectId (пропуская объекты без данных).
        /// </summary>
        public List<CableLine> LoadMany(IEnumerable<ObjectId> ids, Transaction tr)
        {
            var result = new List<CableLine>();
            if (ids == null || tr == null)
                return result;

            foreach (var id in ids)
            {
                if (id.IsNull) continue;
                var line = _repo.Load(id, tr);
                if (line != null)
                    result.Add(line);
            }

            return result;
        }
    }
}
