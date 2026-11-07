using System.Collections.Generic;
using CablePlugin.Core.Data;
using CablePlugin.Core.Models;
using CablePlugin.Core.Services;
using CablePlugin.Core.Serialization;
using Xunit;

namespace CablePlugin.Tests
{
    /// <summary>
    /// Сквозные сценарии, близкие к реальному использованию плагина.
    /// </summary>
    public class IntegrationScenarioTests
    {
        [Fact]
        public void FullScenario_MultiSegmentLine_PipeAutoSelect_Length_Electrical_Serialize()
        {
            // Arrange: линия как в ТЗ — лоток 15 м + кабель-канал 3 м + гофра 46 м
            var line = new CableLine
            {
                GroupName = "Освещение",
                From = "ЩО-1",
                To = "Светильники",
                Cable = new CableInfo
                {
                    Mark = "ВВГнг(А)-LS",
                    Cores = 3,
                    Section = 1.5,
                    CableCount = 1
                },
                Segments = new List<CableSegment>
                {
                    new CableSegment { LayingMethod = "Лоток", Length = 15 },
                    new CableSegment { LayingMethod = "Кабель-канал", Length = 3 },
                    new CableSegment { LayingMethod = "Гофра", Length = 46, IsPipeDiameterAuto = true }
                },
                ReservePercent = 20,
                AdditionalLength = 2.0,
                ScaleFactor = 1.0,
                RoundUpTo = 1.0,
                PowerKw = 1.2,
                Voltage = 220,
                CosPhi = 0.9,
                Phases = 1
            };

            // Act 1: автоподбор трубы
            var pipeSelector = new PipeSelector(DefaultPipeTable.GetDefault());
            pipeSelector.ApplyToLine(line);

            // Act 2: длина
            LengthCalculator.Calculate(line);

            // Act 3: электрика
            var electrical = new ElectricalCalculator();
            electrical.Calculate(line);

            // Act 4: сериализация (как в XRecord)
            var json = CableLineSerializer.Serialize(line);
            var restored = CableLineSerializer.Deserialize(json);

            // Assert — труба
            Assert.Null(line.Segments[0].PipeDiameter);
            Assert.Null(line.Segments[1].PipeDiameter);
            Assert.Equal(20, line.Segments[2].PipeDiameter);

            // Assert — длина: 64 * 1.2 + 2 = 78.8 → 79
            Assert.Equal(64, line.GeometricLength);
            Assert.Equal(79, line.TotalLength);

            // Assert — электрика
            Assert.True(line.Current > 0);
            Assert.True(line.VoltageDropPercent >= 0);
            Assert.False(string.IsNullOrEmpty(line.RecommendedBreaker));

            // Assert — round-trip
            Assert.NotNull(restored);
            Assert.Equal(line.GroupName, restored.GroupName);
            Assert.Equal(line.TotalLength, restored.TotalLength);
            Assert.Equal(20, restored.Segments[2].PipeDiameter);
            Assert.Equal(line.RecommendedBreaker, restored.RecommendedBreaker);
        }

        [Fact]
        public void FullScenario_FRLS_GetsLargerPipe_AndParallelCablesReduceDrop()
        {
            var line = new CableLine
            {
                Cable = new CableInfo
                {
                    Mark = "ВВГнг(А)-FRLS",
                    Cores = 3,
                    Section = 1.5,
                    CableCount = 2
                },
                Segments =
                {
                    new CableSegment { LayingMethod = "Гофра", Length = 30, IsPipeDiameterAuto = true }
                },
                ReservePercent = 10,
                AdditionalLength = 0,
                ScaleFactor = 1.0,
                RoundUpTo = 0.5,
                Current = 18, // задан вручную
                Voltage = 380,
                Phases = 3
            };

            var pipeSelector = new PipeSelector(DefaultPipeTable.GetDefault());
            pipeSelector.ApplyToLine(line);
            LengthCalculator.Calculate(line);

            var electrical = new ElectricalCalculator();
            electrical.Calculate(line);

            // FRLS 3х1.5 → 25 мм
            Assert.Equal(25, line.Segments[0].PipeDiameter);

            // 30 * 1.10 = 33.0, шаг округления 0.5 → 33.0
            Assert.Equal(33.0, line.TotalLength);

            // Параллельные кабели уменьшают потери относительно одного
            var dropOne = electrical.CalculateVoltageDropPercent(18, line.TotalLength, 1.5, 380, 3, 1);
            Assert.True(line.VoltageDropPercent < dropOne);

            // Ручной ток не должен быть затёрт
            Assert.Equal(18, line.Current);
        }
    }
}
