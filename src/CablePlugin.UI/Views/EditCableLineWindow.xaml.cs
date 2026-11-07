using System;
using System.Windows;
using CablePlugin.Core.Models;
using CablePlugin.UI.ViewModels;

namespace CablePlugin.UI.Views
{
    public partial class EditCableLineWindow : Window
    {
        private readonly EditCableLineViewModel _vm;

        public EditCableLineWindow(EditCableLineViewModel viewModel)
        {
            InitializeComponent();
            _vm = viewModel ?? throw new ArgumentNullException(nameof(viewModel));
            DataContext = _vm;
            _vm.RequestClose += OnRequestClose;
        }

        public CableLine ResultLine { get; private set; }

        private void OnRequestClose()
        {
            if (_vm.DialogResult == true)
            {
                ResultLine = _vm.ToModel();
                DialogResult = true;
            }
            else
            {
                DialogResult = false;
            }

            Close();
        }

        protected override void OnClosed(EventArgs e)
        {
            _vm.RequestClose -= OnRequestClose;
            base.OnClosed(e);
        }

        /// <summary>
        /// Показать диалог редактирования. Возвращает обновлённую линию или null при отмене.
        /// </summary>
        public static CableLine ShowDialog(CableLine line, System.Collections.Generic.IEnumerable<PipeCorrespondence> pipeTable = null)
        {
            var vm = new EditCableLineViewModel(pipeTable);
            vm.LoadFrom(line ?? new CableLine());

            var wnd = new EditCableLineWindow(vm);

            // Owner — главное окно AutoCAD, если доступно
            try
            {
                var helper = new System.Windows.Interop.WindowInteropHelper(wnd);
                var acadHandle = AutodeskHandle();
                if (acadHandle != IntPtr.Zero)
                    helper.Owner = acadHandle;
            }
            catch
            {
                // UI-проект не ссылается на AutoCAD — owner опционален
            }

            var result = wnd.ShowDialog();
            return result == true ? wnd.ResultLine : null;
        }

        private static IntPtr AutodeskHandle()
        {
            // Без прямой ссылки на AcMgd: ищем окно по заголовку — ненадёжно.
            // Owner выставляется из слоя Commands при наличии Application.MainWindow.
            return IntPtr.Zero;
        }
    }
}
