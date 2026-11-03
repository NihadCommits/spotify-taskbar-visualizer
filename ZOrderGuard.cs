using static SpotifyTaskbarVisualizer.NativeMethods;

namespace SpotifyTaskbarVisualizer;

// The taskbar re-raises itself on foreground changes; this puts the overlay back on top right after.
internal sealed class ZOrderGuard : IDisposable
{
    private readonly IntPtr _hwnd;
    private readonly WinEventDelegate _callback; // Kept in a field so the GC never collects it while native code holds it.
    private readonly IntPtr _hook;

    public event Action? ForegroundChanged;

    public ZOrderGuard(IntPtr hwnd)
    {
        _hwnd = hwnd;
        _callback = (_, _, _, _, _, _, _) =>
        {
            BringToFront();
            ForegroundChanged?.Invoke();
        };
        // Out-of-context hooks are delivered through the calling (UI) thread's message loop.
        _hook = SetWinEventHook(EVENT_SYSTEM_FOREGROUND, EVENT_SYSTEM_FOREGROUND, IntPtr.Zero, _callback, 0, 0,
            WINEVENT_OUTOFCONTEXT | WINEVENT_SKIPOWNPROCESS);
    }

    public void BringToFront() => SetWindowPos(_hwnd, HWND_TOPMOST, 0, 0, 0, 0, SWP_NOMOVE | SWP_NOSIZE | SWP_NOACTIVATE);

    public void Dispose() => UnhookWinEvent(_hook);
}
