using System.Collections.Generic;
using CablePlugin.Core.Models;

namespace CablePlugin.Core.Data
{
    /// <summary>
    /// Таблица соответствия труб/гофры по умолчанию.
    /// Пользователь может переопределить её в чертеже.
    /// </summary>
    public static class DefaultPipeTable
    {
        public static List<PipeCorrespondence> GetDefault()
        {
            return new List<PipeCorrespondence>
            {
                // ВВГнг(А)-LS
                new PipeCorrespondence { Mark = "ВВГнг(А)-LS", Cores = 3, Section = 1.5, Diameter = 20 },
                new PipeCorrespondence { Mark = "ВВГнг(А)-LS", Cores = 3, Section = 2.5, Diameter = 20 },
                new PipeCorrespondence { Mark = "ВВГнг(А)-LS", Cores = 3, Section = 4.0, Diameter = 25 },
                new PipeCorrespondence { Mark = "ВВГнг(А)-LS", Cores = 3, Section = 6.0, Diameter = 25 },
                new PipeCorrespondence { Mark = "ВВГнг(А)-LS", Cores = 3, Section = 10.0, Diameter = 32 },
                new PipeCorrespondence { Mark = "ВВГнг(А)-LS", Cores = 4, Section = 1.5, Diameter = 20 },
                new PipeCorrespondence { Mark = "ВВГнг(А)-LS", Cores = 4, Section = 2.5, Diameter = 25 },
                new PipeCorrespondence { Mark = "ВВГнг(А)-LS", Cores = 4, Section = 4.0, Diameter = 25 },
                new PipeCorrespondence { Mark = "ВВГнг(А)-LS", Cores = 5, Section = 1.5, Diameter = 25 },
                new PipeCorrespondence { Mark = "ВВГнг(А)-LS", Cores = 5, Section = 2.5, Diameter = 25 },

                // ВВГнг(А)-FRLS
                new PipeCorrespondence { Mark = "ВВГнг(А)-FRLS", Cores = 3, Section = 1.5, Diameter = 25 },
                new PipeCorrespondence { Mark = "ВВГнг(А)-FRLS", Cores = 3, Section = 2.5, Diameter = 25 },
                new PipeCorrespondence { Mark = "ВВГнг(А)-FRLS", Cores = 3, Section = 4.0, Diameter = 32 },
                new PipeCorrespondence { Mark = "ВВГнг(А)-FRLS", Cores = 4, Section = 1.5, Diameter = 25 },
                new PipeCorrespondence { Mark = "ВВГнг(А)-FRLS", Cores = 4, Section = 2.5, Diameter = 32 },

                // ВВГнг-LS
                new PipeCorrespondence { Mark = "ВВГнг-LS", Cores = 3, Section = 1.5, Diameter = 20 },
                new PipeCorrespondence { Mark = "ВВГнг-LS", Cores = 3, Section = 2.5, Diameter = 20 },
                new PipeCorrespondence { Mark = "ВВГнг-LS", Cores = 3, Section = 4.0, Diameter = 25 },
            };
        }
    }
}
