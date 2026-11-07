namespace CablePlugin.Core.Models
{
    /// <summary>
    /// Участок кабельной линии с одним способом прокладки.
    /// </summary>
    public class CableSegment
    {
        /// <summary>
        /// Способ прокладки (Лоток, Кабель-канал, Гофра, Труба и т.д.).
        /// </summary>
        public string LayingMethod { get; set; } = string.Empty;

        /// <summary>
        /// Длина участка, м.
        /// </summary>
        public double Length { get; set; }

        /// <summary>
        /// Диаметр трубы/гофры, мм (автоматически подобранный или заданный вручную).
        /// </summary>
        public double? PipeDiameter { get; set; }

        /// <summary>
        /// Примечание к участку.
        /// </summary>
        public string Note { get; set; } = string.Empty;

        /// <summary>
        /// Признак, что диаметр был подобран автоматически.
        /// </summary>
        public bool IsPipeDiameterAuto { get; set; } = true;

        public CableSegment Clone()
        {
            return new CableSegment
            {
                LayingMethod = LayingMethod,
                Length = Length,
                PipeDiameter = PipeDiameter,
                Note = Note,
                IsPipeDiameterAuto = IsPipeDiameterAuto
            };
        }
    }
}
