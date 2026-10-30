using System;
using System.Threading.Tasks;
using System.Runtime.InteropServices;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using PsExporter.Controls;
using PsExporter.Services;
using SkiaSharp;

namespace PsExporter.Views;

/// <summary>
/// 独立透明的神灯动画承载窗。
/// 必须与真实主窗口分离，否则网格会被主窗口矩形边界裁剪，视觉上退化为普通平移。
/// </summary>
public sealed class GenieAnimationWindow : Window
{
    private readonly GenieMeshControl _surface;

    [DllImport("dwmapi.dll", PreserveSig = true)]
    private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int attrValue, int attrSize);

    public GenieAnimationWindow(
        PixelPoint position,
        double width,
        double height,
        SKBitmap bitmap,
        double textureScale)
    {
        Position = position;
        Width = width;
        Height = height;
        MinWidth = 1;
        MinHeight = 1;
        CanResize = false;
        Title = "PSexp 收纳动画";
        ShowInTaskbar = false;
        ShowActivated = false;
        Topmost = true;
        WindowDecorations = global::Avalonia.Controls.WindowDecorations.None;
        WindowStartupLocation = WindowStartupLocation.Manual;
        Background = Brushes.Transparent;
        TransparencyLevelHint = [WindowTransparencyLevel.Transparent];
        ExtendClientAreaToDecorationsHint = true;
        ExtendClientAreaTitleBarHeightHint = -1;

        Opened += (_, _) =>
        {
            DisableDwmShadow();
            WindowTaskSwitcher.Configure(this, excluded: true, noActivate: true);
        };

        _surface = new GenieMeshControl
        {
            Bitmap = bitmap,
            TextureScale = textureScale,
            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Stretch,
            VerticalAlignment = Avalonia.Layout.VerticalAlignment.Stretch,
            IsHitTestVisible = false
        };
        Content = _surface;
        Closed += (_, _) => { _surface.Bitmap = null; _surface.MeshData = null; };
    }

    public void SetBitmap(SKBitmap bitmap, double textureScale)
    {
        _surface.Bitmap = bitmap;
        _surface.TextureScale = textureScale;
    }

    public void SetMesh(EdgeSnapHelper.GenieMeshData meshData)
        => _surface.MeshData = meshData;

    internal void SetContentOpacity(double opacity)
        => _surface.MeshOpacity = Math.Clamp(opacity, 0, 1);

    internal async Task<bool> WaitForFirstDrawAsync()
    {
        var ready = _surface.WaitForDrawAsync();
        return await Task.WhenAny(ready, Task.Delay(100)) == ready && await ready;
    }

    internal void BeginFrameTiming() => _surface.BeginFrameTiming();
    internal string FrameTimingSummary => _surface.FrameTimingSummary;

    public void Park(PixelPoint position, bool retainTexture = false)
    {
        Opacity = 0.0;
        Position = position;
        _surface.CancelPendingDraw();
        _surface.MeshData = null;
        if (!retainTexture) _surface.Bitmap = null;
        // 保留已创建的透明工具窗，避免 Hide/Show 反复重建呈现路径。
        // Opacity=0、停放屏外、无任务栏入口；没有逐帧请求时不持续绘制。
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
