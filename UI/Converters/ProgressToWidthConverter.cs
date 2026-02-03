using System;
using System.Globalization;
using System.Windows.Data;

namespace MarkdownConverter.UI.Converters
{
    public class ProgressToWidthConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values.Length < 3)
            {
                return 0d;
            }

            var value = values[0] is double v ? v : 0d;
            var maximum = values[1] is double m ? m : 100d;
            var width = values[2] is double w ? w : 0d;

            if (maximum <= 0)
            {
                return 0d;
            }

            return width * (value / maximum);
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            return Array.Empty<object>();
        }
    }
}
