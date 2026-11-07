using System;
using Autodesk.AutoCAD.DatabaseServices;
using CablePlugin.Core.Models;
using CablePlugin.Core.Serialization;

namespace CablePlugin.Data
{
    /// <summary>
    /// Чтение и запись CableLine в Extension Dictionary полилинии.
    /// </summary>
    public class CableLineRepository
    {
        /// <summary>
        /// Прочитать данные кабеля с объекта. Transaction должна быть открыта.
        /// </summary>
        public CableLine Load(ObjectId entityId, Transaction tr)
        {
            if (entityId.IsNull || tr == null)
                return null;

            var entity = tr.GetObject(entityId, OpenMode.ForRead) as Entity;
            if (entity == null)
                return null;

            if (!entity.ExtensionDictionary.IsValid || entity.ExtensionDictionary.IsNull)
                return null;

            var extDict = tr.GetObject(entity.ExtensionDictionary, OpenMode.ForRead) as DBDictionary;
            if (extDict == null || !extDict.Contains(CableDataConstants.EntityDictionaryName))
                return null;

            var nestedId = extDict.GetAt(CableDataConstants.EntityDictionaryName);
            var nested = tr.GetObject(nestedId, OpenMode.ForRead) as DBDictionary;
            if (nested == null || !nested.Contains(CableDataConstants.LineRecordName))
                return null;

            var xrecId = nested.GetAt(CableDataConstants.LineRecordName);
            var xrec = tr.GetObject(xrecId, OpenMode.ForRead) as Xrecord;
            if (xrec == null)
                return null;

            var json = ReadJsonFromXrecord(xrec);
            if (string.IsNullOrWhiteSpace(json))
                return null;

            var line = CableLineSerializer.Deserialize(json);
            if (line != null)
            {
                line.PolylineHandle = entity.Handle.ToString();
            }

            return line;
        }

        /// <summary>
        /// Сохранить данные кабеля на объект. Transaction должна быть открыта.
        /// Entity будет открыт ForWrite при необходимости.
        /// </summary>
        public void Save(ObjectId entityId, CableLine line, Transaction tr)
        {
            if (entityId.IsNull || line == null || tr == null)
                throw new ArgumentNullException(line == null ? nameof(line) : nameof(entityId));

            var entity = tr.GetObject(entityId, OpenMode.ForWrite) as Entity;
            if (entity == null)
                throw new InvalidOperationException("Объект не является Entity или недоступен для записи.");

            line.PolylineHandle = entity.Handle.ToString();

            // Extension dictionary на entity
            if (entity.ExtensionDictionary.IsNull || !entity.ExtensionDictionary.IsValid)
            {
                entity.CreateExtensionDictionary();
            }

            var extDict = tr.GetObject(entity.ExtensionDictionary, OpenMode.ForWrite) as DBDictionary;
            if (extDict == null)
                throw new InvalidOperationException("Не удалось открыть ExtensionDictionary.");

            // Вложенный словарь плагина
            DBDictionary pluginDict;
            if (extDict.Contains(CableDataConstants.EntityDictionaryName))
            {
                pluginDict = tr.GetObject(extDict.GetAt(CableDataConstants.EntityDictionaryName), OpenMode.ForWrite) as DBDictionary;
            }
            else
            {
                pluginDict = new DBDictionary();
                extDict.SetAt(CableDataConstants.EntityDictionaryName, pluginDict);
                tr.AddNewlyCreatedDBObject(pluginDict, true);
            }

            if (pluginDict == null)
                throw new InvalidOperationException("Не удалось создать словарь данных кабеля.");

            // Xrecord с JSON
            var json = CableLineSerializer.Serialize(line);
            Xrecord xrec;

            if (pluginDict.Contains(CableDataConstants.LineRecordName))
            {
                xrec = tr.GetObject(pluginDict.GetAt(CableDataConstants.LineRecordName), OpenMode.ForWrite) as Xrecord;
            }
            else
            {
                xrec = new Xrecord();
                pluginDict.SetAt(CableDataConstants.LineRecordName, xrec);
                tr.AddNewlyCreatedDBObject(xrec, true);
            }

            if (xrec == null)
                throw new InvalidOperationException("Не удалось создать Xrecord.");

            WriteJsonToXrecord(xrec, json);
        }

        /// <summary>
        /// Удалить данные кабеля с объекта.
        /// </summary>
        public bool Remove(ObjectId entityId, Transaction tr)
        {
            if (entityId.IsNull || tr == null)
                return false;

            var entity = tr.GetObject(entityId, OpenMode.ForWrite) as Entity;
            if (entity == null || entity.ExtensionDictionary.IsNull)
                return false;

            var extDict = tr.GetObject(entity.ExtensionDictionary, OpenMode.ForWrite) as DBDictionary;
            if (extDict == null || !extDict.Contains(CableDataConstants.EntityDictionaryName))
                return false;

            extDict.Remove(CableDataConstants.EntityDictionaryName);
            return true;
        }

        /// <summary>
        /// Есть ли на объекте данные кабеля.
        /// </summary>
        public bool HasData(ObjectId entityId, Transaction tr)
        {
            return Load(entityId, tr) != null;
        }

        private static string ReadJsonFromXrecord(Xrecord xrec)
        {
            if (xrec?.Data == null)
                return null;

            foreach (TypedValue tv in xrec.Data)
            {
                if (tv.TypeCode == (int)DxfCode.Text || tv.TypeCode == (int)DxfCode.ExtendedDataAsciiString)
                {
                    var s = tv.Value as string;
                    if (!string.IsNullOrEmpty(s))
                        return s;
                }
            }

            // Склейка длинных строк, разбитых на несколько Text
            var parts = new System.Collections.Generic.List<string>();
            foreach (TypedValue tv in xrec.Data)
            {
                if (tv.TypeCode == (int)DxfCode.Text && tv.Value is string part)
                    parts.Add(part);
            }

            return parts.Count > 0 ? string.Concat(parts) : null;
        }

        private static void WriteJsonToXrecord(Xrecord xrec, string json)
        {
            // Xrecord Text ограничен ~255–2049 символами в зависимости от версии;
            // разбиваем на чанки по 200 символов для надёжности.
            const int chunkSize = 200;
            var values = new System.Collections.Generic.List<TypedValue>();

            if (string.IsNullOrEmpty(json))
            {
                xrec.Data = new ResultBuffer();
                return;
            }

            for (int i = 0; i < json.Length; i += chunkSize)
            {
                var len = Math.Min(chunkSize, json.Length - i);
                values.Add(new TypedValue((int)DxfCode.Text, json.Substring(i, len)));
            }

            xrec.Data = new ResultBuffer(values.ToArray());
        }
    }
}
