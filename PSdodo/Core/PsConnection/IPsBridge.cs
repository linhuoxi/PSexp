using System.Threading;
using System.Threading.Tasks;

namespace PsExporter.Core.PsConnection;

public interface IPsBridge
{
    /// <summary>当前平台是否支持 Photoshop 桥接（Win 用 COM，Mac 用 AppleScript）</summary>
    bool IsAvailable { get; }

    /// <summary>同步执行脚本（在 STA 线程上）</summary>
    PsBridgeResult Execute(string script, string? logFilePath = null);

    /// <summary>异步执行脚本（不阻塞 UI）</summary>
    Task<PsBridgeResult> ExecuteAsync(string script, string? logFilePath = null, CancellationToken ct = default);
}
