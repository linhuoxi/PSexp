namespace PsExporter.Models;

/// <summary>全局应用设置（持久化到 %AppData%/PsExporter/settings.json）</summary>
public sealed class AppSettings
{
    public const double DefaultWindowWidth = 440;
    public const double DefaultWindowHeight = 660;
    public const double MinimumWindowWidth = 360;
    public const double MinimumWindowHeight = 600;
    public const double WindowAspectRatio = 3d / 4d;

    /// <summary>自定义目录；切换到文档位置或桌面时仍保留。</summary>
    public string DefaultExportPath { get; set; } = string.Empty;

    /// <summary>旧配置缺少此项时，保留原先的自定义目录行为。</summary>
    public ExportDestination? DefaultDestination { get; set; }

    public ExportFormat DefaultFormat { get; set; } = ExportFormat.Jpg;

    public int DefaultQuality { get; set; } = 80;

    public bool DefaultAutoRename { get; set; } = true;

    public string? PhotoshopExePath { get; set; }

    /// <summary>文档状态轮询间隔（毫秒）</summary>
    public int PollingIntervalMs { get; set; } = 3000;

    /// <summary>导出一级图层时文件名带文档名前缀</summary>
    public bool LayerExportWithDocName { get; set; } = true;
    public bool LayerExportRecursive { get; set; }
    public bool LayerExportSelectedOnly { get; set; } = true;

    /// <summary>兼容旧配置保留；当前版本不再使用 Photoshop 提示窗。</summary>
    public bool LayerExportShowHint { get; set; } = false;

    /// <summary>动作启动后自动获取 PS 文档名作为导出命名</summary>
    public bool AutoUseDocNameAsPrefix { get; set; } = false;

    /// <summary>应用主题</summary>
    public AppThemeMode ThemeMode { get; set; } = AppThemeMode.Light;

    /// <summary>启动后保持窗口置顶</summary>
    public bool StartAlwaysOnTop { get; set; }

    /// <summary>窗口回到前台时自动识别一次当前文档</summary>
    public bool AutoRefreshOnActivated { get; set; } = true;

    /// <summary>拖拽窗口贴到屏幕边缘吸附与 Q 弹动效</summary>
    public bool EnableEdgeSnap { get; set; } = true;

    public bool AutoCheckForUpdates { get; set; } = true;
    public bool AutoDownloadUpdates { get; set; } = true;
    public bool CloseToTray { get; set; } = true;
    public bool EnableBlankDrag { get; set; } = true;
    public bool EnableQualityHoverAnimation { get; set; } = true;
    public int DocumentSyncIntervalMs { get; set; } = 1200;
    public int DockRetractDelayMs { get; set; } = 420;

    public WindowPlacementState? WindowPlacement { get; set; }

    /// <summary>上次正常窗口宽度</summary>
    public double WindowWidth { get; set; } = DefaultWindowWidth;

    /// <summary>上次正常窗口高度</summary>
    public double WindowHeight { get; set; } = DefaultWindowHeight;
}
