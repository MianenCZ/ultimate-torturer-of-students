using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace UTS.WPF
{
    /// <summary>
    /// Interaction logic for CreateClassDialog.xaml
    /// </summary>
    public partial class CreateClassDialog : Window
    {
        public string[] Students { get; private set; } = [];
        public string ClassName { get; private set; } = string.Empty;
        public int T { get; private set; }
        public int E { get; private set; }
        public int K { get; private set; }

        public CreateClassDialog(string? defaultClassName = null, int defaultT = 20, int defaultE = 10, int defaultK = 10, string? defaultStudents = null)
        {
            InitializeComponent();

            ClassNameTextBox.Text = defaultClassName ?? string.Empty;
            TestsTextBox.Text = defaultT.ToString(CultureInfo.InvariantCulture);
            GradedMinimumTextBox.Text = defaultE.ToString(CultureInfo.InvariantCulture);
            GradedPerTestCountTextBox.Text = defaultK.ToString(CultureInfo.InvariantCulture);
            StudentsTextBox.Text = defaultStudents ?? string.Empty;

            Loaded += (_, _) => ClassNameTextBox.Focus();
        }

        private void Ok_Click(object sender, RoutedEventArgs @event)
        {
            ValidationText.Text = string.Empty;

            var className = (ClassNameTextBox.Text ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(className))
            {
                ValidationText.Text = "Class name is required.";
                ClassNameTextBox.Focus();
                return;
            }

            if (!int.TryParse((TestsTextBox.Text ?? string.Empty).Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var t) || t <= 0)
            {
                ValidationText.Text = "T must be a positive integer.";
                TestsTextBox.Focus();
                TestsTextBox.SelectAll();
                return;
            }

            if (!int.TryParse((GradedMinimumTextBox.Text ?? string.Empty).Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var e) || e <= 0)
            {
                ValidationText.Text = "E must be a positive integer.";
                GradedMinimumTextBox.Focus();
                GradedMinimumTextBox.SelectAll();
                return;
            }

            if (!int.TryParse((GradedPerTestCountTextBox.Text ?? string.Empty).Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var k) || k <= 0)
            {
                ValidationText.Text = "K must be a positive integer.";
                GradedPerTestCountTextBox.Focus();
                GradedPerTestCountTextBox.SelectAll();
                return;
            }

            var studentsRaw = StudentsTextBox.Text ?? string.Empty;
            Students = studentsRaw
                .Split('\r', '\n')
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Select(x => x.Trim())
                .ToArray();

            if (Students.Length == 0)
            {
                ValidationText.Text = "Student count must be a positive integer.";
                StudentsTextBox.Focus();
                return;
            }

            var minimalK = MathF.Ceiling((Students.Length * e) / t);
            if(minimalK > k) 
            {
                ValidationText.Text = 
                    $"K must be a greater than {minimalK}." + Environment.NewLine 
                    + "⌈(Students * e) / t⌉" + Environment.NewLine 
                    + $"⌈({Students.Length} * {e}) / {t}⌉ = {minimalK}";
                GradedPerTestCountTextBox.Focus();
                GradedPerTestCountTextBox.SelectAll();
                return;
            }

            // Students text: keep raw, but you may want to normalize newlines

            ClassName = className;
            T = t;
            E = e;
            K = k;

            DialogResult = true; // closes window
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false; // closes window
        }

        private void TestsTextBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            // allow only digits
            e.Handled = !DigitsOnly().IsMatch(e.Text);
        }

        [GeneratedRegex(@"^\d+$")]
        private static partial Regex DigitsOnly();
    }
}
