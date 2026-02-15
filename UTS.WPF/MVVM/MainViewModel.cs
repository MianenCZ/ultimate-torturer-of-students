namespace UTS.WPF.MVVM
{
    using System.Collections.ObjectModel;
    using System.ComponentModel;
    using System.Globalization;
    using System.Windows.Input;
    using UTS.Backend.Domain;
    using UTS.Backend.Persistence;
    using UTS.Backend.Selection;
    using UTS.WPF.Localization;

    public sealed class MainViewModel : INotifyPropertyChanged
    {
        private UtsDocument? _document;
        private string? _currentPath;

        public UtsDocument? Document
        {
            get => _document;
            set
            {
                if (ReferenceEquals(_document, value)) return;
                _document = value;
                OnPropertyChanged(nameof(Document));
                OnPropertyChanged(nameof(HasDocument));
                RecomputeDiagnostics();
                DocumentChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        public bool HasDocument => Document != null;

        public string? CurrentPath
        {
            get => _currentPath;
            set { _currentPath = value; OnPropertyChanged(nameof(CurrentPath)); }
        }

        public ObservableCollection<string> Warnings { get; } = new();
        public ObservableCollection<BelowEItem> BelowE { get; } = new();

        public ICommand GenerateColumnCommand { get; }
        public ICommand SwitchLanguageCommand { get; }
        public ICommand SaveCommand { get; }
        public ICommand SaveAsCommand { get; }
        public ICommand OpenCommand { get; }

        public event PropertyChangedEventHandler? PropertyChanged;
        public event EventHandler? DocumentChanged;

        public MainViewModel()
        {
            GenerateColumnCommand = new RelayCommand(p => GenerateColumn(Convert.ToInt32(p, CultureInfo.InvariantCulture)),
                                                    p => HasDocument);

            SwitchLanguageCommand = new RelayCommand(p => SwitchLanguage((string)p!));

            SaveCommand = new RelayCommand(_ => Save(), _ => HasDocument && !string.IsNullOrWhiteSpace(CurrentPath));
            SaveAsCommand = new RelayCommand(_ => SaveAs(), _ => HasDocument);
            OpenCommand = new RelayCommand(_ => OpenInteractive());
        }

        public void GenerateColumn(int testIndex)
        {
            if (Document == null) return;
            var result = SelectionEngine.RegenerateColumn(Document, testIndex); // overwrites D for that column
            RecomputeDiagnostics(result.Warnings);
        }

        public void OpenPath(string path)
        {
            Document = UtsCsv.Load(path);
            CurrentPath = path;
        }

        private void Save()
        {
            if (Document == null || string.IsNullOrWhiteSpace(CurrentPath)) return;
            UtsCsv.Save(Document, CurrentPath);
            RecomputeDiagnostics();
        }

        private void SaveAs() { /* FE: file dialog, then UtsCsv.Save */ }
        private void OpenInteractive() { /* FE: file dialog, then OpenPath */ }

        private void SwitchLanguage(string cultureName)
        {
            LocalizationService.ApplyCulture(cultureName);
            // DynamicResource updates for most bound strings; for generated columns, rebuild columns in the view.
            DocumentChanged?.Invoke(this, EventArgs.Empty);
        }

        public void HandleLeftClick(StudentRecord s, int testIndex)
        {
            s[testIndex] = (s[testIndex] == CellState.Absent) ? CellState.None : CellState.Absent;
            RecomputeDiagnostics();
        }

        public void HandleRightClick(StudentRecord s, int testIndex)
        {
            s[testIndex] = s[testIndex] switch
            {
                CellState.None => CellState.Recommended,
                CellState.Recommended => CellState.Graded,
                CellState.Graded => CellState.None,
                CellState.Absent => CellState.Absent, // right-click does not change absence
                _ => CellState.None
            };
            RecomputeDiagnostics();
        }

        private void RecomputeDiagnostics(IEnumerable<string>? engineWarnings = null)
        {
            Warnings.Clear();
            if (Document == null) return;

            // Always show structural infeasibility via current doc state.
            int n = Document.Students.Count;
            if (Document.T * Document.K < n * Document.E)
                Warnings.Add($"Global infeasible: T*K={Document.T * Document.K} < n*E={n * Document.E}.");

            if (engineWarnings != null)
                foreach (var w in engineWarnings) Warnings.Add(w);

            BelowE.Clear();
            foreach (var s in Document.Students.Where(x => x.GradedCount < Document.E))
                BelowE.Add(new BelowEItem(s.Name, s.GradedCount, Document.E - s.GradedCount));
        }

        private void OnPropertyChanged(string name) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }

}
