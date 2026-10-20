using System;
using System.IO;
using System.Net;
using System.Windows;

namespace BlackLauncher
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            // GitHub and Discord only answer over TLS 1.2+; older .NET Framework defaults can still
            // negotiate down. Set it before any window (normal or the hidden crash watcher) starts.
            try { ServicePointManager.SecurityProtocol |= SecurityProtocolType.Tls12; } catch { }

            AppDomain.CurrentDomain.UnhandledException += (s, a) =>
            {
                try
                {
                    string dir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "log", "launcher");
                    Directory.CreateDirectory(dir);
                    File.AppendAllText(Path.Combine(dir, "launcher-crash.log"),
                        DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "  " +
                        (a.ExceptionObject == null ? "unknown" : a.ExceptionObject.ToString()) + "\r\n");
                }
                catch { }
            };

            base.OnStartup(e);

            if (e.Args.Length > 0 && string.Equals(e.Args[0], "--watch", StringComparison.OrdinalIgnoreCase))
            {
                // Hidden watchdog: no launcher window, no music, no update check. It starts the game,
                // waits, and either archives the log silently or shows CrashWindow.
                ShutdownMode = ShutdownMode.OnExplicitShutdown;
                string portDir = e.Args.Length > 1 && !string.IsNullOrWhiteSpace(e.Args[1])
                    ? e.Args[1].Trim().TrimEnd('\\', '/')
                    : AppDomain.CurrentDomain.BaseDirectory.TrimEnd('\\', '/');
                var watcher = new CrashWatcher(portDir);
                Dispatcher.InvokeAsync(async () =>
                {
                    try { await watcher.RunAsync(); }
                    catch (Exception ex) { LogStore.AppendReport(portDir, "watcher falhou: " + ex.Message); }
                    finally { Shutdown(); }
                });
                return;
            }

            var window = new MainWindow();
            MainWindow = window;
            window.Show();
        }
    }
}
