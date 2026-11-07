using CablePlugin.Core.Models;
using CablePlugin.Core.Services;
using Xunit;

namespace CablePlugin.Tests
{
    public class LengthCalculatorTests
    {
        [Fact]
        public void Calculate_WithSegments_AppliesReserveAndAdditionalLength()
        {
            var line = new CableLine
            {
                ScaleFactor = 1.0,
                ReservePercent = 20,
                AdditionalLength = 2.0,
                RoundUpTo = 1.0,
                Segments =
                {
                    new CableSegment { LayingMethod = "Лоток", Length = 15 },
                    new CableSegment { LayingMethod = "Кабель-канал", Length = 3 },
                    new CableSegment { LayingMethod = "Гофра", Length = 46 }
                }
            };

            LengthCalculator.Calculate(line);

            // 15+3+46 = 64
            // 64 * 1.20 = 76.8
            // 76.8 + 2 = 78.8 → округление вверх до 79
            Assert.Equal(64, line.GeometricLength);
            Assert.Equal(79, line.TotalLength);
        }

        [Fact]
        public void Calculate_WithScaleFactor()
        {
            var line = new CableLine
            {
                ScaleFactor = 0.001, // чертёж в мм → метры
                ReservePercent = 10,
                AdditionalLength = 0,
                RoundUpTo = 0.1,
                Segments = { new CableSegment { Length = 12000 } } // 12 м
            };

            LengthCalculator.Calculate(line);

            // 12000 * 0.001 = 12
            // 12 * 1.10 = 13.2 → округление до 13.2
            Assert.Equal(12, line.GeometricLength);
            Assert.Equal(13.2, line.TotalLength);
        }

        [Theory]
        [InlineData(10, 20, 0, 1.0, 12)]
        [InlineData(10, 0, 1.5, 0.5, 11.5)]
        public void CalculateTotal_Works(double baseLen, double reserve, double add, double roundTo, double expected)
        {
            var result = LengthCalculator.CalculateTotal(baseLen, 1.0, reserve, add, roundTo);
            Assert.Equal(expected, result);
        }
    }
}
