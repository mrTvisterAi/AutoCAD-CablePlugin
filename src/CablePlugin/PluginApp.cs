using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.Runtime;

[assembly: ExtensionApplication(typeof(CablePlugin.PluginApp))]
[assembly: CommandClass(typeof(CablePlugin.Commands.CableCommands))]

namespace CablePlugin
{
    public class PluginApp : IExtensionApplication
    {
        public void Initialize()
        {
            var doc = Application.DocumentManager.MdiActiveDocument;
            doc?.Editor.WriteMessage(
                "\n[CablePlugin] Загружен. Команды: CABLEATTACH, CABLEEDIT, CABLEDICT, CABLEEXPORT, CABLEINFO, CABLERECALC, CABLEREMOVE");
        }

        public void Terminate()
        {
        }
    }
}
