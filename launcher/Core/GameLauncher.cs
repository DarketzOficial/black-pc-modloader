using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;

namespace BlackLauncher
{
    /// <summary>Finding the port's files and starting the game (black.exe, the Xbox-image owner).</summary>
    public static class GameLauncher
    {
        public static string BlackExe(string portDir) => Path.Combine(portDir, "black.exe");
        public static string BlackDll(string portDir) => Path.Combine(portDir, "blackrt.dll");
        public static string Xbe(string portDir) => Path.Combine(portDir, "game", "default.xbe");

        /// <summary>The latest game log: log\game\black-*.log, or the root black.log before the first archive.</summary>
        public static string LogFile(string portDir) => LogStore.LatestGameLog(portDir) ?? Path.Combine(portDir, "black.log");

        public static bool HasGame(string portDir) => File.Exists(Xbe(portDir));
        public static bool HasPlayer(string portDir) => File.Exists(BlackExe(portDir));

        /// <summary>A short description of what is missing, or null when everything is in place.</summary>
        public static string MissingReason(string portDir)
        {
            if (!File.Exists(BlackExe(portDir))) return Loc.T("black.exe não encontrado nesta pasta.", "black.exe was not found in this folder.");
            if (!File.Exists(Xbe(portDir))) return Loc.T("O jogo não foi encontrado: falta game\\default.xbe.",
                                                        "The game was not found: game\\default.xbe is missing.");
            if (!File.Exists(BlackDll(portDir))) return Loc.T("blackrt.dll não encontrado nesta pasta.", "blackrt.dll was not found in this folder.");
            return null;
        }

        /// <summary>Start black.exe in the port folder. Returns false (with a reason) when it cannot.</summary>
        public static bool Start(string portDir, out string error)
        {
            error = MissingReason(portDir);
            if (error != null) return false;
            try
            {
                StartGame(portDir).Dispose();
                return true;
            }
            catch (Exception ex)
            {
                error = ex.Message;
                return false;
            }
        }

        /// <summary>Start the game and keep the process handle so the watcher can read its exit code.</summary>
        public static Process StartGame(string portDir)
        {
            var psi = new ProcessStartInfo
            {
                FileName = BlackExe(portDir),
                WorkingDirectory = portDir,
                UseShellExecute = false,
                CreateNoWindow = true,
            };
            return Process.Start(psi);
        }

        /// <summary>
        /// Start a hidden copy of the launcher that starts the game and watches it. The visible
        /// launcher can then close: the watcher is what shows the crash report after the game ends.
        /// </summary>
        public static bool StartWatchdog(string portDir, out string error)
        {
            error = null;
            try
            {
                string exe = null;
                try { exe = Process.GetCurrentProcess().MainModule.FileName; } catch { }
                if (string.IsNullOrEmpty(exe) || !File.Exists(exe))
                    exe = Assembly.GetEntryAssembly().Location;
                string args = "--watch \"" + portDir.Replace("\"", "\\\"") + "\"";
                var psi = new ProcessStartInfo
                {
                    FileName = exe,
                    Arguments = args,
                    WorkingDirectory = portDir,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                };
                Process.Start(psi);
                return true;
            }
            catch (Exception ex)
            {
                error = ex.Message;
                return false;
            }
        }

        /// <summary>Open a folder or file with the shell (Explorer).</summary>
        public static void OpenInShell(string path)
        {
            try
            {
                if (Directory.Exists(path) || File.Exists(path))
                    Process.Start(new ProcessStartInfo { FileName = path, UseShellExecute = true });
            }
            catch (Exception) { }
        }

        /// <summary>Open a web page with the shell (the browser the player already uses).</summary>
        public static void OpenUrl(string url)
        {
            try
            {
                if (!string.IsNullOrEmpty(url))
                    Process.Start(new ProcessStartInfo { FileName = url, UseShellExecute = true });
            }
            catch (Exception) { }
        }

        public static void SelectInShell(string path)
        {
            try
            {
                if (File.Exists(path)) Process.Start("explorer.exe", "/select,\"" + path + "\"");
                else if (!string.IsNullOrEmpty(path)) OpenInShell(Path.GetDirectoryName(path));
            }
            catch (Exception) { }
        }
    }
}
