using System;
using System.Threading;
using System.Threading.Tasks;

namespace PsExporter.Core.PsConnection;

/// <summary>
/// 在 STA 线程上执行 COM 调用（Photoshop COM 要求 STA）
/// </summary>
internal static class StaHelper
{
    public static T Run<T>(Func<T> func)
    {
        if (Thread.CurrentThread.GetApartmentState() == ApartmentState.STA)
            return func();

        T result = default!;
        Exception? error = null;
        var t = new Thread(() =>
        {
            try { result = func(); }
            catch (Exception ex) { error = ex; }
        })
        {
            IsBackground = true
        };
        t.SetApartmentState(ApartmentState.STA);
        t.Start();
        t.Join();
        return error is null ? result : throw error;
    }

    public static Task<T> RunAsync<T>(Func<T> func, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        return Task.Run(() => Run(func), ct);
    }
}
