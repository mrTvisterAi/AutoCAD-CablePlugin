using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.Runtime;

[assembly: ExtensionApplication(typeof(CablePlugin.PluginApp))]
[assembly: CommandClass(typeof(CablePlugin.Commands.CableCommands))]
[assembly: CommandClass(typeof(CablePlugin.Commands.ReportCommands))]
[assembly: CommandClass(typeof(CablePlugin.Commands.SchemeCommands))]
[assembly: CommandClass(typeof(CablePlugin.Commands.LibraryCommands))]
[assembly: CommandClass(typeof(CablePlugin.Commands.PropertiesCommands))]

namespace CablePlugin
{
    public class PluginApp : IExtensionApplication
    {
        public void Initialize()
        {
            var doc = Application.DocumentManager.MdiActiveDocument;
            doc?.Editor.WriteMessage(
                "\n[CablePlugin] Загружен." +
                "\n  Данные: CABLEATTACH CABLEEDIT CABLEDICT CABLEINFO CABLERECALC CABLEREMOVE CABLEEXPORT" +
                "\n  Отчёты: CABLEJOURNAL CABLEBOM CABLEPANEL" +
                "\n  Схема: CABLESCHEME CABLETEMPLATE CABLELIB" +
                "\n  Свойства: CABLEPROPS (палитра при выделении полилинии)");
        }

        public void Terminate()
        {
        }
    }
}
