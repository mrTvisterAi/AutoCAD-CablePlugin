using System;
using System.Collections.Generic;
using System.Linq;

namespace CablePlugin.Core.Models
{
    /// <summary>Материал жил кабеля.</summary>
    public enum ConductorMaterial
    {
        Copper = 0,
        Aluminum = 1
    }

    public class CableLine
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string GroupName { get; set; } = string.Empty;
        public string From { get; set; } = string.Empty;
        public string To { get; set; } = string.Empty;
        public CableInfo Cable { get; set; } = new CableInfo();
        public List<CableSegment> Segments { get; set; } = new List<CableSegment>();

        public double ReservePercent { get; set; } = 20.0;
        public double AdditionalLength { get; set; }
        public double ScaleFactor { get; set; } = 1.0;
        public double RoundUpTo { get; set; } = 1.0;

        /// <summary>
        /// true — при CABLERECALC / сохранении длины сегментов подтягиваются с полилинии.
        /// </summary>
        public bool SyncLengthFromPolyline { get; set; } = true;

        /// <summary>Материал жил (по умолчанию медь).</summary>
        public ConductorMaterial Conductor { get; set; } = ConductorMaterial.Copper;

        public double GeometricLength { get; set; }
        public double TotalLength { get; set; }
        public double Current { get; set; }
        public double VoltageDropPercent { get; set; }
        public string RecommendedBreaker { get; set; } = string.Empty;
        public double? PowerKw { get; set; }
        public double CosPhi { get; set; } = 0.85;
        public double Voltage { get; set; } = 380;
        public int Phases { get; set; } = 3;
        public int DataVersion { get; set; } = 2;
        public string PolylineHandle { get; set; } = string.Empty;

        public double SegmentsLengthSum => Segments?.Sum(s => s.Length) ?? 0;

        public bool IsAluminum
        {
            get => Conductor == ConductorMaterial.Aluminum;
            set => Conductor = value ? ConductorMaterial.Aluminum : ConductorMaterial.Copper;
        }

        public CableLine Clone()
        {
            return new CableLine
            {
                Id = Id,
                GroupName = GroupName,
                From = From,
                To = To,
                Cable = Cable?.Clone() ?? new CableInfo(),
                Segments = Segments?.Select(s => s.Clone()).ToList() ?? new List<CableSegment>(),
                ReservePercent = ReservePercent,
                AdditionalLength = AdditionalLength,
                ScaleFactor = ScaleFactor,
                RoundUpTo = RoundUpTo,
                SyncLengthFromPolyline = SyncLengthFromPolyline,
                Conductor = Conductor,
                GeometricLength = GeometricLength,
                TotalLength = TotalLength,
                Current = Current,
                VoltageDropPercent = VoltageDropPercent,
                RecommendedBreaker = RecommendedBreaker,
                PowerKw = PowerKw,
                CosPhi = CosPhi,
                Voltage = Voltage,
                Phases = Phases,
                DataVersion = DataVersion,
                PolylineHandle = PolylineHandle
            };
        }
    }
}
