using System.Collections.Generic;
using CablePlugin.Core.Models;
using CablePlugin.Core.Reports;
using Xunit;

namespace CablePlugin.Tests
{
    public class ReportBuilderTests
    {
        private static List<CableLine> SampleLines()
        {
            return new List<CableLine>
            {
                new CableLine
                {
                    GroupName = "Освещение",
                    From = "ЩО-1",
                    To = "Коридор",
                    Cable = new CableInfo { Mark = "ВВГнг(А)-LS", Cores = 3, Section = 1.5 },
                    Segments =
                    {
                        new CableSegment { LayingMethod = "Лоток", Length = 15 },
                        new CableSegment { LayingMethod = "Гофра", Length = 46, PipeDiameter = 20 }
                    },
                    TotalLength = 79,
                    Current = 4,
                    VoltageDropPercent = 1.2,
                    RecommendedBreaker = "6",
                    PowerKw = 0.8
                },
                new CableLine
                {
                    GroupName = "Розетки",
                    From = "ЩО-1",
                    To = "Кабинет",
                    Cable = new CableInfo { Mark = "ВВГнг(А)-LS", Cores = 3, Section = 2.5 },
                    Segments =
                    {
                        new CableSegment { LayingMethod = "Гофра", Length = 30, PipeDiameter = 20 }
                    },
                    TotalLength = 36,
                    Current = 12,
                    VoltageDropPercent = 2.0,
                    RecommendedBreaker = "16",
                    PowerKw = 2.5
                },
                new CableLine
                {
                    GroupName = "Силовые",
                    From = "ЩС-1",
                    To = "Станок",
                    Cable = new CableInfo { Mark = "ВВГнг(А)-LS", Cores = 5, Section = 6 },
                    Segments =
                    {
                        new CableSegment { LayingMethod = "Лоток", Length = 50 }
                    },
                    TotalLength = 60,
                    Current = 40,
                    RecommendedBreaker = "50",
                    PowerKw = 20
                }
            };
        }

        [Fact]
        public void BuildJournal_OrdersAndFillsRows()
        {
            var rows = ReportBuilder.BuildJournal(SampleLines());
            Assert.Equal(3, rows.Count);
            Assert.Equal(1, rows[0].Number);
            Assert.Contains("Гофра", rows[0].LayingMethods);
        }

        [Fact]
        public void BuildBom_GroupsCablesAndPipes()
        {
            var bom = ReportBuilder.BuildBom(SampleLines());
            Assert.Equal(3, bom.Cables.Count);
            Assert.True(bom.Pipes.Count >= 2);

            var vvg15 = bom.Cables.Find(c => c.Section == 1.5);
            Assert.NotNull(vvg15);
            Assert.Equal(79, vvg15.TotalLength);
        }

        [Fact]
        public void BuildPanel_FiltersByFrom_AppliesDemandFactor()
        {
            var panel = ReportBuilder.BuildPanel(SampleLines(), "ЩО-1", 0.8);
            Assert.Equal(2, panel.Feeders.Count);
            Assert.Equal(16, panel.SumCurrent); // 4+12
            Assert.Equal(12.8, panel.DemandCurrent);
            Assert.Equal(3.3, panel.SumPowerKw);
            Assert.Equal(2.64, panel.DemandPowerKw);
        }

        [Fact]
        public void ListPanels_ReturnsDistinct()
        {
            var panels = ReportBuilder.ListPanels(SampleLines());
            Assert.Equal(2, panels.Count);
            Assert.Contains("ЩО-1", panels);
            Assert.Contains("ЩС-1", panels);
        }
    }
}
