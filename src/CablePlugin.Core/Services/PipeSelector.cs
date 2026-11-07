using System;
using System.Collections.Generic;
using System.Linq;
using CablePlugin.Core.Models;

namespace CablePlugin.Core.Services
{
    /// <summary>
    /// Автоматический подбор диаметра трубы/гофры по таблице соответствия.
    /// </summary>
    public class PipeSelector
    {
        private readonly Dictionary<string, PipeCorrespondence> _table;

        public PipeSelector(IEnumerable<PipeCorrespondence> correspondences = null)
        {
            _table = new Dictionary<string, PipeCorrespondence>(StringComparer.OrdinalIgnoreCase);

            if (correspondences != null)
            {
                foreach (var item in correspondences)
                {
                    if (item != null && !string.IsNullOrWhiteSpace(item.Mark))
                        _table[item.Key] = item;
                }
            }
        }

        /// <summary>
        /// Найти диаметр по марке, жилам и сечению.
        /// </summary>
        public double? FindDiameter(string mark, int cores, double section)
        {
            if (string.IsNullOrWhiteSpace(mark))
                return null;

            var key = PipeCorrespondence.BuildKey(mark, cores, section);
            return _table.TryGetValue(key, out var item) ? item.Diameter : (double?)null;
        }

        /// <summary>
        /// Применить автоподбор диаметра ко всем сегментам линии,
        /// у которых способ прокладки требует трубы/гофры и диаметр ещё не задан вручную.
        /// </summary>
        public void ApplyToLine(CableLine line, IEnumerable<string> pipeMethods = null)
        {
            if (line?.Cable == null || line.Segments == null)
                return;

            // Способы прокладки, для которых нужен подбор трубы
            var methods = new HashSet<string>(
                pipeMethods ?? new[] { "Гофра", "Труба", "Труба ПВХ", "Гофрированная труба", "Металлорукав" },
                StringComparer.OrdinalIgnoreCase);

            foreach (var segment in line.Segments)
            {
                if (!methods.Contains(segment.LayingMethod))
                    continue;

                // Если пользователь задал диаметр вручную — не трогаем
                if (!segment.IsPipeDiameterAuto && segment.PipeDiameter.HasValue)
                    continue;

                var diameter = FindDiameter(line.Cable.Mark, line.Cable.Cores, line.Cable.Section);
                segment.PipeDiameter = diameter;
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
