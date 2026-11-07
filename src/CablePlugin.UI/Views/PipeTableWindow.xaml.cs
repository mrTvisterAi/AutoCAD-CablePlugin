using System;
using System.Collections.Generic;
using System.Windows;
using CablePlugin.Core.Models;
using CablePlugin.UI.ViewModels;

namespace CablePlugin.UI.Views
{
    public partial class PipeTableWindow : Window
    {
        private readonly PipeTableViewModel _vm;

        public PipeTableWindow(PipeTableViewModel viewModel)
        {
            InitializeComponent();
            _vm = viewModel ?? throw new ArgumentNullException(nameof(viewModel));
            DataContext = _vm;
            _vm.RequestClose += OnRequestClose;
        }

        public List<PipeCorrespondence> ResultTable { get; private set; }

        private void OnRequestClose()
        {
            if (_vm.DialogResult == true)
            {
                ResultTable = _vm.ToList();
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
    }
}
