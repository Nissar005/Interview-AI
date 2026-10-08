using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;

namespace PrivateAIOverlay.Windows;

public sealed class HotkeyManager : IDisposable
{
    private readonly Window _window;
    private readonly Dictionary<int, Action> _hotkeys = new();
    private HwndSource? _source;
    private IntPtr _windowHandle;

    public HotkeyManager(Window window)
    {
        _window = window;
        window.SourceInitialized += OnSourceInitialized;
    }

    public void Register(string name, Key key, ModifierKeys modifiers, Action action)
    {
        if (_windowHandle == IntPtr.Zero)
        {
            throw new InvalidOperationException("The overlay window handle is not ready yet. Register hotkeys after the HWND is created.");
        }

        var id = (int)key + (_hotkeys.Count + 1) * 1000;
        var vk = KeyInterop.VirtualKeyFromKey(key);
        var mod = (uint)modifiers;
        if (!NativeMethods.RegisterHotKey(_windowHandle, id, mod, (uint)vk))
        {
            throw new InvalidOperationException($"The hotkey '{name}' could not be registered. It may already be in use by another application.");
        }

        _hotkeys[id] = action;
    }

    public void UnregisterAll()
    {
        foreach (var id in _hotkeys.Keys.ToList())
        {
            NativeMethods.UnregisterHotKey(_windowHandle, id);
            _hotkeys.Remove(id);
        }
    }

    private void OnSourceInitialized(object? sender, EventArgs e)
    {
        _windowHandle = new WindowInteropHelper(_window).Handle;
        _source = HwndSource.FromHwnd(_windowHandle);
        _source?.AddHook(WndProc);
    }

    private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (msg == NativeMethods.WM_HOTKEY && _hotkeys.TryGetValue(wParam.ToInt32(), out var action))
        {
            action();
            handled = true;
        }

        return IntPtr.Zero;
    }

    public void Dispose()
    {
        UnregisterAll();
        _source?.RemoveHook(WndProc);
        _window.SourceInitialized -= OnSourceInitialized;
    }
}
