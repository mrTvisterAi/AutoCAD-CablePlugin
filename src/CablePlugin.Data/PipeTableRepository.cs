using System;
using System.Collections.Generic;
using System.Text.Json;
using Autodesk.AutoCAD.DatabaseServices;
using CablePlugin.Core.Data;
using CablePlugin.Core.Models;

namespace CablePlugin.Data
{
    /// <summary>
    /// Хранение таблицы соответствия труб в Named Dictionary чертежа.
    /// </summary>
    public class PipeTableRepository
    {
        private static readonly JsonSerializerOptions JsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            PropertyNameCaseInsensitive = true,
            WriteIndented = false
        };

        public List<PipeCorrespondence> Load(Database db, Transaction tr)
        {
            if (db == null || tr == null)
                return DefaultPipeTable.GetDefault();

            var nod = tr.GetObject(db.NamedObjectsDictionaryId, OpenMode.ForRead) as DBDictionary;
            if (nod == null || !nod.Contains(CableDataConstants.DrawingDictionaryName))
                return DefaultPipeTable.GetDefault();

            var pluginDict = tr.GetObject(nod.GetAt(CableDataConstants.DrawingDictionaryName), OpenMode.ForRead) as DBDictionary;
            if (pluginDict == null || !pluginDict.Contains(CableDataConstants.PipeTableRecordName))
                return DefaultPipeTable.GetDefault();

            var xrec = tr.GetObject(pluginDict.GetAt(CableDataConstants.PipeTableRecordName), OpenMode.ForRead) as Xrecord;
            var json = ReadJson(xrec);
            if (string.IsNullOrWhiteSpace(json))
                return DefaultPipeTable.GetDefault();

            try
            {
                var list = JsonSerializer.Deserialize<List<PipeCorrespondence>>(json, JsonOptions);
                return list != null && list.Count > 0 ? list : DefaultPipeTable.GetDefault();
            }
            catch (JsonException)
            {
                return DefaultPipeTable.GetDefault();
            }
        }

        public void Save(Database db, Transaction tr, IList<PipeCorrespondence> table)
        {
            if (db == null || tr == null)
                throw new ArgumentNullException(db == null ? nameof(db) : nameof(tr));

            var nod = tr.GetObject(db.NamedObjectsDictionaryId, OpenMode.ForWrite) as DBDictionary;
            if (nod == null)
                throw new InvalidOperationException("NamedObjectsDictionary недоступен.");

            DBDictionary pluginDict;
            if (nod.Contains(CableDataConstants.DrawingDictionaryName))
            {
                pluginDict = tr.GetObject(nod.GetAt(CableDataConstants.DrawingDictionaryName), OpenMode.ForWrite) as DBDictionary;
            }
            else
            {
                pluginDict = new DBDictionary();
                nod.SetAt(CableDataConstants.DrawingDictionaryName, pluginDict);
                tr.AddNewlyCreatedDBObject(pluginDict, true);
            }

            Xrecord xrec;
            if (pluginDict.Contains(CableDataConstants.PipeTableRecordName))
            {
                xrec = tr.GetObject(pluginDict.GetAt(CableDataConstants.PipeTableRecordName), OpenMode.ForWrite) as Xrecord;
            }
            else
            {
                xrec = new Xrecord();
                pluginDict.SetAt(CableDataConstants.PipeTableRecordName, xrec);
                tr.AddNewlyCreatedDBObject(xrec, true);
            }

            var json = JsonSerializer.Serialize(table ?? new List<PipeCorrespondence>(), JsonOptions);
            WriteJson(xrec, json);
        }

        private static string ReadJson(Xrecord xrec)
        {
            if (xrec?.Data == null) return null;
            var parts = new List<string>();
            foreach (TypedValue tv in xrec.Data)
            {
                if (tv.TypeCode == (int)DxfCode.Text && tv.Value is string s)
                    parts.Add(s);
            }
            return parts.Count > 0 ? string.Concat(parts) : null;
        }

        private static void WriteJson(Xrecord xrec, string json)
        {
            const int chunkSize = 200;
            var values = new List<TypedValue>();
            if (!string.IsNullOrEmpty(json))
            {
                for (int i = 0; i < json.Length; i += chunkSize)
                {
                    var len = Math.Min(chunkSize, json.Length - i);
                    values.Add(new TypedValue((int)DxfCode.Text, json.Substring(i, len)));
                }
            }
            xrec.Data = new ResultBuffer(values.ToArray());
        }
    }
}
