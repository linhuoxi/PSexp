using System.Diagnostics;
using System.Runtime.InteropServices;
using Microsoft.Win32;

namespace PsExporter.Core.PsConnection;

/// <summary>Attach to an existing automation object; probing must never start another Photoshop COM server.</summary>
internal static class RunningPhotoshopConnection
{
    private sealed record Registration(string ProgId, Guid ClassId, string ServerPath);
    private static readonly Lazy<Registration[]> Registrations = new(ReadRegistrations);

    public static object Attach(out string description)
    {
        var runningPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var processes = System.Diagnostics.Process.GetProcessesByName("Photoshop");
        try
        {
            if (processes.Length == 0) throw new PsNotConnectedException("Photoshop 未运行");
            foreach (var process in processes)
                try
                {
                    if (process.MainModule?.FileName is { } path) runningPaths.Add(path);
                }
                catch { /* Access restrictions must not turn process detection into 'no document'. */ }
        }
        finally { foreach (var process in processes) process.Dispose(); }

        var registrations = Registrations.Value;
        var matching = registrations.Where(item => runningPaths.Contains(item.ServerPath)).ToArray();
        // Never attach to a different installed version when the running EXE has a matching registration.
        var candidates = (matching.Length > 0 ? matching : registrations)
            .OrderByDescending(item => item.ProgId == "Photoshop.Application");
        var tried = new HashSet<Guid>();
        var failures = new List<string>();
        foreach (var candidate in candidates)
        {
            if (!tried.Add(candidate.ClassId)) continue;
            var classId = candidate.ClassId;
            var result = GetActiveObject(ref classId, IntPtr.Zero, out var application);
            if (result >= 0 && application is not null)
            {
                description = $"连接已运行实例：{candidate.ProgId}；CLSID={classId}；server={candidate.ServerPath}";
                return application;
            }
            if (application is not null && Marshal.IsComObject(application)) Marshal.ReleaseComObject(application);
            failures.Add($"{candidate.ProgId}/0x{result:X8}");
        }
        throw new PsNotConnectedException("Photoshop 正在运行，但无法连接其自动化接口。请确认 Photoshop 与 PSexp 使用相同的权限级别，并在 Photoshop 就绪后刷新。"
            + "未尝试启动新的 Photoshop 实例。连接记录：" + (failures.Count > 0 ? string.Join("；", failures) : "未找到可用的 COM 注册"));
    }

    private static Registration[] ReadRegistrations()
    {
        var items = new List<Registration>();
        var names = new List<string> { "Photoshop.Application" };
        try
        {
            names.AddRange(Registry.ClassesRoot.GetSubKeyNames().Where(name => name.StartsWith("Photoshop.Application.", StringComparison.OrdinalIgnoreCase)
                && name["Photoshop.Application.".Length..].All(char.IsDigit)));
        }
        catch { /* The generic registration is still checked when enumeration is restricted. */ }
        foreach (var name in names.Distinct(StringComparer.OrdinalIgnoreCase))
        {
            try
            {
                using var key = Registry.ClassesRoot.OpenSubKey(name + @"\CLSID");
                if (!Guid.TryParse(key?.GetValue(null)?.ToString(), out var classId)) continue;
                using var server = Registry.ClassesRoot.OpenSubKey(@"CLSID\" + classId.ToString("B") + @"\LocalServer32");
                var command = server?.GetValue(null)?.ToString() ?? "";
                var end = command.IndexOf(".exe", StringComparison.OrdinalIgnoreCase);
                var path = end >= 0 ? command[..(end + 4)].Trim().Trim('"') : "";
                items.Add(new Registration(name, classId, path));
            }
            catch { /* Keep checking the remaining installed versions. */ }
        }
        return items.ToArray();
    }

    [DllImport("oleaut32.dll", ExactSpelling = true, PreserveSig = true)]
    private static extern int GetActiveObject(ref Guid classId, IntPtr reserved,
        [MarshalAs(UnmanagedType.IUnknown)] out object? application);
}
