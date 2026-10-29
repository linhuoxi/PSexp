using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace PsExporter.Core.Scripting;

/// <summary>
/// 把参数字典注入到脚本模板的 {{NAME}} 占位符中
/// </summary>
public static class ScriptParameterInjector
{
    private static readonly Regex _rx = new(@"\{\{\s*([A-Za-z_][A-Za-z0-9_]*)\s*\}\}",
        System.Text.RegularExpressions.RegexOptions.Compiled);

    /// <summary>
    /// 替换所有 {{KEY}} 占位符；未提供的 KEY 保持原样（便于诊断）
    /// </summary>
    public static string Inject(string template, IReadOnlyDictionary<string, string> args)
    {
        return _rx.Replace(template, m =>
        {
            var key = m.Groups[1].Value;
            return args.TryGetValue(key, out var v) ? v : m.Value;
        });
    }

    /// <summary>转义为 ExtendScript 字符串字面量内容（不带引号）</summary>
    public static string EscapeJsString(string? s)
    {
        if (s is null) return string.Empty;
        return s.Replace("\\", "\\\\")
                .Replace("\"", "\\\"")
                .Replace("'", "\\'")
                .Replace("\n", "\\n")
                .Replace("\r", "\\r");
    }
}
