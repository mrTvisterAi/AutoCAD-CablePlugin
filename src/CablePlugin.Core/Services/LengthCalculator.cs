using System;
using System.Linq;
using CablePlugin.Core.Models;

namespace CablePlugin.Core.Services
{
    public static class LengthCalculator
    {
        /// <summary>
        /// Подтянуть длины сегментов к актуальной длине полилинии.
        /// Один сегмент — полностью; несколько — пропорционально.
        /// </summary>
        public static void SyncSegmentsFromPolyline(CableLine line, double polylineLength)
        {
            if (line == null || polylineLength < 0) return;

            line.GeometricLength = polylineLength;

            if (line.Segments == null || line.Segments.Count == 0)
            {
                line.Segments = new System.Collections.Generic.List<CableSegment>
                {
                    new CableSegment { LayingMethod = "Гофра", Length = polylineLength, IsPipeDiameterAuto = true }
                };
                return;
            }

            if (line.Segments.Count == 1)
            {
                line.Segments[0].Length = polylineLength;
                return;
            }

            double oldSum = line.Segments.Sum(s => s.Length);
            if (oldSum <= 1e-9)
            {
                // Равномерно
                double each = polylineLength / line.Segments.Count;
                foreach (var s in line.Segments)
                    s.Length = each;
            }
            else
            {
                double k = polylineLength / oldSum;
                foreach (var s in line.Segments)
                    s.Length = Math.Round(s.Length * k, 6);
            }
        }

        public static void Calculate(CableLine line, double? geometricLengthFromPolyline = null)
        {
            if (line == null) throw new ArgumentNullException(nameof(line));

            double baseLength;

            if (line.Segments != null && line.Segments.Count > 0)
            {
                baseLength = line.Segments.Sum(s => s.Length);
            }
            else if (geometricLengthFromPolyline.HasValue)
            {
                baseLength = geometricLengthFromPolyline.Value;
            }
            else
            {
                baseLength = line.GeometricLength;
            }

            double scaledLength = baseLength * (line.ScaleFactor <= 0 ? 1.0 : line.ScaleFactor);
            double withReserve = scaledLength * (1.0 + line.ReservePercent / 100.0);
            double total = withReserve + line.AdditionalLength;

            if (line.RoundUpTo > 0)
                total = Math.Ceiling(total / line.RoundUpTo) * line.RoundUpTo;

            line.GeometricLength = baseLength;
            line.TotalLength = Math.Round(total, 3);
        }

        public static double CalculateTotal(double baseLength, double scaleFactor, double reservePercent,
            double additionalLength, double roundUpTo)
        {
            double scaled = baseLength * (scaleFactor <= 0 ? 1.0 : scaleFactor);
            double withReserve = scaled * (1.0 + reservePercent / 100.0);
            double total = withReserve + additionalLength;

            if (roundUpTo > 0)
                total = Math.Ceiling(total / roundUpTo) * roundUpTo;

            return Math.Round(total, 3);
        }
    }
}
