using System.Text.Json;

namespace PsExporter.Models;

/// <summary>当前 Photoshop 文档信息（由 probe/document_info.jsx 返回）</summary>
public sealed class PsDocumentInfo
{
    public string Name { get; set; } = string.Empty;
    public bool HasArtboards { get; set; }
    public int ArtboardCount { get; set; }
    public bool HasSlices { get; set; }
    public int Width { get; set; }
    public int Height { get; set; }
    public string Path { get; set; } = string.Empty;

    public bool IsOpen => !string.IsNullOrEmpty(Name);

    public static PsDocumentInfo Empty { get; } = new();

    public static PsDocumentInfo? Parse(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return null;
        try
        {
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;
            if (root.ValueKind != JsonValueKind.Object || !root.TryGetProperty("name", out var name)
                || name.ValueKind != JsonValueKind.String) return null;
            return new PsDocumentInfo
            {
                Name = root.TryGetProperty("name", out var n) ? n.GetString() ?? string.Empty : string.Empty,
                HasArtboards = root.TryGetProperty("hasArtboards", out var h) && h.GetBoolean(),
                ArtboardCount = root.TryGetProperty("artboardCount", out var c) ? c.GetInt32() : 0,
                HasSlices = root.TryGetProperty("hasSlices", out var hs) && hs.GetBoolean(),
                Width = root.TryGetProperty("width", out var w) ? w.GetInt32() : 0,
                Height = root.TryGetProperty("height", out var ht) ? ht.GetInt32() : 0,
                Path = root.TryGetProperty("path", out var p) ? p.GetString() ?? string.Empty : string.Empty
            };
        }
        catch
        {
            return null;
        }
    }
}
