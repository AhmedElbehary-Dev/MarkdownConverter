using Avalonia.Data.Converters;
using Avalonia.Media;
using MarkdownConverter.ViewModels;
using System;
using System.Globalization;

namespace MarkdownConverter.Desktop.Converters;

public sealed class ToastKindToBrushConverter : IValueConverter
{
    private static readonly IBrush Success = new SolidColorBrush(Color.Parse("#166534"));
    private static readonly IBrush Error = new SolidColorBrush(Color.Parse("#991B1B"));

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value is ToastKind.Error ? Error : Success;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}
