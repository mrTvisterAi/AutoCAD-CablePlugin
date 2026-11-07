using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.Runtime;

[assembly: ExtensionApplication(typeof(CablePlugin.PluginApp))]
[assembly: CommandClass(typeof(CablePlugin.Commands.CableCommands))]

namespace CablePlugin
{
    /// <summary>
    /// Точка входа плагина.
    /// </summary>
    public class PluginApp : IExtensionApplication
    {
        public void Initialize()
        {
            var doc = Application.DocumentManager.MdiActiveDocument;
            doc?.Editor.WriteMessage(
                "\n[CablePlugin] Загружен. Команды: CABLEATTACH, CABLEEDIT, CABLEDICT, CABLEINFO, CABLERECALC, CABLEREMOVE");
        }

        public void Terminate()
        {
        }
    }
}
