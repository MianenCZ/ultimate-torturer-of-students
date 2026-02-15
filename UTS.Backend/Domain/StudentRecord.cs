namespace UTS.Backend.Domain
{
    using System.ComponentModel;
    using System.Linq;

    public sealed class StudentRecord(string id, string name, int t, int k, int e) : INotifyPropertyChanged
    {
        private readonly CellState[] _cells = new CellState[t];
        private readonly int t = t;
        private readonly int k = k;
        private readonly int e = e;

        public string Id { get; set; } = id ?? "";
        public string Name { get; set; } = name ?? throw new ArgumentNullException(nameof(name));

        // Indexer enables WPF binding: Path=[testIndex]
        public CellState this[int testIndex]
        {
            get
            {
                if (testIndex < 0 || testIndex >= _cells.Length) return CellState.None;
                return _cells[testIndex];
            }
            set
            {
                if (testIndex < 0 || testIndex >= _cells.Length) return;
                if (_cells[testIndex] == value) return;
                _cells[testIndex] = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Item[]"));
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(GradedCount)));
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(GradedRemains)));
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(OpenSlots)));
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Note)));
            }
        }

        public int TestsCount => _cells.Length;

        public int GradedCount => _cells.Count(c => c == CellState.Graded);

        public int GradedRemains => Math.Max(0, e - GradedCount);

        public int OpenSlots => _cells.Count(c => c is CellState.Recommended or CellState.None);

        public string Note => Validate();

        public int LastGradedIndex()
        {
            for (int i = _cells.Length - 1; i >= 0; i--)
                if (_cells[i] == CellState.Graded) return i;
            return -1;
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        private string Validate()
        {
            if(GradedRemains > OpenSlots)
            {
                return $"Nelze dosáhnout požadavků!"; //TODO: localization
            }
            return string.Empty;
        }
    }

}
