using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using CablePlugin.Core.Data;
using CablePlugin.Core.Models;

namespace CablePlugin.UI.ViewModels
{
    public class PipeCorrespondenceViewModel : INotifyPropertyChanged
    {
        private string _mark = string.Empty;
        private int _cores = 3;
        private double _section = 1.5;
        private double _diameter = 20;

        public string Mark
        {
            get => _mark;
            set { if (_mark == value) return; _mark = value ?? string.Empty; OnPropertyChanged(); }
        }

        public int Cores
        {
            get => _cores;
            set { if (_cores == value) return; _cores = value; OnPropertyChanged(); }
        }

        public double Section
        {
            get => _section;
            set { if (Math.Abs(_section - value) < 1e-9) return; _section = value; OnPropertyChanged(); }
        }

        public double Diameter
        {
            get => _diameter;
            set { if (Math.Abs(_diameter - value) < 1e-9) return; _diameter = value; OnPropertyChanged(); }
        }

        public static PipeCorrespondenceViewModel FromModel(PipeCorrespondence m)
        {
            if (m == null) return new PipeCorrespondenceViewModel();
            return new PipeCorrespondenceViewModel
            {
                Mark = m.Mark,
                Cores = m.Cores,
                Section = m.Section,
                Diameter = m.Diameter
            };
        }

        public PipeCorrespondence ToModel()
        {
            return new PipeCorrespondence
            {
                Mark = Mark?.Trim() ?? string.Empty,
                Cores = Cores,
                Section = Section,
                Diameter = Diameter
            };
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }

    public class PipeTableViewModel : INotifyPropertyChanged
    {
        private PipeCorrespondenceViewModel _selected;

        public PipeTableViewModel(IEnumerable<PipeCorrespondence> existing = null)
        {
            Items = new ObservableCollection<PipeCorrespondenceViewModel>();

            var source = existing != null && existing.Any()
                ? existing
                : DefaultPipeTable.GetDefault();

            foreach (var row in source.OrderBy(x => x.Mark).ThenBy(x => x.Cores).ThenBy(x => x.Section))
                Items.Add(PipeCorrespondenceViewModel.FromModel(row));

            AddCommand = new RelayCommand(_ => AddRow());
            RemoveCommand = new RelayCommand(_ => RemoveRow(), _ => Selected != null);
            LoadDefaultsCommand = new RelayCommand(_ => LoadDefaults());
            OkCommand = new RelayCommand(_ => Confirm(true));
            CancelCommand = new RelayCommand(_ => Confirm(false));
        }

        public ObservableCollection<PipeCorrespondenceViewModel> Items { get; }

        public PipeCorrespondenceViewModel Selected
        {
            get => _selected;
            set { _selected = value; OnPropertyChanged(); }
        }

        public ICommand AddCommand { get; }
        public ICommand RemoveCommand { get; }
        public ICommand LoadDefaultsCommand { get; }
        public ICommand OkCommand { get; }
        public ICommand CancelCommand { get; }

        public bool? DialogResult { get; private set; }

        public event Action RequestClose;

        public List<PipeCorrespondence> ToList()
        {
            return Items
                .Select(i => i.ToModel())
                .Where(m => !string.IsNullOrWhiteSpace(m.Mark) && m.Cores > 0 && m.Section > 0 && m.Diameter > 0)
                .GroupBy(m => m.Key)
                .Select(g => g.Last())
                .OrderBy(m => m.Mark)
                .ThenBy(m => m.Cores)
                .ThenBy(m => m.Section)
                .ToList();
        }

        private void AddRow()
        {
            var row = new PipeCorrespondenceViewModel
            {
                Mark = "ВВГнг(А)-LS",
                Cores = 3,
                Section = 1.5,
                Diameter = 20
            };
            Items.Add(row);
            Selected = row;
        }

        private void RemoveRow()
        {
            if (Selected == null) return;
            Items.Remove(Selected);
            Selected = null;
        }

        private void LoadDefaults()
        {
            Items.Clear();
            foreach (var row in DefaultPipeTable.GetDefault())
                Items.Add(PipeCorrespondenceViewModel.FromModel(row));
        }

        private void Confirm(bool ok)
        {
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
