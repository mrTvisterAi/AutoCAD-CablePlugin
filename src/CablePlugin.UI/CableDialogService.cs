using System.Collections.Generic;
using CablePlugin.Core.Models;
using CablePlugin.UI.ViewModels;
using CablePlugin.UI.Views;

namespace CablePlugin.UI
{
    /// <summary>
    /// Сервис показа диалогов из команд AutoCAD.
    /// </summary>
    public static class CableDialogService
    {
        /// <summary>
        /// Открыть диалог редактирования линии.
        /// </summary>
        public static CableLine EditLine(
            CableLine line,
            IEnumerable<PipeCorrespondence> pipeTable = null,
            System.IntPtr ownerHandle = default)
        {
            var vm = new EditCableLineViewModel(pipeTable);
            vm.LoadFrom(line ?? new CableLine());

            var wnd = new EditCableLineWindow(vm);
            SetOwner(wnd, ownerHandle);

            var ok = wnd.ShowDialog();
            return ok == true ? wnd.ResultLine : null;
        }

        /// <summary>
        /// Открыть редактор таблицы соответствия труб.
        /// </summary>
        /// <returns>Новая таблица или null при отмене</returns>
        public static List<PipeCorrespondencepondence> EditPipeTable(
            IEnumerable<PipeCorrespondence> currentTable = null,
            System.IntPtr ownerHandle = default)
        {
            var vm = new PipeTableViewModel(currentTable);
            var wnd = new PipeTableWindow(vm);
            SetOwner(wnd, ownerHandle);

            var ok = wnd.ShowDialog();
            return ok == true ? wnd.ResultTable : null;
        }

        private static void SetOwner(System.Windows.Window wnd, System.IntPtr ownerHandle)
        {
            if (ownerHandle == System.IntPtr.Zero) return;
            var helper = new System.Windows.Interop.WindowInteropHelper(wnd);
            helper.Owner = ownerHandle;
        }
    }
}
