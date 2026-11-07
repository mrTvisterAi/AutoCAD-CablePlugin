using System.Windows.Controls;
using CablePlugin.UI.ViewModels;

namespace CablePlugin.UI.Views
{
    public partial class CablePropertiesControl : UserControl
    {
        public CablePropertiesControl()
        {
            InitializeComponent();
        }

        public CablePropertiesControl(CablePropertiesViewModel vm) : this()
        {
            DataContext = vm;
        }

        public CablePropertiesViewModel ViewModel => DataContext as CablePropertiesViewModel;
    }
}
