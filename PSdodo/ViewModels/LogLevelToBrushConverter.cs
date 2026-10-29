using System;
using System.Globalization;
using Avalonia.Data.Converters;

namespace PsExporter.ViewModels;

public sealed class LogLevelToBrushConverter : IValueConverter
{
    public static readonly LogLevelToBrushConverter Instance = new();

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var level = value as string ?? "Info";
        return DesignBrushResolver.Get(level switch
        {
            "Error" => "Brush.Danger",
            "Warn" => "Brush.Warning",
            "Debug" => "Brush.Debug",
            _ => "Brush.Info"
        });
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
