using System;
using System.Linq;
using CablePlugin.Core.Models;

namespace CablePlugin.Core.Services
{
    /// <summary>
    /// Расчёт итоговой длины кабеля.
    /// </summary>
    public static class LengthCalculator
    {
        /// <summary>
        /// Рассчитывает итоговую длину линии.
        /// </summary>
        /// <param name="line">Кабельная линия</param>
        /// <param name="geometricLengthFromPolyline">Геометрическая длина полилинии в единицах чертежа (если Segments пусты)</param>
        public static void Calculate(CableLine line, double? geometricLengthFromPolyline = null)
        {
            if (line == null) throw new ArgumentNullException(nameof(line));

            double baseLength;

            if (line.Segments != null && line.Segments.Count > 0)
            {
                // Приоритет — сумма длин сегментов
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

            // Применяем масштаб
            double scaledLength = baseLength * (line.ScaleFactor <= 0 ? 1.0 : line.ScaleFactor);

            // Запас в процентах
            double withReserve = scaledLength * (1.0 + line.ReservePercent / 100.0);

            // Дополнительная длина на опуски/подъёмы
            double total = withReserve + line.AdditionalLength;

            // Округление вверх
            if (line.RoundUpTo > 0)
            {
                total = Math.Ceiling(total / line.RoundUpTo) * line.RoundUpTo;
            }

            line.GeometricLength = baseLength;
            line.TotalLength = Math.Round(total, 3);
        }

        /// <summary>
        /// Быстрый расчёт без изменения объекта.
        /// </summary>
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
