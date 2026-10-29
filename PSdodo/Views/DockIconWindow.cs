using System;
using System.Runtime.InteropServices;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using PsExporter.Services;

namespace PsExporter.Views;

/// <summary>
/// 收纳态专用图标窗：一个可见、聚焦、始终置顶的 ico 触发器。
/// 主窗口收纳后不再留下完整窗口或难以发现的窄条。
/// </summary>
public sealed class DockIconWindow : Window
{
    private DockDesktopIntegration? _desktop;
    public const double TriggerPaddingDip = 16;
    private readonly Image _icon;
    public event EventHandler? RevealRequested;

    [DllImport("dwmapi.dll", PreserveSig = true)]
    private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int attrValue, int attrSize);

    public DockIconWindow(PixelPoint position, double sizeDip)
    {
        Position = position;
        Width = sizeDip;
        Height = sizeDip;
        MinWidth = sizeDip;
        MaxWidth = sizeDip;
        MinHeight = sizeDip;
        MaxHeight = sizeDip;
        CanResize = false;
        Title = "PSexp 收纳图标";
        ShowInTaskbar = false;
        ShowActivated = false;
        Topmost = true;
        WindowStartupLocation = WindowStartupLocation.Manual;
        WindowDecorations = global::Avalonia.Controls.WindowDecorations.None;
        Background = Brushes.Transparent;
        TransparencyLevelHint = [WindowTransparencyLevel.Transparent];
        ExtendClientAreaToDecorationsHint = true;
        ExtendClientAreaTitleBarHeightHint = -1;

        Opened += (_, _) =>
        {
            DisableDwmShadow();
            if (OperatingSystem.IsWindows() && TryGetPlatformHandle() is { } handle)
                _desktop = new DockDesktopIntegration(handle.Handle);
        };
        Closed += (_, _) => { _desktop?.Dispose(); _desktop = null; };

        _icon = new Image
        {
            Source = new Bitmap(AssetLoader.Open(new Uri("avares://PSexpV3/Assets/ico.ico"))),
            Stretch = Stretch.Uniform,
            Margin = new Thickness(2),
            Width = sizeDip,
            Height = sizeDip,
            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
            VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center
        };
        var button = new Border
        {
            Background = Brushes.Transparent,
            Child = _icon,
            Cursor = new Cursor(StandardCursorType.Hand)
        };
        button.PointerPressed += (_, _) => RevealRequested?.Invoke(this, EventArgs.Empty);
        button.PointerEntered += (_, _) => RevealRequested?.Invoke(this, EventArgs.Empty);
        Content = button;
    }

    public void ShowAt(PixelPoint position, double sizeDip, double scaling)
    {
        // 复用时也按当前显示器 DPI 更新尺寸。
        MinWidth = MinHeight = 1;
        MaxWidth = MaxHeight = double.PositiveInfinity;
        Width = Height = sizeDip + TriggerPaddingDip * 2;
        _icon.Width = _icon.Height = sizeDip;
        var paddingPx=(int)Math.Ceiling(TriggerPaddingDip * scaling);
        Position = new PixelPoint(position.X-paddingPx,position.Y-paddingPx);
        Opacity = 1.0;
        _desktop?.SetActive(true);
    }

    public void Park(PixelPoint position)
    {
        _desktop?.SetActive(false);
        Opacity = 0.0;
        Position = position;
    }

    private void DisableDwmShadow()
    {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) return;
        try
        {
            var handle = TryGetPlatformHandle()?.Handle ?? IntPtr.Zero;
            if (handle == IntPtr.Zero) return;

            var cornerPreference = 1; // DWMWCP_DONOTROUND
            _ = DwmSetWindowAttribute(handle, 33, ref cornerPreference, sizeof(int));
            var ncaPolicy = 1; // DWMNCRP_DISABLED
            _ = DwmSetWindowAttribute(handle, 2, ref ncaPolicy, sizeof(int));
        }
        catch
        {
        }
    }
}
