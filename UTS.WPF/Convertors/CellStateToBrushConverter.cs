using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;
using UTS.Backend.Domain;

namespace UTS.WPF.Convertors
{
    public sealed class CellStateToBrushConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is not CellState s) return Brushes.White;
            return s switch
            {
                CellState.Absent => Brushes.MistyRose,       // red-ish
                CellState.Recommended => Brushes.LightBlue,  // blue-ish
                CellState.Graded => Brushes.LightGreen,      // green-ish
                _ => Brushes.White
            };
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
            throw new NotSupportedException();
    }

    public sealed class CellStateToGlyphConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is not CellState s) return "";
            return s switch
            {
                CellState.Absent => "A",
                CellState.Recommended => "D",
                CellState.Graded => "H",
                _ => ""
            };
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
            throw new NotSupportedException();
    }
}
