using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace PsExporter.Core.PsConnection;

public sealed class ExportDirectorySnapshot
{
    internal ExportDirectorySnapshot(IReadOnlyDictionary<string, ExportFileStamp> files)
    {
        Files = files;
    }

    internal IReadOnlyDictionary<string, ExportFileStamp> Files { get; }
}

public sealed class ExportFileChangeResult
{
    public IReadOnlyList<string> Files { get; init; } = [];

    public bool TimedOut { get; init; }

    public int Count => Files.Count;
}

internal readonly record struct ExportFileStamp(long Length, long LastWriteTicks);

/// <summary>
/// 通过文件路径、长度和修改时间快照检测新增或覆盖写入的导出文件。
/// </summary>
public static class ExportFileWatcher
{
    public const int DefaultTimeoutMs = 120_000;
    public const int FirstChangeTimeoutMs = 30_000;
    public const int PollIntervalMs = 250;
    public const int StablePeriodMs = 2_000;

    public static ExportDirectorySnapshot CaptureSnapshot(string exportPath, string extension)
        => new(ReadSnapshot(exportPath, extension));

    public static async Task<ExportFileChangeResult> WaitForChangesAsync(
        string exportPath,
        string extension,
        ExportDirectorySnapshot before,
        int? expectedFileCount = null,
        int timeoutMs = DefaultTimeoutMs,
        Action<string, int>? onProgress = null,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(exportPath) || !Directory.Exists(exportPath))
            return new ExportFileChangeResult { TimedOut = true };

        var stopwatch = Stopwatch.StartNew();
        var reported = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        IReadOnlyDictionary<string, ExportFileStamp> lastChanged = EmptySnapshot;
        var stableMs = 0;

        while (stopwatch.ElapsedMilliseconds < timeoutMs)
        {
            await Task.Delay(PollIntervalMs, ct);
            ct.ThrowIfCancellationRequested();

            var current = ReadSnapshot(exportPath, extension);
            var changed = current
                .Where(pair => !before.Files.TryGetValue(pair.Key, out var oldStamp) || oldStamp != pair.Value)
                .ToDictionary(pair => pair.Key, pair => pair.Value, StringComparer.OrdinalIgnoreCase);

            if (changed.Count == 0)
            {
                if (reported.Count == 0 && stopwatch.ElapsedMilliseconds >= FirstChangeTimeoutMs)
                    return new ExportFileChangeResult { TimedOut = true };

                stableMs = 0;
                lastChanged = EmptySnapshot;
                continue;
            }

            foreach (var path in changed.Keys.OrderBy(path => path, StringComparer.OrdinalIgnoreCase))
            {
                if (!reported.Add(path)) continue;
                onProgress?.Invoke(Path.GetFileName(path), reported.Count);
            }

            if (SnapshotsEqual(lastChanged, changed))
            {
                stableMs += PollIntervalMs;
                var reachedExpectedCount =
                    expectedFileCount is null || reported.Count >= expectedFileCount.Value;
                if (stableMs >= StablePeriodMs && reachedExpectedCount)
                {
                    return new ExportFileChangeResult
                    {
                        Files = reported.OrderBy(path => path, StringComparer.OrdinalIgnoreCase).ToArray()
                    };
                }
            }
            else
            {
                stableMs = 0;
                lastChanged = changed;
            }
        }

        return new ExportFileChangeResult
        {
            Files = reported.OrderBy(path => path, StringComparer.OrdinalIgnoreCase).ToArray(),
            TimedOut = true
        };
    }

    private static IReadOnlyDictionary<string, ExportFileStamp> ReadSnapshot(string exportPath, string extension)
    {
        if (string.IsNullOrWhiteSpace(exportPath) || !Directory.Exists(exportPath))
            return EmptySnapshot;

        try
        {
            var pattern = "*." + extension.TrimStart('.').ToLowerInvariant();
            var result = new Dictionary<string, ExportFileStamp>(StringComparer.OrdinalIgnoreCase);
            foreach (var path in Directory.EnumerateFiles(exportPath, pattern, SearchOption.TopDirectoryOnly))
            {
                try
                {
                    var file = new FileInfo(path);
                    result[file.FullName] = new ExportFileStamp(file.Length, file.LastWriteTimeUtc.Ticks);
                }
                catch (IOException)
                {
                    // Photoshop 可能仍占用刚创建的文件，下一轮继续检测。
                }
                catch (UnauthorizedAccessException)
                {
                    // 单个文件读取失败不应中断整个任务。
                }
            }
            return result;
        }
        catch (IOException)
        {
            return EmptySnapshot;
        }
        catch (UnauthorizedAccessException)
        {
            return EmptySnapshot;
        }
    }

    private static bool SnapshotsEqual(
        IReadOnlyDictionary<string, ExportFileStamp> left,
        IReadOnlyDictionary<string, ExportFileStamp> right)
    {
        if (left.Count != right.Count) return false;
        foreach (var pair in left)
        {
            if (!right.TryGetValue(pair.Key, out var value) || value != pair.Value)
                return false;
        }
        return true;
    }

    private static readonly IReadOnlyDictionary<string, ExportFileStamp> EmptySnapshot =
        new Dictionary<string, ExportFileStamp>(StringComparer.OrdinalIgnoreCase);
}
