using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace MarkdownConverter.UI.Converters
{
    public class SegmentCornerRadiusConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values.Length < 2)
            {
                return new CornerRadius(0);
            }

            var index = values[0] is int i ? i : 0;
            var count = values[1] is int c ? c : 0;

            if (count <= 1)
            {
                return new CornerRadius(10);
            }

            if (index == 0)
            {
                return new CornerRadius(10, 0, 0, 10);
            }

            if (index == count - 1)
            {
                return new CornerRadius(0, 10, 10, 0);
            }

            return new CornerRadius(0);
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            return Array.Empty<object>();
        }
    }
}
