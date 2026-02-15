namespace UTS.Backend.Domain
{
    using System.ComponentModel;

    public sealed class StudentRecord : INotifyPropertyChanged
    {
        private readonly CellState[] _cells;

        public StudentRecord(string id, string name, int testsCount)
        {
            Id = id ?? "";
            Name = name ?? throw new ArgumentNullException(nameof(name));
            _cells = new CellState[testsCount];
        }

        public string Id { get; set; }
        public string Name { get; set; }

        // Indexer enables WPF binding: Path=[testIndex]
        public CellState this[int testIndex]
        {
            get => _cells[testIndex];
            set
            {
                if (_cells[testIndex] == value) return;
                _cells[testIndex] = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Item[]"));
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(GradedCount)));
            }
        }

        public int TestsCount => _cells.Length;

        // Authoritative evaluation count is Z-only.
        public int GradedCount => _cells.Count(c => c == CellState.Graded);

        public int LastGradedIndex()
        {
            for (int i = _cells.Length - 1; i >= 0; i--)
                if (_cells[i] == CellState.Graded) return i;
            return -1;
        }

        public event PropertyChangedEventHandler? PropertyChanged;
    }

}
