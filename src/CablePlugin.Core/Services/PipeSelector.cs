using System;
using System.Collections.Generic;
using CablePlugin.Core.Models;

namespace CablePlugin.Core.Services
{
    public class PipeSelector
    {
        private readonly Dictionary<string, PipeCorrespondence> _table;

        public PipeSelector(IEnumerable<PipeCorrespondence> correspondences = null)
        {
            _table = new Dictionary<string, PipeCorrespondence>(StringComparer.OrdinalIgnoreCase);
            UpdateTable(correspondences);
        }

        public double? FindDiameter(string mark, int cores, double section)
        {
            if (string.IsNullOrWhiteSpace(mark))
                return null;

            var key = PipeCorrespondence.BuildKey(mark, cores, section);
            return _table.TryGetValue(key, out var item) ? item.Diameter : (double?)null;
        }

        /// <summary>
        /// Применить диаметры из текущей таблицы.
        /// forceAutoOnly=false и forceAll=true — перезаписать все «трубные» сегменты по таблице.
        /// </summary>
        public void ApplyToLine(CableLine line, bool forceAll = false, IEnumerable<string> pipeMethods = null)
        {
            if (line?.Cable == null || line.Segments == null)
                return;

            var methods = new HashSet<string>(
                pipeMethods ?? new[] { "Гофра", "Труба", "Труба ПВХ", "Гофрированная труба", "Металлорукав" },
                StringComparer.OrdinalIgnoreCase);

            foreach (var segment in line.Segments)
            {
                if (!methods.Contains(segment.LayingMethod ?? string.Empty))
                    continue;

                // Ручной диаметр не трогаем, если не forceAll
                if (!forceAll && !segment.IsPipeDiameterAuto && segment.PipeDiameter.HasValue)
                    continue;

                var diameter = FindDiameter(line.Cable.Mark, line.Cable.Cores, line.Cable.Section);
                segment.PipeDiameter = diameter;
                if (forceAll || segment.IsPipeDiameterAuto || !segment.PipeDiameter.HasValue)
                    segment.IsPipeDiameterAuto = true;
            }
        }

        public IReadOnlyDictionary<string, PipeCorrespondence> Table => _table;

        public void UpdateTable(IEnumerable<PipeCorrespondence> correspondences)
        {
            _table.Clear();
            if (correspondences == null) return;

            foreach (var item in correspondences)
            {
                if (item != null && !string.IsNullOrWhiteSpace(item.Mark))
                    _table[item.Key] = item;
            }
        }
    }
}
