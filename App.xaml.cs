using System.Diagnostics;
using System.Windows;
using PrivateAIOverlay.Settings;
using PrivateAIOverlay.Windows;

namespace PrivateAIOverlay;

public partial class App : System.Windows.Application
{
    private static Mutex? _singleInstanceMutex;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        var createdNew = false;
        _singleInstanceMutex = new Mutex(true, "PrivateAIOverlay.SingleInstance", out createdNew);
        if (!createdNew)
        {
            foreach (var process in Process.GetProcessesByName(Process.GetCurrentProcess().ProcessName))
            {
                if (process.Id != Environment.ProcessId && process.MainWindowHandle != IntPtr.Zero)
                {
                    NativeMethods.AllowSetForegroundWindow((uint)process.Id);
                    NativeMethods.SetForegroundWindow(process.MainWindowHandle);
                    break;
                }
            }

            Shutdown();
            return;
        }

        var settings = SettingsService.Load();
        var mainWindow = new MainWindow(settings);
        MainWindow = mainWindow;
        mainWindow.Show();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _singleInstanceMutex?.ReleaseMutex();
        _singleInstanceMutex?.Dispose();
        base.OnExit(e);
    }
}

