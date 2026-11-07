using CablePlugin.Core.Models;
using CablePlugin.Core.Services;
using Xunit;

namespace CablePlugin.Tests
{
    public class ElectricalCalculatorTests
    {
        private readonly ElectricalCalculator _calc = new ElectricalCalculator();

        // ===== Расчёт тока =====

        [Fact]
        public void CalculateCurrent_ThreePhase_Correct()
        {
            // P = 10 кВт, U = 380 В, cosφ = 0.85, 3 фазы
            // I = 10000 / (√3 * 380 * 0.85) ≈ 17.87 А
            var current = _calc.CalculateCurrent(10, 380, 0.85, 3);

            Assert.InRange(current, 17.8, 17.9);
        }

        [Fact]
        public void CalculateCurrent_SinglePhase_Correct()
        {
            // P = 2 кВт, U = 220 В, cosφ = 0.95, 1 фаза
            // I = 2000 / (220 * 0.95) ≈ 9.57 А
            var current = _calc.CalculateCurrent(2, 220, 0.95, 1);

            Assert.InRange(current, 9.5, 9.6);
        }

        [Theory]
        [InlineData(0, 380, 0.85, 3)]
        [InlineData(10, 0, 0.85, 3)]
        [InlineData(10, 380, 0, 3)]
        public void CalculateCurrent_InvalidInput_ReturnsZero(double p, double u, double cos, int phases)
        {
            var current = _calc.CalculateCurrent(p, u, cos, phases);
            Assert.Equal(0, current);
        }

        // ===== Потери напряжения =====

        [Fact]
        public void CalculateVoltageDrop_ThreePhase_ReasonableValue()
        {
            // 20 А, 50 м, 2.5 мм², 380 В, 3 фазы, 1 кабель
            // ΔU% = (√3 * 0.0175 * 50 * 20) / 2.5 / 380 * 100 ≈ 3.18 %
            var drop = _calc.CalculateVoltageDropPercent(
                current: 20,
                lengthMeters: 50,
                sectionMm2: 2.5,
                voltage: 380,
                phases: 3,
                cableCount: 1);

            Assert.InRange(drop, 3.0, 3.4);
        }

        [Fact]
        public void CalculateVoltageDrop_ParallelCables_ReducesDrop()
        {
            var dropOne = _calc.CalculateVoltageDropPercent(20, 50, 2.5, 380, 3, cableCount: 1);
            var dropTwo = _calc.CalculateVoltageDropPercent(20, 50, 2.5, 380, 3, cableCount: 2);

            Assert.True(dropTwo < dropOne);
            Assert.InRange(dropTwo, dropOne * 0.45, dropOne * 0.55); // примерно в 2 раза меньше
        }

        [Fact]
        public void CalculateVoltageDrop_SinglePhase_HigherThanThreePhase()
        {
            var drop1 = _calc.CalculateVoltageDropPercent(10, 30, 1.5, 220, 1);
            var drop3 = _calc.CalculateVoltageDropPercent(10, 30, 1.5, 220, 3);

            Assert.True(drop1 > drop3);
        }

        [Theory]
        [InlineData(0, 50, 2.5, 380, 3)]
        [InlineData(20, 0, 2.5, 380, 3)]
        [InlineData(20, 50, 0, 380, 3)]
        public void CalculateVoltageDrop_InvalidInput_ReturnsZero(
            double i, double l, double s, double u, int ph)
        {
            var drop = _calc.CalculateVoltageDropPercent(i, l, s, u, ph);
            Assert.Equal(0, drop);
        }

        // ===== Подбор АВ =====

        [Theory]
        [InlineData(9.5, "10")]
        [InlineData(15, "16")]
        [InlineData(16, "16")]
        [InlineData(16.1, "20")]
        [InlineData(30, "32")]
        [InlineData(63, "63")]
        [InlineData(0, "")]
        public void SelectBreaker_ReturnsCorrectRating(double current, string expected)
        {
            var result = _calc.SelectBreaker(current);
            Assert.Equal(expected, result);
        }

        [Fact]
        public void SelectBreaker_WithSafetyFactor()
        {
            _calc.BreakerSafetyFactor = 1.25;
            // 20 А * 1.25 = 25 → должен выбрать 25
            var result = _calc.SelectBreaker(20);
            Assert.Equal("25", result);
        }

        // ===== Полный расчёт линии =====

        [Fact]
        public void Calculate_FullLine_FillsAllFields()
        {
            var line = new CableLine
            {
                PowerKw = 8,
                Voltage = 380,
                CosPhi = 0.85,
                Phases = 3,
                TotalLength = 40,
                Cable = new CableInfo
                {
                    Mark = "ВВГнг(А)-LS",
                    Cores = 3,
                    Section = 2.5,
                    CableCount = 1
                }
            };

            _calc.Calculate(line);

            Assert.True(line.Current > 0);
            Assert.True(line.VoltageDropPercent > 0);
            Assert.False(string.IsNullOrEmpty(line.RecommendedBreaker));
        }

        [Fact]
        public void Calculate_NoPower_DoesNotOverwriteCurrent()
        {
            var line = new CableLine
            {
                Current = 12.5, // задан вручную
                TotalLength = 30,
                Cable = new CableInfo { Section = 1.5, CableCount = 1 },
                Voltage = 380,
                Phases = 3
            };

            _calc.Calculate(line);

            Assert.Equal(12.5, line.Current); // не должен затереть
            Assert.True(line.VoltageDropPercent > 0);
            Assert.Equal("13", line.RecommendedBreaker);
        }
    }
}
