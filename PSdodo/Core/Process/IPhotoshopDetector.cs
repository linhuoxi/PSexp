using System.Threading;
using System.Threading.Tasks;

namespace PsExporter.Core.Process;

public interface IPhotoshopDetector
{
    /// <summary>检测当前是否运行着 Photoshop 进程，并尽量获取路径/版本</summary>
    PhotoshopProcessInfo Detect();

    /// <summary>尝试启动 Photoshop（未运行时）</summary>
    Task<bool> TryLaunchAsync(string? explicitPath, CancellationToken ct = default);
}
