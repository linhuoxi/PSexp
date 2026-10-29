using System.Collections.Generic;
using System.Text.Json;

namespace PsExporter.Models;

/// <summary>一个画板（由 probe/list_artboards.jsx 返回）</summary>
public sealed class ArtboardInfo
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;

    public static List<ArtboardInfo> ParseList(string? json)
    {
        var list = new List<ArtboardInfo>();
        if (string.IsNullOrWhiteSpace(json)) return list;
        try
        {
            using var doc = JsonDocument.Parse(json);
            if (doc.RootElement.ValueKind != JsonValueKind.Array) return list;
            foreach (var el in doc.RootElement.EnumerateArray())
            {
                list.Add(new ArtboardInfo
                {
                    Id = el.TryGetProperty("id", out var id) ? id.GetInt32() : 0,
                    Name = el.TryGetProperty("name", out var n) ? n.GetString() ?? string.Empty : string.Empty
                });
            }
        }
        catch
        {
            // 解析失败返回已收集的部分
        }
        return list;
    }
}
