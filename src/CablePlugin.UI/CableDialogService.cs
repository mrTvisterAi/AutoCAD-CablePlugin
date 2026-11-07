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
        /// <param name="line">Исходные данные (может быть new CableLine())</param>
        /// <param name="pipeTable">Таблица соответствия труб</param>
        /// <param name="ownerHandle">HWND окна AutoCAD (опционально)</param>
        /// <returns>Обновлённая линия или null, если отмена</returns>
        public static CableLine EditLine(
            CableLine line,
            IEnumerable<PipeCorrespondence> pipeTable = null,
            System.IntPtr ownerHandle = default)
        {
            var vm = new EditCableLineViewModel(pipeTable);
            vm.LoadFrom(line ?? new CableLine());

            var wnd = new EditCableLineWindow(vm);

            if (ownerHandle != System.IntPtr.Zero)
            {
                var helper = new System.Windows.Interop.WindowInteropHelper(wnd);
                helper.Owner = ownerHandle;
            }

            var ok = wnd.ShowDialog();
            return ok == true ? wnd.ResultLine : null;
        }
    }
}
