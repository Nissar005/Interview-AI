using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;

namespace PrivateAIOverlay.Windows;

public static class DisplayAffinityManager
{
    public static bool TryApply(Window window)
    {
        var helper = new WindowInteropHelper(window);
        var hwnd = helper.Handle;
        if (hwnd == IntPtr.Zero)
        {
            return false;
        }

        var success = NativeMethods.SetWindowDisplayAffinity(hwnd, NativeMethods.WDA_EXCLUDEFROMCAPTURE);
        if (!success)
        {
            var error = Marshal.GetLastWin32Error();
            System.Diagnostics.Debug.WriteLine($"SetWindowDisplayAffinity failed with Win32 error {error}.");
        }

        return success;
    }
}
