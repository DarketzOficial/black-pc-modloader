using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;

namespace BlackLauncher
{
    /// <summary>Finding the port's files and starting the game (black.exe, the Xbox-image owner).</summary>
    public static class GameLauncher
    {
        public enum TargetKind { PcPort, Recomp }

        public static string BlackExe(string portDir) => Path.Combine(portDir, "black.exe");
        public static string BlackDll(string portDir) => Path.Combine(portDir, "blackrt.dll");
        public static string Xbe(string portDir) => Path.Combine(portDir, "game", "default.xbe");

        /// <summary>The latest game log: log\game\black-*.log, or the root black.log before the first archive.</summary>
        public static string LogFile(string portDir) => LogStore.LatestGameLog(portDir) ?? Path.Combine(portDir, "black.log");

        public static bool HasGame(string portDir) => File.Exists(Xbe(portDir));
        public static bool HasPlayer(string portDir) => File.Exists(BlackExe(portDir));

        public static string RecompExe(string recompDir) => Path.Combine(recompDir, "BLACK.exe");
        public static string RecompXbe(string recompDir) => Path.Combine(recompDir, "assets", "default.xbe");

        public static bool IsRecomp(string dir)
        {
            if (string.IsNullOrWhiteSpace(dir) || !File.Exists(RecompExe(dir))) return false;
            if (File.Exists(RecompXbe(dir))) return true;
            string assets = Path.Combine(dir, "assets");
            if (!Directory.Exists(assets)) return false;
            try
            {
                return Directory.GetFiles(assets).Any(path =>
                {
                    string ext = Path.GetExtension(path);
                    return ext.Equals(".iso", StringComparison.OrdinalIgnoreCase) ||
                           ext.Equals(".xbe", StringComparison.OrdinalIgnoreCase) ||
                           ext.Equals(".img", StringComparison.OrdinalIgnoreCase);
                });
            }
            catch (Exception) { return false; }
        }

        public static string RecompVersion(string dir)
        {
            if (!IsRecomp(dir)) return Loc.T("recompilação não identificada", "Recomp not identified");
            try
            {
                var info = FileVersionInfo.GetVersionInfo(RecompExe(dir));
                if (!string.IsNullOrWhiteSpace(info.ProductVersion)) return info.ProductVersion;
                if (!string.IsNullOrWhiteSpace(info.FileVersion)) return info.FileVersion;
            }
            catch (Exception) { }
            return Loc.T("versão desconhecida", "version unknown");
        }

        public static string RecompImageIdentity(string dir)
        {
            string xbe = RecompXbe(dir);
            if (!File.Exists(xbe)) return Loc.T("XBE não encontrado", "XBE not found");
            try
            {
                using (var sha1 = SHA1.Create())
                using (var stream = File.OpenRead(xbe))
                    return "XBE SHA-1 " + BitConverter.ToString(sha1.ComputeHash(stream)).Replace("-", "");
            }
            catch (Exception) { return Loc.T("identidade do XBE indisponível", "XBE identity unavailable"); }
        }

        /// <summary>A short description of what is missing, or null when everything is in place.</summary>
        public static string MissingReason(string portDir)
        {
            if (!File.Exists(BlackExe(portDir))) return Loc.T("black.exe não encontrado nesta pasta.", "black.exe was not found in this folder.");
            if (!File.Exists(Xbe(portDir))) return Loc.T("O jogo não foi encontrado: falta game\\default.xbe.",
                                                        "The game was not found: game\\default.xbe is missing.");
            if (!File.Exists(BlackDll(portDir))) return Loc.T("blackrt.dll não encontrado nesta pasta.", "blackrt.dll was not found in this folder.");
            return null;
        }

        public static string RecompMissingReason(string recompDir)
        {
            if (string.IsNullOrWhiteSpace(recompDir) || !Directory.Exists(recompDir))
                return Loc.T("Selecione a pasta da recompilação.", "Select the Recomp folder.");
            if (!File.Exists(RecompExe(recompDir))) return Loc.T("BLACK.exe não encontrado nesta pasta.", "BLACK.exe was not found in this folder.");
            if (!IsRecomp(recompDir))
                return Loc.T("Coloque o XBE, ISO ou arquivos extraídos do jogo em assets.", "Put the XBE, ISO, or extracted game files in assets.");
            return null;
        }

        public static bool StartRecomp(string recompDir, out string error)
        {
            error = RecompMissingReason(recompDir);
            if (error != null) return false;
            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = RecompExe(recompDir),
                    WorkingDirectory = recompDir,
                    UseShellExecute = false
                }).Dispose();
                return true;
            }
            catch (Exception ex) { error = ex.Message; return false; }
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
