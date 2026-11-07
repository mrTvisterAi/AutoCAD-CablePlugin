using System.Collections.Generic;
using CablePlugin.Core.Data;
using CablePlugin.Core.Models;
using CablePlugin.Core.Services;
using Xunit;

namespace CablePlugin.Tests
{
    public class PipeSelectorTests
    {
        private readonly PipeSelector _selector;

        public PipeSelectorTests()
        {
            _selector = new PipeSelector(DefaultPipeTable.GetDefault());
        }

        [Theory]
        [InlineData("ВВГнг(А)-LS", 3, 1.5, 20)]
        [InlineData("ВВГнг(А)-LS", 3, 2.5, 20)]
        [InlineData("ВВГнг(А)-LS", 3, 4.0, 25)]
        [InlineData("ВВГнг(А)-FRLS", 3, 1.5, 25)]
        [InlineData("ВВГнг(А)-FRLS", 3, 2.5, 25)]
        [InlineData("ВВГнг(А)-FRLS", 4, 2.5, 32)]
        public void FindDiameter_KnownCable_ReturnsCorrectDiameter(
            string mark, int cores, double section, double expectedDiameter)
        {
            var result = _selector.FindDiameter(mark, cores, section);

            Assert.NotNull(result);
            Assert.Equal(expectedDiameter, result.Value);
        }

        [Fact]
        public void FindDiameter_UnknownCable_ReturnsNull()
        {
            var result = _selector.FindDiameter("НесуществующийКабель", 3, 1.5);

            Assert.Null(result);
        }

        [Fact]
        public void FindDiameter_EmptyMark_ReturnsNull()
        {
            var result = _selector.FindDiameter("", 3, 1.5);

            Assert.Null(result);
        }

        [Fact]
        public void ApplyToLine_SetsDiameterForPipeMethods()
        {
            var line = new CableLine
            {
                Cable = new CableInfo
                {
                    Mark = "ВВГнг(А)-LS",
                    Cores = 3,
                    Section = 1.5
                },
                Segments = new List<CableSegment>
                {
                    new CableSegment { LayingMethod = "Лоток", Length = 15 },
                    new CableSegment { LayingMethod = "Гофра", Length = 46, IsPipeDiameterAuto = true },
                    new CableSegment { LayingMethod = "Кабель-канал", Length = 3 }
                }
            };

            _selector.ApplyToLine(line);

            Assert.Null(line.Segments[0].PipeDiameter);          // Лоток — не труба
            Assert.Equal(20, line.Segments[1].PipeDiameter);     // Гофра — должна подобраться
            Assert.Null(line.Segments[2].PipeDiameter);          // Кабель-канал — не труба
        }

        [Fact]
        public void ApplyToLine_DoesNotOverwriteManualDiameter()
        {
            var line = new CableLine
            {
                Cable = new CableInfo
                {
                    Mark = "ВВГнг(А)-LS",
                    Cores = 3,
                    Section = 1.5
                },
                Segments = new List<CableSegment>
                {
                    new CableSegment
                    {
                        LayingMethod = "Гофра",
                        Length = 46,
                        PipeDiameter = 32,          // задано вручную
                        IsPipeDiameterAuto = false
                    }
                }
            };

            _selector.ApplyToLine(line);

            Assert.Equal(32, line.Segments[0].PipeDiameter);
            Assert.False(line.Segments[0].IsPipeDiameterAuto);
        }

        [Fact]
        public void ApplyToLine_FRLS_SelectsLargerDiameter()
        {
            var line = new CableLine
            {
                Cable = new CableInfo
                {
                    Mark = "ВВГнг(А)-FRLS",
                    Cores = 3,
                    Section = 1.5
                },
                Segments = new List<CableSegment>
                {
                    new CableSegment { LayingMethod = "Гофра", Length = 10, IsPipeDiameterAuto = true }
                }
            };

            _selector.ApplyToLine(line);

            Assert.Equal(25, line.Segments[0].PipeDiameter);
        }

        [Fact]
        public void UpdateTable_ReplacesExistingEntries()
        {
            var custom = new List<PipeCorrespondence>
            {
                new PipeCorrespondence { Mark = "Тест", Cores = 3, Section = 1.5, Diameter = 40 }
            };

            _selector.UpdateTable(custom);

            Assert.Equal(40, _selector.FindDiameter("Тест", 3, 1.5));
            Assert.Null(_selector.FindDiameter("ВВГнг(А)-LS", 3, 1.5)); // старая таблица очищена
        }
    }
}
