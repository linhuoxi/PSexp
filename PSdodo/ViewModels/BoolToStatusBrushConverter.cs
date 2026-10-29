using System;
using System.Globalization;
using Avalonia.Data.Converters;

namespace PsExporter.ViewModels;

public sealed class BoolToStatusBrushConverter : IValueConverter
{
    public static readonly BoolToStatusBrushConverter Instance = new();

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is true
            ? DesignBrushResolver.Get("Brush.Success")
            : DesignBrushResolver.Get("Brush.TextFaint");

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
