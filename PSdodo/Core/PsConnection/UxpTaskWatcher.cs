using System.Diagnostics;
using System.Text.Json;

namespace PsExporter.Core.PsConnection;

/// <summary>等待脚本明确回执，不以文件静默期推断批量任务完成。</summary>
public static class UxpTaskWatcher
{
    public const int DefaultTimeoutMs = 30 * 60 * 1000;
    public const int FirstReceiptTimeoutMs = 30_000;
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    public static async Task<PsBridgeResult> WaitForCompletionAsync(
        string resultPath, string exportPath, Action<string, int>? onProgress = null,
        CancellationToken ct = default, int timeoutMs = DefaultTimeoutMs)
    {
        var timer = Stopwatch.StartNew();
        var reported = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        IReadOnlyList<string> files = [];
        var receivedReceipt = false;
        while (timer.ElapsedMilliseconds < timeoutMs)
        {
            ct.ThrowIfCancellationRequested();
            UxpTaskStatus? status = null;
            string json = "";
            try
            {
                json = await File.ReadAllTextAsync(resultPath, ct);
                status = JsonSerializer.Deserialize<UxpTaskStatus>(json, JsonOptions);
            }
            catch (IOException) { /* 文件尚未创建，或脚本正在写入。 */ }
            catch (JsonException) { /* 下一轮读取完整 JSON。 */ }
            if (status is not null)
            {
                receivedReceipt = true;
                var names = status.Files ?? [];
                var validNames = names.Where(name => !string.IsNullOrWhiteSpace(name)
                    && Path.GetFileName(name) == name && !Path.IsPathRooted(name)
                    && (name.EndsWith(".png", StringComparison.OrdinalIgnoreCase)
                        || name.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase)))
                    .Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
                files = validNames.Select(name => Path.Combine(exportPath, name)).Where(File.Exists).ToArray();
                foreach (var file in files)
                    if (reported.Add(file)) onProgress?.Invoke(Path.GetFileName(file), reported.Count);
                if (status.Done)
                {
                    var success = status.Success && files.Count > 0 && files.Count == names.Count;
                    return new PsBridgeResult
                    {
                        Success = success, ReturnValue = json, OutputFiles = files, ExportedCount = files.Count,
                        Error = success ? null : new IOException(string.IsNullOrWhiteSpace(status.Error)
                            ? "UXP 导出未完成或输出文件缺失" : status.Error)
                    };
                }
            }
            if (!receivedReceipt && timer.ElapsedMilliseconds >= FirstReceiptTimeoutMs)
                return PsBridgeResult.Failed(new TimeoutException("Photoshop 未返回任务回执，请检查脚本启动和目录权限：" + resultPath));
            await Task.Delay(250, ct);
        }
        return new PsBridgeResult
        {
            Success = false, OutputFiles = files, ExportedCount = files.Count,
            Error = new TimeoutException("等待 UXP 导出回执超时；Photoshop 可能仍在执行，已保留输出和任务记录：" + resultPath)
        };
    }

    private sealed class UxpTaskStatus
    {
        public bool Done { get; set; }
        public bool Success { get; set; }
        public List<string>? Files { get; set; }
        public string? Error { get; set; }
    }
}
