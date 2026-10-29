using System.Reflection;
using System.Runtime.InteropServices;

namespace PsExporter.Services;

/// <summary>独立收纳图标的 Shell 生命周期；不改变主窗、不占用工作区、不拦截全局快捷键。</summary>
[Obfuscation(Exclude = true, ApplyToMembers = true)]
internal sealed class DockDesktopIntegration : IDisposable
{
    private readonly IntPtr _hwnd;
    private readonly SubclassProc _callback;
    private readonly uint _shellMessage;
    private readonly uint _taskbarCreated;
    private bool _active;
    private bool _registered;
    private bool _raising;
    private readonly Avalonia.Threading.DispatcherTimer _zOrderGuard;
    private const uint Flags = 0x0001 | 0x0002 | 0x0010 | 0x0040; // no size/move/activate + show

    internal DockDesktopIntegration(IntPtr hwnd)
    {
        _hwnd = hwnd;
        _zOrderGuard = new Avalonia.Threading.DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
        _zOrderGuard.Tick += (_, _) => EnsureShown(forceRaise: true);
        _callback = WindowProc;
        _shellMessage = RegisterWindowMessage("PSexp.Dock.Appbar");
        _taskbarCreated = RegisterWindowMessage("TaskbarCreated");
        var style = GetWindowLongPtr(hwnd, -20).ToInt64();
        SetWindowLongPtr(hwnd, -20, new IntPtr((style | 0x80L | 0x08000000L) & ~0x00040000L));
        if (!SetWindowSubclass(hwnd, _callback, 1, UIntPtr.Zero))
            throw new System.ComponentModel.Win32Exception(Marshal.GetLastWin32Error());
    }

    internal void SetActive(bool active)
    {
        _active = active;
        if (!active)
        {
            _zOrderGuard.Stop();
            Unregister();
            return;
        }
        Register();
        EnsureShown(forceRaise: true);
        _zOrderGuard.Start();
    }

    private AppbarData Data() => new() { Size = (uint)Marshal.SizeOf<AppbarData>(), Window = _hwnd, Callback = _shellMessage };
    private void Register()
    {
        if (_registered) return;
        var data = Data();
        // 仅注册 Shell 工具窗身份，不发送 ABM_SETPOS，不为小图标预留一整条屏幕空间。
        _registered = SHAppBarMessage(0, ref data) != UIntPtr.Zero;
    }
    private void Unregister()
    {
        if (!_registered) return;
        var data = Data();
        SHAppBarMessage(1, ref data);
        _registered = false;
    }
    private void EnsureShown(bool forceRaise = false)
    {
        if (!_active || _raising) return;
        // Shell 通知可能由 Z 序变化再次触发；只修复状态，不能每次通知都重复 Show/SetWindowPos。
        if (!IsWindowVisible(_hwnd) || IsIconic(_hwnd)) ShowWindow(_hwnd, 4);
        if (forceRaise || (GetWindowLongPtr(_hwnd, -20).ToInt64() & 0x8) == 0)
        {
            _raising = true;
            try { SetWindowPos(_hwnd, new IntPtr(-1), 0, 0, 0, 0, Flags); }
            finally { _raising = false; }
        }
    }
    private IntPtr WindowProc(IntPtr hwnd, uint message, IntPtr wParam, IntPtr lParam, UIntPtr id, UIntPtr data)
    {
        if (_active)
        {
            if (message == 0x0112 && (wParam.ToInt64() & 0xFFF0) == 0xF020) return IntPtr.Zero;
            if (message == 0x0046 && lParam != IntPtr.Zero) // WM_WINDOWPOSCHANGING
            {
                var position = Marshal.PtrToStructure<WindowPosition>(lParam);
                if ((position.Flags & 0x0004) == 0) position.InsertAfter = new IntPtr(-1);
                position.Flags &= ~0x0080u; // 收纳图标不参与 Show Desktop 的隐藏。
                Marshal.StructureToPtr(position, lParam, false);
            }
            if (message == _taskbarCreated)
            {
                _registered = false;
                Register();
                EnsureShown();
            }
            if (message == _shellMessage && wParam.ToInt32() == 1) EnsureShown(); // ABN_POSCHANGED
        }
        return DefSubclassProc(hwnd, message, wParam, lParam);
    }
    public void Dispose()
    {
        _active = false;
        _zOrderGuard.Stop();
        Unregister();
        RemoveWindowSubclass(_hwnd, _callback, 1);
    }
    [StructLayout(LayoutKind.Sequential)] private struct AppbarData
    {
        public uint Size; public IntPtr Window; public uint Callback; public uint Edge;
        public int Left, Top, Right, Bottom; public IntPtr Param;
    }
    [StructLayout(LayoutKind.Sequential)] private struct WindowPosition
    {
        public IntPtr Window, InsertAfter; public int X, Y, Width, Height; public uint Flags;
    }
    private delegate IntPtr SubclassProc(IntPtr hwnd, uint message, IntPtr wParam, IntPtr lParam, UIntPtr id, UIntPtr data);
    [DllImport("comctl32.dll", SetLastError = true)] [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool SetWindowSubclass(IntPtr hwnd, SubclassProc proc, UIntPtr id, UIntPtr data);
    [DllImport("comctl32.dll")] private static extern bool RemoveWindowSubclass(IntPtr hwnd, SubclassProc proc, UIntPtr id);
    [DllImport("comctl32.dll")] private static extern IntPtr DefSubclassProc(IntPtr hwnd, uint message, IntPtr wParam, IntPtr lParam);
    [DllImport("shell32.dll")] private static extern UIntPtr SHAppBarMessage(uint message, ref AppbarData data);
    [DllImport("user32.dll", EntryPoint = "RegisterWindowMessageW", CharSet = CharSet.Unicode)] private static extern uint RegisterWindowMessage(string name);
    [DllImport("user32.dll")] private static extern IntPtr GetWindowLongPtr(IntPtr hwnd, int index);
    [DllImport("user32.dll")] private static extern IntPtr SetWindowLongPtr(IntPtr hwnd, int index, IntPtr value);
    [DllImport("user32.dll")] private static extern bool SetWindowPos(IntPtr hwnd, IntPtr after, int x, int y, int width, int height, uint flags);
    [DllImport("user32.dll")] private static extern bool ShowWindow(IntPtr hwnd, int command);
    [DllImport("user32.dll")] private static extern bool IsWindowVisible(IntPtr hwnd);
    [DllImport("user32.dll")] private static extern bool IsIconic(IntPtr hwnd);
}
