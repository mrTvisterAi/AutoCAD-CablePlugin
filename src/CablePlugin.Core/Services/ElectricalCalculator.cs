using System;
using System.Collections.Generic;
using CablePlugin.Core.Models;

namespace CablePlugin.Core.Services
{
    /// <summary>
    /// Электрические расчёты: ток, потери напряжения, подбор АВ.
    /// Потери напряжения — упрощённая реализация по ГОСТ Р 50571.5.52-2011 Приложение G.
    /// </summary>
    public class ElectricalCalculator
    {
        // Удельное сопротивление меди при 20°C, Ом·мм²/м
        private const double RhoCopper = 0.0175;

        // Стандартный ряд номиналов автоматических выключателей
        private static readonly double[] BreakerRatings =
        {
            6, 10, 13, 16, 20, 25, 32, 40, 50, 63, 80, 100, 125, 160, 200, 250, 315, 400, 630
        };

        /// <summary>
        /// Коэффициент запаса при подборе АВ (по умолчанию 1.0 = без запаса).
        /// </summary>
        public double BreakerSafetyFactor { get; set; } = 1.0;

        /// <summary>
        /// Рассчитать ток по мощности.
        /// </summary>
        public double CalculateCurrent(double powerKw, double voltage, double cosPhi, int phases)
        {
            if (powerKw <= 0 || voltage <= 0 || cosPhi <= 0)
                return 0;

            if (phases == 1)
            {
                // Однофазная сеть
                return (powerKw * 1000.0) / (voltage * cosPhi);
            }

            // Трёхфазная сеть
            return (powerKw * 1000.0) / (Math.Sqrt(3) * voltage * cosPhi);
        }

        /// <summary>
        /// Рассчитать потери напряжения, %.
        /// Упрощённая формула для активной составляющей (приложение G).
        /// ΔU = (b * ρ * L * I) / (A * U) * 100%
        /// где b = 2 для однофазной, b = √3 для трёхфазной.
        /// </summary>
        public double CalculateVoltageDropPercent(
            double current,
            double lengthMeters,
            double sectionMm2,
            double voltage,
            int phases,
            int cableCount = 1,
            double rho = RhoCopper)
        {
            if (current <= 0 || lengthMeters <= 0 || sectionMm2 <= 0 || voltage <= 0)
                return 0;

            // Учитываем параллельные кабели
            double effectiveSection = sectionMm2 * Math.Max(1, cableCount);

            double b = phases == 1 ? 2.0 : Math.Sqrt(3);

            // ΔU в вольтах
            double deltaU = (b * rho * lengthMeters * current) / effectiveSection;

            // В процентах
            return Math.Round((deltaU / voltage) * 100.0, 2);
        }

        /// <summary>
        /// Подобрать ближайший больший номинал АВ.
        /// </summary>
        public string SelectBreaker(double current)
        {
            if (current <= 0)
                return string.Empty;

            double required = current * BreakerSafetyFactor;

            foreach (var rating in BreakerRatings)
            {
                if (rating >= required)
                    return rating.ToString("0.##");
            }

            // Если ток больше самого большого номинала
            return BreakerRatings[BreakerRatings.Length - 1].ToString("0.##") + "+";
        }

        /// <summary>
        /// Выполнить полный расчёт для линии.
        /// </summary>
        public void Calculate(CableLine line)
        {
            if (line == null) return;

            // 1. Ток
            if (line.PowerKw.HasValue && line.PowerKw.Value > 0)
            {
                line.Current = Math.Round(
                    CalculateCurrent(line.PowerKw.Value, line.Voltage, line.CosPhi, line.Phases), 2);
            }

            // 2. Потери напряжения (используем итоговую длину)
            if (line.Cable != null && line.TotalLength > 0 && line.Current > 0)
            {
                line.VoltageDropPercent = CalculateVoltageDropPercent(
                    line.Current,
                    line.TotalLength,
                    line.Cable.Section,
                    line.Voltage,
                    line.Phases,
                    line.Cable.CableCount);
            }

            // 3. Подбор АВ
            line.RecommendedBreaker = SelectBreaker(line.Current);
        }
    }
}
