using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Platform;
using Avalonia.Rendering.SceneGraph;
using Avalonia.Skia;
using PsExporter.Services;
using SkiaSharp;

namespace PsExporter.Controls;

// 每张纹理只创建一次 image/shader/paint；帧操作持有引用，防止 UI 交接时释放渲染线程仍在使用的资源。
internal sealed class GenieTexture : IDisposable
{
    private int _references = 1;
    private readonly SKImage _image;
    private readonly SKShader _shader;
    internal SKPaint Paint { get; }
    internal GenieTexture(SKBitmap bitmap)
    {
        _image = SKImage.FromBitmap(bitmap);
        _shader = _image.ToShader();
        Paint = new SKPaint { Shader = _shader, IsAntialias = true };
    }
    internal GenieTexture Acquire() { System.Threading.Interlocked.Increment(ref _references); return this; }
    public void Dispose()
    {
        if (System.Threading.Interlocked.Decrement(ref _references) != 0) return;
        Paint.Dispose(); _shader.Dispose(); _image.Dispose();
    }
}

internal class GenieMeshOperation : ICustomDrawOperation
{
    public Rect Bounds { get; }
    private GenieTexture? _texture;
    public EdgeSnapHelper.GenieMeshData? MeshData { get; }
    public SKPoint[]? TextureCoordinates { get; }

    public GenieMeshOperation(
        Rect bounds,
        GenieTexture texture,
        EdgeSnapHelper.GenieMeshData? meshData,
        SKPoint[]? textureCoordinates)
    {
        Bounds = bounds;
        _texture = texture.Acquire();
        MeshData = meshData;
        TextureCoordinates = textureCoordinates;
    }

    public void Dispose() => System.Threading.Interlocked.Exchange(ref _texture, null)?.Dispose();

    public bool Equals(ICustomDrawOperation? other) => false;

    public bool HitTest(Point p) => false;

    public void Render(ImmediateDrawingContext context)
    {
        var skiaContext = context.TryGetFeature<ISkiaSharpApiLeaseFeature>();
        if (skiaContext is null || _texture is null || MeshData is null || TextureCoordinates is null) return;

        using var lease = skiaContext.Lease();
        var canvas = lease.SkCanvas;

        canvas.DrawVertices(
            SKVertexMode.Triangles,
            MeshData.Vertices,
            TextureCoordinates,
            null,
            MeshData.Indices,
            _texture.Paint);
    }
}

public class GenieMeshControl : Control
{
    private SKPoint[]? _cachedTextureCoordinates;
    private SKPoint[]? _textureCoordinatesSource;
    private GenieTexture? _texture;
    private double _cachedTextureScale = double.NaN;

    public static readonly StyledProperty<SKBitmap?> BitmapProperty =
        AvaloniaProperty.Register<GenieMeshControl, SKBitmap?>(nameof(Bitmap));

    public static readonly StyledProperty<EdgeSnapHelper.GenieMeshData?> MeshDataProperty =
        AvaloniaProperty.Register<GenieMeshControl, EdgeSnapHelper.GenieMeshData?>(nameof(MeshData));

    public static readonly StyledProperty<double> TextureScaleProperty =
        AvaloniaProperty.Register<GenieMeshControl, double>(nameof(TextureScale), 1.0);

    public SKBitmap? Bitmap
    {
        get => GetValue(BitmapProperty);
        set => SetValue(BitmapProperty, value);
    }

    public EdgeSnapHelper.GenieMeshData? MeshData
    {
        get => GetValue(MeshDataProperty);
        set => SetValue(MeshDataProperty, value);
    }

    public double TextureScale
    {
        get => GetValue(TextureScaleProperty);
        set => SetValue(TextureScaleProperty, value);
    }

    static GenieMeshControl()
    {
        AffectsRender<GenieMeshControl>(BitmapProperty, MeshDataProperty, TextureScaleProperty);
    }

    private SKPoint[]? GetTextureCoordinates()
    {
        if (MeshData is null) return null;
        if (_cachedTextureCoordinates is not null
            && Math.Abs(_cachedTextureScale - TextureScale) < 0.0001
            && ReferenceEquals(_textureCoordinatesSource, MeshData.TexCoords))
            return _cachedTextureCoordinates;

        _textureCoordinatesSource = MeshData.TexCoords;
        _cachedTextureScale = Math.Max(TextureScale, 0.1);
        _cachedTextureCoordinates = new SKPoint[MeshData.TexCoords.Length];
        for (var index = 0; index < _cachedTextureCoordinates.Length; index++)
        {
            var point = MeshData.TexCoords[index];
            _cachedTextureCoordinates[index] = new SKPoint(
                point.X * (float)_cachedTextureScale,
                point.Y * (float)_cachedTextureScale);
        }
        return _cachedTextureCoordinates;
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == BitmapProperty)
        {
            var previous = _texture;
            _texture = Bitmap is null ? null : new GenieTexture(Bitmap);
            previous?.Dispose();
        }
    }

    public override void Render(DrawingContext context)
    {
        base.Render(context);
        if (_texture is null || MeshData is null || Bounds.Width <= 0 || Bounds.Height <= 0) return;
        context.Custom(new GenieMeshOperation(
            new Rect(0, 0, Bounds.Width, Bounds.Height),
            _texture,
            MeshData,
            GetTextureCoordinates()));
    }
}
