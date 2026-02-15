using Microsoft.Win32;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Text.Json;
using System.Windows;
using System.Windows.Input;
using UTS.Backend.Domain;
using UTS.Backend.Persistence;
using UTS.Backend.Selection;
using UTS.WPF.Localization;

namespace UTS.WPF.MVVM
{
    public sealed class MainViewModel : INotifyPropertyChanged
    {
        private UtsDocument? _document;
        private string? _currentPath;

        // Persistence
        private readonly string _settingsFilePath;
        private AppSettings _settings = new();

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

        public string CurrentLocale => _settings.CultureName ?? "cs-CZ";

        public bool HasDocument => Document != null;

        public string? CurrentPath
        {
            get => _currentPath;
            set { _currentPath = value; OnPropertyChanged(nameof(CurrentPath)); }
        }

        public ObservableCollection<string> Warnings { get; } = new();
        public ObservableCollection<BelowEItem> BelowE { get; } = new();
        public AboutModel About { get; } = new();

        public ICommand GenerateColumnCommand { get; }
        public ICommand ToggleColumnLockCommand { get; }
        public ICommand SwitchLanguageCommand { get; }
        public ICommand SaveCommand { get; }
        public ICommand SaveAsCommand { get; }
        public ICommand OpenCommand { get; }
        public ICommand NewCommand { get; }

        public event PropertyChangedEventHandler? PropertyChanged;
        public event EventHandler? DocumentChanged;

        // New settings POCO
        private class AppSettings
        {
            public string? CultureName { get; set; }
            public string? LastPath { get; set; }
        }

        public MainViewModel()
        {
            // compute settings file location: %AppData%/UTS/settings.json
            var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            var dir = Path.Combine(appData, "UTS");
            Directory.CreateDirectory(dir);
            _settingsFilePath = Path.Combine(dir, "settings.json");

            GenerateColumnCommand = new RelayCommand(p => GenerateColumn(Convert.ToInt32(p, CultureInfo.InvariantCulture)),
                                                    p => HasDocument);

            ToggleColumnLockCommand = new RelayCommand(p => ToggleColumnLock(Convert.ToInt32(p, CultureInfo.InvariantCulture)),
                                                    p => HasDocument);

            SwitchLanguageCommand = new RelayCommand(p => SwitchLanguage((string)p!));

            SaveCommand = new RelayCommand(_ => Save(), _ => HasDocument && !string.IsNullOrWhiteSpace(CurrentPath));
            SaveAsCommand = new RelayCommand(_ => SaveAs(), _ => HasDocument);
            OpenCommand = new RelayCommand(_ => OpenInteractive());
            NewCommand = new RelayCommand(_ => NewClass());

            // Load settings from disk, apply culture and try to open last path
            LoadSettingsFromDisk();

            SwitchLanguage(!string.IsNullOrWhiteSpace(_settings.CultureName)
                ? _settings.CultureName
                : "cs-CZ");

            if (!string.IsNullOrWhiteSpace(_settings.LastPath) && File.Exists(_settings.LastPath))
            {
                try
                {
                    OpenPath(_settings.LastPath);
                }
                catch
                {
                    // ignore load errors on startup; user can open manually
                }
            }

            // Ensure any other initialization that depended on previous SwitchLanguage call
            DocumentChanged?.Invoke(this, EventArgs.Empty);
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

            // persist last opened path
            _settings.LastPath = path;
            SaveSettingsToDisk();
        }

        private void SwitchLanguage(string cultureName)
        {
            LocalizationService.ApplyCulture(cultureName);
            // DynamicResource updates for most bound strings; for generated columns, rebuild columns in the view.
            DocumentChanged?.Invoke(this, EventArgs.Empty);

            // persist selection
            _settings.CultureName = cultureName;
            SaveSettingsToDisk();

            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(CurrentLocale)));
        }

        private void Save()
        {
            if (Document == null || string.IsNullOrWhiteSpace(CurrentPath)) return;
            UtsCsv.Save(Document, CurrentPath);
            RecomputeDiagnostics();
        }

        private void ToggleColumnLock(int testIndex)
        {
            if (Document == null) return;
            foreach (var student in Document.Students)
            {
                student[testIndex] = student[testIndex] switch
                {
                    CellState.None => CellState.Locked,
                    CellState.Recommended => CellState.Locked,
                    CellState.Locked => CellState.None,
                    _ => student[testIndex]
                };
            }
        }

        private void SaveAs() 
        { 
            if(Document == null) return;

            var saveFile = new SaveFileDialog()
            {
                DefaultExt = ".uts",
                DefaultDirectory = Environment.CurrentDirectory,
                FileName = CurrentPath ?? "untitled.uts"
            };

            if(saveFile.ShowDialog() == true)
            {
                UtsCsv.Save(Document!, saveFile.FileName);
                CurrentPath = saveFile.FileName;
                RecomputeDiagnostics();

                // persist last opened path
                _settings.LastPath = CurrentPath;
                SaveSettingsToDisk();
            }
        }

        private void NewClass()
        {
            var dlg = new CreateClassDialog(defaultClassName: "1A", defaultT: 6);

            if (dlg.ShowDialog() == true)
            {
                Document = UtsDocument.NewEmpty(dlg.ClassName, dlg.T, dlg.K, dlg.E, dlg.Students);
                SaveAs();
            }
        }

        private void OpenInteractive() 
        {
            OpenFileDialog openFile = new OpenFileDialog()
            {
                DefaultExt = ".uts",
                DefaultDirectory = Environment.CurrentDirectory,
            };
            if (openFile.ShowDialog() == true)
            {
                OpenPath(openFile.FileName);
            }
        }

        public void HandleLeftClick(StudentRecord s, int testIndex)
        {
            s[testIndex] = s[testIndex] switch
            {
                CellState.None => CellState.Graded,
                CellState.Recommended => CellState.Graded,
                CellState.Graded => CellState.None,
                _ => s[testIndex]
            };
            RecomputeDiagnostics();
        }

        public void HandleRightClick(StudentRecord s, int testIndex)
        {
            s[testIndex] = s[testIndex] switch
            {
                CellState.None => CellState.Absent,
                CellState.Absent => CellState.None,
                CellState.Recommended => CellState.Absent,
                _ => s[testIndex]
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

        // Settings IO helpers
        private void LoadSettingsFromDisk()
        {
            try
            {
                if (File.Exists(_settingsFilePath))
                {
                    var json = File.ReadAllText(_settingsFilePath);
                    var opts = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                    var s = JsonSerializer.Deserialize<AppSettings>(json, opts);
                    if (s != null) _settings = s;
                }
            }
            catch
            {
                // ignore errors (corrupt file, permission issues) and continue with defaults
                _settings = new AppSettings();
            }
        }

        private void SaveSettingsToDisk()
        {
            try
            {
                var opts = new JsonSerializerOptions { WriteIndented = true };
                var json = JsonSerializer.Serialize(_settings, opts);
                File.WriteAllText(_settingsFilePath, json);
            }
            catch
            {
                // ignore IO errors - settings are best-effort
            }
        }
    }

}
