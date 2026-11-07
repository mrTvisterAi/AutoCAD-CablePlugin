using System.Collections.Generic;
using CablePlugin.Core.Models;

namespace CablePlugin.Core.Reports
{
    /// <summary>Строка кабельного журнала.</summary>
    public class JournalRow
    {
        public int Number { get; set; }
        public string GroupName { get; set; } = string.Empty;
        public string From { get; set; } = string.Empty;
        public string To { get; set; } = string.Empty;
        public string CableMark { get; set; } = string.Empty;
        public int Cores { get; set; }
        public double Section { get; set; }
        public int CableCount { get; set; }
        public double Length { get; set; }
        public string LayingMethods { get; set; } = string.Empty;
        public string PipeDiameters { get; set; } = string.Empty;
        public double Current { get; set; }
        public double VoltageDropPercent { get; set; }
        public string Breaker { get; set; } = string.Empty;
        public string Note { get; set; } = string.Empty;
    }

    /// <summary>Строка ведомости по кабелю.</summary>
    public class BomCableRow
    {
        public string Mark { get; set; } = string.Empty;
        public int Cores { get; set; }
        public double Section { get; set; }
        public int LineCount { get; set; }
        public double TotalLength { get; set; }
        public string DisplayName => string.IsNullOrWhiteSpace(Mark) ? string.Empty : $"{Mark} {Cores}х{Section}";
    }

    /// <summary>Строка ведомости по трубам/гофре.</summary>
    public class BomPipeRow
    {
        public string LayingMethod { get; set; } = string.Empty;
        public double? Diameter { get; set; }
        public double TotalLength { get; set; }
        public int SegmentCount { get; set; }
    }

    /// <summary>Ведомость объёмов.</summary>
    public class BillOfMaterials
    {
        public List<BomCableRow> Cables { get; set; } = new List<BomCableRow>();
        public List<BomPipeRow> Pipes { get; set; } = new List<BomPipeRow>();
    }

    /// <summary>Строка отходящей линии щита.</summary>
    public class PanelFeederRow
    {
        public int Number { get; set; }
        public string Designation { get; set; } = string.Empty;
        public string To { get; set; } = string.Empty;
        public string CableMark { get; set; } = string.Empty;
        public int Cores { get; set; }
        public double Section { get; set; }
        public double Length { get; set; }
        public double Current { get; set; }
        public string Breaker { get; set; } = string.Empty;
        public double VoltageDropPercent { get; set; }
        public double? PowerKw { get; set; }
    }

    /// <summary>Данные схемы/таблицы щита.</summary>
    public class PanelReport
    {
        public string PanelName { get; set; } = string.Empty;
        public double DemandFactor { get; set; } = 1.0;
        public List<PanelFeederRow> Feeders { get; set; } = new List<PanelFeederRow>();
        public double SumCurrent { get; set; }
        public double DemandCurrent { get; set; }
        public double SumPowerKw { get; set; }
        public double DemandPowerKw { get; set; }
    }
}
