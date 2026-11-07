using Autodesk.AutoCAD.Runtime;
using CablePlugin.UIHost;

namespace CablePlugin.Commands
{
    public class PropertiesCommands
    {
        /// <summary>
        /// Показать / скрыть палитру свойств кабеля.
        /// </summary>
        [CommandMethod("CABLEPROPS", CommandFlags.Modal)]
        public void CableProps()
        {
            CablePropertiesPalette.Toggle();
        }

        /// <summary>
        /// Обновить палитру по текущему выделению.
        /// </summary>
        [CommandMethod("CABLEPROPSREFRESH", CommandFlags.Modal)]
        public void CablePropsRefresh()
        {
            CablePropertiesPalette.Show();
            CablePropertiesPalette.RefreshFromSelection();
        }
    }
}
