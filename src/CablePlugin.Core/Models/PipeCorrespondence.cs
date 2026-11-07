namespace CablePlugin.Core.Models
{
    /// <summary>
    /// Запись таблицы соответствия: марка + жилы + сечение → диаметр трубы/гофры.
    /// </summary>
    public class PipeCorrespondence
    {
        public string Mark { get; set; } = string.Empty;
        public int Cores { get; set; }
        public double Section { get; set; }

        /// <summary>
        /// Диаметр трубы/гофры, мм.
        /// </summary>
        public double Diameter { get; set; }

        /// <summary>
        /// Ключ для быстрого поиска.
        /// </summary>
        public string Key => BuildKey(Mark, Cores, Section);

        public static string BuildKey(string mark, int cores, double section)
        {
            return $"{mark?.Trim().ToUpperInvariant()}|{cores}|{section:0.###}";
        }

        public PipeCorrespondence Clone()
        {
            return new PipeCorrespondence
            {
                Mark = Mark,
                Cores = Cores,
                Section = Section,
                Diameter = Diameter
            };
        }
    }
}
