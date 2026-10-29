namespace PsExporter.Core.Logging;

/// <summary>
/// 日志级别（与脚本端 PSX_LOG|level|msg 中的 level 对应）
/// </summary>
public enum PsLogLevel
{
    Info,
    Warn,
    Error,
    Debug
}
