using System;
using System.Collections.Generic;
using System.Linq;

namespace CablePlugin.Core.Models
{
    /// <summary>
    /// Кабельная линия, привязанная к полилинии AutoCAD.
    /// </summary>
    public class CableLine
    {
        public Guid Id { get; set; } = Guid.NewGuid();

        /// <summary>
        /// Наименование группы (для распределённых сетей освещения, розеток и т.д.).
        /// </summary>
        public string GroupName { get; set; } = string.Empty;

        /// <summary>
        /// Начало линии (от какого щита).
        /// </summary>
        public string From { get; set; } = string.Empty;

        /// <summary>
        /// Конец линии (до какого потребителя).
        /// </summary>
        public string To { get; set; } = string.Empty;

        public CableInfo Cable { get; set; } = new CableInfo();

        /// <summary>
        /// Участки прокладки с разными способами.
        /// </summary>
        public List<CableSegment> Segments { get; set; } = new List<CableSegment>();

        /// <summary>
        /// Процент запаса длины.
        /// </summary>
        public double ReservePercent { get; set; } = 20.0;

        /// <summary>
        /// Дополнительная длина на опуски и подъёмы, м.
        /// </summary>
        public double AdditionalLength { get; set; }

        /// <summary>
        /// Масштаб чертежа (1.0 = 1:1 в метрах).
        /// </summary>
        public double ScaleFactor { get; set; } = 1.0;

        /// <summary>
        /// Правило округления итоговой длины вверх (м).
        /// 0 = без округления, 0.1, 0.5, 1.0 и т.д.
        /// </summary>
        public double RoundUpTo { get; set; } = 1.0;

        // ===== Расчётные поля =====

        /// <summary>
        /// Геометрическая длина полилинии (до применения масштаба и запаса), м.
        /// </summary>
        public double GeometricLength { get; set; }

        /// <summary>
        /// Итоговая длина кабеля с запасом и доп. длиной, м.
        /// </summary>
        public double TotalLength { get; set; }

        /// <summary>
        /// Расчётный ток, А.
        /// </summary>
        public double Current { get; set; }

        /// <summary>
        /// Потери напряжения, %.
        /// </summary>
        public double VoltageDropPercent { get; set; }

        /// <summary>
        /// Рекомендуемый номинал автоматического выключателя.
        /// </summary>
        public string RecommendedBreaker { get; set; } = string.Empty;

        /// <summary>
        /// Мощность нагрузки, кВт (если задана).
        /// </summary>
        public double? PowerKw { get; set; }

        /// <summary>
        /// Коэффициент мощности.
        /// </summary>
        public double CosPhi { get; set; } = 0.85;

        /// <summary>
        /// Напряжение сети, В.
        /// </summary>
        public double Voltage { get; set; } = 380;

        /// <summary>
        /// Количество фаз (1 или 3).
        /// </summary>
        public int Phases { get; set; } = 3;

        /// <summary>
        /// Версия формата данных (для миграций).
        /// </summary>
        public int DataVersion { get; set; } = 1;

        /// <summary>
        /// Handle полилинии в чертеже (строка).
        /// </summary>
        public string PolylineHandle { get; set; } = string.Empty;

        /// <summary>
        /// Сумма длин всех сегментов (без запаса).
        /// </summary>
        public double SegmentsLengthSum => Segments?.Sum(s => s.Length) ?? 0;

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
