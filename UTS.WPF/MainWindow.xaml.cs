using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using UTS.Backend.Domain;
using UTS.WPF.MVVM;

namespace UTS.WPF
{
    public partial class MainWindow : Window
    {
        private readonly Dictionary<DataGridColumn, int> _colToTestIndex = new();

        public MainWindow()
        {
            InitializeComponent();
            DataContextChanged += (_, __) => AttachAndBuild();
            Loaded += (_, __) => AttachAndBuild();

            if(DataContext is not MainViewModel vm)
            {
                DataContext = vm = new MainViewModel();
            }

            MenuItem_File_Open.Command = vm.OpenCommand;
            MenuItem_File_Save.Command = vm.SaveCommand;
            MenuItem_File_SaveAs.Command = vm.SaveAsCommand;
            MenuItem_File_New.Command = vm.NewCommand;
        }

        private void AttachAndBuild()
        {
            if (DataContext is not MainViewModel vm) return;

            vm.DocumentChanged -= Vm_DocumentChanged;
            vm.DocumentChanged += Vm_DocumentChanged;

            RebuildColumns();
        }

        private void Vm_DocumentChanged(object? sender, EventArgs e) => RebuildColumns();

        private void RebuildColumns()
        {
            if (DataContext is not MainViewModel vm || vm.Document == null) return;

            Grid.Columns.Clear();
            _colToTestIndex.Clear();

            Grid.Columns.Add(new DataGridTextColumn
            {
                Header = TryFindResource("Col_Student") ?? "Student",
                Binding = new Binding(nameof(StudentRecord.Name))
            });

            var brushConv = (IValueConverter)FindResource("CellStateToBrush");
            var glyphConv = (IValueConverter)FindResource("CellStateToGlyph");

            for (int t = 0; t < vm.Document.T; t++)
            {
                int testIndex = t;

                var headerButton = new Button
                {
                    Content = $"{t + 1}",
                    Padding = new Thickness(6, 2, 6, 2)
                };
                headerButton.Command = vm.GenerateColumnCommand;
                headerButton.CommandParameter = testIndex;

                // Build a DataTemplate: Border(Background=converter([t])) -> TextBlock(Text=converter([t]))
                var border = new FrameworkElementFactory(typeof(Border));
                border.SetValue(Border.PaddingProperty, new Thickness(0));
                border.SetValue(Border.MarginProperty, new Thickness(0));
                border.SetBinding(Border.BackgroundProperty, new Binding($"[{testIndex}]") { Converter = brushConv });

                var text = new FrameworkElementFactory(typeof(TextBlock));
                text.SetValue(TextBlock.HorizontalAlignmentProperty, HorizontalAlignment.Center);
                text.SetValue(TextBlock.VerticalAlignmentProperty, VerticalAlignment.Center);
                text.SetValue(TextBlock.FontWeightProperty, FontWeights.SemiBold);
                text.SetBinding(TextBlock.TextProperty, new Binding($"[{testIndex}]") { Converter = glyphConv });

                border.AppendChild(text);

                var template = new DataTemplate { VisualTree = border };

                var col = new DataGridTemplateColumn
                {
                    Header = headerButton,
                    CellTemplate = template,
                    Width = new DataGridLength(1, DataGridLengthUnitType.Star),
                    MinWidth = 42
                };

                _colToTestIndex[col] = testIndex;
                Grid.Columns.Add(col);
            }
        }


        public void Grid_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (TryResolveStudentAndTestIndex(e, out var student, out var testIndex))
            {
                ((MainViewModel)DataContext).HandleLeftClick(student, testIndex);
                // Allow selection/focus to proceed; do not set e.Handled.
            }
        }

        public void Grid_PreviewMouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (TryResolveStudentAndTestIndex(e, out var student, out var testIndex))
            {
                ((MainViewModel)DataContext).HandleRightClick(student, testIndex);
                e.Handled = true; // prevents context menu / default right-click behaviors
            }
        }

        private bool TryResolveStudentAndTestIndex(MouseButtonEventArgs e, out StudentRecord student, out int testIndex)
        {
            student = null!;
            testIndex = -1;

            if (DataContext is not MainViewModel vm || vm.Document == null) return false;

            var hit = VisualTreeHelper.HitTest(Grid, e.GetPosition(Grid));
            if (hit?.VisualHit is null) return false;

            DependencyObject d = hit.VisualHit;
            while (d != null && d is not DataGridCell)
                d = VisualTreeHelper.GetParent(d);

            if (d is not DataGridCell cell) return false;
            if (cell.DataContext is not StudentRecord s) return false;

            if (!_colToTestIndex.TryGetValue(cell.Column, out int t)) return false;

            student = s;
            testIndex = t;
            return true;
        }
    }
}