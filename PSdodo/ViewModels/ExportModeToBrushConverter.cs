using System;
using System.Globalization;
using Avalonia;
using Avalonia.Data.Converters;
using Avalonia.Media;
using PsExporter.Models;

namespace PsExporter.ViewModels;

internal static class DesignBrushResolver
{
    public static IBrush Get(string key)
    {
        if (Application.Current is { } app &&
            app.TryGetResource(key, null, out var value) &&
            value is IBrush brush)
        {
            return brush;
        }

        return Brushes.Transparent;
    }
}

public sealed class ExportModeToBrushConverter : IValueConverter
{
    public static readonly ExportModeToBrushConverter Instance = new();

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is ExportMode mode && parameter is ExportMode target && mode == target
            ? DesignBrushResolver.Get("Brush.Surface")
            : Brushes.Transparent;

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}

public sealed class ExportModeBgConverter : IValueConverter
{
    public static readonly ExportModeBgConverter Instance = new();

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is ExportMode mode && parameter is ExportMode target && mode == target
            ? DesignBrushResolver.Get("Brush.Surface")
            : DesignBrushResolver.Get("Brush.SurfaceSubtle");

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}

public sealed class ExportModeTextColorConverter : IValueConverter
{
    public static readonly ExportModeTextColorConverter Instance = new();

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is ExportMode mode && parameter is ExportMode target && mode == target
            ? DesignBrushResolver.Get("Brush.TextPrimary")
            : DesignBrushResolver.Get("Brush.TextSecondary");

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}

public sealed class ExportModeIconColorConverter : IValueConverter
{
    public static readonly ExportModeIconColorConverter Instance = new();

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is ExportMode mode && parameter is ExportMode target && mode == target
            ? DesignBrushResolver.Get("Brush.TextPrimary")
            : DesignBrushResolver.Get("Brush.TextSecondary");

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}

public sealed class BoolToSettingBrushConverter : IValueConverter
{
    public static readonly BoolToSettingBrushConverter Instance = new();

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is true ? DesignBrushResolver.Get("Brush.Surface") : Brushes.Transparent;

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}

public sealed class BoolToSettingBgConverter : IValueConverter
{
    public static readonly BoolToSettingBgConverter Instance = new();

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is true
            ? DesignBrushResolver.Get("Brush.Surface")
            : DesignBrushResolver.Get("Brush.SurfaceSubtle");

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}

public sealed class BoolToTextColorConverter : IValueConverter
{
    public static readonly BoolToTextColorConverter Instance = new();

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is true
            ? DesignBrushResolver.Get("Brush.TextPrimary")
            : DesignBrushResolver.Get("Brush.TextSecondary");

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}

public sealed class BoolToIconColorConverter : IValueConverter
{
    public static readonly BoolToIconColorConverter Instance = new();

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is true
            ? DesignBrushResolver.Get("Brush.TextPrimary")
            : DesignBrushResolver.Get("Brush.TextSecondary");

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}

public sealed class BoolToRefreshBrushConverter : IValueConverter
{
    public static readonly BoolToRefreshBrushConverter Instance = new();

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is true
            ? DesignBrushResolver.Get("Brush.Success")
            : DesignBrushResolver.Get("Brush.TextSecondary");

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}

public sealed class ExportPathOptionDisplayConverter : IValueConverter
{
    public static readonly ExportPathOptionDisplayConverter Instance = new();

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value is ExportPathOption option
            ? option switch
            {
                ExportPathOption.ImageOnly => "直接保存到所选输出目录",
                ExportPathOption.NewFolder => "按文档名新建目录，重名自动编号",
                ExportPathOption.NewFolderOverwrite => "按文档名复用目录并更新同名文件",
                _ => option.ToString()
            }
            : value?.ToString() ?? string.Empty;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
