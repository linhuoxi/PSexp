using Avalonia.Data.Converters;
using Avalonia.Media;
using System.Globalization;
namespace PsExporter.ViewModels;
public sealed class ChoiceBorderConverter : IValueConverter
{
    public static readonly ChoiceBorderConverter Instance = new();
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => (parameter is null ? value is true : string.Equals(value?.ToString(),parameter?.ToString(),StringComparison.OrdinalIgnoreCase))
            ? DesignBrushResolver.Get("Brush.Border") : Brushes.Transparent;
    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
