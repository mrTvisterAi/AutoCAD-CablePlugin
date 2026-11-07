using System;
using System.Collections.Generic;
using System.Text.Json;
using Autodesk.AutoCAD.DatabaseServices;
using CablePlugin.Core.Templates;

namespace CablePlugin.Data
{
    /// <summary>
    /// Хранение набора шаблонов отчётов в Named Dictionary чертежа.
    /// </summary>
    public class TemplateRepository
    {
        private static readonly JsonSerializerOptions JsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            PropertyNameCaseInsensitive = true,
            WriteIndented = false
        };

        public TemplateSet Load(Database db, Transaction tr)
        {
            if (db == null || tr == null)
                return TemplateSet.CreateDefault();

            var nod = tr.GetObject(db.NamedObjectsDictionaryId, OpenMode.ForRead) as DBDictionary;
            if (nod == null || !nod.Contains(CableDataConstants.DrawingDictionaryName))
                return TemplateSet.CreateDefault();

            var pluginDict = tr.GetObject(nod.GetAt(CableDataConstants.DrawingDictionaryName), OpenMode.ForRead) as DBDictionary;
            if (pluginDict == null || !pluginDict.Contains(CableDataConstants.TemplateSetRecordName))
                return TemplateSet.CreateDefault();

            var xrec = tr.GetObject(pluginDict.GetAt(CableDataConstants.TemplateSetRecordName), OpenMode.ForRead) as Xrecord;
            var json = ReadJson(xrec);
            if (string.IsNullOrWhiteSpace(json))
                return TemplateSet.CreateDefault();

            try
            {
                var set = JsonSerializer.Deserialize<TemplateSet>(json, JsonOptions);
                return MergeWithDefaults(set);
            }
            catch (JsonException)
            {
                return TemplateSet.CreateDefault();
            }
        }

        public void Save(Database db, Transaction tr, TemplateSet set)
        {
            if (db == null || tr == null)
                throw new ArgumentNullException(db == null ? nameof(db) : nameof(tr));

            var nod = (DBDictionary)tr.GetObject(db.NamedObjectsDictionaryId, OpenMode.ForWrite);

            DBDictionary pluginDict;
            if (nod.Contains(CableDataConstants.DrawingDictionaryName))
            {
                pluginDict = (DBDictionary)tr.GetObject(nod.GetAt(CableDataConstants.DrawingDictionaryName), OpenMode.ForWrite);
            }
            else
            {
                pluginDict = new DBDictionary();
                nod.SetAt(CableDataConstants.DrawingDictionaryName, pluginDict);
                tr.AddNewlyCreatedDBObject(pluginDict, true);
            }

            Xrecord xrec;
            if (pluginDict.Contains(CableDataConstants.TemplateSetRecordName))
            {
                xrec = (Xrecord)tr.GetObject(pluginDict.GetAt(CableDataConstants.TemplateSetRecordName), OpenMode.ForWrite);
            }
            else
            {
                xrec = new Xrecord();
                pluginDict.SetAt(CableDataConstants.TemplateSetRecordName, xrec);
                tr.AddNewlyCreatedDBObject(xrec, true);
            }

            var json = JsonSerializer.Serialize(set ?? TemplateSet.CreateDefault(), JsonOptions);
            WriteJson(xrec, json);
        }

        private static TemplateSet MergeWithDefaults(TemplateSet set)
        {
            var def = TemplateSet.CreateDefault();
            if (set == null) return def;
            if (set.Journal == null || set.Journal.Columns == null || set.Journal.Columns.Count == 0)
                set.Journal = def.Journal;
            if (set.Bom == null || set.Bom.Columns == null || set.Bom.Columns.Count == 0)
                set.Bom = def.Bom;
            if (set.Panel == null || set.Panel.Columns == null || set.Panel.Columns.Count == 0)
                set.Panel = def.Panel;
            if (set.PanelBlocks == null)
                set.PanelBlocks = def.PanelBlocks;
            return set;
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
