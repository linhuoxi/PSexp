using System;

namespace PsExporter.Core.Logging;

/// <summary>
/// 一条来自 Photoshop 脚本的日志记录
/// </summary>
public sealed class PsLogEntry
{
    public DateTime Timestamp { get; } = DateTime.Now;

    public PsLogLevel Level { get; init; } = PsLogLevel.Info;

    public string Message { get; init; } = string.Empty;

    /// <summary>
    /// 解析脚本输出的 "PSX_LOG|level|message" 行；非该格式的行作为 Info 返回
    /// </summary>
    public static PsLogEntry Parse(string line)
    {
        if (string.IsNullOrEmpty(line))
            return new PsLogEntry();

        const string prefix = "PSX_LOG|";
        if (!line.StartsWith(prefix, StringComparison.Ordinal))
            return new PsLogEntry { Level = PsLogLevel.Info, Message = line };

        var rest = line.Substring(prefix.Length);
        var sep = rest.IndexOf('|');
        if (sep < 0)
            return new PsLogEntry { Level = PsLogLevel.Info, Message = rest };

        var levelStr = rest.Substring(0, sep);
        var msg = rest.Substring(sep + 1);
        var level = levelStr.ToLowerInvariant() switch
        {
            "info" => PsLogLevel.Info,
            "warn" or "warning" => PsLogLevel.Warn,
            "error" or "err" => PsLogLevel.Error,
            "debug" => PsLogLevel.Debug,
            _ => PsLogLevel.Info
        };
        return new PsLogEntry { Level = level, Message = msg };
    }

    public override string ToString() => $"[{Timestamp:HH:mm:ss}] [{Level}] {Message}";
}
