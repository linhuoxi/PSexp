using System.Diagnostics;
using System.Text.Json;
using PsExporter.Core.Logging;
using PsExporter.Core.PsConnection;
using PsExporter.Core.Scripting;
using PsExporter.Models;

namespace PsExporter.Services;

public sealed partial class PhotoshopService
{
    private readonly UxpScriptBridge? _probeUxpBridge;
    private readonly FileLogger? _probeLogger;
    private bool _useUxpProbe;
    public Task<(PsDocumentInfo Doc, PsBridgeResult Result)> ProbeDocumentInfoWithUxpAsync(
        Func<bool>? canDispatch = null, CancellationToken ct = default)
    {
        _useUxpProbe = true;
        return ProbeWithUxpAsync(ct, canDispatch);
    }

    private async Task<(PsDocumentInfo Doc, PsBridgeResult Result)> ProbeWithUxpAsync(CancellationToken ct, Func<bool>? canDispatch = null)
    {
        if (_probeUxpBridge is null)
            return (PsDocumentInfo.Empty, PsBridgeResult.Failed(new IOException("未配置 UXP 文档识别通道")));
        var taskDirectory = Path.Combine(Path.GetTempPath(), "PSexp", "probes", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(taskDirectory);
        var template = ScriptLibrary.GetScript("probe/document_info_uxp");
        var script = ScriptParameterInjector.Inject(template, new Dictionary<string, string>
        {
            ["RESULT_FOLDER_URL"] = ScriptParameterInjector.EscapeJsString(ExportService.ToUxpFolderUrl(taskDirectory))
        });
        var dispatched = await _probeUxpBridge.ExecuteAsync(script, ct, canDispatch);
        _probeLogger?.Info($"UXP 文档识别启动 success={dispatched.Success}；脚本={dispatched.TempFilePath}；回执={taskDirectory}");
        if (!dispatched.Success)
            return (PsDocumentInfo.Empty, PsBridgeResult.Failed(dispatched.Error ?? new IOException("UXP 文档识别脚本未启动")));
        var clock = Stopwatch.StartNew();
        while (clock.Elapsed < TimeSpan.FromSeconds(30))
        {
            ct.ThrowIfCancellationRequested();
            string? raw = null;
            try { raw = await File.ReadAllTextAsync(Path.Combine(taskDirectory, "document.json"), ct); }
            catch (IOException) { }
            if (raw is not null)
            {
                JsonDocument? receipt = null;
                try { receipt = JsonDocument.Parse(raw); }
                catch (JsonException) { /* Writer may be replacing the receipt. */ }
                using (receipt)
                {
                    if (receipt is not null && receipt.RootElement.GetProperty("done").GetBoolean())
                    {
                        var root = receipt.RootElement;
                        _probeLogger?.Info("UXP 文档识别回执=" + raw);
                        if (!root.GetProperty("success").GetBoolean())
                            return (PsDocumentInfo.Empty, PsBridgeResult.Failed(new IOException("UXP 文档识别失败：" + root.GetProperty("error").GetString())));
                        var document = PsDocumentInfo.Parse(root.GetProperty("document").GetRawText());
                        if (document is null)
                            return (PsDocumentInfo.Empty, PsBridgeResult.Failed(new InvalidDataException("UXP 文档识别返回无效数据")));
                        // UXP Document.path is a full filename; the existing model expects its parent directory.
                        document.Path = Path.IsPathFullyQualified(document.Path) ? Path.GetDirectoryName(document.Path) ?? "" : "";
                        var logs = new List<PsLogEntry>
                        {
                            new() { Level = PsLogLevel.Info, Message = "已通过 UXP 读取文档；前台识别仅在 PSexp 获得焦点时执行一次，导出前另行刷新。" }
                        };
                        foreach (var warning in root.GetProperty("warnings").EnumerateArray())
                            logs.Add(new PsLogEntry { Level = PsLogLevel.Warn, Message = warning.GetString() ?? "" });
                        // Receipt parsing has finished before asynchronous cleanup.
                        _ = _probeUxpBridge.CleanupTempFileAsync(dispatched.TempFilePath);
                        try { Directory.Delete(taskDirectory, recursive: true); }
                        catch (IOException) { }
                        catch (UnauthorizedAccessException) { }
                        return (document, new PsBridgeResult { Success = true, ReturnValue = raw, Logs = logs });
                    }
                }
            }
            await Task.Delay(150, ct);
        }
        return (PsDocumentInfo.Empty, PsBridgeResult.Failed(new TimeoutException(
            "UXP 文档识别未收到完成回执，请检查 .psjs 关联及 Photoshop 状态。诊断已保留：" + taskDirectory)));
    }
}
