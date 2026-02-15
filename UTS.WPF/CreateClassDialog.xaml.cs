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

        public CreateClassDialog(string? defaultClassName = null, int defaultT = 6, string? defaultStudents = null)
        {
            InitializeComponent();

            ClassNameTextBox.Text = defaultClassName ?? string.Empty;
            TestsTextBox.Text = defaultT.ToString(CultureInfo.InvariantCulture);
            StudentsTextBox.Text = defaultStudents ?? string.Empty;

            Loaded += (_, _) => ClassNameTextBox.Focus();
        }

        private void Ok_Click(object sender, RoutedEventArgs e)
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

            // Students text: keep raw, but you may want to normalize newlines
            var studentsRaw = StudentsTextBox.Text ?? string.Empty;

            // Optional: basic cleanup (remove trailing whitespace lines)
            // Keep empty lines out if you want canonical storage:
            var normalized = string.Join("\n",
                studentsRaw
                    .Replace("\r\n", "\n")
                    .Replace("\r", "\n")
                    .Split('\n')
                    .Select(x => x.Trim())
                    .Where(x => x.Length > 0));

            Students = studentsRaw
                .Split('\r', '\n')
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Select(x => x.Trim())
                .ToArray();

            ClassName = className;
            T = t;

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
