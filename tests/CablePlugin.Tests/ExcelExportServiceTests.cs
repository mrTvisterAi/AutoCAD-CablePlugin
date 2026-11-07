using System.Collections.Generic;
using System.IO;
using CablePlugin.Core.Models;
using CablePlugin.Core.Services;
using Xunit;

namespace CablePlugin.Tests
{
    public class ExcelExportServiceTests
    {
        [Fact]
        public void Export_CreatesFile_WithLinesAndSegments()
        {
            var lines = new List<CableLine>
            {
                new CableLine
                {
                    GroupName = "Освещение",
                    From = "ЩО-1",
                    To = "Коридор",
                    Cable = new CableInfo { Mark = "ВВГнг(А)-LS", Cores = 3, Section = 1.5, CableCount = 1 },
                    Segments = new List<CableSegment>
                    {
                        new CableSegment { LayingMethod = "Лоток", Length = 15 },
                        new CableSegment { LayingMethod = "Гофра", Length = 46, PipeDiameter = 20, IsPipeDiameterAuto = true }
                    },
                    ReservePercent = 20,
                    AdditionalLength = 2,
                    TotalLength = 79,
                    GeometricLength = 64,
                    Current = 4.2,
                    VoltageDropPercent = 1.1,
                    RecommendedBreaker = "6",
                    Voltage = 220,
                    Phases = 1,
                    CosPhi = 0.9,
                    PowerKw = 0.8
                }
            };

            var path = Path.Combine(Path.GetTempPath(), "CablePlugin_Test_Export.xlsx");
            if (File.Exists(path))
                File.Delete(path);

            var svc = new ExcelExportService();
            svc.Export(lines, path);

            Assert.True(File.Exists(path));
            Assert.True(new FileInfo(path).Length > 1000);

            try { File.Delete(path); } catch { /* ignore */ }
        }
    }
}
