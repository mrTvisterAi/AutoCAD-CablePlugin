using System;
using System.Collections.Generic;
using CablePlugin.Core.Models;
using CablePlugin.Core.Serialization;
using Xunit;

namespace CablePlugin.Tests
{
    public class CableLineSerializerTests
    {
        private CableLine CreateSampleLine()
        {
            return new CableLine
            {
                Id = Guid.Parse("11111111-1111-1111-1111-111111111111"),
                GroupName = "Освещение этажа 1",
                From = "ЩО-1",
                To = "Светильники коридора",
                Cable = new CableInfo
                {
                    Mark = "ВВГнг(А)-LS",
                    Cores = 3,
                    Section = 1.5,
                    CableCount = 1
                },
                Segments = new List<CableSegment>
                {
                    new CableSegment { LayingMethod = "Лоток", Length = 15, Note = "по коридору" },
                    new CableSegment { LayingMethod = "Гофра", Length = 46, PipeDiameter = 20, IsPipeDiameterAuto = true },
                    new CableSegment { LayingMethod = "Кабель-канал", Length = 3 }
                },
                ReservePercent = 20,
                AdditionalLength = 2.5,
                ScaleFactor = 1.0,
                RoundUpTo = 1.0,
                GeometricLength = 64,
                TotalLength = 79,
                Current = 4.2,
                VoltageDropPercent = 1.15,
                RecommendedBreaker = "6",
                PowerKw = 1.5,
                CosPhi = 0.9,
                Voltage = 220,
                Phases = 1,
                DataVersion = 1,
                PolylineHandle = "1A2B"
            };
        }

        [Fact]
        public void Serialize_Deserialize_RoundTrip_PreservesAllData()
        {
            var original = CreateSampleLine();

            var json = CableLineSerializer.Serialize(original);
            var restored = CableLineSerializer.Deserialize(json);

            Assert.NotNull(restored);
            Assert.Equal(original.Id, restored.Id);
            Assert.Equal(original.GroupName, restored.GroupName);
            Assert.Equal(original.From, restored.From);
            Assert.Equal(original.To, restored.To);

            Assert.Equal(original.Cable.Mark, restored.Cable.Mark);
            Assert.Equal(original.Cable.Cores, restored.Cable.Cores);
            Assert.Equal(original.Cable.Section, restored.Cable.Section);
            Assert.Equal(original.Cable.CableCount, restored.Cable.CableCount);

            Assert.Equal(3, restored.Segments.Count);
            Assert.Equal("Лоток", restored.Segments[0].LayingMethod);
            Assert.Equal(15, restored.Segments[0].Length);
            Assert.Equal("Гофра", restored.Segments[1].LayingMethod);
            Assert.Equal(20, restored.Segments[1].PipeDiameter);
            Assert.True(restored.Segments[1].IsPipeDiameterAuto);

            Assert.Equal(original.ReservePercent, restored.ReservePercent);
            Assert.Equal(original.AdditionalLength, restored.AdditionalLength);
            Assert.Equal(original.TotalLength, restored.TotalLength);
            Assert.Equal(original.Current, restored.Current);
            Assert.Equal(original.VoltageDropPercent, restored.VoltageDropPercent);
            Assert.Equal(original.RecommendedBreaker, restored.RecommendedBreaker);
            Assert.Equal(original.PowerKw, restored.PowerKw);
            Assert.Equal(original.PolylineHandle, restored.PolylineHandle);
        }

        [Fact]
        public void SerializeToBytes_DeserializeFromBytes_Works()
        {
            var original = CreateSampleLine();

            var bytes = CableLineSerializer.SerializeToBytes(original);
            var restored = CableLineSerializer.DeserializeFromBytes(bytes);

            Assert.NotNull(restored);
            Assert.Equal(original.GroupName, restored.GroupName);
            Assert.Equal(original.Segments.Count, restored.Segments.Count);
        }

        [Fact]
        public void Deserialize_NullOrEmpty_ReturnsNull()
        {
            Assert.Null(CableLineSerializer.Deserialize(null));
            Assert.Null(CableLineSerializer.Deserialize(""));
            Assert.Null(CableLineSerializer.Deserialize("   "));
        }

        [Fact]
        public void Deserialize_InvalidJson_ReturnsNull()
        {
            Assert.Null(CableLineSerializer.Deserialize("{ not valid json }"));
        }

        [Fact]
        public void Serialize_Null_Throws()
        {
            Assert.Throws<ArgumentNullException>(() => CableLineSerializer.Serialize(null));
        }

        [Fact]
        public void Deserialize_MinimalJson_Works()
        {
            var json = "{\"groupName\":\"Тест\",\"cable\":{\"mark\":\"ВВГнг-LS\",\"cores\":3,\"section\":1.5}}";
            var line = CableLineSerializer.Deserialize(json);

            Assert.NotNull(line);
            Assert.Equal("Тест", line.GroupName);
            Assert.Equal("ВВГнг-LS", line.Cable.Mark);
            Assert.Equal(3, line.Cable.Cores);
            Assert.Equal(1.5, line.Cable.Section);
        }
    }
}
