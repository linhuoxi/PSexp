using System;
using System.Diagnostics;
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
/// 收纳态专用图标窗：始终置顶且不抢焦点的 ico 触发器。
/// 主窗口收纳后不再留下完整窗口或难以发现的窄条。
/// </summary>
public sealed class DockIconWindow : Window
{
    private DockDesktopIntegration? _desktop;
    public const double TriggerPaddingDip = 16;
    private readonly Image _icon;
    private readonly ScaleTransform _arrivalScale = new();
    private readonly Stopwatch _arrivalClock = new();
    private int _arrivalGeneration;
    public event EventHandler<DockIconRevealEventArgs>? RevealRequested;

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
        Closed += (_, _) => { ++_arrivalGeneration; _arrivalClock.Stop(); _desktop?.Dispose(); _desktop = null; };

        _icon = new Image
        {
            Source = new Bitmap(AssetLoader.Open(new Uri("avares://PSexpV3/Assets/ico.ico"))),
            Stretch = Stretch.Uniform,
            RenderTransform = _arrivalScale,
            RenderTransformOrigin = RelativePoint.Center,
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
        button.PointerPressed += (_, _) => RevealRequested?.Invoke(this, new DockIconRevealEventArgs(true));
        button.PointerEntered += (_, _) => RevealRequested?.Invoke(this, new DockIconRevealEventArgs(false));
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
        StopArrival();
        _desktop?.SetActive(false);
        Opacity = 0.0;
        Position = position;
    }

    internal void SetTransition(double collapse)
    {
        StopArrival();
        _icon.Opacity = DockAnimationMotion.IconVisibility(collapse);
    }

    internal void PulseArrival(DockEdge edge)
    {
        StopArrival();
        var generation = _arrivalGeneration;
        _arrivalClock.Restart();
        RequestAnimationFrame(_ => AdvanceArrival(generation, edge));
    }

    private void AdvanceArrival(int generation, DockEdge edge)
    {
        if (generation != _arrivalGeneration || Opacity <= 0) return;
        var t = Math.Clamp(_arrivalClock.Elapsed.TotalMilliseconds / 220, 0, 1);
        if (t >= 1) { StopArrival(); return; }
        // 吸入结束的轻挤压与回弹，仅改变图像，不改变窗口和鼠标热区。
        var pulse = Math.Sin(t * Math.PI * 2) * (1 - t) * (1 - t);
        var main = 1 - 0.07 * pulse;
        var cross = 1 + 0.025 * pulse;
        var vertical = edge is DockEdge.Left or DockEdge.Right;
        _arrivalScale.ScaleX = vertical ? main : cross;
        _arrivalScale.ScaleY = vertical ? cross : main;
        RequestAnimationFrame(_ => AdvanceArrival(generation, edge));
    }

    private void StopArrival()
    {
        ++_arrivalGeneration;
        _arrivalClock.Reset();
        _arrivalScale.ScaleX = _arrivalScale.ScaleY = 1;
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

public sealed class DockIconRevealEventArgs(bool isClick) : EventArgs
{
    public bool IsClick { get; } = isClick;
}
