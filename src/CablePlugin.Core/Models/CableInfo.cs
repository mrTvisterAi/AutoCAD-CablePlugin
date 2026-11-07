namespace CablePlugin.Core.Models
{
    /// <summary>
    /// Информация о марке и параметрах кабеля.
    /// </summary>
    public class CableInfo
    {
        /// <summary>
        /// Марка кабеля (например, ВВГнг(А)-LS).
        /// </summary>
        public string Mark { get; set; } = string.Empty;

        /// <summary>
        /// Количество жил.
        /// </summary>
        public int Cores { get; set; }

        /// <summary>
        /// Сечение жилы, мм².
        /// </summary>
        public double Section { get; set; }

        /// <summary>
        /// Количество параллельных кабелей в линии.
        /// </summary>
        public int CableCount { get; set; } = 1;

        /// <summary>
        /// Отображаемое имя (Марка NxS).
        /// </summary>
        public string DisplayName => string.IsNullOrWhiteSpace(Mark)
            ? string.Empty
            : $"{Mark} {Cores}х{Section}";

        public CableInfo Clone()
        {
            return new CableInfo
            {
                Mark = Mark,
                Cores = Cores,
                Section = Section,
                CableCount = CableCount
            };
        }
    }
}
