using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using PsExporter.Core.PsConnection;
using PsExporter.Core.Process;
using PsExporter.Core.Scripting;
using PsExporter.Models;

namespace PsExporter.Services;

/// <summary>
/// Photoshop 高层服务：进程探测 + 脚本执行 + 文档/画板信息查询
/// </summary>
public interface IPhotoshopService
{
    bool IsBridgeAvailable { get; }
    PhotoshopProcessInfo DetectProcess();
    Task<bool> TryLaunchAsync(string? explicitPath, CancellationToken ct = default);

    Task<PsDocumentInfo> GetDocumentInfoAsync(CancellationToken ct = default);
    /// <summary>探测文档信息，并通过 out 返回底层执行结果（含错误/日志）</summary>
    Task<(PsDocumentInfo Doc, PsBridgeResult Result)> ProbeDocumentInfoAsync(CancellationToken ct = default);

    /// <summary>前台激活时明确使用 UXP；派发前再次检查焦点。默认实现供替代服务兼容。</summary>
    Task<(PsDocumentInfo Doc, PsBridgeResult Result)> ProbeDocumentInfoWithUxpAsync(
        System.Func<bool>? canDispatch = null, CancellationToken ct = default)
        => ProbeDocumentInfoAsync(ct);

    Task<List<ArtboardInfo>> ListArtboardsAsync(CancellationToken ct = default);

    Task<PsBridgeResult> ExecuteScriptAsync(
        string scriptName, IReadOnlyDictionary<string, string> args, CancellationToken ct = default);
}
