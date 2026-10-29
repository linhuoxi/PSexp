namespace PsExporter.Core.Process;

/// <summary>
/// Photoshop 进程探测结果
/// </summary>
public sealed class PhotoshopProcessInfo
{
    public bool IsRunning { get; init; }

    public int ProcessId { get; init; }

    /// <summary>PS 可执行文件完整路径（可能为空，需提权或在 Mac 下）</summary>
    public string ExecutablePath { get; init; } = string.Empty;

    /// <summary>推测的 PS 版本号（如 "25.0"），可能为空</summary>
    public string Version { get; init; } = string.Empty;

    public static PhotoshopProcessInfo NotRunning => new();
}
