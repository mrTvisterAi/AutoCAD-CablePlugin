using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using CablePlugin.Core.Models;
using CablePlugin.Core.Services;

namespace CablePlugin.UI.ViewModels
{
    /// <summary>
    /// VM панели свойств выбранной кабельной линии.
    /// </summary>
    public class CablePropertiesViewModel : INotifyPropertyChanged
    {
        private readonly ElectricalCalculator _electrical = new ElectricalCalculator();
        private bool _suppressNotify;
        private bool _hasSelection;
        private bool _isDirty;
        private string _status = "Выберите полилинию с данными кабеля";
        private string _handle = string.Empty;

        private string _groupName = string.Empty;
        private string _from = string.Empty;
        private string _to = string.Empty;
        private string _mark = string.Empty;
        private int _cores = 3;
        private double _section = 1.5;
        private int _cableCount = 1;
        private double _reservePercent = 20;
        private double _additionalLength;
        private double _scaleFactor = 1.0;
        private double _roundUpTo = 1.0;
        private double? _powerKw;
        private double _cosPhi = 0.85;
        private double _voltage = 380;
        private int _phases = 3;
        private double _current;
        private double _voltageDropPercent;
        private string _recommendedBreaker = string.Empty;
        private double _geometricLength;
        private double _totalLength;

        public CablePropertiesViewModel()
        {
            Segments = new ObservableCollection<CableSegmentViewModel>();
            ApplyCommand = new RelayCommand(_ => RequestApply?.Invoke(), _ => HasSelection && IsDirty);
            EditFullCommand = new RelayCommand(_ => RequestEditFull?.Invoke(), _ => HasSelection);
            RecalcCommand = new RelayCommand(_ => RecalculateLocal(), _ => HasSelection);
        }

        public ObservableCollection<CableSegmentViewModel> Segments { get; }

        public bool HasSelection
        {
            get => _hasSelection;
            private set { if (_hasSelection == value) return; _hasSelection = value; OnPropertyChanged(); OnPropertyChanged(nameof(IsEmpty)); }
        }

        public bool IsEmpty => !HasSelection;

        public bool IsDirty
        {
            get => _isDirty;
            private set { if (_isDirty == value) return; _isDirty = value; OnPropertyChanged(); }
        }

        public string Status
        {
            get => _status;
            private set { if (_status == value) return; _status = value ?? string.Empty; OnPropertyChanged(); }
        }

        public string Handle
        {
            get => _handle;
            private set { if (_handle == value) return; _handle = value ?? string.Empty; OnPropertyChanged(); }
        }

        public string GroupName { get => _groupName; set => SetField(ref _groupName, value ?? string.Empty); }
        public string From { get => _from; set => SetField(ref _from, value ?? string.Empty); }
        public string To { get => _to; set => SetField(ref _to, value ?? string.Empty); }
        public string Mark { get => _mark; set => SetField(ref _mark, value ?? string.Empty); }
        public int Cores { get => _cores; set => SetField(ref _cores, value); }
        public double Section { get => _section; set => SetField(ref _section, value); }
        public int CableCount { get => _cableCount; set => SetField(ref _cableCount, value < 1 ? 1 : value); }
        public double ReservePercent { get => _reservePercent; set => SetField(ref _reservePercent, value); }
        public double AdditionalLength { get => _additionalLength; set => SetField(ref _additionalLength, value); }
        public double ScaleFactor { get => _scaleFactor; set => SetField(ref _scaleFactor, value); }
        public double RoundUpTo { get => _roundUpTo; set => SetField(ref _roundUpTo, value); }
        public double? PowerKw { get => _powerKw; set => SetField(ref _powerKw, value); }
        public double CosPhi { get => _cosPhi; set => SetField(ref _cosPhi, value); }
        public double Voltage { get => _voltage; set => SetField(ref _voltage, value); }
        public int Phases { get => _phases; set => SetField(ref _phases, value); }

        public double Current { get => _current; private set { _current = value; OnPropertyChanged(); } }
        public double VoltageDropPercent { get => _voltageDropPercent; private set { _voltageDropPercent = value; OnPropertyChanged(); } }
        public string RecommendedBreaker { get => _recommendedBreaker; private set { _recommendedBreaker = value ?? string.Empty; OnPropertyChanged(); } }
        public double GeometricLength { get => _geometricLength; private set { _geometricLength = value; OnPropertyChanged(); } }
        public double TotalLength { get => _totalLength; private set { _totalLength = value; OnPropertyChanged(); } }

        public ICommand ApplyCommand { get; }
        public ICommand EditFullCommand { get; }
        public ICommand RecalcCommand { get; }

        /// <summary>Запросить запись в DWG (палитра / команда).</summary>
        public event Action RequestApply;

        /// <summary>Открыть полный диалог CABLEEDIT.</summary>
        public event Action RequestEditFull;

        public void Clear()
        {
            _suppressNotify = true;
            HasSelection = false;
            IsDirty = false;
            Handle = string.Empty;
            Status = "Выберите полилинию с данными кабеля";
            Segments.Clear();
            GroupName = From = To = Mark = string.Empty;
            RecommendedBreaker = string.Empty;
            Current = VoltageDropPercent = GeometricLength = TotalLength = 0;
            _suppressNotify = false;
            RaiseAll();
        }

        public void LoadFrom(CableLine line, string handle, string statusMessage = null)
        {
            if (line == null)
            {
                Clear();
                return;
            }

            _suppressNotify = true;
            HasSelection = true;
            Handle = handle ?? line.PolylineHandle ?? string.Empty;
            Status = statusMessage ?? "Свойства кабельной линии";

            GroupName = line.GroupName;
            From = line.From;
            To = line.To;

            if (line.Cable != null)
            {
                Mark = line.Cable.Mark;
                Cores = line.Cable.Cores;
                Section = line.Cable.Section;
                CableCount = line.Cable.CableCount;
            }

            ReservePercent = line.ReservePercent;
            AdditionalLength = line.AdditionalLength;
            ScaleFactor = line.ScaleFactor <= 0 ? 1.0 : line.ScaleFactor;
            RoundUpTo = line.RoundUpTo;
            PowerKw = line.PowerKw;
            CosPhi = line.CosPhi;
            Voltage = line.Voltage;
            Phases = line.Phases;
            Current = line.Current;
            VoltageDropPercent = line.VoltageDropPercent;
            RecommendedBreaker = line.RecommendedBreaker;
            GeometricLength = line.GeometricLength;
            TotalLength = line.TotalLength;

            Segments.Clear();
            if (line.Segments != null)
            {
                foreach (var s in line.Segments)
                    Segments.Add(CableSegmentViewModel.FromModel(s));
            }

            _suppressNotify = false;
            IsDirty = false;
            RaiseAll();
        }

        public CableLine ToModel(CableLine existing = null)
        {
            var line = existing?.Clone() ?? new CableLine();
            line.GroupName = GroupName;
            line.From = From;
            line.To = To;
            line.Cable = new CableInfo
            {
                Mark = Mark,
                Cores = Cores,
                Section = Section,
                CableCount = CableCount
            };
            line.Segments = Segments.Select(s => s.ToModel()).ToList();
            line.ReservePercent = ReservePercent;
            line.AdditionalLength = AdditionalLength;
            line.ScaleFactor = ScaleFactor;
            line.RoundUpTo = RoundUpTo;
            line.PowerKw = PowerKw;
            line.CosPhi = CosPhi;
            line.Voltage = Voltage;
            line.Phases = Phases;

            LengthCalculator.Calculate(line);
            _electrical.Calculate(line);

            Current = line.Current;
            VoltageDropPercent = line.VoltageDropPercent;
            RecommendedBreaker = line.RecommendedBreaker;
            GeometricLength = line.GeometricLength;
            TotalLength = line.TotalLength;

            return line;
        }

        private void RecalculateLocal()
        {
            if (!HasSelection) return;
            ToModel();
            IsDirty = true;
            Status = "Пересчитано (не сохранено)";
        }

        public void MarkSaved()
        {
            IsDirty = false;
            Status = "Сохранено";
        }

        private void SetField<T>(ref T field, T value, [CallerMemberName] string name = null)
        {
            if (Equals(field, value)) return;
            field = value;
            OnPropertyChanged(name);
            if (!_suppressNotify && HasSelection)
            {
                IsDirty = true;
                Status = "Изменено — нажмите Применить";
            }
        }

        private void RaiseAll()
        {
            OnPropertyChanged(nameof(GroupName));
            OnPropertyChanged(nameof(From));
            OnPropertyChanged(nameof(To));
            OnPropertyChanged(nameof(Mark));
            OnPropertyChanged(nameof(Cores));
            OnPropertyChanged(nameof(Section));
            OnPropertyChanged(nameof(CableCount));
            OnPropertyChanged(nameof(ReservePercent));
            OnPropertyChanged(nameof(AdditionalLength));
            OnPropertyChanged(nameof(ScaleFactor));
            OnPropertyChanged(nameof(RoundUpTo));
            OnPropertyChanged(nameof(PowerKw));
            OnPropertyChanged(nameof(CosPhi));
            OnPropertyChanged(nameof(Voltage));
            OnPropertyChanged(nameof(Phases));
            OnPropertyChanged(nameof(Current));
            OnPropertyChanged(nameof(VoltageDropPercent));
            OnPropertyChanged(nameof(RecommendedBreaker));
            OnPropertyChanged(nameof(GeometricLength));
            OnPropertyChanged(nameof(TotalLength));
            OnPropertyChanged(nameof(HasSelection));
            OnPropertyChanged(nameof(IsEmpty));
            OnPropertyChanged(nameof(IsDirty));
            OnPropertyChanged(nameof(Status));
            OnPropertyChanged(nameof(Handle));
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}
