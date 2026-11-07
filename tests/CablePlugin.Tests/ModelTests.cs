using System.Linq;
using CablePlugin.Core.Models;
using Xunit;

namespace CablePlugin.Tests
{
    public class ModelTests
    {
        [Fact]
        public void CableInfo_DisplayName_Format()
        {
            var info = new CableInfo
            {
                Mark = "ВВГнг(А)-LS",
                Cores = 3,
                Section = 1.5
            };

            Assert.Equal("ВВГнг(А)-LS 3х1.5", info.DisplayName);
        }

        [Fact]
        public void CableInfo_DisplayName_EmptyMark_ReturnsEmpty()
        {
            var info = new CableInfo();
            Assert.Equal(string.Empty, info.DisplayName);
        }

        [Fact]
        public void CableInfo_Clone_CreatesIndependentCopy()
        {
            var original = new CableInfo
            {
                Mark = "ВВГнг-LS",
                Cores = 5,
                Section = 2.5,
                CableCount = 2
            };

            var clone = original.Clone();
            clone.Mark = "Changed";
            clone.CableCount = 99;

            Assert.Equal("ВВГнг-LS", original.Mark);
            Assert.Equal(2, original.CableCount);
            Assert.Equal("Changed", clone.Mark);
        }

        [Fact]
        public void PipeCorrespondence_Key_IsStableAndCaseInsensitiveSource()
        {
            var a = PipeCorrespondence.BuildKey("ВВГнг(А)-LS", 3, 1.5);
            var b = PipeCorrespondence.BuildKey("ввгнг(а)-ls", 3, 1.5);

            Assert.Equal(a, b);
            Assert.Contains("3", a);
            Assert.Contains("1.5", a);
        }

        [Fact]
        public void CableSegment_Clone_PreservesPipeFlags()
        {
            var original = new CableSegment
            {
                LayingMethod = "Гофра",
                Length = 46,
                PipeDiameter = 20,
                IsPipeDiameterAuto = true,
                Note = "тест"
            };

            var clone = original.Clone();
            clone.PipeDiameter = 32;
            clone.IsPipeDiameterAuto = false;

            Assert.Equal(20, original.PipeDiameter);
            Assert.True(original.IsPipeDiameterAuto);
            Assert.Equal(32, clone.PipeDiameter);
            Assert.False(clone.IsPipeDiameterAuto);
        }

        [Fact]
        public void CableLine_Clone_DeepCopiesSegments()
        {
            var original = new CableLine
            {
                GroupName = "Освещение",
                Cable = new CableInfo { Mark = "ВВГнг(А)-LS", Cores = 3, Section = 1.5 },
                Segments =
                {
                    new CableSegment { LayingMethod = "Лоток", Length = 10 },
                    new CableSegment { LayingMethod = "Гофра", Length = 20, PipeDiameter = 20 }
                },
                ReservePercent = 20,
                TotalLength = 36
            };

            var clone = original.Clone();
            clone.GroupName = "Changed";
            clone.Segments[0].Length = 999;
            clone.Cable.Mark = "Other";

            Assert.Equal("Освещение", original.GroupName);
            Assert.Equal(10, original.Segments[0].Length);
            Assert.Equal("ВВГнг(А)-LS", original.Cable.Mark);
            Assert.Equal(2, clone.Segments.Count);
            Assert.Equal(999, clone.Segments[0].Length);
        }
    }
}
