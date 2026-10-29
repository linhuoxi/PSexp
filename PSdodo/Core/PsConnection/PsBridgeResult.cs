using System.Collections.Generic;
using PsExporter.Core.Logging;

namespace PsExporter.Core.PsConnection;

/// <summary>
/// 一次 Photoshop 脚本执行的结果
/// </summary>
public sealed class PsBridgeResult
{
    public bool Success { get; init; }

    /// <summary>脚本最后一个表达式的返回值（已转成字符串）</summary>
    public string ReturnValue { get; init; } = string.Empty;

    /// <summary>脚本通过 $.writeln 输出并被捕获的日志</summary>
    public IReadOnlyList<PsLogEntry> Logs { get; init; } = [];

    /// <summary>本次任务确认导出的文件数量</summary>
    public int ExportedCount { get; init; }

    /// <summary>本次任务确认新增或覆盖写入的文件</summary>
    public IReadOnlyList<string> OutputFiles { get; init; } = [];

    public Exception? Error { get; init; }

    public static PsBridgeResult Failed(Exception ex) => new()
    {
        Success = false,
        Error = ex,
        ReturnValue = string.Empty,
        Logs = []
    };
}
