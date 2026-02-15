using System.Configuration;
using System.Data;
using System.IO;
using System.Windows;
using UTS.WPF.Localization;
using UTS.WPF.MVVM;

namespace UTS.WPF
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        public App()
        {
            InitializeComponent();
        }

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            LocalizationService.ApplyCulture("cs-CZ");

            var vm = new MainViewModel();
            var win = new MainWindow { DataContext = vm };

            MainWindow = win;
            win.Show();

            if (e.Args.Length == 1 &&
                File.Exists(e.Args[0]) &&
                string.Equals(Path.GetExtension(e.Args[0]), ".uts", StringComparison.OrdinalIgnoreCase))
            {
                vm.OpenPath(e.Args[0]);
            }
        }
    }
}
