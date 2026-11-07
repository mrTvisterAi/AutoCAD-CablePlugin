using System;
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
        public void Calculate_WithScaleFactor_MmToMeters()
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

        [Fact]
        public void Calculate_NoSegments_UsesGeometricLengthFromPolyline()
        {
            var line = new CableLine
            {
                ScaleFactor = 1.0,
                ReservePercent = 15,
                AdditionalLength = 1.0,
                RoundUpTo = 0.5,
                Segments = { }
            };

            LengthCalculator.Calculate(line, geometricLengthFromPolyline: 20);

            // 20 * 1.15 = 23 + 1 = 24 → округление до 24.0
            Assert.Equal(20, line.GeometricLength);
            Assert.Equal(24, line.TotalLength);
        }

        [Fact]
        public void Calculate_SegmentsHavePriorityOverPolylineLength()
        {
            var line = new CableLine
            {
                ScaleFactor = 1.0,
                ReservePercent = 0,
                AdditionalLength = 0,
                RoundUpTo = 0,
                Segments = { new CableSegment { Length = 50 } }
            };

            LengthCalculator.Calculate(line, geometricLengthFromPolyline: 999);

            Assert.Equal(50, line.GeometricLength);
            Assert.Equal(50, line.TotalLength);
        }

        [Fact]
        public void Calculate_ZeroRoundUp_NoRounding()
        {
            var line = new CableLine
            {
                ScaleFactor = 1.0,
                ReservePercent = 10,
                AdditionalLength = 0,
                RoundUpTo = 0,
                Segments = { new CableSegment { Length = 10 } }
            };

            LengthCalculator.Calculate(line);

            Assert.Equal(11, line.TotalLength); // 10 * 1.1 = 11 точно
        }

        [Fact]
        public void Calculate_NullLine_Throws()
        {
            Assert.Throws<ArgumentNullException>(() => LengthCalculator.Calculate(null));
        }

        [Theory]
        [InlineData(10, 20, 0, 1.0, 12)]      // 10*1.2=12
        [InlineData(10, 0, 1.5, 0.5, 11.5)]   // 10+1.5=11.5
        [InlineData(10, 10, 0.3, 1.0, 12)]     // 11+0.3=11.3 → 12
        [InlineData(7.2, 0, 0, 0.5, 7.5)]      // округление 7.2 → 7.5
        public void CalculateTotal_Works(double baseLen, double reserve, double add, double roundTo, double expected)
        {
            var result = LengthCalculator.CalculateTotal(baseLen, 1.0, reserve, add, roundTo);
            Assert.Equal(expected, result);
        }

        [Fact]
        public void CalculateTotal_WithScale()
        {
            // base=1000 (мм), scale=0.001 → 1 м, запас 20% → 1.2
            var result = LengthCalculator.CalculateTotal(1000, 0.001, 20, 0, 0.1);
            Assert.Equal(1.2, result);
        }

        [Fact]
        public void SegmentsLengthSum_ReturnsCorrectSum()
        {
            var line = new CableLine
            {
                Segments =
                {
                    new CableSegment { Length = 15 },
                    new CableSegment { Length = 3 },
                    new CableSegment { Length = 46 }
                }
            };

            Assert.Equal(64, line.SegmentsLengthSum);
        }
    }
}
