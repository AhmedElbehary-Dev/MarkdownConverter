using Avalonia.Data.Converters;
using Avalonia.Media;
using MarkdownConverter.ViewModels;
using System;
using System.Globalization;

namespace MarkdownConverter.Desktop.Converters;

public sealed class StatusKindToBrushConverter : IValueConverter
{
    private static readonly IBrush Neutral = Brushes.Gray;
    private static readonly IBrush Info = new SolidColorBrush(Color.Parse("#2563EB"));
    private static readonly IBrush Success = new SolidColorBrush(Color.Parse("#15803D"));
    private static readonly IBrush Error = new SolidColorBrush(Color.Parse("#B91C1C"));

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value switch
        {
            StatusKind.Info => Info,
            StatusKind.Success => Success,
            StatusKind.Error => Error,
            _ => Neutral
        };
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}
