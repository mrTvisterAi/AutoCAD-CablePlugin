using System;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.Runtime;
using Autodesk.Windows;
using CablePlugin.Ribbon;

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
                "\n[CablePlugin] Загружен (net8). Вкладка ленты: «Кабель»." +
                "\n  CABLEATTACH CABLEEDIT CABLEPROPS CABLEDICT CABLEEXPORT ...");

            // Лента может быть ещё не готова в момент NETLOAD
            try
            {
                if (ComponentManager.Ribbon != null)
                    CableRibbon.EnsureRibbon();
                else
                    ComponentManager.ItemInitialized += OnRibbonReady;
            }
            catch (System.Exception ex)
            {
                doc?.Editor.WriteMessage("\n[CablePlugin] Лента отложена: " + ex.Message);
                try { ComponentManager.ItemInitialized += OnRibbonReady; }
                catch { /* ignore */ }
            }
        }

        private static void OnRibbonReady(object sender, RibbonItemEventArgs e)
        {
            if (ComponentManager.Ribbon == null) return;
            try
            {
                ComponentManager.ItemInitialized -= OnRibbonReady;
            }
            catch { /* ignore */ }

            CableRibbon.EnsureRibbon();
        }

        public void Terminate()
        {
            try { ComponentManager.ItemInitialized -= OnRibbonReady; }
            catch { /* ignore */ }
        }
    }
}
