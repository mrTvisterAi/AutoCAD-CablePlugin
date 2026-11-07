using System.ComponentModel;
using System.Runtime.CompilerServices;
using CablePlugin.Core.Models;

namespace CablePlugin.UI.ViewModels
{
    public class CableSegmentViewModel : INotifyPropertyChanged
    {
        private string _layingMethod = "Гофра";
        private double _length;
        private double? _pipeDiameter;
        private bool _isPipeDiameterAuto = true;
        private string _note = string.Empty;

        public string LayingMethod
        {
            get => _layingMethod;
            set { if (_layingMethod == value) return; _layingMethod = value ?? string.Empty; OnPropertyChanged(); }
        }

        public double Length
        {
            get => _length;
            set { if (System.Math.Abs(_length - value) < 1e-9) return; _length = value; OnPropertyChanged(); }
        }

        public double? PipeDiameter
        {
            get => _pipeDiameter;
            set { if (_pipeDiameter == value) return; _pipeDiameter = value; OnPropertyChanged(); }
        }

        public bool IsPipeDiameterAuto
        {
            get => _isPipeDiameterAuto;
            set { if (_isPipeDiameterAuto == value) return; _isPipeDiameterAuto = value; OnPropertyChanged(); }
        }

        public string Note
        {
            get => _note;
            set { if (_note == value) return; _note = value ?? string.Empty; OnPropertyChanged(); }
        }

        public static CableSegmentViewModel FromModel(CableSegment s)
        {
            if (s == null) return new CableSegmentViewModel();
            return new CableSegmentViewModel
            {
                LayingMethod = s.LayingMethod,
                Length = s.Length,
                PipeDiameter = s.PipeDiameter,
                IsPipeDiameterAuto = s.IsPipeDiameterAuto,
                Note = s.Note ?? string.Empty
            };
        }

        public CableSegment ToModel()
        {
            return new CableSegment
            {
                LayingMethod = LayingMethod,
                Length = Length,
                PipeDiameter = PipeDiameter,
                IsPipeDiameterAuto = IsPipeDiameterAuto,
                Note = Note
            };
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}
