namespace PsExporter.Models;

/// <summary>画板导出路径选项</summary>
public enum ExportPathOption
{
    /// <summary>直接导出到用户选择的目录</summary>
    ImageOnly,
    /// <summary>按文档名新建目录；重名时自动追加序号</summary>
    NewFolder,
    /// <summary>按文档名创建或复用同名目录</summary>
    NewFolderOverwrite
}