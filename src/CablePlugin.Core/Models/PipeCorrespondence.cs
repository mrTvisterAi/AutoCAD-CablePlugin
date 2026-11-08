using System.Globalization;

namespace CablePlugin.Core.Models
{
    public class PipeCorrespondence
    {
        public string Mark { get; set; } = string.Empty;
        public int Cores { get; set; }
        public double Section { get; set; }
        public double Diameter { get; set; }

        public string Key => BuildKey(Mark, Cores, Section);

        public static string BuildKey(string mark, int cores, double section)
        {
            // Инвариантная культура — иначе 1.5 и 1,5 дают разные ключи
            return string.Format(
                CultureInfo.InvariantCulture,
                "{0}|{1}|{2:0.###}",
                (mark ?? string.Empty).Trim().ToUpperInvariant(),
                cores,
                section);
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
