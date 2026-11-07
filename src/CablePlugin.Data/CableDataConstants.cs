namespace CablePlugin.Data
{
    /// <summary>
    /// Имена словарей и записей для хранения данных плагина в DWG.
    /// </summary>
    public static class CableDataConstants
    {
        /// <summary>
        /// Extension Dictionary на полилинии.
        /// </summary>
        public const string EntityDictionaryName = "GrokCableData";

        /// <summary>
        /// XRecord внутри extension dictionary с JSON линии.
        /// </summary>
        public const string LineRecordName = "CableLine";

        /// <summary>
        /// Named Dictionary чертежа для глобальных справочников.
        /// </summary>
        public const string DrawingDictionaryName = "GrokCableDictionaries";

        /// <summary>
        /// XRecord с таблицей соответствия труб (JSON).
        /// </summary>
        public const string PipeTableRecordName = "PipeTable";

        /// <summary>
        /// RegApp для XData (резерв / совместимость).
        /// </summary>
        public const string RegAppName = "GROK_CABLE_PLUGIN";
    }
}
