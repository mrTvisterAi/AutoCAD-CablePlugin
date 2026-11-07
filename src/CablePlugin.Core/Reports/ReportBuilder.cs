using System;
using System.Collections.Generic;
using System.Linq;
using CablePlugin.Core.Models;

namespace CablePlugin.Core.Reports
{
    /// <summary>
    /// Построение данных отчётов из списка CableLine.
    /// </summary>
    public static class ReportBuilder
    {
        public static List<JournalRow> BuildJournal(IEnumerable<CableLine> lines)
        {
            var list = Normalize(lines);
            var rows = new List<JournalRow>();
            int n = 1;

            foreach (var line in list.OrderBy(l => l.GroupName).ThenBy(l => l.From).ThenBy(l => l.To))
            {
                var cable = line.Cable ?? new CableInfo();
                var segs = line.Segments ?? new List<CableSegment>();

                var methods = string.Join("; ",
                    segs.Where(s => !string.IsNullOrWhiteSpace(s.LayingMethod))
                        .Select(s => $"{s.LayingMethod} {s.Length:0.###}м"));

                var pipes = string.Join("; ",
                    segs.Where(s => s.PipeDiameter.HasValue)
                        .Select(s => $"∅{s.PipeDiameter}"));

                rows.Add(new JournalRow
                {
                    Number = n++,
                    GroupName = line.GroupName ?? "",
                    From = line.From ?? "",
                    To = line.To ?? "",
                    CableMark = cable.Mark ?? "",
                    Cores = cable.Cores,
                    Section = cable.Section,
                    CableCount = cable.CableCount,
                    Length = line.TotalLength,
                    LayingMethods = methods,
                    PipeDiameters = pipes,
                    Current = line.Current,
                    VoltageDropPercent = line.VoltageDropPercent,
                    Breaker = line.RecommendedBreaker ?? "",
                    Note = ""
                });
            }

            return rows;
        }

        public static BillOfMaterials BuildBom(IEnumerable<CableLine> lines)
        {
            var list = Normalize(lines);
            var bom = new BillOfMaterials();

            bom.Cables = list
                .GroupBy(l => new
                {
                    Mark = l.Cable?.Mark ?? "",
                    Cores = l.Cable?.Cores ?? 0,
                    Section = l.Cable?.Section ?? 0
                })
                .OrderBy(g => g.Key.Mark)
                .ThenBy(g => g.Key.Cores)
                .ThenBy(g => g.Key.Section)
                .Select(g => new BomCableRow
                {
                    Mark = g.Key.Mark,
                    Cores = g.Key.Cores,
                    Section = g.Key.Section,
                    LineCount = g.Count(),
                    TotalLength = Math.Round(g.Sum(x => x.TotalLength), 3)
                })
                .ToList();

            bom.Pipes = list
                .SelectMany(l => l.Segments ?? Enumerable.Empty<CableSegment>())
                .GroupBy(s => new
                {
                    Method = s.LayingMethod ?? "",
                    Diameter = s.PipeDiameter
                })
                .OrderBy(g => g.Key.Method)
                .ThenBy(g => g.Key.Diameter)
                .Select(g => new BomPipeRow
                {
                    LayingMethod = g.Key.Method,
                    Diameter = g.Key.Diameter,
                    TotalLength = Math.Round(g.Sum(s => s.Length), 3),
                    SegmentCount = g.Count()
                })
                .ToList();

            return bom;
        }

        /// <summary>
        /// Линии щита: поле From совпадает с именем щита (без учёта регистра).
        /// </summary>
        public static PanelReport BuildPanel(IEnumerable<CableLine> lines, string panelName, double demandFactor = 1.0)
        {
            if (demandFactor <= 0) demandFactor = 1.0;
            panelName = panelName?.Trim() ?? "";

            var feeders = Normalize(lines)
                .Where(l => string.Equals(l.From?.Trim(), panelName, StringComparison.OrdinalIgnoreCase))
                .OrderBy(l => l.GroupName)
                .ThenBy(l => l.To)
                .ToList();

            var report = new PanelReport
            {
                PanelName = panelName,
                DemandFactor = demandFactor
            };

            int n = 1;
            foreach (var line in feeders)
            {
                var cable = line.Cable ?? new CableInfo();
                report.Feeders.Add(new PanelFeederRow
                {
                    Number = n++,
                    Designation = string.IsNullOrWhiteSpace(line.GroupName)
                        ? (line.To ?? $"L{n}")
                        : line.GroupName,
                    To = line.To ?? "",
                    CableMark = cable.Mark ?? "",
                    Cores = cable.Cores,
                    Section = cable.Section,
                    Length = line.TotalLength,
                    Current = line.Current,
                    Breaker = line.RecommendedBreaker ?? "",
                    VoltageDropPercent = line.VoltageDropPercent,
                    PowerKw = line.PowerKw
                });
            }

            report.SumCurrent = Math.Round(report.Feeders.Sum(f => f.Current), 2);
            report.DemandCurrent = Math.Round(report.SumCurrent * demandFactor, 2);
            report.SumPowerKw = Math.Round(report.Feeders.Sum(f => f.PowerKw ?? 0), 3);
            report.DemandPowerKw = Math.Round(report.SumPowerKw * demandFactor, 3);

            return report;
        }

        /// <summary>Уникальные имена щитов (поле From).</summary>
        public static List<string> ListPanels(IEnumerable<CableLine> lines)
        {
            return Normalize(lines)
                .Select(l => l.From?.Trim())
                .Where(s => !string.IsNullOrWhiteSpace(s))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(s => s)
                .ToList();
        }

        private static List<CableLine> Normalize(IEnumerable<CableLine> lines)
        {
            return lines?.Where(l => l != null).ToList() ?? new List<CableLine>();
        }
    }
}
