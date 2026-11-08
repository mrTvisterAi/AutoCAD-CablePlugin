using System;
using CablePlugin.Core.Models;

namespace CablePlugin.Core.Services
{
    /// <summary>
    /// Ток, потери напряжения (прил. G, упрощённо), подбор АВ.
    /// ρ меди / алюминия учитывается через ConductorMaterial.
    /// </summary>
    public class ElectricalCalculator
    {
        /// <summary>Удельное сопротивление меди, Ом·мм²/м (≈20°C).</summary>
        public const double RhoCopper = 0.0175;

        /// <summary>Удельное сопротивление алюминия, Ом·мм²/м (≈20°C).</summary>
        public const double RhoAluminum = 0.0283;

        private static readonly double[] BreakerRatings =
        {
            6, 10, 13, 16, 20, 25, 32, 40, 50, 63, 80, 100, 125, 160, 200, 250, 315, 400, 630
        };

        public double BreakerSafetyFactor { get; set; } = 1.0;

        public static double RhoFor(ConductorMaterial material)
        {
            return material == ConductorMaterial.Aluminum ? RhoAluminum : RhoCopper;
        }

        public double CalculateCurrent(double powerKw, double voltage, double cosPhi, int phases)
        {
            if (powerKw <= 0 || voltage <= 0 || cosPhi <= 0)
                return 0;

            if (phases == 1)
                return (powerKw * 1000.0) / (voltage * cosPhi);

            return (powerKw * 1000.0) / (Math.Sqrt(3) * voltage * cosPhi);
        }

        /// <summary>
        /// ΔU% = (b · ρ · L · I) / (A · U) · 100
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

            double effectiveSection = sectionMm2 * Math.Max(1, cableCount);
            double b = phases == 1 ? 2.0 : Math.Sqrt(3);
            double deltaU = (b * rho * lengthMeters * current) / effectiveSection;
            return Math.Round((deltaU / voltage) * 100.0, 2);
        }

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

            return BreakerRatings[BreakerRatings.Length - 1].ToString("0.##") + "+";
        }

        public void Calculate(CableLine line)
        {
            if (line == null) return;

            if (line.PowerKw.HasValue && line.PowerKw.Value > 0)
            {
                line.Current = Math.Round(
                    CalculateCurrent(line.PowerKw.Value, line.Voltage, line.CosPhi, line.Phases), 2);
            }

            if (line.Cable != null && line.TotalLength > 0 && line.Current > 0)
            {
                double rho = RhoFor(line.Conductor);
                line.VoltageDropPercent = CalculateVoltageDropPercent(
                    line.Current,
                    line.TotalLength,
                    line.Cable.Section,
                    line.Voltage,
                    line.Phases,
                    line.Cable.CableCount,
                    rho);
            }
            else
            {
                line.VoltageDropPercent = 0;
            }

            line.RecommendedBreaker = SelectBreaker(line.Current);
        }
    }
}
