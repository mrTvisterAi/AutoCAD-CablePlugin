using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using CablePlugin.Core.Models;
using CablePlugin.Core.Services;

namespace CablePlugin.UI.ViewModels
{
    public class EditCableLineViewModel : INotifyPropertyChanged
    {
        private readonly PipeSelector _pipeSelector;
        private readonly ElectricalCalculator _electrical = new ElectricalCalculator();

        private string _groupName = string.Empty;
        private string _from = string.Empty;
        private string _to = string.Empty;
        private string _mark = "ВВГнг(А)-LS";
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
        private CableSegmentViewModel _selectedSegment;

        public EditCableLineViewModel(IEnumerable<PipeCorrespondence> pipeTable = null)
        {
            _pipeSelector = new PipeSelector(pipeTable);

            Segments = new ObservableCollection<CableSegmentViewModel>();
            LayingMethods = new ObservableCollection<string>
            {
                "Лоток",
                "Кабель-канал",
                "Гофра",
                "Труба",
                "Труба ПВХ",
                "Металлорукав",
                "В земле",
                "Открыто"
            };

            AddSegmentCommand = new RelayCommand(_ => AddSegment());
            RemoveSegmentCommand = new RelayCommand(_ => RemoveSegment(), _ => SelectedSegment != null);
            AutoPipeCommand = new RelayCommand(_ => ApplyAutoPipe());
            RecalculateCommand = new RelayCommand(_ => Recalculate());
            OkCommand = new RelayCommand(_ => Confirm(true));
            CancelCommand = new RelayCommand(_ => Confirm(false));
        }

        public ObservableCollection<CableSegmentViewModel> Segments { get; }
        public ObservableCollection<string> LayingMethods { get; }

        public CableSegmentViewModel SelectedSegment
        {
            get => _selectedSegment;
            set { _selectedSegment = value; OnPropertyChanged(); }
        }

        public string GroupName { get => _groupName; set { _groupName = value ?? string.Empty; OnPropertyChanged(); } }
        public string From { get => _from; set { _from = value ?? string.Empty; OnPropertyChanged(); } }
        public string To { get => _to; set { _to = value ?? string.Empty; OnPropertyChanged(); } }
        public string Mark { get => _mark; set { _mark = value ?? string.Empty; OnPropertyChanged(); } }
        public int Cores { get => _cores; set { _cores = value; OnPropertyChanged(); } }
        public double Section { get => _section; set { _section = value; OnPropertyChanged(); } }
        public int CableCount { get => _cableCount; set { _cableCount = value < 1 ? 1 : value; OnPropertyChanged(); } }
        public double ReservePercent { get => _reservePercent; set { _reservePercent = value; OnPropertyChanged(); } }
        public double AdditionalLength { get => _additionalLength; set { _additionalLength = value; OnPropertyChanged(); } }
        public double ScaleFactor { get => _scaleFactor; set { _scaleFactor = value; OnPropertyChanged(); } }
        public double RoundUpTo { get => _roundUpTo; set { _roundUpTo = value; OnPropertyChanged(); } }
        public double? PowerKw { get => _powerKw; set { _powerKw = value; OnPropertyChanged(); } }
        public double CosPhi { get => _cosPhi; set { _cosPhi = value; OnPropertyChanged(); } }
        public double Voltage { get => _voltage; set { _voltage = value; OnPropertyChanged(); } }
        public int Phases { get => _phases; set { _phases = value; OnPropertyChanged(); } }

        public double Current { get => _current; private set { _current = value; OnPropertyChanged(); } }
        public double VoltageDropPercent { get => _voltageDropPercent; private set { _voltageDropPercent = value; OnPropertyChanged(); } }
        public string RecommendedBreaker { get => _recommendedBreaker; private set { _recommendedBreaker = value ?? string.Empty; OnPropertyChanged(); } }
        public double GeometricLength { get => _geometricLength; private set { _geometricLength = value; OnPropertyChanged(); } }
        public double TotalLength { get => _totalLength; private set { _totalLength = value; OnPropertyChanged(); } }

        public ICommand AddSegmentCommand { get; }
        public ICommand RemoveSegmentCommand { get; }
        public ICommand AutoPipeCommand { get; }
        public ICommand RecalculateCommand { get; }
        public ICommand OkCommand { get; }
        public ICommand CancelCommand { get; }

        /// <summary>true = OK, false = Cancel</summary>
        public bool? DialogResult { get; private set; }

        public event Action RequestClose;

        public void LoadFrom(CableLine line)
        {
            if (line == null) return;

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

            if (Segments.Count == 0)
                AddSegment();
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
            line.Current = Current;
            line.VoltageDropPercent = VoltageDropPercent;
            line.RecommendedBreaker = RecommendedBreaker;
            line.GeometricLength = GeometricLength;
            line.TotalLength = TotalLength;

            return line;
        }

        private void AddSegment()
        {
            Segments.Add(new CableSegmentViewModel
            {
                LayingMethod = "Гофра",
                Length = 0,
                IsPipeDiameterAuto = true
            });
        }

        private void RemoveSegment()
        {
            if (SelectedSegment == null) return;
            Segments.Remove(SelectedSegment);
            SelectedSegment = null;
        }

        private void ApplyAutoPipe()
        {
            var temp = ToModel();
            _pipeSelector.ApplyToLine(temp);
            for (int i = 0; i < Segments.Count && i < temp.Segments.Count; i++)
            {
                Segments[i].PipeDiameter = temp.Segments[i].PipeDiameter;
                Segments[i].IsPipeDiameterAuto = temp.Segments[i].IsPipeDiameterAuto;
            }
        }

        public void Recalculate()
        {
            ApplyAutoPipe();
            var line = ToModel();
            LengthCalculator.Calculate(line);
            _electrical.Calculate(line);

            GeometricLength = line.GeometricLength;
            TotalLength = line.TotalLength;
            Current = line.Current;
            VoltageDropPercent = line.VoltageDropPercent;
            RecommendedBreaker = line.RecommendedBreaker;
        }

        private void Confirm(bool ok)
        {
            if (ok)
                Recalculate();
            DialogResult = ok;
            RequestClose?.Invoke();
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}
