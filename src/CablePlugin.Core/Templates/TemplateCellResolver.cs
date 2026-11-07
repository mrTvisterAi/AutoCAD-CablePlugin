using System.Globalization;
using CablePlugin.Core.Reports;

namespace CablePlugin.Core.Templates
{
    /// <summary>
    /// Преобразует id колонки шаблона в текст ячейки.
    /// </summary>
    public static class TemplateCellResolver
    {
        public static string Journal(string columnId, JournalRow row)
        {
            if (row == null) return string.Empty;
            switch ((columnId ?? string.Empty).ToLowerInvariant())
            {
                case "num": return row.Number.ToString(CultureInfo.InvariantCulture);
                case "group": return row.GroupName ?? "";
                case "from": return row.From ?? "";
                case "to": return row.To ?? "";
                case "mark": return row.CableMark ?? "";
                case "cores": return row.Cores.ToString(CultureInfo.InvariantCulture);
                case "section": return Format(row.Section);
                case "cores_section": return row.Cores + "х" + Format(row.Section);
                case "count": return row.CableCount.ToString(CultureInfo.InvariantCulture);
                case "length": return Format(row.Length);
                case "laying": return row.LayingMethods ?? "";
                case "pipe": return row.PipeDiameters ?? "";
                case "current": return Format(row.Current);
                case "du": return Format(row.VoltageDropPercent);
                case "breaker": return row.Breaker ?? "";
                case "note": return row.Note ?? "";
                default: return string.Empty;
            }
        }

        public static string BomCable(string columnId, BomCableRow row)
        {
            if (row == null) return string.Empty;
            switch ((columnId ?? string.Empty).ToLowerInvariant())
            {
                case "mark": return row.Mark ?? "";
                case "cores": return row.Cores.ToString(CultureInfo.InvariantCulture);
                case "section": return Format(row.Section);
                case "lines": return row.LineCount.ToString(CultureInfo.InvariantCulture);
                case "length": return Format(row.TotalLength);
                case "display": return row.DisplayName;
                default: return string.Empty;
            }
        }

        public static string Panel(string columnId, PanelFeederRow row)
        {
            if (row == null) return string.Empty;
            switch ((columnId ?? string.Empty).ToLowerInvariant())
            {
                case "num": return row.Number.ToString(CultureInfo.InvariantCulture);
                case "designation": return row.Designation ?? "";
                case "to": return row.To ?? "";
                case "mark": return row.CableMark ?? "";
                case "cores": return row.Cores.ToString(CultureInfo.InvariantCulture);
                case "section": return Format(row.Section);
                case "cable": return (row.CableMark ?? "") + " " + row.Cores + "х" + Format(row.Section);
                case "length": return Format(row.Length);
                case "current": return Format(row.Current);
                case "breaker": return row.Breaker ?? "";
                case "du": return Format(row.VoltageDropPercent);
                case "power": return row.PowerKw.HasValue ? Format(row.PowerKw.Value) : "";
                default: return string.Empty;
            }
        }

        private static string Format(double v) => v.ToString("0.###", CultureInfo.InvariantCulture);
    }
}
